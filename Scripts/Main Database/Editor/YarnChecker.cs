using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Core.Combat.Scripts;
using Core.Visual_Novel.Scripts;
using Main_Database.Audio;
using Main_Database.Combat;
using Main_Database.Visual_Novel;
using UnityEditor;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

namespace Main_Database.Editor
{
    public static class YarnChecker
    {
        private const string NewlyCreatedVariablesFolder = "Assets/Core/Visual Novel/Data/Other Variables/";
        private const string YarnScriptsFolder = "Assets/Core/Visual Novel/Data";

        [MenuItem("Tools/Check Yarn Files")]
        public static void CheckYarnFiles()
        {
            // get all text assets that have the extension .yarn

            List<TextAsset> textAssets = Utils.FindAssetsByType<TextAsset>(extension: ".yarn");
            
            // keep only files in the Assets/Data folder

            foreach (TextAsset asset in textAssets.ToArray())
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (!path.Contains(YarnScriptsFolder)) 
                    textAssets.Remove(asset);
            }

            if (textAssets.Count == 0)
            {
                Debug.LogError("No yarn files found");
                return;
            }

            StringBuilder mainBuilder = new();
            CheckCGs(textAssets, mainBuilder);
            CheckAmbienceSounds(textAssets, mainBuilder); 
            CheckSfxs(textAssets, mainBuilder);
            CheckMusic(textAssets, mainBuilder);
            CheckCombatScripts(textAssets, mainBuilder);
            CheckVariables(textAssets, mainBuilder);
            CheckPortraits(textAssets, mainBuilder);

            if (mainBuilder.Length > 0)
                Debug.LogWarning(mainBuilder.ToString());
        }

        private static void CheckCGs(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out CgDatabase cgDatabase);

            StringBuilder missingDoubleArrowsBuilder = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingCGs = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingAnimations = new();
            
            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                using StringReader reader = new(asset.text) ;
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;

