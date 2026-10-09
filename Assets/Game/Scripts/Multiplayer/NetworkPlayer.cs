using UnityEngine;
using Unity.Netcode;
using Ngecor.Player;

namespace Ngecor.Multiplayer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private CapsuleCollider _proxyCollider;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private AudioListener _audioListener;

        private void Awake()
        {
            EnsureComponentReferences();
        }

        private void EnsureComponentReferences()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            if (_proxyCollider == null)
            {
                _proxyCollider = GetComponent<CapsuleCollider>();
                if (_proxyCollider == null)
                {
                    _proxyCollider = gameObject.AddComponent<CapsuleCollider>();
                    if (_characterController != null)
                    {
                        _proxyCollider.height = _characterController.height;
                        _proxyCollider.radius = _characterController.radius;
                        _proxyCollider.center = _characterController.center;
                    }
                    else
                    {
                        _proxyCollider.height = 1.8f;
                        _proxyCollider.radius = 0.35f;
                        _proxyCollider.center = new Vector3(0f, 0.9f, 0f);
                    }
                }
            }

            if (_playerCamera == null)
                _playerCamera = GetComponentInChildren<Camera>(true);

            if (_audioListener == null)
                _audioListener = GetComponentInChildren<AudioListener>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ApplyOwnership(IsOwner);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            ApplyOwnership(false);
        }

        public void ApplyOwnership(bool isOwner)
        {
            EnsureComponentReferences();

            if (_playerMovement != null)
            {
                _playerMovement.SetLocalPlayer(isOwner);
            }

            if (_characterController != null)
            {
                _characterController.enabled = isOwner;
            }

            if (_proxyCollider != null)
            {
                _proxyCollider.enabled = !isOwner;
            }

            if (_playerCamera != null)
            {
                _playerCamera.enabled = isOwner;
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = isOwner;
            }
        }
    }
}
