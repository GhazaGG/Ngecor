using UnityEngine;
using Unity.Netcode;
using Ngecor.Interaction;

namespace Ngecor.Multiplayer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGrab))]
    public class NetworkPlayerInteraction : NetworkBehaviour
    {
        [SerializeField] private PlayerGrab _playerGrab;

        private void Awake()
        {
            EnsurePlayerGrab();
        }

        private void EnsurePlayerGrab()
        {
            if (_playerGrab == null)
            {
                _playerGrab = GetComponent<PlayerGrab>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EnsurePlayerGrab();

            if (_playerGrab != null)
            {
                // In an active network session, the host's server-authority NetworkTransform
                // controls the physical placement of the carried object in world space.
                // Clients observe that transform via NetworkTransform instead of overwriting it locally.
                _playerGrab.UpdateCarriedTransform = IsServer;

                if (IsOwner)
                {
                    _playerGrab.GrabRequestHandler = HandleLocalGrabRequest;
                    _playerGrab.DropRequestHandler = HandleLocalDropRequest;
                    _playerGrab.ThrowRequestHandler = HandleLocalThrowRequest;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (_playerGrab != null)
            {
                _playerGrab.UpdateCarriedTransform = true;

                if (IsOwner)
                {
                    _playerGrab.GrabRequestHandler = null;
                    _playerGrab.DropRequestHandler = null;
                    _playerGrab.ThrowRequestHandler = null;
                }
            }

            if (IsServer && _playerGrab != null && _playerGrab.IsCarrying)
            {
                _playerGrab.ExecuteDrop();
                ReplicateDropClientRpc();
            }
        }

        public bool ValidateGrabTarget(GrabbableObject grabbable, float maxDistance)
        {
            if (grabbable == null || !grabbable.CanInteract(gameObject))
                return false;

            float distance = Vector3.Distance(transform.position, grabbable.transform.position);
            return distance <= maxDistance;
        }

        public bool HandleLocalGrabRequest(GrabbableObject target)
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || target == null)
                return false;

            if (!ValidateGrabTarget(target, _playerGrab.MaxGrabDistance))
                return false;

            var targetNetObj = target.GetComponent<NetworkObject>();
            if (targetNetObj == null || !targetNetObj.IsSpawned)
            {
                // In an active network session, non-networked objects cannot be grabbed locally
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    return false;

                // Fallback for offline single-player mode
                return _playerGrab.ExecuteGrab(target);
            }

            if (IsServer)
            {
                ExecuteGrabAndReplicate(targetNetObj);
                return true;
            }

            RequestGrabServerRpc(targetNetObj.NetworkObjectId);
            return true;
        }

        [ServerRpc]
        private void RequestGrabServerRpc(ulong networkObjectId)
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || _playerGrab.IsCarrying)
                return;

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
                return;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var targetNetObj))
            {
                if (targetNetObj != null && targetNetObj.TryGetComponent<GrabbableObject>(out var grabbable))
                {
                    if (ValidateGrabTarget(grabbable, _playerGrab.MaxGrabDistance))
                    {
                        ExecuteGrabAndReplicate(targetNetObj);
                    }
                }
            }
        }

        private void ExecuteGrabAndReplicate(NetworkObject targetNetObj)
        {
            if (targetNetObj == null || _playerGrab == null)
                return;

            var grabbable = targetNetObj.GetComponent<GrabbableObject>();
            if (grabbable == null)
                return;

            if (_playerGrab.ExecuteGrab(grabbable))
            {
                ReplicateGrabClientRpc(targetNetObj.NetworkObjectId);
            }
        }

        [ClientRpc]
        private void ReplicateGrabClientRpc(ulong networkObjectId)
        {
            if (IsServer)
                return;

            EnsurePlayerGrab();
            if (_playerGrab == null || _playerGrab.IsCarrying)
                return;

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
                return;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var targetNetObj))
            {
                if (targetNetObj != null && targetNetObj.TryGetComponent<GrabbableObject>(out var grabbable))
                {
                    _playerGrab.ExecuteGrab(grabbable);
                }
            }
        }

        public bool HandleLocalDropRequest()
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return false;

            var carried = _playerGrab.CarriedObject;
            var targetNetObj = carried != null ? carried.GetComponent<NetworkObject>() : null;
            if (targetNetObj == null || !targetNetObj.IsSpawned)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    return false;

                return _playerGrab.ExecuteDrop();
            }

            if (IsServer)
            {
                ExecuteDropAndReplicate();
                return true;
            }

            RequestDropServerRpc();
            return true;
        }

        [ServerRpc]
        private void RequestDropServerRpc()
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            ExecuteDropAndReplicate();
        }

        private void ExecuteDropAndReplicate()
        {
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            if (_playerGrab.ExecuteDrop())
            {
                ReplicateDropClientRpc();
            }
        }

        [ClientRpc]
        private void ReplicateDropClientRpc()
        {
            if (IsServer)
                return;

            EnsurePlayerGrab();
            if (_playerGrab != null && _playerGrab.IsCarrying)
            {
                _playerGrab.ExecuteDrop();
            }
        }

        public bool HandleLocalThrowRequest(Vector3 throwDir)
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return false;

            if (!float.IsFinite(throwDir.x) || !float.IsFinite(throwDir.y) || !float.IsFinite(throwDir.z) || throwDir.sqrMagnitude < 0.0001f)
                return false;

            Vector3 unitDir = throwDir.normalized;
            var carried = _playerGrab.CarriedObject;
            var targetNetObj = carried != null ? carried.GetComponent<NetworkObject>() : null;
            if (targetNetObj == null || !targetNetObj.IsSpawned)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    return false;

                return _playerGrab.ExecuteThrow(unitDir);
            }

            if (IsServer)
            {
                ExecuteThrowAndReplicate(unitDir);
                return true;
            }

            RequestThrowServerRpc(unitDir);
            return true;
        }

        [ServerRpc]
        private void RequestThrowServerRpc(Vector3 throwDir)
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            if (!float.IsFinite(throwDir.x) || !float.IsFinite(throwDir.y) || !float.IsFinite(throwDir.z) || throwDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 unitDir = throwDir.normalized;
            ExecuteThrowAndReplicate(unitDir);
        }

        private void ExecuteThrowAndReplicate(Vector3 throwDir)
        {
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            Vector3 unitDir = throwDir.normalized;
            if (_playerGrab.ExecuteThrow(unitDir))
            {
                ReplicateThrowClientRpc(unitDir);
            }
        }

        [ClientRpc]
        private void ReplicateThrowClientRpc(Vector3 throwDir)
        {
            if (IsServer)
                return;

            EnsurePlayerGrab();
            if (_playerGrab != null && _playerGrab.IsCarrying)
            {
                _playerGrab.ExecuteThrow(throwDir);
            }
        }
    }
}
