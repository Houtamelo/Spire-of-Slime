using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Save_Management;
using Save_Management.Serialization;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Main_Menu.Scripts
{
    public sealed class SaveButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField, Required] 
        private TMP_Text saveName;
        
        [SerializeField, Required] 
        private Button deleteButton;
        
        [SerializeField, Required]
        private Button confirmDeleteButton;
        
        [SerializeField, Required] 
        private Button loadButton;
        
        private SaveRecord _saveRecord;
        private AudioSource _pointerEnterAudioSource, _clickDeleteAudioSource, _confirmDeleteAudioSource;

        private void Start()
        {
            confirmDeleteButton.onClick.AddListener(DeleteSave);
            deleteButton.onClick.AddListener(ActivateConfirmDeleteButton);
            loadButton.onClick.AddListener(LoadSave);
        }

        private void ActivateConfirmDeleteButton()
        {
            _clickDeleteAudioSource.Play();
            confirmDeleteButton.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            confirmDeleteButton.gameObject.SetActive(false);
        }

        public void AssignAudioSources(AudioSource pointerEnterAudioSource, AudioSource clickDeleteAudioSource, AudioSource confirmDeleteAudioSource)
        {
            _pointerEnterAudioSource = pointerEnterAudioSource;
            _clickDeleteAudioSource = clickDeleteAudioSource;
            _confirmDeleteAudioSource = confirmDeleteAudioSource;
        }

        public void SetSave(SaveRecord record)
        {
            saveName.text = record.Name;
            _saveRecord = record;
            confirmDeleteButton.gameObject.SetActive(false);
        }

        private void DeleteSave()
        {
            Option<SaveFilesManager> savesManager = SaveFilesManager.Instance;
            if (savesManager.IsNone)
            {
                Debug.LogError("Save files manager not found.", this);
                return;
            }
            
            _confirmDeleteAudioSource.Play();
            savesManager.Value.DeleteSave(_saveRecord);
        }

        private void LoadSave()
        {
            if (GameManager.AssertInstance(out GameManager gameManager) == false)
                return;

            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.LoadGame.Play();
            
            gameManager.LoadSave(_saveRecord);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerEnterAudioSource.Play();
        }

        public void ResetMe()
        {
            saveName.text = "";
            _saveRecord = null;
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            saveName = transform.Find(n: "SaveName").GetComponent<TMP_Text>();
            deleteButton = transform.Find(n: "DeleteButton").GetComponent<Button>();
            loadButton = transform.Find(n: "LoadButton").GetComponent<Button>();
            
            if (saveName == null) 
                Debug.Log("SaveName is null");

            if (deleteButton == null) 
                Debug.Log("DeleteButton is null");
            
            if (loadButton == null) 
                Debug.Log("LoadButton is null");
        }
    }
}