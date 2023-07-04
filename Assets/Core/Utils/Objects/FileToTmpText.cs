using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Utils.Objects
{
    public class FileToTmpText : MonoBehaviour
    {
        [SerializeField, Required]
        private TextAsset file;

        [SerializeField, Required]
        private TMP_Text tmp;

        private void Awake()
        {
            if (file == null)
            {
                Debug.LogWarning($"No file assigned to {gameObject.name}", context: this);
                return;
            }
            
            tmp.text = file.text;
        }

        private void Reset()
        {
            tmp = GetComponent<TMP_Text>();
        }

        private void OnValidate()
        {
            if (file == null || tmp == null)
                return;
            
            tmp.text = file.text;
        }
    }
}