using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Combat.Scripts.Barks;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using CsvHelper;
using DeepL;
using DeepL.Model;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEditor;
using UnityEngine;
using static Core.Localization.Scripts.TranslationUtils;

namespace Core.Localization.Scripts.Editor
{
    public static class TranslationBuilder
    {
        private const string AuthKeyPath = "Core/Localization/Scripts/Editor/DeepLAuthKey.txt";
        private static readonly string AuthKey;

        private static readonly Dictionary<Language, string> LanguageCodes;

        static TranslationBuilder()
        {
            AuthKey = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/{AuthKeyPath}").text;
            LanguageCodes = new Dictionary<Language, string>
            {
                [Language.Bulgarian] = LanguageCode.Bulgarian,
                [Language.Chinese] = LanguageCode.Chinese,
                [Language.English] = LanguageCode.English,
                [Language.Czech] = LanguageCode.Czech,
                [Language.Danish] = LanguageCode.Danish,
                [Language.Dutch] = LanguageCode.Dutch,
                [Language.Estonian] = LanguageCode.Estonian,
                [Language.Finnish] = LanguageCode.Finnish,
                [Language.French] = LanguageCode.French,
                [Language.German] = LanguageCode.German,
                [Language.Greek] = LanguageCode.Greek,
                [Language.Hungarian] = LanguageCode.Hungarian,
                [Language.Indonesian] = LanguageCode.Indonesian,
                [Language.Italian] = LanguageCode.Italian,
                [Language.Japanese] = LanguageCode.Japanese,
                [Language.Korean] = LanguageCode.Korean,
                [Language.Latvian] = LanguageCode.Latvian,
                [Language.Lithuanian] = LanguageCode.Lithuanian,
                [Language.Norwegian] = LanguageCode.Norwegian,
                [Language.Polish] = LanguageCode.Polish,
                [Language.Romanian] = LanguageCode.Romanian,
                [Language.Russian] = LanguageCode.Russian,
                [Language.Slovak] = LanguageCode.Slovak,
                [Language.Slovenian] = LanguageCode.Slovenian,
                [Language.Spanish] = LanguageCode.Spanish,
                [Language.Swedish] = LanguageCode.Swedish,
                [Language.Turkish] = LanguageCode.Turkish,
                [Language.Ukrainian] = LanguageCode.Ukrainian,
                [Language.PortugueseBrazilian] = LanguageCode.PortugueseBrazilian,
                [Language.PortugueseEuropean] = LanguageCode.PortugueseEuropean
            };
        }
        
        [MenuItem("Tools/Localization/Fetch Translations")]
        private static async void TranslateAllFiles()
        {
            Translator translator = new(AuthKey);
            await TranslateMainFiles(translator);
            await TranslateDescriptionFiles(translator);
            await TranslateBarkFiles(translator);
            
            Debug.Log("Finished translating");
        }

        private static async Task TranslateMainFiles(Translator translator)
        {
            Dictionary<CleanString, string> source = ReadTranslationsFromFile($"{Application.dataPath}/{MainPaths[Language.English]}", MainConfiguration);
            foreach ((Language language, string path) in MainPaths.Where(pair => pair.Key is not Language.English))
            {
                string fullPath = $"{Application.dataPath}/{path}";
                Dictionary<CleanString, string> existingTranslations = ReadTranslationsFromFile(fullPath, MainConfiguration);
                HashSet<CleanString> missingKeys = source.Keys.ToHashSet();
                missingKeys.Remove(existingTranslations.Keys);
                
                if (missingKeys.Count == 0)
                    continue;

                Dictionary<CleanString, string> results = await TranslateFromDeepL(keysToTranslate: missingKeys, source, language, translator);
                foreach ((CleanString key, string trans) in results)
                    existingTranslations[key] = trans;

                WriteTranslationsToFile(fullPath, existingTranslations, MainConfiguration);
            }
        }
        
        [UsedImplicitly]
        private struct DescriptionData
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string FlavorText { get; set; }
        }

