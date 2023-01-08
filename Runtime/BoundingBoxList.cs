using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    public class BoundingBoxList : MonoBehaviour
    {
        [SerializeField]
        List<BoundingBox> boundingBoxes;

        public IReadOnlyList<BoundingBox> BoundingBoxes => boundingBoxes;

        public BoundingBox MainBound => boundingBoxes.Count > 0 ? boundingBoxes[0] : null;

#if UNITY_EDITOR
        [ContextMenu("Find all bounds")]
        public void FindAllBounds()
        {
            boundingBoxes = new List<BoundingBox>(GetComponentsInChildren<BoundingBox>());
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}