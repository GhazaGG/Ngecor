using UnityEngine;

namespace Ngecor.Material
{
    // Sak semen: isi bubuk mengalir ke arah gravitasi (center of mass + bentuk),
    // dan tumbukan diserap seperti karung berisi, bukan kotak kaku.
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(MeshFilter))]
    public sealed class CementBag : MonoBehaviour
    {
        [Header("Isi bergeser")]
        [Tooltip("Seberapa jauh center of mass bisa bergeser, per sumbu, sebagai fraksi setengah ukuran bag.")]
        [SerializeField] private Vector3 _maxFillShift = new Vector3(0.4f, 0.35f, 0.2f);
        [Tooltip("Kecepatan bubuk mengalir ke sisi bawah (unit normalisasi per detik).")]
        [SerializeField, Min(0.01f)] private float _flowSpeed = 1.5f;

        [Header("Tumbukan")]
        [SerializeField, Min(0f)] private float _impactSpeed = 1f;
        [Tooltip("Fraksi putaran yang diserap isi saat tumbukan.")]
        [SerializeField, Range(0f, 1f)] private float _impactAbsorb = 0.7f;
        [Tooltip("Batas kecepatan saat fisika memisahkan bag dari objek yang menembusnya (default proyek 10 m/s membuat bag terlempar).")]
        [SerializeField, Min(0.1f)] private float _maxDepenetrationSpeed = 0.5f;

        [Header("Bentuk (visual)")]
        [SerializeField, Range(2, 12)] private int _segments = 6;
        [Tooltip("Tebal di ujung jahitan dibanding tengah.")]
        [SerializeField, Range(0.05f, 1f)] private float _endThickness = 0.3f;
        [Tooltip("Tebal di sisi panjang dibanding tengah.")]
        [SerializeField, Range(0.05f, 1f)] private float _sideThickness = 0.6f;
        [Tooltip("Sisi kosong menipis sebanyak ini saat isi turun ke ujung lain.")]
        [SerializeField, Range(0f, 1f)] private float _emptyThin = 0.55f;
        [Tooltip("Permukaan atas melandai saat bag rebah.")]
        [SerializeField, Range(0f, 0.5f)] private float _topSag = 0.15f;
        [SerializeField, Range(0f, 0.3f)] private float _pinch = 0.08f;
        [SerializeField, Range(0f, 0.2f)] private float _unevenness = 0.06f;

        private Rigidbody _body;
        private Vector3 _halfExtents;
        private Vector3 _fill;
        private Vector3 _appliedFill;
        private Mesh _mesh;
        private Vector3[] _baseVertices;
        private Vector3[] _vertices;
        private Vector2 _noiseSeed;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.maxDepenetrationVelocity = _maxDepenetrationSpeed;
            var box = GetComponent<BoxCollider>();
            // Rigidbody.centerOfMass memakai posisi + rotasi transform, tanpa scale.
            _halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
            _noiseSeed = new Vector2(Random.value * 100f, Random.value * 100f);
            BuildMesh(box);
            _fill = LocalDown();
            Apply();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void FixedUpdate()
        {
            if (_body.IsSleeping())
                return;

            _fill = Vector3.MoveTowards(_fill, LocalDown(), _flowSpeed * Time.fixedDeltaTime);
            if ((_fill - _appliedFill).sqrMagnitude > 0.0004f)
                Apply();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < _impactSpeed)
                return;

            // Isi bubuk berubah bentuk dan menelan energi: putaran teredam, tidak ada lompatan balik.
            var contact = collision.GetContact(0);
            var normal = contact.normal;
            if (Vector3.Dot(normal, _body.worldCenterOfMass - contact.point) < 0f)
                normal = -normal;

            // Pantulan dihitung relatif terhadap penabrak, supaya bag tetap terbawa oleh objek yang bergerak.
            var otherVelocity = collision.rigidbody != null ? collision.rigidbody.GetPointVelocity(contact.point) : Vector3.zero;
            var separating = Vector3.Dot(_body.linearVelocity - otherVelocity, normal);
            if (separating > 0f)
                _body.linearVelocity -= normal * separating;
            _body.angularVelocity *= 1f - _impactAbsorb;
        }

        private Vector3 LocalDown() =>
            transform.InverseTransformDirection(Physics.gravity).normalized;

        private void Apply()
        {
            _appliedFill = _fill;
            _body.centerOfMass = Vector3.Scale(_fill, Vector3.Scale(_maxFillShift, _halfExtents));
            DeformMesh();
        }

        private void BuildMesh(BoxCollider box)
        {
            int n = _segments;
            int perFace = (n + 1) * (n + 1);
            _baseVertices = new Vector3[perFace * 6];
            _vertices = new Vector3[_baseVertices.Length];
            var triangles = new int[n * n * 6 * 6];

            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int v = 0, t = 0;
            foreach (var normal in normals)
            {
                var axisA = new Vector3(normal.y, normal.z, normal.x);
                var axisB = Vector3.Cross(normal, axisA);
                int start = v;
                for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    var p = normal * 0.5f + axisA * ((float)i / n - 0.5f) + axisB * ((float)j / n - 0.5f);
                    _baseVertices[v++] = box.center + Vector3.Scale(p, box.size);
                }

                for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    int a = start + i * (n + 1) + j;
                    int b = a + n + 1;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = b; triangles[t++] = b + 1; triangles[t++] = a + 1;
                }
            }

            _mesh = new Mesh { name = "CementBag (runtime)" };
            _mesh.MarkDynamic();
            _mesh.vertices = _baseVertices;
            _mesh.triangles = triangles;
            _mesh.bounds = new Bounds(box.center, box.size);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        // Sumbu lokal: x = panjang, y = tebal, z = lebar. Visual tidak melewati BoxCollider.
        private void DeformMesh()
        {
            var fillPlan = new Vector2(_fill.x, _fill.z);
            float fillAmount = fillPlan.magnitude;
            float sag = _topSag * Mathf.Abs(_fill.y);
            float upSign = -Mathf.Sign(_fill.y);

            for (int i = 0; i < _baseVertices.Length; i++)
            {
                var p = _baseVertices[i];
                float u = Mathf.Clamp(p.x * 2f, -1f, 1f);
                float w = Mathf.Clamp(p.z * 2f, -1f, 1f);
                float noise = Mathf.PerlinNoise(_noiseSeed.x + p.x * 4f + p.y * 3f, _noiseSeed.y + p.z * 4f + p.y * 5f) - 0.5f;

                float thickness = Mathf.Lerp(_endThickness, 1f, 1f - Mathf.Abs(u * u * u))
                                  * Mathf.Lerp(_sideThickness, 1f, 1f - w * w * w * w);
                // Sisi tempat isi berkumpul tetap penuh; sisi seberangnya kempis.
                thickness *= 1f - _emptyThin * (fillAmount - (u * fillPlan.x + w * fillPlan.y)) * 0.5f;
                if (p.y * upSign > 0f)
                    thickness *= 1f - sag;

                _vertices[i] = new Vector3(
                    p.x * (1f - _pinch * (1f - w * w)) + noise * _unevenness * 0.3f,
                    p.y * Mathf.Clamp01(thickness + noise * _unevenness),
                    p.z * (1f - _pinch * 1.5f * (1f - u * u)) + noise * _unevenness * 0.3f);
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
        }
    }
}
