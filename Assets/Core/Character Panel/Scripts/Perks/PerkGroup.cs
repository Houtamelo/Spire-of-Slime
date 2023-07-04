using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using UnityEngine;

namespace Core.Character_Panel.Scripts.Perks
{
    [CreateAssetMenu(fileName = "perk-group_", menuName = "Database/Combat/Perks/Group")]
    public class PerkGroup : ScriptableObject
    {
        [field: SerializeField] public PerkScriptable[] Perks { get; private set; } = new PerkScriptable[0];
        [field: SerializeField] public SkillScriptable[] Skills { get; private set; } = new SkillScriptable[0];
        [field: SerializeField] public bool BelongsToEthel { get; private set; }
        [SerializeField, HideInInspector] private bool triedToLoad;

#if UNITY_EDITOR
        [ContextMenu("Load Perks")]
        private void ForceLoad()
        {
            triedToLoad = false;
            OnValidate();
        }
        private void OnValidate()
        {
            if (triedToLoad || UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
                return;

            triedToLoad = true;
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path))
            {
                triedToLoad = false;
                return;
            }
            
            string folder = path[..path.LastIndexOf('/')];
            string folderName = folder[(folder.LastIndexOf('/') + 1)..];
            UnityEditor.AssetDatabase.RenameAsset(path, name);
            name = $"perk-group_{folderName}";

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PerkScript", new[] {folder});
            List<PerkScriptable> perks = new();
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PerkScriptable perk = UnityEditor.AssetDatabase.LoadAssetAtPath<PerkScriptable>(assetPath);
                if (perk != null)
                    perks.Add(perk);
            }
            
            Perks = perks.ToArray();
            BelongsToEthel = Perks.Any(p => p.name.Contains("ethel", StringComparison.InvariantCultureIgnoreCase));
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif        
    }
}