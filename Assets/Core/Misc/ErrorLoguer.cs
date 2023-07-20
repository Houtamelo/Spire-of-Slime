using System;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Utils.Patterns;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Misc
{
    public sealed class ErrorLoguer : MonoBehaviour
    {
        [SerializeField, Required] 
        private GameObject logPanel;
        
        [SerializeField, Required] 
        private TMP_Text logText;
        
        [SerializeField, Required] 
        private TMP_Text logsPath;
        
        [SerializeField, Required]
        private Button closeButton;
        
        private static readonly StringBuilder LOG = new();
        
        private static Option<ErrorLoguer> _instance;
        private static ErrorLoguer Instance
        {
            get
            {
                if (_instance.IsNone)
                {
                    GameObject obj = Resources.Load<GameObject>("ErrorLoguer");
                    _instance = Instantiate(obj).GetComponent<ErrorLoguer>();
                }
                return _instance.Value;
            }
        }
        
        private string _logsFolder;
        private bool _awoken;

        private void Awake()
        {
            if (_awoken)
                return;
            
            _awoken = true;
            _logsFolder = $"{Application.streamingAssetsPath}/Logs";
            closeButton.onClick.AddListener(ClosePanel);
            logsPath.text = $"Unity logs can be found on: {Application.consoleLogPath}\n Custom logs can be found on: {_logsFolder}";
            logsPath.text = $"{logsPath.text}\nThe developers are interested in all logs, but most importantly the custom ones.";
            Application.logMessageReceived += OnLogMessageReceived;

            try
            {
                if (Directory.Exists(_logsFolder) == false)
                    Directory.CreateDirectory(_logsFolder);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void Update()
        {
            if (Keyboard.current.capsLockKey.wasPressedThisFrame) 
                logPanel.SetActive(!logPanel.activeSelf);
        }

        private void OnApplicationQuit()
        {
            if (logText.text == null)
                return;
            
            string path = $"{_logsFolder}/{DateTime.Now.ToString(provider: CultureInfo.InvariantCulture).Replace(oldValue: "/", newValue: "-").Replace(oldValue: ":",newValue: "-")}.txt";
            File.WriteAllText(path: path, contents: LOG.ToString());
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            string originalCondition = condition;
            condition = $"[{DateTime.Now.Hour.ToString()}:{DateTime.Now.Minute.ToString()}:{DateTime.Now.Second.ToString()}] {originalCondition}";
            LOG.AppendLine($"{condition}: {Enum<LogType>.ToString(type)} : {stackTrace}");
            
            if (type is LogType.Assert or LogType.Error or LogType.Exception)
                Instance.LogInternal();
        }

        private void LogInternal()
        {
            logText.text = LOG.ToString();
            logPanel.SetActive(true);
        }

        private void ClosePanel()
        {
            logPanel.SetActive(false);
        }
    }
}