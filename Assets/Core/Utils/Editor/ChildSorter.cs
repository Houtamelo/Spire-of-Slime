using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Utils.Editor
{
    public static class ChildSorter
    {
        [MenuItem("CONTEXT/Transform/Sort Children")]
        private static void Sort()
        {
            Transform transform = Selection.activeTransform;
            if (transform == null)
                return;

            List<Transform> children = new();
            foreach (Transform child in transform)
                children.Add(child);

            children = children.OrderBy(child => child.name).ToList();
            for (int i = 0; i < children.Count; i++)
                children[i].SetSiblingIndex(i);
            
            EditorUtility.SetDirty(target: transform);
        }
    }
}