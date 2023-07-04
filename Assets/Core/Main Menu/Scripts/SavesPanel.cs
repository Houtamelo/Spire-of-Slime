using System.Collections.Generic;
using Save_Management;
using Save_Management.Serialization;
using UnityEngine;

namespace Core.Main_Menu.Scripts
{
    public sealed class SavesPanel : MonoBehaviour
    {
        [SerializeField]
        private SaveButton saveButtonPrefab;
        
        [SerializeField]
        private Transform savesParent;
        
        [SerializeField]
        private AudioSource pointerEnterAudioSource, clickDeleteAudioSource, confirmDeleteAudioSource;
        
        private readonly List<SaveButton> _saveButtons = new();

        private void Awake()
        {
            if (SaveFilesManager.AssertInstance(out SaveFilesManager saveFilesManager) == false)
                return;
            
            CreateSaveButtons(saveFilesManager.GetRecords);
            saveFilesManager.OnSaveFilesChanged += CreateSaveButtons;
        }
        
        private void OnDestroy()
        {
            if (SaveFilesManager.Instance.TrySome(out SaveFilesManager saveFilesManager))
                saveFilesManager.OnSaveFilesChanged -= CreateSaveButtons;
        }

        private void CreateSaveButtons(IReadOnlyCollection<SaveRecord> saves)
        {
            for (int index = _saveButtons.Count; index < saves.Count; index++)
            {
                SaveButton saveButton = Instantiate(original: saveButtonPrefab, parent: savesParent);
                saveButton.AssignAudioSources(pointerEnterAudioSource, clickDeleteAudioSource, confirmDeleteAudioSource);
                _saveButtons.Add(saveButton);
            }
            
            int saveButtonIndex = 0;
            foreach (SaveRecord save in saves)
            {
                _saveButtons[saveButtonIndex].SetSave(record: save);
                saveButtonIndex++;
            }
            
            for (; saveButtonIndex < _saveButtons.Count; saveButtonIndex++) 
                _saveButtons[saveButtonIndex].ResetMe();
        }
    }
}