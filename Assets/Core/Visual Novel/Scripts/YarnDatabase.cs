using Core.Main_Database;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace Core.Visual_Novel.Scripts
{
    public class YarnDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private YarnProject yarnProject;

        public static bool SceneExists([NotNull] string sceneName) => Instance.YarnDatabase.yarnProject.Program.Nodes.ContainsKey(sceneName);
    }
}