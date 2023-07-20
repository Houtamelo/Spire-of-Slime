using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

namespace Core.Character_Panel.Scripts.Perks
{
    public class PerkButtonsCreator : MonoBehaviour
    {
        [SerializeField]
        private Transform ethelArrowsParent;

        [SerializeField]
        private Transform nemaArrowsParent;

        [SerializeField]
        private UIArrow arrowPrefab;

        [SerializeField]
        private List<UIArrow> cachedArrows = new();

        [SerializeField]
        private PerkButton perkButtonPrefab;

        [SerializeField]
        private SkillUnlockerButton skillUnlockerButtonPrefab;

        [SerializeField]
        private Transform ethelPerksParent;

        [SerializeField]
        private Transform nemaPerksParent;

        [SerializeField]
        private List<GameObject> cachedParents = new();

        [SerializeField]
        private List<PerkButton> cachedEthelPerkButtons = new();

        [SerializeField]
        private List<PerkButton> cachedNemaPerkButtons = new();

        [SerializeField]
        private List<SkillUnlockerButton> cachedEthelSkillUnlockerButtons = new();

        [SerializeField]
        private List<SkillUnlockerButton> cachedNemaSkillUnlockerButtons = new();

        [SerializeField]
        private AudioSource pointerEnterSource, confirmPerkSource, invalidClickSource;

#if UNITY_EDITOR

        [ContextMenu("CreateButtons"), Button(DirtyOnClick = true, Name = "Create Buttons"), ButtonGroup("buttons")]
        private void CreateButtons()
        {
            foreach (GameObject obj in cachedParents)
                DestroyImmediate(obj);
            
            cachedParents.Clear();
            cachedEthelPerkButtons.Clear();
            cachedNemaPerkButtons.Clear();
            cachedEthelSkillUnlockerButtons.Clear();
            cachedNemaSkillUnlockerButtons.Clear();

            PerkGroup[] allPerkGroups = FindAssetsByType<PerkGroup>();
            foreach (PerkGroup perkGroup in allPerkGroups.Where(p => p.BelongsToEthel))
                CreatePerkGroup(perkGroup);

            foreach (PerkGroup perkGroup in allPerkGroups.Where(p => !p.BelongsToEthel))
                CreatePerkGroup(perkGroup);
        }

        private void CreatePerkGroup([NotNull] PerkGroup group)
        {
            const float radius = 150f;
            int count = group.Perks.Length + group.Skills.Length;
            if (count == 0)
            {
                Debug.Log($"Group {group.name} has no perks or skills");
                return;
            }
            
            List<PerkButton> perkButtons = group.BelongsToEthel ? cachedEthelPerkButtons : cachedNemaPerkButtons;
            List<SkillUnlockerButton> skillButtons = group.BelongsToEthel ? cachedEthelSkillUnlockerButtons : cachedNemaSkillUnlockerButtons;
            Transform parent = new GameObject(group.name[(group.name.IndexOf('_') + 1)..]).AddComponent<RectTransform>();
            parent.SetParent(group.BelongsToEthel ? ethelPerksParent : nemaPerksParent, worldPositionStays: true);
            parent.localScale = Vector3.one;
            parent.position = Vector3.zero;
            cachedParents.Add(parent.gameObject);
            float anglePerItem = 360f / count;
            float currentAngle = 90f;
            for (int index = 0; index < group.Perks.Length; index++)
            {
                PerkScriptable perk = group.Perks[index];
                Vector3 position = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;
                PerkButton button = Instantiate(perkButtonPrefab, parent, worldPositionStays: true);
                Transform buttonTransform = button.transform;
                buttonTransform.localPosition = position;
                buttonTransform.localScale = perkButtonPrefab.transform.localScale;
                button.SetPerk(perk);
                button.SetSources(pointerEnterSource, confirmPerkSource, invalidClickSource);
                perkButtons.Add(button);
                currentAngle += anglePerItem;
            }
            
            for (int i = 0; i < group.Skills.Length; i++)
            {
                SkillScriptable skill = group.Skills[i];
                Vector3 position = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * radius;
                SkillUnlockerButton button = Instantiate(skillUnlockerButtonPrefab, parent, worldPositionStays: true);
                Transform buttonTransform = button.transform;
                buttonTransform.localPosition = position;
                buttonTransform.localScale = perkButtonPrefab.transform.localScale;
                button.SetSkill(skill);
                button.SetSources(pointerEnterSource, confirmPerkSource, invalidClickSource);
                skillButtons.Add(button);
                currentAngle += anglePerItem;
            }
        }