        private static async Task TranslateDescriptionFiles(Translator translator)
        {
            Dictionary<CleanString, string> source = ReadTranslationsFromFile($"{Application.dataPath}/{TargetDescriptionPaths[Language.English]}", DescriptionConfiguration);
            HashSet<CleanString> forceReTranslate = new();
            {
                Dictionary<CleanString, string> descriptionsSheet = GetEnglishDescriptions();
                foreach ((CleanString key, string trans) in descriptionsSheet)
                {
                    if (source.TryGetValue(key, out string sourceTrans) == false)
                    {
                        source.Add(key, trans);
                        continue;
                    }

                    if (sourceTrans != trans)
                    {
                        source[key] = trans;
                        forceReTranslate.Add(key);
                    }
                }
            }
            
            WriteTranslationsToFile($"{Application.dataPath}/{TargetDescriptionPaths[Language.English]}", source, DescriptionConfiguration);

            foreach ((Language language, string path) in TargetDescriptionPaths.Where(pair => pair.Key is not Language.English))
            {
                string fullPath = $"{Application.dataPath}/{path}";
                Dictionary<CleanString, string> existingTranslations = ReadTranslationsFromFile(fullPath, DescriptionConfiguration);
                HashSet<CleanString> missingKeys = source.Keys.ToHashSet();
                missingKeys.Remove(existingTranslations.Keys);
                if (forceReTranslate.Count != 0)
                    missingKeys.Add(forceReTranslate);

                Dictionary<CleanString, string> results = await TranslateFromDeepL(keysToTranslate: missingKeys, source, language, translator);
                foreach ((CleanString key, string trans) in results)
                    existingTranslations[key] = trans;

                WriteTranslationsToFile(fullPath, existingTranslations, DescriptionConfiguration);
            }
        }
        
        private static Dictionary<CleanString, string> GetEnglishDescriptions()
        {
            List<DescriptionData> descriptionsData;
            using (StreamReader reader = new($"{Application.dataPath}/{SourceDescriptionsPath}"))
            using (CsvReader csvReader = new(reader, DescriptionConfiguration))
            {
                descriptionsData = csvReader.GetRecords<DescriptionData>().ToList();
            }
            
            Dictionary<CleanString, string> descriptions = new(descriptionsData.Count);
            foreach (DescriptionData data in descriptionsData)
            {
                CleanString baseKey = data.Key;
                if (data.DisplayName.IsSome())
                {
                    CleanString displayKey = $"{baseKey.ToString()}_display-name";
                    descriptions.Add(displayKey, data.DisplayName);
                }

                if (data.Description.IsSome())
                {
                    CleanString descriptionKey = $"{baseKey.ToString()}_description";
                    descriptions.Add(descriptionKey, data.Description);
                }

                if (data.FlavorText.IsSome())
                {
                    CleanString flavorTextKey = $"{baseKey.ToString()}_flavor-text";
                    descriptions.Add(flavorTextKey, data.FlavorText);
                }
            }
            
            return descriptions;
        }
        
        [UsedImplicitly]
        private struct BarkData
        {
            public BarkType BarkType { get; set; }
            public string CharacterKeyOne { get; set; }
            public string BarkOne { get; set; }
            public string CharacterKeyTwo { get; set; }
            public string BarkTwo { get; set; }
        }

        private static async Task TranslateBarkFiles(Translator translator)
        {
            Dictionary<CleanString, string> source = ReadTranslationsFromFile($"{Application.dataPath}/{TargetBarkPaths[Language.English]}", BarkConfiguration);
            HashSet<CleanString> forceReTranslate = new();
            {
                Dictionary<CleanString, string> barksSheet = GetEnglishBarks();
                foreach ((CleanString key, string trans) in barksSheet)
                {
                    if (source.TryGetValue(key, out string sourceTrans) == false)
                    {
                        source.Add(key, trans);
                        continue;
                    }

                    if (sourceTrans != trans)
                    {
                        source[key] = trans;
                        forceReTranslate.Add(key);
                    }
                }
            }
            
            WriteTranslationsToFile($"{Application.dataPath}/{TargetBarkPaths[Language.English]}", source, BarkConfiguration);
            
            foreach ((Language language, string path) in TargetBarkPaths.Where(pair => pair.Key is not Language.English))
            {
                string fullPath = $"{Application.dataPath}/{path}";
                Dictionary<CleanString, string> existingTranslations = ReadTranslationsFromFile(fullPath, BarkConfiguration);
                HashSet<CleanString> missingKeys = source.Keys.ToHashSet();
                missingKeys.Remove(existingTranslations.Keys);
                if (forceReTranslate.Count != 0)
                    missingKeys.Add(forceReTranslate);

                Dictionary<CleanString, string> results = await TranslateFromDeepL(keysToTranslate: missingKeys, source, language, translator);
                foreach ((CleanString key, string trans) in results)
                    existingTranslations[key] = trans;

                WriteTranslationsToFile(fullPath, existingTranslations, BarkConfiguration);
            }
        }

