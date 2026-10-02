using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Ngecor.Multiplayer
{
    public enum SessionState
    {
        Disconnected,
        StartingHost,
        Hosting,
        StartingClient,
        ConnectedClient
    }

    [DisallowMultipleComponent]
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [SerializeField] private string _defaultAddress = "127.0.0.1";
        [SerializeField] private ushort _defaultPort = 7777;

        public event Action<ulong> OnClientConnectedEvent;
        public event Action<ulong> OnClientDisconnectedEvent;
        public event Action<SessionState> OnSessionStateChanged;

        public SessionState CurrentState { get; private set; } = SessionState.Disconnected;
        public string CurrentAddress => _currentAddress;
        public ushort CurrentPort => _currentPort;

        private string _currentAddress;
        private ushort _currentPort;
        private UnityTransport _transport;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _currentAddress = _defaultAddress;
            _currentPort = _defaultPort;
        }

        private void Start()
        {
            EnsureTransport();
            RegisterNetworkCallbacks();
            CheckCommandLineArgs();
        }

        private void CheckCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            string mode = null;
            string ip = _defaultAddress;
            ushort port = _defaultPort;
            float autoCloseSec = 0f;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    mode = args[i + 1].ToLowerInvariant();
                }
                else if (args[i].Equals("-ip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    ip = args[i + 1];
                }
                else if (args[i].Equals("-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (ushort.TryParse(args[i + 1], out var p)) port = p;
                }
                else if (args[i].Equals("-autoclose", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s)) autoCloseSec = s;
                }
            }

            if (autoCloseSec > 0f)
            {
                StartCoroutine(AutoCloseRoutine(autoCloseSec));
            }

            if (mode == "host")
            {
                StartCoroutine(DelayedStartHost(ip, port));
            }
            else if (mode == "client")
            {
                StartCoroutine(DelayedStartClient(ip, port));
            }
        }

        private System.Collections.IEnumerator DelayedStartHost(string ip, ushort port)
        {
            yield return null;
            StartHostSession(ip, port);
        }

        private System.Collections.IEnumerator DelayedStartClient(string ip, ushort port)
        {
            yield return null;
            StartClientSession(ip, port);
        }

        private System.Collections.IEnumerator AutoCloseRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"[SessionManager] AutoClose triggered after {delay}s.");
            DisconnectSession();
            Application.Quit();
        }

        private void OnDestroy()
        {
            UnregisterNetworkCallbacks();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureTransport()
        {
            var netManager = NetworkManager.Singleton;
            if (netManager != null)
            {
                _transport = netManager.GetComponent<UnityTransport>();
            }
        }

        private void RegisterNetworkCallbacks()
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null) return;

            netManager.OnClientConnectedCallback += HandleClientConnected;
            netManager.OnClientDisconnectCallback += HandleClientDisconnected;
            netManager.OnServerStarted += HandleServerStarted;
            netManager.NetworkConfig.ConnectionApproval = true;
            netManager.ConnectionApprovalCallback = HandleConnectionApproval;
        }

        private void UnregisterNetworkCallbacks()
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null) return;

            netManager.OnClientConnectedCallback -= HandleClientConnected;
            netManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            netManager.OnServerStarted -= HandleServerStarted;
            if (netManager.ConnectionApprovalCallback == HandleConnectionApproval)
            {
                netManager.ConnectionApprovalCallback = null;
            }
        }

        private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            float offset = (float)(request.ClientNetworkId % 4) * 2.0f;
            response.Position = new Vector3(-2f + offset, 0.05f, 0f);
            response.Rotation = Quaternion.identity;
        }

        public bool StartHostSession(string ip = null, ushort port = 0)
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null)
            {
                Debug.LogError("[SessionManager] NetworkManager.Singleton not found.");
                return false;
            }

            if (netManager.IsListening)
            {
                Debug.LogWarning("[SessionManager] Session is already running.");
                return false;
            }

            _currentAddress = string.IsNullOrWhiteSpace(ip) ? _defaultAddress : ip.Trim();
            _currentPort = port == 0 ? _defaultPort : port;

            ConfigureTransport(_currentAddress, _currentPort);
            SetState(SessionState.StartingHost);

            bool success = netManager.StartHost();
            if (!success)
            {
                Debug.LogError("[SessionManager] Failed to start Host.");
                SetState(SessionState.Disconnected);
                return false;
            }

            SetState(SessionState.Hosting);
            Debug.Log($"[SessionManager] Host started on {_currentAddress}:{_currentPort}");
            return true;
        }

        public bool StartClientSession(string ip = null, ushort port = 0)
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null)
            {
                Debug.LogError("[SessionManager] NetworkManager.Singleton not found.");
                return false;
            }

            if (netManager.IsListening)
            {
                Debug.LogWarning("[SessionManager] Session is already running.");
                return false;
            }

            _currentAddress = string.IsNullOrWhiteSpace(ip) ? _defaultAddress : ip.Trim();
            _currentPort = port == 0 ? _defaultPort : port;

            ConfigureTransport(_currentAddress, _currentPort);
            SetState(SessionState.StartingClient);

            bool success = netManager.StartClient();
            if (!success)
            {
                Debug.LogError("[SessionManager] Failed to start Client.");
                SetState(SessionState.Disconnected);
                return false;
            }

            Debug.Log($"[SessionManager] Connecting to host at {_currentAddress}:{_currentPort}...");
            return true;
        }

        public void DisconnectSession()
        {
            var netManager = NetworkManager.Singleton;
            if (netManager == null || !netManager.IsListening)
            {
                SetState(SessionState.Disconnected);
                return;
            }

            Debug.Log("[SessionManager] Shutting down network session...");
            netManager.Shutdown();
            SetState(SessionState.Disconnected);
        }

        private void ConfigureTransport(string ip, ushort port)
        {
            EnsureTransport();
            if (_transport != null)
            {
                _transport.SetConnectionData(ip, port);
            }
            else
            {
                Debug.LogWarning("[SessionManager] UnityTransport component not found on NetworkManager.");
            }
        }

        private void HandleServerStarted()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                SetState(SessionState.Hosting);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            var netManager = NetworkManager.Singleton;
            if (netManager != null && !netManager.IsServer && clientId == netManager.LocalClientId)
            {
                SetState(SessionState.ConnectedClient);
                Debug.Log($"[SessionManager] Connected to Host as Client (ID: {clientId})");
            }
            else
            {
                Debug.Log($"[SessionManager] Client {clientId} connected to server.");
            }

            OnClientConnectedEvent?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            var netManager = NetworkManager.Singleton;
            if (netManager != null)
            {
                if (netManager.IsServer)
                {
                    Debug.Log($"[SessionManager] Client {clientId} disconnected from server.");
                }
                else if (clientId == netManager.LocalClientId)
                {
                    Debug.Log("[SessionManager] Disconnected from server.");
                    SetState(SessionState.Disconnected);
                }
            }
            else
            {
                SetState(SessionState.Disconnected);
            }

            OnClientDisconnectedEvent?.Invoke(clientId);
        }

        private void SetState(SessionState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnSessionStateChanged?.Invoke(newState);
        }
    }
}
