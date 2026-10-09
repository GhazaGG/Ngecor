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

            if (IsOwner && _playerGrab != null)
            {
                _playerGrab.GrabRequestHandler = HandleLocalGrabRequest;
                _playerGrab.DropRequestHandler = HandleLocalDropRequest;
                _playerGrab.ThrowRequestHandler = HandleLocalThrowRequest;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner && _playerGrab != null)
            {
                _playerGrab.GrabRequestHandler = null;
                _playerGrab.DropRequestHandler = null;
                _playerGrab.ThrowRequestHandler = null;
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
                // Fallback for offline or non-networked objects
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

            var carried = _playerGrab.CarriedObject;
            var targetNetObj = carried != null ? carried.GetComponent<NetworkObject>() : null;
            if (targetNetObj == null || !targetNetObj.IsSpawned)
            {
                return _playerGrab.ExecuteThrow(throwDir);
            }

            if (IsServer)
            {
                ExecuteThrowAndReplicate(throwDir);
                return true;
            }

            RequestThrowServerRpc(throwDir);
            return true;
        }

        [ServerRpc]
        private void RequestThrowServerRpc(Vector3 throwDir)
        {
            EnsurePlayerGrab();
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            ExecuteThrowAndReplicate(throwDir);
        }

        private void ExecuteThrowAndReplicate(Vector3 throwDir)
        {
            if (_playerGrab == null || !_playerGrab.IsCarrying)
                return;

            if (_playerGrab.ExecuteThrow(throwDir))
            {
                ReplicateThrowClientRpc(throwDir);
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
