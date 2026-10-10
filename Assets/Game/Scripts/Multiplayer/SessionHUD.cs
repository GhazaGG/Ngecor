using UnityEngine;
using Unity.Netcode;

namespace Ngecor.Multiplayer
{
    [DisallowMultipleComponent]
    public class SessionHUD : MonoBehaviour
    {
        [SerializeField] private SessionManager _sessionManager;

        private string _ipAddress = "127.0.0.1";
        private string _portString = "7777";
        private string _statusMessage = "";

        private static readonly Rect HudArea = new Rect(10, 10, 280, 230);
        private static bool _isMouseOverHud;

        private void OnEnable()
        {
            Ngecor.Player.PlayerMovement.IsCursorOverUIHandler = () =>
            {
                if (_isMouseOverHud)
                    return true;
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return true;
                return false;
            };
        }

        private void OnDisable()
        {
            if (Ngecor.Player.PlayerMovement.IsCursorOverUIHandler != null)
            {
                Ngecor.Player.PlayerMovement.IsCursorOverUIHandler = null;
            }
        }

        private void Awake()
        {
            if (_sessionManager == null)
            {
                _sessionManager = GetComponent<SessionManager>();
            }
        }

        private void Start()
        {
            if (_sessionManager != null)
            {
                _ipAddress = _sessionManager.CurrentAddress;
                _portString = _sessionManager.CurrentPort.ToString();
            }
        }

        private void OnGUI()
        {
            if (Event.current != null)
            {
                _isMouseOverHud = HudArea.Contains(Event.current.mousePosition);
            }

            if (_sessionManager == null)
            {
                GUILayout.BeginArea(new Rect(10, 10, 260, 50), GUI.skin.box);
                GUILayout.Label("SessionManager not found.");
                GUILayout.EndArea();
                return;
            }

            var netManager = NetworkManager.Singleton;
            bool isListening = netManager != null && netManager.IsListening;

            GUILayout.BeginArea(new Rect(10, 10, 280, 230), GUI.skin.box);
            GUILayout.Label("<b>Ngecor — Multiplayer Prototype</b>");

            string stateLabel = _sessionManager.CurrentState.ToString();
            int connectedCount = netManager != null ? netManager.ConnectedClientsIds.Count : 0;

            if (_sessionManager.CurrentState == SessionState.Hosting)
            {
                GUILayout.Label($"<b>Status:</b> Hosting ({connectedCount} players)");
                GUILayout.Label($"Port: {_sessionManager.CurrentPort}");
            }
            else if (_sessionManager.CurrentState == SessionState.ConnectedClient)
            {
                GUILayout.Label($"<b>Status:</b> Connected Client (ID: {netManager?.LocalClientId})");
                GUILayout.Label($"Connected to: {_sessionManager.CurrentAddress}:{_sessionManager.CurrentPort}");
            }
            else
            {
                GUILayout.Label($"<b>Status:</b> {stateLabel}");
            }

            if (netManager != null && !isListening && !string.IsNullOrEmpty(netManager.DisconnectReason))
            {
                if (!netManager.DisconnectReason.Contains("TransportShutdown"))
                {
                    GUILayout.Label($"<color=red>Reason: {netManager.DisconnectReason}</color>");
                }
            }
            else if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Label($"<color=yellow>{_statusMessage}</color>");
            }

            GUILayout.Space(5);

            if (!isListening)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("IP:", GUILayout.Width(35));
                _ipAddress = GUILayout.TextField(_ipAddress);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Port:", GUILayout.Width(35));
                _portString = GUILayout.TextField(_portString);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (GUILayout.Button("Start Host"))
                {
                    if (TryParsePort(out ushort port))
                    {
                        _statusMessage = "";
                        _sessionManager.StartHostSession(_ipAddress, port);
                    }
                    else
                    {
                        _statusMessage = "Invalid port number.";
                    }
                }

                if (GUILayout.Button("Join Session"))
                {
                    if (TryParsePort(out ushort port))
                    {
                        _statusMessage = "";
                        _sessionManager.StartClientSession(_ipAddress, port);
                    }
                    else
                    {
                        _statusMessage = "Invalid port number.";
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Disconnect / Leave"))
                {
                    _statusMessage = "";
                    _sessionManager.DisconnectSession();
                }
            }

            GUILayout.EndArea();
        }

        private bool TryParsePort(out ushort port)
        {
            if (ushort.TryParse(_portString.Trim(), out port) && port > 0)
            {
                return true;
            }
            port = 7777;
            return false;
        }
    }
}
