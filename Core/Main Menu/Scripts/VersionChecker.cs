using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Main_Menu.Scripts
{
    public class VersionChecker : MonoBehaviour
    {
        [SerializeField, Required]
        private TMP_Text label;
        private void Start()
        {
            label.text = $"Current Version: {Application.version}. Checking for newer ones...";
            try
            {
                UnityWebRequest request = UnityWebRequest.Get("https://kls96ofqhf.execute-api.us-east-1.amazonaws.com/version");
                UnityWebRequestAsyncOperation handler = request.SendWebRequest();

                handler.completed += _ =>
                {
                    try
                    {
                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogWarning(request.error);
                            return;
                        }

                        string newestVersion = request.downloadHandler.text;
                        label.text = Application.version != newestVersion ? 
                                         $"Newer version available: {newestVersion} . Current: {Application.version}" 
                                         : $"Current Version: {Application.version}. No newer versions available.";
                        request.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e.Message);
                        try
                        {
                            request.Dispose();
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning(exception.Message);
                        }
                    }
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }
}