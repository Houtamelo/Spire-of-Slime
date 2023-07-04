using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Combat.Scripts.UI
{
    public class CombatChoiceButton : MonoBehaviour
    {
        [field: SerializeField] public Button Button { get; private set; }
        [field: SerializeField] public TMP_Text Tmp { get; private set; }
    }
}