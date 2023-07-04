using UnityEngine;

namespace Utils.Editor
{
    public static class Tools
    {
        [UnityEditor.MenuItem("Tools/Log Persistent Data Path")]
        private static void LogPersistentDataPath()
        {
            Debug.Log(Application.persistentDataPath);
        }
    }
}