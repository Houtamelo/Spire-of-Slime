using UnityEngine;

namespace Core.Utils.Editor
{
    public static class AudioSourcePlayer
    {
        [UnityEditor.MenuItem(itemName: "CONTEXT/AudioSource/Play")]
        private static void Play()
        {
            foreach (Object obj in UnityEditor.Selection.objects)
                if (obj is GameObject gameObject && gameObject.TryGetComponent(out AudioSource audioSource))
                    audioSource.Play();
        }
    }
}