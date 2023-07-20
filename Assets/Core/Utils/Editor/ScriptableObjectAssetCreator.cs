using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Core.Utils.Editor
{
    public static class ScriptableObjectAssetCreator
    {
        [MenuItem (itemName: "Assets/Create ScriptableObject")]
        public static void Create ()
        {
            MonoScript script = Selection.activeObject as MonoScript;
            if (script == null)
                return;
            
            Type type = script.GetClass();
            ScriptableObject scriptableObject = ScriptableObject.CreateInstance (type);
            string path = Path.GetDirectoryName (path: AssetDatabase.GetAssetPath (assetObject: script));
            AssetDatabase.CreateAsset (asset: scriptableObject, path: $"{path}/{Selection.activeObject.name}.asset");
        }

        [MenuItem (itemName: "Assets/Create ScriptableObject", isValidateFunction: true)]
        public static bool ValidateCreate ()
        {
            var script = Selection.activeObject as MonoScript;
            return script != null && script.GetClass ().IsSubclassOf (c: typeof(ScriptableObject));
        }
    }
}