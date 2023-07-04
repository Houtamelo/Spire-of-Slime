using System.Collections;
using Core.Game_Manager.Scripts;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Start_Screen.Scripts
{
    public class StartScreenManager : MonoBehaviour
    {
        [SerializeField, Required]
        private GameObject ageWarningText;

        [SerializeField, Required]
        private GameObject prototypeWarningText;

        [SerializeField, Required]
        private GameObject supportersTextObj;
        
        [SerializeField, Required]
        private TMP_Text supportersText;

        [SerializeField, Required]
        private Button clickableArea;

        [SerializeField, Required]
        private TextAsset supportersList;

        private bool _loading;
        private int _screenCount;

        private void Awake()
        {
            if (Keyboard.current == null)
                Debug.LogWarning("Keyboard not detected!");

            clickableArea.onClick.AddListener(ButtonPressed);
            supportersText.text = supportersList.text;
        }

        private void Start()
        {
            Scene scene = SceneManager.GetSceneByName(SceneRef.GameManager.Name);
            if (!scene.IsValid() || !scene.isLoaded)
                SceneManager.LoadScene(sceneName: SceneRef.GameManager.Name, mode: LoadSceneMode.Additive);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.anyKey.wasPressedThisFrame)
                return;
            
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
                return;
            }

            ButtonPressed();
        }

        private void ButtonPressed()
        {
            switch (_screenCount)
            {
                case <= 0:
                    _screenCount = 1;
                    ageWarningText.SetActive(false);
                    supportersTextObj.SetActive(true);
                    break;
                case 1:
                    _screenCount = 2;
                    supportersTextObj.SetActive(false);
                    prototypeWarningText.SetActive(true);
                    break;
                case 2 when !_loading:
                    StartCoroutine(LoadGameManager());
                    _loading = true;
                    break;
            }
        }

        private IEnumerator LoadGameManager()
        {
            Scene scene = SceneManager.GetSceneByName(SceneRef.GameManager.Name);
            if (!scene.IsValid() || !scene.isLoaded)
                SceneManager.LoadScene(sceneName: SceneRef.GameManager.Name, mode: LoadSceneMode.Additive);
            while (GameManager.Instance.IsNone)
                yield return null;
            
            GameManager.Instance.Value.InitializeFromStartScreen();
        }
    }
}