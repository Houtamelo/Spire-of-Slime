using DG.Tweening;
using TMPro;
using UnityEngine;
using Utils.Extensions;

namespace Core.Game_Manager.Scripts
{
    public class WorldTextCue : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text tmp;

        private Sequence _sequence;
        
        public bool IsBusy => _sequence is { active: true };

        public Sequence Show(WorldCueOptions textCueOptions)
        {
            _sequence.KillIfActive();
            gameObject.SetActive(true);
            
            tmp.text = textCueOptions.Text;
            tmp.fontSize = textCueOptions.Size;
            tmp.color = textCueOptions.Color;
            
            tmp.horizontalAlignment = textCueOptions.Alignment;
         
            transform.position = textCueOptions.WorldPosition;
            Vector3 endPosition = textCueOptions.WorldPosition + textCueOptions.Speed * (textCueOptions.StayDuration + textCueOptions.FadeDuration);

            _sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
            _sequence.Append(transform.DOMove(endPosition, textCueOptions.StayDuration + textCueOptions.FadeDuration));
            _sequence.Insert(textCueOptions.StayDuration, tmp.DOFade(0, textCueOptions.FadeDuration));
            return _sequence;
        }

        private void OnDisable()
        {
            _sequence.KillIfActive();
        }

        public void Hide()
        {
            _sequence.KillIfActive();
            gameObject.SetActive(false);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tmp == null)
            {
                tmp = GetComponentInChildren<TMP_Text>();
                if (tmp == null) 
                    Debug.LogWarning("TMP Text not found on GeneralPurposeTextCue", context: this);
                else
                    UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}