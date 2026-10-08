using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    public sealed class ConcreteBatch : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _batchNumber = 1;
        public int BatchNumber => _batchNumber;
        public void Initialize(int batchNumber) => _batchNumber = Mathf.Max(1, batchNumber);

        private void Awake()
        {
            var pickup = new GameObject("BucketPickupTrigger");
            pickup.transform.SetParent(transform, false);
            pickup.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var collider = pickup.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.5f, 0.8f, 1.5f);
            pickup.AddComponent<DebugPourReceiverTrigger>();
        }
    }
}
