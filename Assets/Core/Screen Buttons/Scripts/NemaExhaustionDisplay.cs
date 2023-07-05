using System.Collections;
using System.Linq;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Localization.Scripts;
using Core.Save_Management;
using Core.Utils.Async;
using Core.Utils.Extensions;
using Core.Utils.Math;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Screen_Buttons.Scripts
{
    public class NemaExhaustionDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly LocalizedText NemaExhaustionTrans = new("screen-buttons_nema-exhaustion");
        
        [SerializeField, Required, SceneObjectsOnly]
        private Slider slider;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text percentageText;

        [SerializeField, Required, SceneObjectsOnly]
        private GameObject percentageGameObject;
        
        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource mouseOverAudioSource;

        [SerializeField]
        private Vector3 textCueStartOffset;
        
        private Tween _tween;
        private CoroutineWrapper _waitForCombatRoutine;

        private void Start()
        {
            Save.NemaExhaustionChanged += UpdateDisplay;
            GameManager.OnRootEnabled += OnRootEnabled;
            GameManager.OnRootDisabled += OnRootDisabled;
            Save.Handler.Changed += OnSaveChanged;
            Save save = Save.Current;
            if (save != null)
                UpdateDisplay(save.GetFullNemaStatus());
            
            Scene combatScene = SceneManager.GetSceneByName(SceneRef.Combat);
            Scene localMapScene = SceneManager.GetSceneByName(SceneRef.LocalMap);
            if (combatScene.IsValid() && combatScene.isLoaded && combatScene.GetRootGameObjects().Any(obj => obj.activeInHierarchy))
            {
                if (_waitForCombatRoutine is not { Running: true })
                    _waitForCombatRoutine = new CoroutineWrapper(WaitForCombatRoutine(), nameof(WaitForCombatRoutine), this, autoStart: true);
            }
            else if (localMapScene.IsValid() && localMapScene.isLoaded && localMapScene.GetRootGameObjects().Any(obj => obj.activeInHierarchy))
                gameObject.SetActive(true);
            else
                gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            Save.NemaExhaustionChanged -= UpdateDisplay;
            GameManager.OnRootEnabled -= OnRootEnabled;
            GameManager.OnRootDisabled -= OnRootDisabled;
            Save.Handler.Changed -= OnSaveChanged;
        }

        private void OnSaveChanged(Save save)
        {
            if (save != null)
                UpdateDisplay(save.GetFullNemaStatus());
        }

        private void OnRootEnabled(SceneRef sceneName)
        {
            if (sceneName == SceneRef.LocalMap)
            {
                gameObject.SetActive(true);
                return;
            }

            if (sceneName == SceneRef.Combat && _waitForCombatRoutine is not { Running: true })
                _waitForCombatRoutine = new CoroutineWrapper(WaitForCombatRoutine(), nameof(WaitForCombatRoutine), this, autoStart: true);
        }

        private void OnRootDisabled(SceneRef scene)
        {
            if (scene == SceneRef.LocalMap)
            {
                Scene combatScene = SceneManager.GetSceneByName(SceneRef.Combat.Name);
                if (combatScene.IsValid() == false || combatScene.isLoaded == false || combatScene.GetRootGameObjects().All(obj => obj.activeInHierarchy == false))
                {
                    gameObject.SetActive(false);
                }
            }
            else if (scene == SceneRef.Combat)
            {
                Scene localMapScene = SceneManager.GetSceneByName(SceneRef.LocalMap.Name);
                if (localMapScene.IsValid() == false || localMapScene.isLoaded == false || localMapScene.GetRootGameObjects().All(obj => obj.activeInHierarchy == false))
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator WaitForCombatRoutine()
        {
            while (CombatManager.Instance.IsNone)
                yield return null;

            CombatManager combatManager;
            while (CombatManager.Instance.TrySome(out combatManager) && combatManager.Running == false)
                yield return null;

            if (CombatManager.Instance.TrySome(out combatManager))
            {
                gameObject.SetActive(combatManager.CombatSetupInfo.MistExists);
            }
        }

        private void UpdateDisplay(NemaStatus status)
        {
            _tween.KillIfActive();

            float previous = status.Exhaustion.previous;
            float current = status.Exhaustion.current;
            _tween = slider.DOValue(current, 0.5f).SetEase(Ease.InOutQuad).SetSpeedBased().SetUpdate(isIndependentUpdate: true);
            if (percentageGameObject.activeSelf)
                percentageText.text = current.ToPercentageString();

            int delta = Mathf.RoundToInt((current - previous) * 100f);
            if (delta != 0f && WorldTextCueManager.AssertInstance(out WorldTextCueManager generalPurposeTextCueManager))
            {
                string text = delta.WithSymbol();
                WorldCueOptions options = new(text, 35f, worldPosition: transform.position + textCueStartOffset, color: delta > 0 ? ColorReferences.Lust : ColorReferences.Heal, stayDuration: 1f,
                                             fadeDuration: 0.5f, speed: Vector3.up * 0.3f, alignment: HorizontalAlignmentOptions.Center, stopOthers: false);
                generalPurposeTextCueManager.Show(options);
            }
        }
        private void Reset()
        {
            slider = GetComponent<Slider>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            percentageGameObject.SetActive(true);
            percentageText.text = $"{NemaExhaustionTrans.Translate()}{slider.value.ToPercentageString()}";
            mouseOverAudioSource.Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            percentageGameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slider != null)
                return;
            
            slider = GetComponent<Slider>();
            if (slider == null)
                Debug.LogWarning($"No slider found on {name}", this);
            else
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}