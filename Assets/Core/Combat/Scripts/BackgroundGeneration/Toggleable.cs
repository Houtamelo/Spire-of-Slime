using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public record ToggleableRecord(bool IsActive) : BackgroundChildRecord;
    
    public class Toggleable : MonoBehaviour, IBackgroundChild
    {
        [SerializeField] 
        private float appearChance = 0.5f;

        public void Generate() { gameObject.SetActive(Random.value <= appearChance); }

        public void GenerateFromRecord([CanBeNull] BackgroundChildRecord record)
        {
            if (record is not ToggleableRecord toggleableRecord)
            {
                Generate();
                Debug.LogWarning($"Invalid {nameof(record)} type on {nameof(Toggleable)} generation. Expected: {nameof(ToggleableRecord)}, got: {(record == null ? "null" : record.GetType().Name)}");
                return;
            }
            
            gameObject.SetActive(toggleableRecord.IsActive);
        }

        [NotNull]
        public BackgroundChildRecord GetRecord() => new ToggleableRecord(gameObject.activeSelf);

        public bool IsDataValid([CanBeNull] BackgroundChildRecord data, StringBuilder errors)
        {
            if (data is not ToggleableRecord toggleableRecord)
            {
                errors.AppendLine($"Invalid {nameof(data)} type on {nameof(Toggleable)} validation. Expected: {nameof(ToggleableRecord)}, got: {(data == null ? "null" : data.GetType().Name)}");
                return false;
            }
            
            return true;
        }
    }
}