        [ContextMenu("Spawn Arrows"), Button, ButtonGroup("buttons")]
        private void SpawnArrows()
        {
            foreach (UIArrow arrow in cachedArrows)
                DestroyImmediate(arrow.gameObject);

            cachedArrows.Clear();
            foreach (PerkButton button in cachedEthelPerkButtons)
            {
                PerkScriptable perk = button.Perk;
                if (perk == null)
                    continue;

                foreach (PerkScriptable perkScript in perk.PerkPrerequisites)
                {
                    foreach (PerkButton otherButton in cachedEthelPerkButtons)
                    {
                        if (otherButton.Perk == perkScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, ethelArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                            cachedArrows.Add(arrow);
                        }
                    }
                }

                foreach (SkillScriptable skillScript in perk.SkillPrerequisites)
                {
                    foreach (SkillUnlockerButton otherButton in cachedEthelSkillUnlockerButtons)
                    {
                        if (otherButton.Skill == skillScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, ethelArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                            cachedArrows.Add(arrow);
                        }
                    }
                }
            }

            foreach (SkillUnlockerButton button in cachedEthelSkillUnlockerButtons)
            {
                SkillScriptable skill = button.Skill;
                if (skill == null)
                    continue;

                foreach (PerkScriptable perkScript in skill.PerkPrerequisites)
                {
                    foreach (PerkButton otherButton in cachedEthelPerkButtons)
                    {
                        if (otherButton.Perk == perkScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, ethelArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                            cachedArrows.Add(arrow);
                        }
                    }
                }

                foreach (SkillScriptable skillScript in skill.SkillPrerequisites)
                {
                    foreach (SkillUnlockerButton otherButton in cachedEthelSkillUnlockerButtons)
                    {
                        if (otherButton.Skill == skillScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, ethelArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                            cachedArrows.Add(arrow);
                        }
                    }
                }
            }
            
            foreach (PerkButton button in cachedNemaPerkButtons)
            {
                PerkScriptable perk = button.Perk;
                if (perk == null)
                    continue;

                foreach (PerkScriptable perkScript in perk.PerkPrerequisites)
                {
                    foreach (PerkButton otherButton in cachedNemaPerkButtons)
                    {
                        if (otherButton.Perk == perkScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, nemaArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                        }
                    }
                }

                foreach (SkillScriptable skillScript in perk.SkillPrerequisites)
                {
                    foreach (SkillUnlockerButton otherButton in cachedNemaSkillUnlockerButtons)
                    {
                        if (otherButton.Skill == skillScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, nemaArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                        }
                    }
                }
            }
            
            foreach (SkillUnlockerButton button in cachedNemaSkillUnlockerButtons)
            {
                SkillScriptable skill = button.Skill;
                if (skill == null)
                    continue;

                foreach (PerkScriptable perkScript in skill.PerkPrerequisites)
                {
                    foreach (PerkButton otherButton in cachedNemaPerkButtons)
                    {
                        if (otherButton.Perk == perkScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, nemaArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                        }
                    }
                }

                foreach (SkillScriptable skillScript in skill.SkillPrerequisites)
                {
                    foreach (SkillUnlockerButton otherButton in cachedNemaSkillUnlockerButtons)
                    {
                        if (otherButton.Skill == skillScript)
                        {
                            UIArrow arrow = Instantiate(arrowPrefab, nemaArrowsParent);
                            arrow.SetPoints(otherButton.transform.position, button.transform.position);
                        }
                    }
                }
            }
        }

        [NotNull]
        private static T[] FindAssetsByType<T>() where T : Object
        {
            List<T> assets = new();
            
            string typeName = typeof(T).Name;
            string filter = $"t:{typeName}";
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                    assets.Add(asset);
            }

            return assets.ToArray();
        }
#endif
    }
}