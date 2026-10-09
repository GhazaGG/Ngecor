using UnityEngine;

namespace Ngecor.Material
{
    // Marks a container whose contents can only be collected as one type (concrete in a mixing spot,
    // water in a drum). Ingredients that are still being mixed cannot be scooped back out.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer))]
    public sealed class CollectOnlyType : MonoBehaviour
    {
        [SerializeField] private MaterialType _type = MaterialType.Concrete;
        [SerializeField] private bool _shovelCanCollect = true;

        public MaterialType Type => _type;

        public static bool TryGetCollectType(BulkMaterialContainer container, bool byShovel, out MaterialType type)
        {
            if (!container.TryGetComponent<CollectOnlyType>(out var rule))
                return container.TryGetMaterialType(out type);
            type = rule._type;
            return (!byShovel || rule._shovelCanCollect) && container.GetUnits(type) > 0;
        }
    }
}
