using System;
using System.Text;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public record MutuallyExclusivePickerRecord(bool IsActive, int SpriteIndex) : BackgroundChildRecord;
    public class MutuallyExclusivePicker : MonoBehaviour, IBackgroundChild
    {
        [SerializeField] 
        private SpriteRenderer spriteRenderer;
        
        [SerializeField] 
        private Sprite[] sprites = new Sprite[0];
        
        [SerializeField]
        private float chanceToAppear = 1f;
        
        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Generate()
        {
            if (sprites.IsNullOrEmpty())
            {
                Debug.LogWarning($"Sprites are null on background generation: {name}", this);
                gameObject.SetActive(false);
                return;
            }
            
            if (Random.value > chanceToAppear)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            spriteRenderer.sprite = sprites.GetRandom();
        }

        public void GenerateFromRecord([CanBeNull] BackgroundChildRecord record)
        {
            if (record is not MutuallyExclusivePickerRecord mutuallyExclusivePickerRecord)
            {
                Generate();
                Debug.LogWarning($"Invalid {nameof(record)} type on {nameof(MutuallyExclusivePicker)} generation. Expected: {nameof(MutuallyExclusivePickerRecord)}, got: {(record == null ? "null" : record.GetType().Name)}");
                return;
            }

            if (sprites.IsNullOrEmpty())
            {
                Debug.LogWarning($"Sprites are null on background generation: {name}", this);
                gameObject.SetActive(false);
                return;
            }
            
            if (mutuallyExclusivePickerRecord.SpriteIndex < 0 || mutuallyExclusivePickerRecord.SpriteIndex >= sprites.Length)
            {
                Generate();
                Debug.LogWarning($"Invalid {nameof(mutuallyExclusivePickerRecord.SpriteIndex)} on {nameof(MutuallyExclusivePicker)} generation. Expected: 0 <= {nameof(mutuallyExclusivePickerRecord.SpriteIndex)} < {nameof(sprites)}.Length, got: {mutuallyExclusivePickerRecord.SpriteIndex}");
                return;
            }
            
            gameObject.SetActive(mutuallyExclusivePickerRecord.IsActive);
            spriteRenderer.sprite = sprites[mutuallyExclusivePickerRecord.SpriteIndex];
        }

        [NotNull]
        public BackgroundChildRecord GetRecord()
        {
            bool isActive = gameObject.activeSelf;

            if (sprites.IsNullOrEmpty())
                return new MutuallyExclusivePickerRecord(isActive, 0);
            
            int spriteIndex = Array.IndexOf(array: sprites, value: spriteRenderer.sprite);
            return new MutuallyExclusivePickerRecord(isActive, spriteIndex == -1 ? 0 : spriteIndex);
        }

        public bool IsDataValid([CanBeNull] BackgroundChildRecord data, StringBuilder errors)
        {
            if (data is not MutuallyExclusivePickerRecord mutuallyExclusivePickerRecord)
            {
                errors.AppendLine($"Invalid {nameof(data)} type on {nameof(MutuallyExclusivePicker)} validation. Expected: {nameof(MutuallyExclusivePickerRecord)}, got: {(data == null ? "null" : data.GetType().Name)}");
                return false;
            }
            
            if (mutuallyExclusivePickerRecord.SpriteIndex < 0 || mutuallyExclusivePickerRecord.SpriteIndex >= sprites.Length)
            {
                errors.AppendLine($"Invalid {nameof(mutuallyExclusivePickerRecord.SpriteIndex)} on {nameof(MutuallyExclusivePicker)} validation. Expected: 0 <= {nameof(mutuallyExclusivePickerRecord.SpriteIndex)} < {nameof(sprites)}.Length, got: {mutuallyExclusivePickerRecord.SpriteIndex}");
                return false;
            }
            
            return true;
        }
    }
}