                    int startIndex = line.IndexOf($"<<{YarnCommands.Cg} ", StringComparison.Ordinal);
                    if (startIndex != -1 && startIndex < commentIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 5;

                        string cgName = line[startIndex..endIndex];
                        if (string.IsNullOrEmpty(cgName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing cg name in {assetPath} at line: {line}");
                            continue;
                        }

                        string fileName = cgName;
                        fileName = fileName.ToAlphaNumericLower();
                        Option<Sprite> option = CgDatabase.GetCg(cgDatabase, fileName);
                        if (option.IsSome)
                            continue;
                        
                        if (missingCGs.TryGetValue(fileName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingCGs.Add(fileName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                    else if ((startIndex = line.IndexOf($"<<{YarnCommands.CgAnim} ", StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 10;
                        string animName = line[startIndex..endIndex];

                        if (string.IsNullOrEmpty(animName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing cg animation name in {assetPath} at line: {line}");
                            continue;
                        }

                        Option<string> option = cgDatabase.GetCgAnimationFilePath(animName);
                        if (option.IsSome)
                            continue;
                        
                        if (missingAnimations.TryGetValue(animName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingAnimations.Add(animName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                    else if ((startIndex = line.IndexOf($"<<{YarnCommands.CgAnimAsync} ", StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 16;
                        string animName = line[startIndex..endIndex];

                        if (string.IsNullOrEmpty(animName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing cg animation name in {assetPath} at line: {line}");
                            continue;
                        }

                        Option<string> option = cgDatabase.GetCgAnimationFilePath(animName);
                        if (option.IsSome)
                            continue;
                        
                        if (missingAnimations.TryGetValue(animName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingAnimations.Add(animName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                }
            }

            if (missingDoubleArrowsBuilder.Length > 0)
            {
                builder.AppendLine(missingDoubleArrowsBuilder.ToString());
                builder.AppendLine("\n");
            }

            if (missingCGs.Count > 0)
            {
                builder.AppendLine("-------Missing CGs-------");
                foreach ((string file, List<(int lineNumber, string asset)> lines) in missingCGs)
                {
                    builder.AppendLine(file);
                    foreach ((int lineNumber, string asset) in lines)
                        builder.AppendLine($"        line:{lineNumber}, at {asset}");
                }
                
                builder.AppendLine("\n");
            }

            if (missingAnimations.Count <= 0)
                return;
            
            builder.AppendLine("-------Missing CG Animations-------");
            foreach ((string file, List<(int lineNumber, string asset)> lines) in missingAnimations)
            {
                builder.AppendLine(file);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }
        
        // lines that have the <<set_ambience name volume>> or <<set_ambience name>> or <<add_ambience name volume>> or <<add_ambience name>> tags
        private static void CheckAmbienceSounds(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out AudioPathsDatabase soundDatabase);

            StringBuilder missingDoubleArrowsBuilder = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingFiles = new();

            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                using StringReader reader = new(asset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;

                    int startIndex = line.IndexOf($"<<{YarnCommands.AmbienceSet} ", StringComparison.Ordinal);
                    if (startIndex == -1)
                        startIndex = line.IndexOf($"<<{YarnCommands.AmbienceAdd} ", StringComparison.Ordinal);

                    if (startIndex == -1 || startIndex >= commentIndex)
                        continue;

                    int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                    if (endIndex == -1)
                    {
                        missingDoubleArrowsBuilder.AppendLine($"Missing \">>\" in {assetPath} at line: {line}");
                        continue;
                    }

                    startIndex += 15;
                    int volumeIndex = line.IndexOf(" ", startIndex + 1, count: endIndex - startIndex, StringComparison.Ordinal);
                    string soundName = volumeIndex == -1 ? line[startIndex..endIndex] : line[startIndex..volumeIndex];

                    if (string.IsNullOrEmpty(soundName))
                    {
                        missingDoubleArrowsBuilder.AppendLine($"Missing sound name in {assetPath} at line: {line}");
                        continue;
                    }

                    Option<string> option = soundDatabase.GetAudioPathInternal(soundName);
                    if (option.IsSome)
                        continue;

                    if (missingFiles.TryGetValue(soundName, out List<(int lineNumber, string asset)> lines))
                        lines.Add((lineNumber, asset.name));
                    else
                        missingFiles.Add(soundName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                }
            }

            if (missingDoubleArrowsBuilder.Length > 0)
            {
                builder.AppendLine(missingDoubleArrowsBuilder.ToString());
                builder.AppendLine("\n");
            }

            if (missingFiles.Count <= 0)
                return;
            
            builder.AppendLine("-------Missing ambience sounds-------");
            foreach ((string file, List<(int lineNumber, string asset)> lines) in missingFiles)
            {
                builder.AppendLine(file);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }
        
        // lines that have the <<sfx name>> or <<sfx name volume>> or <<wait_sfx name>> or <<wait_sfx name volume>> tags
        private static void CheckSfxs(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out AudioPathsDatabase soundDatabase);

            StringBuilder missingDoubleArrowsBuilder = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingFiles = new();

            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);

                using StringReader reader = new(asset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;

                    int commentsIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentsIndex == -1)
                        commentsIndex = int.MaxValue;
                    
                    int startIndex = line.IndexOf($"<<{YarnCommands.Sfx} ", StringComparison.Ordinal);
                    if (startIndex != -1 && startIndex < commentsIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 6;
                        int volumeIndex = line.IndexOf(" ", startIndex, count: endIndex - startIndex, StringComparison.Ordinal);

                        string soundName = volumeIndex == -1 ? line[startIndex..endIndex] : line[startIndex..volumeIndex];

                        if (string.IsNullOrEmpty(soundName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing sound name in {assetPath} at line: {line}");
                            continue;
                        }

                        Option<string> option = soundDatabase.GetAudioPathInternal(soundName);
                        if (option.IsSome)
                            continue;

                        if (missingFiles.TryGetValue(soundName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingFiles.Add(soundName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                    else if ((startIndex = line.IndexOf($"<{YarnCommands.SfxWait} ", StringComparison.Ordinal)) != -1 && startIndex < commentsIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 10;
                        int volumeIndex = line.IndexOf(" ", startIndex + 1, count: endIndex - startIndex, StringComparison.Ordinal);

                        string soundName = volumeIndex == -1 ? line[startIndex..endIndex] : line[startIndex..volumeIndex];

                        if (string.IsNullOrEmpty(soundName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing sound name in {assetPath} at line: {line}");
                            continue;
                        }

                        Option<string> option = soundDatabase.GetAudioPathInternal(soundName);
                        if (option.IsSome)
                            continue;

                        if (missingFiles.TryGetValue(soundName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingFiles.Add(soundName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                    else if ((startIndex = line.IndexOf($"<{YarnCommands.SfxMulti} ", StringComparison.Ordinal)) != -1 && startIndex < commentsIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 11;
                        int volumeIndex = line.IndexOf(" ", startIndex + 1, count: endIndex - startIndex, StringComparison.Ordinal);

                        string soundName = volumeIndex == -1 ? line[startIndex..endIndex] : line[startIndex..volumeIndex];

                        if (string.IsNullOrEmpty(soundName))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing sound name in {assetPath} at line: {line}");
                            continue;
                        }

                        Option<string> option = soundDatabase.GetAudioPathInternal(soundName);
                        if (option.IsSome)
                            continue;

                        if (missingFiles.TryGetValue(soundName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingFiles.Add(soundName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                }
            }

            if (missingDoubleArrowsBuilder.Length > 0)
            {
                builder.AppendLine(missingDoubleArrowsBuilder.ToString());
                builder.AppendLine("\n");
            }

            if (missingFiles.Count <= 0)
                return;

            builder.AppendLine("-------Missing sfx sounds-------");
            foreach ((string file, List<(int lineNumber, string asset)> lines) in missingFiles)
            {
                builder.AppendLine(file);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }
        
        // lines that have the <<music name>> or <<music name volume>> tags
        private static void CheckMusic(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out AudioPathsDatabase soundDatabase);
            StringBuilder missingDoubleArrowsBuilder = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingFiles = new();

            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                using StringReader reader = new(asset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;

                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;
                    
                    int startIndex = line.IndexOf($"<<{YarnCommands.Music} ", StringComparison.Ordinal);
                    if (startIndex == -1 || startIndex >= commentIndex)
                        continue;
                    
                    int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                    if (endIndex == -1)
                    {
                        missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                        continue;
                    }

                    startIndex += 8;
                    int volumeIndex = line.IndexOf(" ", startIndex, count: endIndex - startIndex, StringComparison.Ordinal);
                    string soundName = volumeIndex == -1 ? line[startIndex..endIndex] : line[startIndex..volumeIndex];

                    if (string.IsNullOrEmpty(soundName))
                    {
                        missingDoubleArrowsBuilder.AppendLine($"Missing sound name in {assetPath} at line: {line}");
                        continue;
                    }

                    Option<string> option = soundDatabase.GetAudioPathInternal(soundName);
                    if (option.IsSome)
                        continue;
                    
                    if (missingFiles.TryGetValue(soundName, out List<(int lineNumber, string asset)> lines))
                        lines.Add((lineNumber, asset.name));
                    else
                        missingFiles.Add(soundName, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                }
            }

            if (missingDoubleArrowsBuilder.Length > 0)
            {
                builder.AppendLine(missingDoubleArrowsBuilder.ToString());
                builder.AppendLine("\n");
            }

            if (missingFiles.Count <= 0)
                return;
            
            builder.AppendLine("-------Missing music sounds-------");
            foreach ((string file, List<(int lineNumber, string asset)> lines) in missingFiles)
            {
                builder.AppendLine(file);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }

        // lines that have the <<combat key onWinScene onLossScene>> tag
        // scenes are on the line with format: title: name
        private static void CheckCombatScripts(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out CombatScriptDatabase combatScriptDatabase);
            StringBuilder missingDoubleArrowsBuilder = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingFiles = new();
            HashSet<string> allScenes = new();
            Dictionary<string, List<(int lineNumber, string asset)>> missingScenes = new();

            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                using StringReader reader = new(asset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;

                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;
                    
                    int startIndex = line.IndexOf($"<<{YarnCommands.Combat} ", StringComparison.Ordinal);
                    if (startIndex != -1 && startIndex < commentIndex)
                    {
                        int endIndex = line.IndexOf(">>", startIndex, StringComparison.Ordinal);
                        if (endIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing >> in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex += 9;
                        int onWinSceneIndex = line.IndexOf(" ", startIndex, count: endIndex - startIndex, StringComparison.Ordinal);
                        if (onWinSceneIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing on win scene in {assetPath} at line: {line}");
                            continue;
                        }

                        string key = line[startIndex..onWinSceneIndex];
                        if (string.IsNullOrEmpty(key))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing key in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex = onWinSceneIndex + 1;
                        int onLossSceneIndex = line.IndexOf(" ", startIndex, count: endIndex - startIndex, StringComparison.Ordinal);
                        if (onLossSceneIndex == -1)
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing on loss scene in {assetPath} at line: {line}");
                            continue;
                        }

                        string onWinScene = line[startIndex..onLossSceneIndex];
                        if (string.IsNullOrEmpty(onWinScene))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing on win scene in {assetPath} at line: {line}");
                            continue;
                        }

                        startIndex = onLossSceneIndex + 1;
                        string onLossScene = line[startIndex..endIndex];
                        if (string.IsNullOrEmpty(onLossScene))
                        {
                            missingDoubleArrowsBuilder.AppendLine($"Missing on loss scene in {assetPath} at line: {line}");
                            continue;
                        }

                        if (missingScenes.TryGetValue(onWinScene, out List<(int lineNumber, string asset)> scenesLine))
                            scenesLine.Add((lineNumber, asset.name));
                        else
                            missingScenes.Add(onWinScene, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });

                        if (missingScenes.TryGetValue(onLossScene, out scenesLine))
                            scenesLine.Add((lineNumber, asset.name));
                        else
                            missingScenes.Add(onLossScene, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                        
                        if (combatScriptDatabase.Exists(key))
                            continue;
                        
                        if (missingFiles.TryGetValue(key, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, asset.name));
                        else
                            missingFiles.Add(key, new List<(int lineNumber, string asset)> { (lineNumber, asset.name) });
                    }
                    else if ((startIndex = line.IndexOf("title: ", StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        startIndex += 7;
                        string scene = line[startIndex..].Trim();
                        allScenes.Add(scene);
                    }
                }
            }
            
            foreach (string scene in allScenes)
                missingScenes.Remove(scene);
            
            if (missingDoubleArrowsBuilder.Length > 0)
            {
                builder.AppendLine(missingDoubleArrowsBuilder.ToString());
                builder.AppendLine("\n");
            }

            if (missingFiles.Count > 0)
            {
                builder.AppendLine("-------Missing combat scripts-------");
                foreach ((string file, List<(int lineNumber, string asset)> lines) in missingFiles)
                {
                    builder.AppendLine(file);
                    foreach ((int lineNumber, string asset) in lines)
                        builder.AppendLine($"        line:{lineNumber}, at {asset}");
                }
                
                builder.AppendLine("\n");
            }
            
            if (missingScenes.Count <= 0)
                return;
            
            builder.AppendLine("-------Missing scenes-------");
            foreach ((string file, List<(int lineNumber, string asset)> lines) in missingScenes)
            {
                builder.AppendLine(file);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }

        // variables can be found in every single line, their format is $variableName, variables are alphanumeric and can contain underscores and dashes
        private static void CheckVariables(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out VariableDatabase variableDatabase);
            Dictionary<string, List<(string asset, int lineNumber)>> missingFiles = new();
            foreach (TextAsset asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                using StringReader reader = new(asset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;

                    int startIndex = 0;
                    while ((startIndex = line.IndexOf("$", startIndex, StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        // check each letter after the $ to see if it's a valid character

                        startIndex += 1;

                        int endIndex = startIndex;
                        while (endIndex < line.Length)
                        {
                            char c = line[endIndex];
                            if (char.IsLetterOrDigit(c) || c is '_' or '-')
                                endIndex++;
                            else
                                break;
                        }

                        string variableName = line[startIndex..endIndex];

                        if (string.IsNullOrEmpty(variableName))
                        {
                            builder.AppendLine($"Missing variable name in {assetPath} at line: {line}");
                            startIndex = endIndex;
                            continue;
                        }

                        startIndex = endIndex;
                        if (variableDatabase.VariableAssetExists(variableName))
                            continue;
                        
                        if (missingFiles.TryGetValue(variableName, out List<(string asset, int lineNumber)> lines))
                            lines.Add((asset.name, lineNumber));
                        else
                            missingFiles.Add(variableName, new List<(string asset, int lineNumber)> { (asset.name, lineNumber) });

                        // create the variable scriptable object on the variable folder
                        string path = $"{NewlyCreatedVariablesFolder}{variableName}.asset";
                        SerializedVariable variable = ScriptableObject.CreateInstance<SerializedVariable>();
                        AssetDatabase.CreateAsset(variable, path);
                    }
                }
            }

            if (missingFiles.Count <= 0)
                return;

            builder.AppendLine($"-------Missing variables-------\n-------Those variables were automatically created in the folder: {NewlyCreatedVariablesFolder}-------");
            foreach ((string file, List<(string asset, int lineNumber)> lines) in missingFiles)
            {
                builder.AppendLine(file);
                foreach ((string asset, int lineNumber) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
                
            AssetDatabase.Refresh();
            builder.AppendLine("\n");
        }

        private static void CheckPortraits(List<TextAsset> assets, StringBuilder builder)
        {
            Utils.TryFindAssetWithType(out PortraitDatabase portraitDatabase);
            
            Dictionary<string, List<(int lineNumber, string asset)>> missingPortraits = new();
            
            foreach (TextAsset textAsset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(textAsset);
                using StringReader reader = new(textAsset.text);
                int lineNumber = -1;
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex == -1)
                        commentIndex = int.MaxValue;

                    int startIndex = 0;
                    if ((startIndex = line.IndexOf("#left:", StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        startIndex += 6;
                        int endIndex = line.IndexOf(' ', startIndex);
                        if (endIndex == -1)
                            endIndex = line.Length;
                        
                        string portraitName = line[startIndex..endIndex];
                        if (portraitDatabase.PortraitExists(portraitName))
                            continue;
                        
                        if (missingPortraits.TryGetValue(portraitName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, assetPath));
                        else
                            missingPortraits.Add(portraitName, new List<(int lineNumber, string asset)> { (lineNumber, assetPath) });
                    }
                    
                    if ((startIndex = line.IndexOf("#right:", StringComparison.Ordinal)) != -1 && startIndex < commentIndex)
                    {
                        startIndex += 7;
                        int endIndex = line.IndexOf(' ', startIndex);
                        if (endIndex == -1)
                            endIndex = line.Length;
                        
                        string portraitName = line[startIndex..endIndex];
                        if (portraitDatabase.PortraitExists(portraitName))
                            continue;
                        
                        if (missingPortraits.TryGetValue(portraitName, out List<(int lineNumber, string asset)> lines))
                            lines.Add((lineNumber, assetPath));
                        else
                            missingPortraits.Add(portraitName, new List<(int lineNumber, string asset)> { (lineNumber, assetPath) });
                    }
                }
            }
            
            if (missingPortraits.Count <= 0)
                return;

            builder.AppendLine("-------Missing portraits-------");
            foreach ((string portrait, List<(int lineNumber, string asset)> lines) in missingPortraits)
            {
                builder.AppendLine(portrait);
                foreach ((int lineNumber, string asset) in lines)
                    builder.AppendLine($"        line:{lineNumber}, at {asset}");
            }
            
            builder.AppendLine("\n");
        }
    }
}
