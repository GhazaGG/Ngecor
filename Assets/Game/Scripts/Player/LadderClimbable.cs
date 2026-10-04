using UnityEngine;

namespace Ngecor.Player
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LadderClimbable : MonoBehaviour
    {
        private BoxCollider _trigger;

        public float TopExitHeight => _trigger.bounds.max.y;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
        }
    }
}
