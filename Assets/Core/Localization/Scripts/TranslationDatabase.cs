using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Core.Main_Database;
using Core.Pause_Menu.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using CsvHelper;
using CsvHelper.Configuration;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Localization.Scripts
{
    public class TranslationDatabase : SerializedScriptableObject
    {
        private static readonly Configuration Configuration = new() { IgnoreQuotes = true, BadDataFound = context => { Debug.LogError($"Bad data found in translation file: {context.RawRecord}"); }};
        public static readonly string[] Substitutions = {"{0}", "{1}", "{2}", "{3}", "{4}", "{5}", "{6}", "{7}", "{8}", "{9}", "{10}", "{11}", "{12}", "{13}", "{14}", "{15}", "{16}", "{17}", "{18}", "{19}"};
                
        private static TranslationDatabase Instance => DatabaseManager.Instance.TranslationDatabase;

        [SerializeField, Required]
        private TextAsset[] languageFiles;

        private readonly Dictionary<Language, TextAsset> _mappedFiles = new();

        [OdinSerialize, Required]
        private HashSet<CleanString> _missingTranslations;

        private readonly Dictionary<Language, Dictionary<CleanString, TranslationResult>> _allTranslations = Enum<Language>.GetValues().ToDictionary(l => l, elementSelector: _ => new Dictionary<CleanString, TranslationResult>());

        [Pure]
        public static TranslationResult Get(CleanString key) => Get(key, PauseMenuManager.CurrentLanguage);

        public static TranslationResult Get(CleanString key, Language language)
        {
#if UNITY_EDITOR
            if (Instance._allTranslations[Language.English].ContainsKey(key) == false)
            {
                Instance._missingTranslations.Add(key);
                UnityEditor.EditorUtility.SetDirty(Instance);
            }
#endif
            if (Instance._allTranslations[language].TryGetValue(key, out TranslationResult translation))
                return translation;
            
            Debug.LogWarning($"Translation for key: {key} in language: {language} not found.");
            return new TranslationResult { Text = string.Empty };
        }

        public void Initialize()
        {
            foreach (TextAsset textAsset in languageFiles)
            {
                if (Enum<Language>.TryParse(textAsset.name, ignoreCase: true, out Language language))
                    _mappedFiles[language] = textAsset;
                else
                    Debug.LogError($"Language file: {textAsset.name} is not a valid language.");
            }

            foreach ((Language language, TextAsset file) in _mappedFiles)
            {
                Dictionary<CleanString, TranslationResult> languageTrans = new();
                _allTranslations[language] = languageTrans;
                
                using TextReader textReader = new StringReader(file.text);
                using CsvReader csvReader = new(textReader, Configuration);

                while (csvReader.Read())
                {
                    TranslationRow row = csvReader.GetRecord<TranslationRow>();
                    if (row == null)
                    {
                        Debug.Log("Skipped: null, language: " + language);
                        continue;
                    }
                        
                    if (row.Key.IsSome() && row.Text.IsSome())
                    {
                        string text = row.Text;
                        int count = 0;
                        while (true)
                        {
                            if (text.Contains(Substitutions[count]))
                            {
                                count++;
                                continue;
                            }
                            
                            break;
                        }
                        
                        languageTrans.Add(row.Key, new TranslationResult(text, count));
                    }
                    else
                    {
                        Debug.Log("Skipped:" + row);
                    }
                }
            }
        }
        
#if UNITY_EDITOR
        [Button]
        public void UpdateMissingTranslations()
        {
            Initialize();
            _missingTranslations ??= new HashSet<CleanString>();
            Dictionary<CleanString, TranslationResult> english = _allTranslations[Language.English];
            _missingTranslations.Remove(english.Keys);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}