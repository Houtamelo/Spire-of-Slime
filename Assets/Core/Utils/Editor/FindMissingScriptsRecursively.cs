using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Editor
{
    public class FindMissingScriptsRecursively : UnityEditor.EditorWindow 
    {
        static int _goCount, _componentsCount, _missingCount;
 
        [UnityEditor.MenuItem(itemName: "Window/FindMissingScriptsRecursively")]
        public static void ShowWindow()
        {
            GetWindow(t: typeof(FindMissingScriptsRecursively));
        }
 
        public void OnGUI()
        {
            if (GUILayout.Button(text: "Find Missing Scripts in selected GameObjects"))
            {
                FindInSelected();
            }
        }
        private static void FindInSelected()
        {
            GameObject[] go = UnityEditor.Selection.gameObjects;
            _goCount = 0;
            _componentsCount = 0;
            _missingCount = 0;
            foreach (GameObject g in go)
            {
                FindInGo(g: g);
            }
            Debug.Log(string.Format(format: "Searched {0} GameObjects, {1} components, found {2} missing", arg0: _goCount, arg1: _componentsCount, arg2: _missingCount));
        }
 
        private static void FindInGo([NotNull] GameObject g)
        {
            _goCount++;
            Component[] components = g.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                _componentsCount++;
                if (components[i] == null)
                {
                    _missingCount++;
                    string s = g.name;
                    Transform t = g.transform;
                    while (t.parent != null) 
                    {
                        Transform parent = t.parent;
                        s = $"{parent.name}/{s}";
                        t = parent;
                    }
                    Debug.Log (message: $"{s} has an empty script attached in position: {i}", context: g);
                }
            }
            // Now recurse through each child GO (if there are any):
            foreach (Transform childT in g.transform)
            {
                //Debug.Log("Searching " + childT.name  + " " );
                FindInGo(g: childT.gameObject);
            }
        }
    }
}
