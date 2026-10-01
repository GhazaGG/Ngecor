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

        private void Awake()
        {
            if (_sessionManager == null)
            {
                _sessionManager = GetComponent<SessionManager>();
                if (_sessionManager == null)
                {
                    _sessionManager = SessionManager.Instance;
                }
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
            var manager = _sessionManager != null ? _sessionManager : SessionManager.Instance;
            if (manager == null)
            {
                GUILayout.BeginArea(new Rect(10, 10, 260, 50), GUI.skin.box);
                GUILayout.Label("SessionManager not found.");
                GUILayout.EndArea();
                return;
            }

            var netManager = NetworkManager.Singleton;
            bool isListening = netManager != null && netManager.IsListening;

            GUILayout.BeginArea(new Rect(10, 10, 280, 220), GUI.skin.box);
            GUILayout.Label("<b>Ngecor — Multiplayer Prototype</b>");

            string stateLabel = manager.CurrentState.ToString();
            int connectedCount = netManager != null ? netManager.ConnectedClientsIds.Count : 0;

            if (manager.CurrentState == SessionState.Hosting)
            {
                GUILayout.Label($"<b>Status:</b> Hosting ({connectedCount} players)");
                GUILayout.Label($"Port: {manager.CurrentPort}");
            }
            else if (manager.CurrentState == SessionState.ConnectedClient)
            {
                GUILayout.Label($"<b>Status:</b> Connected Client (ID: {netManager?.LocalClientId})");
                GUILayout.Label($"Connected to: {manager.CurrentAddress}:{manager.CurrentPort}");
            }
            else
            {
                GUILayout.Label($"<b>Status:</b> {stateLabel}");
            }

            if (!string.IsNullOrEmpty(_statusMessage))
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
                        manager.StartHostSession(_ipAddress, port);
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
                        manager.StartClientSession(_ipAddress, port);
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
                    manager.DisconnectSession();
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
