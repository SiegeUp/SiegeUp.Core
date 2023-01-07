using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    public class BoundingBoxList : MonoBehaviour
    {
        [SerializeField]
        List<BoundingBoxComponent> boundingBoxes;

        public IReadOnlyList<BoundingBoxComponent> BoundingBoxes => boundingBoxes;

        public BoundingBoxComponent MainBound => boundingBoxes.Count > 0 ? boundingBoxes[0] : null;

#if UNITY_EDITOR
        [ContextMenu("Find all bounds")]
        public void FindAllBounds()
        {
            boundingBoxes = new List<BoundingBoxComponent>(GetComponentsInChildren<BoundingBoxComponent>());
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}