        private static Dictionary<CleanString, string> GetEnglishBarks()
        {
            Dictionary<BarkType, List<BarkData>> barksData = Enum<BarkType>.GetValues().ToDictionary(bark => bark, _ => new List<BarkData>());
            using (StreamReader reader = new($"{Application.dataPath}/{SourceBarksPath}"))
            using (CsvReader csvReader = new(reader))
            {
                foreach (BarkData data in csvReader.GetRecords<BarkData>())
                {
                    if (data.CharacterKeyOne.IsNone() || data.CharacterKeyTwo.IsNone() || data.BarkOne.IsNone() || data.BarkTwo.IsNone())
                        continue;
                    
                    barksData[data.BarkType].Add(data);
                }
            }
            
            Dictionary<CleanString, string> allBarks = new(barksData.Count);
            foreach ((BarkType barkType, List<BarkData> barks) in barksData)
            {
                int index = 0;
                foreach (BarkData data in barks)
                {
                    CleanString ethelKey = $"{data.CharacterKeyOne}_{barkType}_{index}";
                    allBarks.Add(ethelKey, data.BarkOne);
                    
                    CleanString nemaKey = $"{data.CharacterKeyTwo}_{barkType}_{index}";
                    allBarks.Add(nemaKey, data.BarkTwo);
                    
                    index++;
                }
            }

            return allBarks;
        }

        private static async Task<Dictionary<CleanString, string>> TranslateFromDeepL(HashSet<CleanString> keysToTranslate, Dictionary<CleanString, string> source, Language language, Translator translator)
        {
            TextTranslateOptions options = new() { PreserveFormatting = true };
            CleanString[] indexableKeys = keysToTranslate.ToArray();
            string[] indexableSource = indexableKeys.Select(key => source[key]).ToArray();
            Debug.Log($"Requesting: {indexableSource.Length} translations, with a total of: {indexableSource.Sum(s => s.Length).ToString()} characters, language: {language}");
            using Task<TextResult[]> task = translator.TranslateTextAsync(indexableSource, LanguageCode.English, LanguageCodes[language], options);
            await task;

            if (task.IsCompletedSuccessfully == false)
            {
                throw task.Exception!;
            }

            TextResult[] result = task.Result;
            Dictionary<CleanString, string> translationResults = new();
            for (int index = 0; index < result.Length; index++)
            {
                translationResults[indexableKeys[index]] = result[index].Text;
            }

            return translationResults;
        }

        [MenuItem("Tools/Localization/Test Auth Key")]
        private static async void TestKey()
        {
            Translator translator = new(AuthKey);
            string source = "Hello World";
            using Task<TextResult> task = translator.TranslateTextAsync(source, LanguageCode.English, LanguageCode.PortugueseBrazilian);
            await task;
            if (task.IsCompletedSuccessfully == false)
            {
                throw task.Exception!;
            }
            
            Debug.Log($"Translated: {task.Result.Text}");
        }

        [MenuItem("Tools/Localization/Create empty files")]
        private static void CreateEmptyFiles()
        {
            foreach ((Language language, string path) in MainPaths.Concat(TargetDescriptionPaths).Concat(TargetBarkPaths))
            {
                if (language == Language.English)
                    continue;
                
                string filePath = $"{Application.dataPath}/{path}";
                if (File.Exists(filePath) == false)
                    File.WriteAllText(filePath, "Key,Text");
            }

            Debug.Log("Finished creating files");
        }
    }
}