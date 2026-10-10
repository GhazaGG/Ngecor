using Unity.Netcode;
using UnityEngine;
using Ngecor.Player;

namespace Ngecor.Multiplayer
{
    // Client contact push as intent (docs/DECISIONS.md 2026-10-02): host-simulated bodies are kinematic on the client,
    // so the owner reports the body its controller ran into and the host pushes it with the same force law and limit
    // as a local push. The client never sets the body's position.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public class NetworkPlayerPush : NetworkBehaviour
    {
        private const float MinContactHeight = -0.5f;
        private const float MaxContactHeight = 2.5f;

        [SerializeField] private PlayerMovement _playerMovement;
        [Tooltip("Minimum seconds between two push intents from the owner.")]
        [SerializeField, Min(0.01f)] private float _sendInterval = 0.05f;
        [Tooltip("Seconds the host keeps applying one intent, bridging the gap until the next one arrives.")]
        [SerializeField, Min(0.02f)] private float _intentDuration = 0.15f;
        [Tooltip("Furthest horizontal distance (m) from this player's position on the host to the reported contact; covers movement lag.")]
        [SerializeField, Min(0.1f)] private float _maxContactReach = 1.5f;

        private float _nextSendTime;
        private Rigidbody _intentBody;
        private Vector3 _intentLocalPoint;
        private Vector3 _intentDirection;
        private float _intentExpiresAt;

        private void Awake()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // The host's own player pushes dynamic bodies directly; only a client needs the intent path.
            if (IsOwner && !IsServer && _playerMovement != null)
                _playerMovement.KinematicContactPushHandler = HandleKinematicContact;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner && _playerMovement != null)
                _playerMovement.KinematicContactPushHandler = null;
            _intentBody = null;
        }

        public static bool IsPushRequestValid(Vector3 playerPosition, Vector3 contactPoint, Vector3 direction, float maxReach)
        {
            if (!IsFinite(contactPoint) || !IsFinite(direction))
                return false;

            if (Vector3.ProjectOnPlane(direction, Vector3.up).sqrMagnitude < 0.0001f)
                return false;

            var offset = contactPoint - playerPosition;
            if (offset.y < MinContactHeight || offset.y > MaxContactHeight)
                return false;

            offset.y = 0f;
            return offset.magnitude <= maxReach;
        }

        // Only a body that is itself a spawned network object can be pushed through the host. Another player's
        // RemoteProxy is a kinematic child without one, so walking into a player sends nothing.
        public static bool TryGetPushTarget(Rigidbody body, out ulong networkObjectId)
        {
            networkObjectId = 0;
            if (body == null || !body.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.IsSpawned)
                return false;

            networkObjectId = networkObject.NetworkObjectId;
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private void HandleKinematicContact(Rigidbody body, Vector3 contactPoint, Vector3 direction)
        {
            if (!IsSpawned || Time.time < _nextSendTime || !TryGetPushTarget(body, out var networkObjectId))
                return;

            _nextSendTime = Time.time + _sendInterval;
            RequestPushServerRpc(networkObjectId, contactPoint, direction);
        }

        [ServerRpc]
        private void RequestPushServerRpc(ulong networkObjectId, Vector3 contactPoint, Vector3 direction)
        {
            if (_playerMovement == null || !IsPushRequestValid(transform.position, contactPoint, direction, _maxContactReach))
                return;

            var manager = NetworkManager.Singleton;
            if (manager == null || manager.SpawnManager == null ||
                !manager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject) ||
                networkObject == null || !networkObject.TryGetComponent<Rigidbody>(out var body) || body.isKinematic)
                return;

            _intentBody = body;
            _intentLocalPoint = body.transform.InverseTransformPoint(contactPoint);
            _intentDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            _intentExpiresAt = Time.time + _intentDuration;
        }

        // LateUpdate: runs after PlayerMovement.Update has refilled this player's per-frame push budget on the host.
        private void LateUpdate()
        {
            if (!IsServer || _intentBody == null)
                return;

            if (Time.time > _intentExpiresAt || _intentBody.isKinematic)
            {
                _intentBody = null;
                return;
            }

            _playerMovement.ApplyMovementPush(
                _intentBody, _intentBody.transform.TransformPoint(_intentLocalPoint), _intentDirection, 1f, Time.deltaTime);
        }
    }
}
