/*using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.AI.Training
{
    public class ProbabilityDisplay : Singleton<ProbabilityDisplay>
    {
        [SerializeField] private GameObject boxPrefab;
        
        private readonly List<GameObject> _spawnedBoxes = new();

        public void DisplayProbabilities(List<(string text, float probability)> probabilities, Vector3 worldPosition)
        {
            for (int i = _spawnedBoxes.Count; i < probabilities.Count; i++) 
                CreateBox();
            
            for (int i = 0; i < probabilities.Count; i++)
            {
                GameObject box = _spawnedBoxes[i];
                TMP_Text text = box.GetComponentInChildren<TMP_Text>();
                text.text = $"{probabilities[i].text}: {probabilities[i].probability.ToString()}";
                box.gameObject.SetActive(true);
            }
            
            for (int i = probabilities.Count; i < _spawnedBoxes.Count; i++)
                _spawnedBoxes[i].gameObject.SetActive(false);
            
            transform.position = worldPosition;
            gameObject.SetActive(true);
        }

        private void CreateBox()
        {
            GameObject box = Instantiate(boxPrefab, transform);
            _spawnedBoxes.Add(box);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}*/