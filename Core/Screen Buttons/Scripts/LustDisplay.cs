using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Game_Manager.Scripts;
using Core.Main_Characters.Nema.Combat;
using Data.Main_Characters.Ethel;
using Save_Management;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Async;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Screen_Buttons.Scripts
{
    public class LustDisplay : MonoBehaviour
    {
        [SerializeField, Required, AssetsOnly]
        private LustSlider sliderPrefab;
        
        [SerializeField, Required, SceneObjectsOnly]
        private Transform slidersParent;
        
        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource pointerEnterSound;
        
        private readonly List<LustSlider> _sliders = new();
        private CoroutineWrapper _waitForCombatRoutine;

        private void Start()
        {
            Save.FloatChanged += OnFloatChanged;
            Save.Handler.Changed += OnSaveChanged;
            GameManager.OnRootEnabled += OnRootEnabled;
            GameManager.OnRootDisabled += OnRootDisabled;
            if (Save.Current != null)
                UpdateSliders();
            
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
            Save.FloatChanged -= OnFloatChanged;
            Save.Handler.Changed -= OnSaveChanged;
            GameManager.OnRootEnabled -= OnRootEnabled;
            GameManager.OnRootDisabled -= OnRootDisabled;
        }

        private void OnSaveChanged(Save save)
        {
            if (save != null)
                UpdateSliders();
        }

        private void OnFloatChanged(CleanString variableName, float oldValue, float newValue)
        {
            CleanString characterKey;
            if (variableName == VariablesName.Ethel_Lust)
                characterKey = Ethel.GlobalKey;
            else if (variableName == VariablesName.Nema_Lust)
                characterKey = Nema.GlobalKey;
            else
                return;

            UpdateSliders();
            int delta = (int)(newValue - oldValue);
            if (gameObject.activeInHierarchy == false || delta == 0f || CombatManager.Instance.IsSome || WorldTextCueManager.AssertInstance(out WorldTextCueManager generalPurposeTextCueManager) == false)
                return;
            
            if (GetSliderThatBelongsToCharacter(characterKey).TrySome(out LustSlider slider) == false)
                return;

            Vector3 worldPosition = slider.GetTextCueStartWorldPosition();
            string text = delta.WithSymbol();
            WorldCueOptions options = new(text, 35f, worldPosition, color: delta > 0 ? ColorReferences.Lust : ColorReferences.Heal, stayDuration: 1f,
                                         fadeDuration: 0.5f, speed: Vector3.up * 0.3f, alignment: HorizontalAlignmentOptions.Center, stopOthers: false);
            generalPurposeTextCueManager.Show(options);
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
                else if (_waitForCombatRoutine is not { Running: true })
                    _waitForCombatRoutine = new CoroutineWrapper(WaitForCombatRoutine(), nameof(WaitForCombatRoutine), this, autoStart: true);
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
                gameObject.SetActive(combatManager.CombatSetupInfo.AllowLust);
            }
        }

        private LustSlider CreateSlider()
        {
            LustSlider slider = sliderPrefab.InstantiateWithFixedLocalScale(slidersParent);
            slider.AssignPointerEnterSound(pointerEnterSound);
            _sliders.Add(slider);
            return slider;
        }

        private void UpdateSliders()
        {
            if (Save.AssertInstance(out Save save) == false)
                return;

            ReadOnlySpan<IReadonlyCharacterStats> stats = save.GetAllReadOnlyCharacterStats();
            for (int j = _sliders.Count; j < stats.Length; j++)
                CreateSlider();

            int i;
            for (i = 0; i < stats.Length; i++)
                _sliders[i].SetCharacter(stats[i]);

            for (; i < _sliders.Count; i++)
                _sliders[i].ResetMe();
        }

        private Option<LustSlider> GetSliderThatBelongsToCharacter(CleanString characterKey)
        {
            foreach (LustSlider slider in _sliders)
                if (slider.gameObject.activeSelf && slider.AssignedCharacter.TrySome(out IReadonlyCharacterStats character) && character.GetScript().Key == characterKey)
                    return Option<LustSlider>.Some(slider);

            return Option<LustSlider>.None;
        }
    }
}