using UnityEngine;

namespace SiegeUp.Core
{
    public class RectZone : MonoBehaviour
    {
        [SerializeField]
        Bounds bounds;

        public Bounds Bounds { get => bounds; set => bounds = value; }

        public bool IsInZone(Vector3 position)
        {
            return bounds.Contains(position - transform.position);
        }
    }
}