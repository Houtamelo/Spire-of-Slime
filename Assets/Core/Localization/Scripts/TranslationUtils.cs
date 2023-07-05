using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using CsvHelper;
using CsvHelper.Configuration;
using KGySoft.CoreLibraries;
using UnityEngine;

namespace Core.Localization.Scripts
{
    public static class TranslationUtils
    {
        public const string MainFilesPath = "Core/Localization/Translations/";
        public static readonly Dictionary<Language, string> MainPaths;
        public static readonly Configuration MainConfiguration = new() { IgnoreQuotes = true, BadDataFound = context => { Debug.LogError($"Bad data found in translation file: {context.RawRecord}"); }};

        public const string SourceDescriptionsPath = "Core/Txt/Descriptions.txt";
        public static readonly Dictionary<Language, string> TargetDescriptionPaths;
        public static readonly Configuration DescriptionConfiguration = new() { MissingFieldFound = (_, _, _) => {},};

        public const string SourceBarksPath = "Core/Txt/Barks.txt";
        public static readonly Dictionary<Language, string> TargetBarkPaths;
        public static readonly Configuration BarkConfiguration = new();

        static TranslationUtils()
        {
            Language[] languages = Enum<Language>.GetValues();
            MainPaths = languages.ToDictionary(keySelector: language => language,              elementSelector: language => $"{MainFilesPath}{Enum<Language>.ToString(language).ToLowerInvariant()}.csv");
            TargetDescriptionPaths = languages.ToDictionary(keySelector: language => language, elementSelector: language => $"{MainFilesPath}{Enum<Language>.ToString(language).ToLowerInvariant()}_descriptions.csv");
            TargetBarkPaths = languages.ToDictionary(keySelector: language => language,        elementSelector: language => $"{MainFilesPath}{Enum<Language>.ToString(language).ToLowerInvariant()}_barks.csv");
        }
        
        public static Dictionary<CleanString, string> ReadTranslationsFromFile(string filePath, Configuration configuration)
        {
            Dictionary<CleanString, string> translations;
            if (File.Exists(filePath) == false)
            {
                return new Dictionary<CleanString, string>();
            }

            using (StreamReader reader = new(filePath))
            using (CsvReader csvReader = new(reader, configuration))
            {
                translations = csvReader.GetRecords<TranslationRow>().Where(r => r.Key.IsSome() && r.Text.IsSome()).ToDictionary(r => (CleanString)r.Key, r => r.Text);
            }

            return translations;
        }
        
        public static void WriteTranslationsToFile(string filePath, Dictionary<CleanString, string> translations, Configuration configuration)
        {
            using (StreamWriter writer = new(File.OpenWrite(filePath)))
            using (CsvWriter csvWriter = new(writer, configuration))
            {
                csvWriter.WriteRecords(translations.Select(kvp => new TranslationRow(kvp.Key.ToString(), kvp.Value)).OrderBy(row => row.Key));
            }
        }
        
        public static void CleanKey(CleanString keyToClean, TranslationType translationType)
        {
            Dictionary<Language, string> paths = translationType switch
            {
                TranslationType.Main        => MainPaths,
                TranslationType.Description => TargetDescriptionPaths,
                TranslationType.Bark        => TargetBarkPaths,
                _                           => throw new ArgumentOutOfRangeException(nameof(translationType), translationType, null)
            };
            
            Configuration configuration = translationType switch
            {
                TranslationType.Main        => MainConfiguration,
                TranslationType.Description => DescriptionConfiguration,
                TranslationType.Bark        => BarkConfiguration,
                _                           => throw new ArgumentOutOfRangeException(nameof(translationType), translationType, null)
            };

            HashSet<Language> removedFrom = new();
            foreach ((Language language, string path) in paths.Where(pair => pair.Key is not Language.English))
            {
                string fullPath = $"{Application.dataPath}/{path}";
                Dictionary<CleanString, string> existingTranslations = ReadTranslationsFromFile(fullPath, configuration);
                if (existingTranslations.ContainsKey(keyToClean) == false)
                    continue;

                removedFrom.Add(language);
                existingTranslations.Remove(keyToClean);
                WriteTranslationsToFile(fullPath, existingTranslations, configuration);
            }
            
            if (removedFrom.Count == 0)
                return;
            
            Debug.Log($"Removed key {keyToClean} from {string.Join(", ", removedFrom)}");
        }
    }
}