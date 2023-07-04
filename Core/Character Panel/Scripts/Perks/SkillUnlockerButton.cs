using Core.Character_Panel.Scripts.Skills;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Game_Manager.Scripts;
using DG.Tweening;
using ListPool;
using Save_Management;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Extensions;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Character_Panel.Scripts.Perks
{
    public class SkillUnlockerButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        private const float FadeDuration = 0.5f;
        
        [SerializeField]
        private Image background;

        [SerializeField]
        private Image baseFx, highFx;

        [SerializeField]
        private Image baseIcon, highIcon;

        [SerializeField]
        private Image frame;

        [SerializeField]
        private SkillScriptable skill;
        public SkillScriptable Skill => skill;

        [SerializeField]
        private Color baseColor, mouseOverColor;

        [SerializeField]
        private Material desaturateMaterial;

        [SerializeField]
        private AudioSource pointerEnterSource, confirmPerkSource, invalidClickSource;
        
        private Sequence _sequence;
        private bool _unlocked;
        private bool _hasBaseFx, _hasHighFx;
        private bool _prerequisitesMet;
        
        private void Start()
        {
            if (skill == null)
            {
                Debug.LogWarning("Skill is null.", this);
                return;
            }
            
            _hasBaseFx = baseFx.sprite != null;
            _hasHighFx = highFx.sprite != null;
        }
        
        public void UpdateSelf(ref ValueListPool<CleanString> unlockedPerks)
        {
            _unlocked = unlockedPerks.Contains(skill.Key);
            if (_unlocked)
            {
                SetMaterials(null);
                _prerequisitesMet = true;
            }
            else
            {
                SetMaterials(desaturateMaterial);
                _prerequisitesMet = ArePrerequisitesMet(skill, ref unlockedPerks);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            if (_unlocked)
                return;

            if (_prerequisitesMet == false)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("Prerequisites aren't met.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));
                
                invalidClickSource.Play();
                return;
            }
            
            pointerEnterSource.Play();
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Skill unlock button clicked but no save is active.", this);
                return;
            }

            if (CombatManager.Instance.IsSome)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
                    cueManager.Show(new WorldCueOptions("You can't do that in combat.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));

                invalidClickSource.Play();
                return;
            }

            Option<CharacterMenuManager> instance = CharacterMenuManager.Instance;
            if (instance.IsNone)
            {
                Debug.LogWarning("Skill unlock button clicked but no character menu manager found.", this);
                return;
            }
            
            Option<IReadonlyCharacterStats> character = instance.Value.SelectedCharacter.AsOption();
            if (character.IsNone)
            {
                Debug.LogWarning("Skill unlock button clicked but no character is selected.", this);
                return;
            }

            IReadonlyCharacterStats stats = character.Value;
            if (stats.IsSkillUnlocked(skill.Key, save))
                return;
            
            if (ArePrerequisitesMet(skill, stats, save) == false)
            {
                Debug.LogWarning($"Skill unlock button is interactable but prerequisites aren't met. Character is {stats.Key}", this);
                return;
            }
                
            if (stats.AvailablePerkPoints <= 0)
            {
                if (WorldTextCueManager.AssertInstance(out WorldTextCueManager textCueManager))
                    textCueManager.Show(new WorldCueOptions("Not enough perk points.", 35f, transform.position, Color.red, 1f, 0.5f, Vector3.zero, HorizontalAlignmentOptions.Center, true));

                invalidClickSource.Play();
                return;
            }
            
            if (AreYouSurePanel.AssertInstance(out AreYouSurePanel areYouSurePanel) == false)
                return;

            areYouSurePanel.Show(() => OnYes(confirmPerkSource, button: this, stats.Key, save), message: $"Are you sure you wish to unlock {skill.DisplayName}?");

            static void OnYes(AudioSource confirmPerkSource, SkillUnlockerButton button, CleanString characterKey, Save save)
            {
                if (save.GetReadOnlyStats(characterKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                    return;
                
                if (stats.IsSkillUnlocked(button.skill.Key, save))
                {
                    Debug.LogWarning("On Yes panel is interactable but skill is already unlocked.", button);
                    return;
                }
            
                if (ArePrerequisitesMet(button.skill, stats, save) == false)
                {
                    Debug.LogWarning($"On Yes panel is interactable but prerequisites aren't met. Character is {stats.Key}", button);
                    return;
                }
                
                if (stats.AvailablePerkPoints <= 0)
                {
                    if (WorldTextCueManager.AssertInstance(out WorldTextCueManager textCueManager))
                        textCueManager.Show(new WorldCueOptions("Not enough perk points.", size: 35f, worldPosition: button.transform.position, color: Color.red, stayDuration: 1f,
                                                                fadeDuration: 0.5f, speed: Vector3.zero, alignment: HorizontalAlignmentOptions.Center, stopOthers: true));

                    return;
                }
                
                confirmPerkSource.Play();
                save.UnlockPerk(button.skill.Key);
            }
        }

        private void SetColor(Color color)
        {
            background.SetColorIgnoringAlpha(color);
            baseIcon.SetColorIgnoringAlpha(color);
            baseFx.SetColorIgnoringAlpha(color);
            highIcon.SetColorIgnoringAlpha(color);
            highFx.SetColorIgnoringAlpha(color);
            frame.SetColorIgnoringAlpha(color);
        }
        
        private static bool ArePrerequisitesMet(SkillScriptable skillScriptable, IReadonlyCharacterStats stats, Save save)
        {
            using (ValueListPool<CleanString> unlockedPerksAndSkills = stats.GetUnlockedPerksAndSkills(save))
            {
                foreach (PerkScriptable prerequisite in skillScriptable.PerkPrerequisites)
                    if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                        return false;

                foreach (SkillScriptable prerequisite in skillScriptable.SkillPrerequisites)
                    if (unlockedPerksAndSkills.Contains(prerequisite.Key) == false)
                        return false;
            }

            return true;
        }

        private static bool ArePrerequisitesMet(SkillScriptable skill, ref ValueListPool<CleanString> unlockedPerks)
        {
            foreach (PerkScriptable prerequisite in skill.PerkPrerequisites)
                if (unlockedPerks.Contains(prerequisite.Key) == false)
                    return false;
            
            foreach (SkillScriptable prerequisite in skill.SkillPrerequisites)
                if (unlockedPerks.Contains(prerequisite.Key) == false)
                    return false;
            
            return true;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (SkillTooltip.AssertInstance(out SkillTooltip skillTooltip))
                skillTooltip.RawTooltip(skill);
            
            pointerEnterSource.Play();
            SetColor(mouseOverColor);
            _sequence.KillIfActive();
            
            _sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
            _sequence.Append(baseIcon.DOFade(0f, FadeDuration));
            _sequence.Join(highIcon.DOFade(1f, FadeDuration));
            if (_hasBaseFx)
                _sequence.Join(baseFx.DOFade(0f, FadeDuration));
            
            if (_hasHighFx)
                _sequence.Join(highFx.DOFade(1f, FadeDuration));
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (SkillTooltip.Instance.TrySome(out SkillTooltip skillTooltip))
                skillTooltip.Hide();
            else
                Debug.LogWarning("Skill unlock button mouse exit but no skill tooltip found.", this);
            
            SetColor(baseColor);
            _sequence.KillIfActive();
            
            _sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
            _sequence.Append(baseIcon.DOFade(1f, FadeDuration));
            _sequence.Join(highIcon.DOFade(0f, FadeDuration));
            if (_hasBaseFx)
                _sequence.Join(baseFx.DOFade(1f, FadeDuration));
            
            if (_hasHighFx)
                _sequence.Join(highFx.DOFade(0f, FadeDuration));
        }

        private void SetMaterials(Material material)
        {
            background.material = material;
            baseIcon.material = material;
            highIcon.material = material;
            baseFx.material = material;
            highFx.material = material;
            frame.material = material;
        }

#if UNITY_EDITOR

        public void SetSkill(SkillScriptable skillScriptable)
        {
            skill = skillScriptable;
            AssignSprites();
        }

        public void SetSources(AudioSource pointerEnter, AudioSource confirmPerk, AudioSource invalidClick)
        {
            pointerEnterSource = pointerEnter;
            confirmPerkSource = confirmPerk;
            invalidClickSource = invalidClick;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void AssignSprites()
        {
            if (skill == null)
                return;
            
            SetMaterials(desaturateMaterial);
            if (background != null)
            {
                if (skill.IconBackground != null)
                {
                    background.sprite = skill.IconBackground;
                    background.color = baseColor;
                }
                else
                {
                    background.sprite = null;
                    background.color = Color.clear;
                }
            }
            if (baseIcon != null)
            {
                if (skill.IconBaseSprite != null)
                {
                    baseIcon.sprite = skill.IconBaseSprite;
                    baseIcon.color = baseColor;
                }
                else
                {
                    baseIcon.sprite = null;
                    baseIcon.color = Color.clear;
                }
            }
            if (baseFx != null)
            {
                if (skill.IconBaseFx != null)
                {
                    baseFx.sprite = skill.IconBaseFx;
                    baseFx.color = baseColor;
                }
                else
                {
                    baseFx.sprite = null;
                    baseFx.color = Color.clear;
                }
            }
            if (highIcon != null)
            {
                highIcon.sprite = skill.IconHighlightedSprite;
                highIcon.color = Color.clear;
            }
            if (highFx != null)
            {
                highFx.sprite = skill.IconHighlightedFx;
                highFx.color = Color.clear;
            }
                
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
                name = skill.Key.Remove("ethel_").Remove("nema_").ToString();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}