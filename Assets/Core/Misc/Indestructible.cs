using UnityEngine;

namespace Core.Misc
{
    public sealed class Indestructible : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(target: gameObject);
        }
    }
}