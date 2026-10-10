using UnityEngine;
using Unity.Netcode;
using Ngecor.Interaction;
using Ngecor.Player;

namespace Ngecor.Multiplayer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGrab))]
    public class NetworkPlayerInteraction : NetworkBehaviour
    {
        [SerializeField] private PlayerGrab _playerGrab;
        [SerializeField] private PlayerMovement _playerMovement;

        private const ulong NoObject = ulong.MaxValue;

        // Host-written mirror of what this player carries. Every client applies it, including one that joins later, so
        // an object is never free on one peer and held on another.
        private readonly NetworkVariable<ulong> _carriedObjectId = new(
            NoObject,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private GrabbableObject _mirroredCarried;
        private ulong _pendingCarriedId = NoObject;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void EnsureDependencies()
        {
            if (_playerGrab == null)
            {
                _playerGrab = GetComponent<PlayerGrab>();
            }

            if (_playerMovement == null)
            {
                _playerMovement = GetComponent<PlayerMovement>();
            }
        }

        private void EnsurePlayerGrab() => EnsureDependencies();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EnsureDependencies();

            if (_playerGrab != null)
            {
                // In an active network session, the host's server-authority NetworkTransform
                // controls the physical placement of the carried object in world space.
                // Clients observe that transform via NetworkTransform instead of overwriting it locally.
                _playerGrab.UpdateCarriedTransform = IsServer;

                if (IsServer)
                {
                    _playerGrab.AutoDropEnabled = true;
                    _playerGrab.AutoDropHandler = ExecuteDropAndReplicate;
                }
                else
                {
                    _playerGrab.AutoDropEnabled = false;
                    _playerGrab.AutoDropHandler = null;
                }

                if (IsOwner)
                {
                    _playerGrab.GrabRequestHandler = HandleLocalGrabRequest;
                    _playerGrab.DropRequestHandler = HandleLocalDropRequest;
                    _playerGrab.ThrowRequestHandler = HandleLocalThrowRequest;
                }
            }

            if (!IsServer)
            {
                _carriedObjectId.OnValueChanged += OnCarriedObjectChanged;
                ApplyCarriedObject(_carriedObjectId.Value);
            }
        }

        public void ExecuteDropAndReplicate()
        {
            EnsureDependencies();
            if (_playerGrab != null && _playerGrab.IsCarrying)
                _playerGrab.ExecuteDrop(); // Update mirrors the change to clients.
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            _carriedObjectId.OnValueChanged -= OnCarriedObjectChanged;
            _pendingCarriedId = NoObject;
            _mirroredCarried = null;

            if (_playerGrab != null)
            {
                _playerGrab.UpdateCarriedTransform = true;
                _playerGrab.AutoDropEnabled = true;
                _playerGrab.AutoDropHandler = null;

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
                // Note: ReplicateDropClientRpc is omitted during despawn because the NetworkObject
                // is already despawning and remote peers automatically drop via PlayerGrab.OnDisable.
            }
        }

        public Vector3 GetEyePosition()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            if (_playerMovement != null && _playerMovement.CameraPivot != null)
                return _playerMovement.CameraPivot.position;

            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                return cam.transform.position;

            return transform.position + Vector3.up * 1.5f;
        }

        public bool ValidateGrabTarget(GrabbableObject grabbable, float maxDistance)
        {
            if (grabbable == null || !grabbable.CanInteract(gameObject))
                return false;

            float distance = Vector3.Distance(transform.position, grabbable.transform.position);
            if (distance > maxDistance)
                return false;

            Physics.SyncTransforms();

            Vector3 eyePos = GetEyePosition();
            Vector3 targetPos = grabbable.transform.position;
            if (grabbable.Colliders != null && grabbable.Colliders.Length > 0 && grabbable.Colliders[0] != null)
            {
                targetPos = grabbable.Colliders[0].bounds.center;
            }

            Vector3 toTarget = targetPos - eyePos;
            float rayDist = toTarget.magnitude;
            if (rayDist > maxDistance)
                return false;

            if (rayDist < 0.001f)
                return true;

            Vector3 dir = toTarget / rayDist;

            // Line-of-sight raycast: ignore player colliders and triggers.
            // First hit must be target collider (or its children).
            var hits = Physics.RaycastAll(eyePos, dir, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null)
                        continue;

                    // Abaikan collider milik player sendiri
                    if (hit.collider.transform.IsChildOf(transform))
                        continue;

                    // Hit pertama non-player harus target atau anaknya
                    if (hit.collider.transform.IsChildOf(grabbable.transform))
                        return true;

                    // Terhalang dinding / rintangan lain
                    return false;
                }
            }

            // Fallback: jika bounds center tidak kena (misal bentuk ireguler), cek ke grabbable.transform.position
            if (targetPos != grabbable.transform.position)
            {
                toTarget = grabbable.transform.position - eyePos;
                rayDist = toTarget.magnitude;
                if (rayDist <= maxDistance && rayDist >= 0.001f)
                {
                    dir = toTarget / rayDist;
                    hits = Physics.RaycastAll(eyePos, dir, rayDist, ~0, QueryTriggerInteraction.Ignore);
                    if (hits != null && hits.Length > 0)
                    {
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                        foreach (var hit in hits)
                        {
                            if (hit.collider == null)
                                continue;

                            if (hit.collider.transform.IsChildOf(transform))
                                continue;

                            if (hit.collider.transform.IsChildOf(grabbable.transform))
                                return true;

                            return false;
                        }
                    }
                }
            }

            // Jika tidak ada collider yang terdeteksi di sepanjang raycast (misal grabbable tanpa collider)
            return grabbable.Colliders == null || grabbable.Colliders.Length == 0;
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

            _playerGrab.ExecuteGrab(grabbable);
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
            // A client's copy is kinematic under NetworkRigidbody, so to the client a throw is just a release;
            // the flight arrives through NetworkTransform.
            _playerGrab.ExecuteThrow(unitDir);
        }

        private void Update()
        {
            if (!IsSpawned || _playerGrab == null)
                return;

            if (IsServer)
                MirrorCarriedObject();
            else if (_pendingCarriedId != NoObject)
                ApplyCarriedObject(_pendingCarriedId);
        }

        private void MirrorCarriedObject()
        {
            var carried = _playerGrab.CarriedObject;
            // ReferenceEquals: a destroyed carried object compares equal to null under Unity's ==, and must still
            // publish NoObject.
            if (ReferenceEquals(carried, _mirroredCarried))
                return;

            _mirroredCarried = carried;
            var id = NoObject;
            if (carried != null && carried.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned)
                id = networkObject.NetworkObjectId;
            _carriedObjectId.Value = id;
        }

        private void OnCarriedObjectChanged(ulong previous, ulong current) => ApplyCarriedObject(current);

        private void ApplyCarriedObject(ulong networkObjectId)
        {
            _pendingCarriedId = NoObject;
            EnsureDependencies();
            if (_playerGrab == null)
                return;

            var current = _playerGrab.CarriedObject;
            if (networkObjectId == NoObject)
            {
                if (current != null)
                    _playerGrab.ExecuteDrop();
                return;
            }

            GrabbableObject target = null;
            if (TryGetSpawned(networkObjectId, out var networkObject))
                networkObject.TryGetComponent(out target);

            if (target != null && ReferenceEquals(current, target))
                return;

            if (current != null)
                _playerGrab.ExecuteDrop();

            // Late join or out-of-order updates: the object may not be spawned yet, or its previous holder's drop may
            // not have arrived. Update retries until it applies.
            if (target == null || !_playerGrab.ExecuteReplicatedGrab(target))
                _pendingCarriedId = networkObjectId;
        }

        private static bool TryGetSpawned(ulong networkObjectId, out NetworkObject networkObject)
        {
            networkObject = null;
            var manager = NetworkManager.Singleton;
            return manager != null && manager.SpawnManager != null &&
                   manager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out networkObject) &&
                   networkObject != null;
        }
    }
}
