using UnityEngine;

namespace Ngecor.Construction
{
    /// <summary>
    /// Lightweight test character controller for validating scaffolding collider stability,
    /// climbing, and movement traversal in Playground before PLAYER-001 is merged.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class TestScaffoldingWalker : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float jumpForce = 5.5f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody rb;
        private CapsuleCollider col;
        private Vector3 moveInput;
        private bool jumpRequested;
        private bool isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Camera-relative or world-aligned movement
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam != null)
            {
                Vector3 forward = cam.forward;
                Vector3 right = cam.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                moveInput = (forward * v + right * h).normalized;
            }
            else
            {
                moveInput = new Vector3(h, 0f, v).normalized;
            }

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                jumpRequested = true;
            }
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            Vector3 currentVel = rb.linearVelocity;
            Vector3 targetVel = moveInput * moveSpeed;
            targetVel.y = currentVel.y;

            if (jumpRequested)
            {
                jumpRequested = false;
                if (isGrounded)
                {
                    targetVel.y = jumpForce;
                }
            }

            rb.linearVelocity = targetVel;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, 15f * Time.fixedDeltaTime);
            }
        }

        private void CheckGrounded()
        {
            float checkDist = 0.15f;
            Vector3 bottom = transform.position + Vector3.down * (col.height * 0.5f - col.radius);
            isGrounded = Physics.SphereCast(bottom, col.radius * 0.85f, Vector3.down, out _, checkDist, groundLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
