using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using Utils.Extensions;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.BackgroundGeneration
{
    public record PropPickerRecord(bool[] Actives) : BackgroundChildRecord;
    
    public class PropPicker : MonoBehaviour, IBackgroundChild
    {
        [SerializeField]
        private GameObject[] props = new GameObject[0];
        
        [SerializeField]
        private float propQuantity = 0.2f;
        
        [SerializeField]
        private float thresholdAmplitude = 0.3f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (props.IsNullOrEmpty())
                Debug.Log($"Props are null on {name}");
            else if (props.Any(predicate: p => p == null))
                Debug.Log($"Some props are null on {name}");
        }
#endif
        public void Generate()
        {
            if (props.IsNullOrEmpty())
            {
                Debug.LogWarning($"Props are null on {name}", this);
                gameObject.SetActive(false);
                return;
            }

            using var pool = ListPool<GameObject>.Get(out List<GameObject> propPool);
            propPool.Add(props);

            float quantity = Random.Range(minInclusive: propQuantity - thresholdAmplitude, maxInclusive: propQuantity + thresholdAmplitude) * props.Length;
            for (int i = 0; i < quantity; i++) 
                propPool.TakeRandom().SetActive(true);

            foreach (GameObject obj in propPool) 
                obj.SetActive(false);
        }

        public void GenerateFromRecord(BackgroundChildRecord record)
        {
            if (record is not PropPickerRecord propPickerRecord)
            {
                Generate();
                Debug.LogWarning($"Invalid {nameof(record)} type on {nameof(PropPicker)} generation. Expected: {nameof(PropPickerRecord)}, got: {(record == null ? "null" : record.GetType().Name)}");
                return;
            }
            
            if (props.IsNullOrEmpty())
            {
                Debug.LogWarning($"Props are null on background generation: {name}", this);
                gameObject.SetActive(false);
                return;
            }
            
            if (propPickerRecord.Actives.IsNullOrEmpty())
            {
                Debug.LogWarning($"Actives are null on background generation: {name}", this);
                gameObject.SetActive(false);
                return;
            }
            
            if (propPickerRecord.Actives.Length != props.Length)
            {
                Debug.LogWarning($"Actives length is not equal to props length on background generation: {name}", this);
                gameObject.SetActive(false);
                return;
            }
            
            for (int index = 0; index < propPickerRecord.Actives.Length; index++)
                props[index].SetActive(propPickerRecord.Actives[index]);
        }

        public BackgroundChildRecord GetRecord()
        {
            if (props.IsNullOrEmpty())
            {
                Debug.LogWarning($"Props are null on {name}", this);
                return new PropPickerRecord(Array.Empty<bool>());
            }

            return new PropPickerRecord(props.Select(p => p.activeSelf).ToArray());
        }

        public bool IsDataValid(BackgroundChildRecord data, StringBuilder errors)
        {
            if (data is not PropPickerRecord propPickerRecord)
            {
                errors.AppendLine($"Invalid {nameof(data)} type on {nameof(PropPicker)} validation. Expected: {nameof(PropPickerRecord)}, got: {(data == null ? "null" : data.GetType().Name)}");
                return false;
            }
            
            if (props.IsNullOrEmpty())
            {
                errors.AppendLine($"Props are null on {name}");
                return false;
            }
            
            if (propPickerRecord.Actives.IsNullOrEmpty())
            {
                errors.AppendLine($"Actives are null on {name}");
                return false;
            }
            
            if (propPickerRecord.Actives.Length != props.Length)
            {
                errors.AppendLine($"Actives length is not equal to props length on {name}. Expected: {props.Length}, got: {propPickerRecord.Actives.Length}");
                return false;
            }
            
            return true;
        }
    }
}