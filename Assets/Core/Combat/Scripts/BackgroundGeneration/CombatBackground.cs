using System.Text;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public record BackgroundRecord(BackgroundChildRecord[] ChildrenData, CleanString Key)
    {
        public bool IsDataValid(StringBuilder errors)
        {
            if (ChildrenData == null)
            {
                errors.AppendLine("Invalid ", nameof(BackgroundRecord), " data. ", nameof(ChildrenData), " is null");
                return false;
            }

            if (Key.IsNullOrEmpty())
            {
                errors.AppendLine("Invalid ", nameof(BackgroundRecord), " data. ", nameof(Key), " is null or empty");
                return false;
            }

            if (BackgroundDatabase.GetBackgroundPrefab(Key).TrySome(out CombatBackground prefab) == false)
            {
                errors.AppendLine("Invalid ", nameof(BackgroundRecord), " data. ", nameof(Key), ": ", Key.ToString(), " not found in database.");
                return false;
            }

            if (prefab.IsDataValid(this, errors) == false)
                return false;

            return true;
        }
    }
    public sealed class CombatBackground : MonoBehaviour
    {
        public CleanString Key => name.Replace("(Clone)", "");
        
        [SerializeField]
        private bool hasLocation;

        [SerializeField, ShowIf(nameof(hasLocation))]
        private BothWays location;
        
        public Option<BothWays> GetLocation => hasLocation ? location : Option<BothWays>.None;
        public BothWays SetLocation { set => location = value; }

        [field: SerializeField]
        public Color FillColor { get; private set; }

        private Option<SpriteRenderer[]> _childRenderers;
        private Option<LightController[]> _childLights;

        [NotNull]
        public BackgroundRecord GetRecord()
        {
            IBackgroundChild[] children = gameObject.GetComponentsInChildren<IBackgroundChild>(includeInactive: true);
            BackgroundChildRecord[] data = new BackgroundChildRecord[children.Length];
            for (int i = 0; i < children.Length; i++)
                data[i] = children[i].GetRecord();
            
            return new BackgroundRecord(data, Key);
        }
        
        public void Fade(float alpha, float duration)
        {
            if (_childRenderers.IsNone)
                return;
            
            foreach (SpriteRenderer childRenderer in _childRenderers.Value)
            {
                if (childRenderer.gameObject.activeInHierarchy)
                    childRenderer.DOFade(endValue: alpha, duration);
            }
        }

        public void SwitchLightsToSkillAnimation(float duration)
        {
            if (_childLights.IsNone)
                return;
            
            foreach (LightController childLight in _childLights.Value)
                childLight.SwitchToSkillAnimation(duration);
        }

        public void SwitchLightsToNormal(float duration)
        {
            if (_childLights.IsNone)
                return;
            
            foreach (LightController childLight in _childLights.Value)
                childLight.SwitchToNormal(duration);
        }

        [Button("Generate")]
        public void Generate()
        {
            Dive(transform);

            _childRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            _childLights = GetComponentsInChildren<LightController>(includeInactive: true);
            gameObject.SetActive(true);

            void Dive([NotNull] Transform current)
            {
                if (current.TryGetComponent(out IBackgroundChild backgroundChild))
                    backgroundChild.Generate();

                foreach (Transform child in current)
                    Dive(current: child);
            }
        }

        public void GenerateFromData(BackgroundRecord backgroundData)
        {
            _childRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            _childLights = GetComponentsInChildren<LightController>(includeInactive: true);
            
            IBackgroundChild[] children = gameObject.GetComponentsInChildren<IBackgroundChild>(includeInactive: true);
            for (int i = 0; i < children.Length; i++)
                children[i].GenerateFromRecord(backgroundData.ChildrenData[i]);
            
            gameObject.SetActive(true);
        }

        public bool IsDataValid([NotNull] BackgroundRecord data, StringBuilder errors)
        {
            IBackgroundChild[] children = gameObject.GetComponentsInChildren<IBackgroundChild>(includeInactive: true);
            if (children.Length != data.ChildrenData.Length)
            {
                errors.AppendLine("Invalid ", nameof(BackgroundRecord), " data. ", nameof(children), " length mismatch. Expected: ", children.Length.ToString(), " Got: ", data.ChildrenData.Length.ToString());
                return false;
            }
            
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].IsDataValid(data.ChildrenData[i], errors) == false)
                    return false;
            }

            if (data.Key != Key)
            {
                errors.AppendLine("Invalid ", nameof(BackgroundRecord), " data. ", nameof(Key), " mismatch. Expected: ", Key.ToString(), " Got: ", data.Key.ToString());
                return false;
            }
            
            return true;
        }
    }
}