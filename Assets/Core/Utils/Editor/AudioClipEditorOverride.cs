/*using UnityEngine;
using System;
using System.Collections;
using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace Utils.Editor
{
    [CustomEditor(typeof(AudioImporter)), CanEditMultipleObjects]
    public class AudioClipEditorOverride : AssetImporterEditor
    {
        static class Style
        {
            public static readonly GUIContent[] kSampleRateStrings = new[] {"8,000 Hz", "11,025 Hz", "22,050 Hz", "44,100 Hz", "48,000 Hz", "96,000 Hz", "192,000 Hz"}.Select(s => new GUIContent(s)).ToArray();
            public static readonly int[] kSampleRateValues = {8000, 11025, 22050, 44100, 48000, 96000, 192000};

            public static GUIContent LoadType = EditorGUIUtility.TrTextContent("Load Type");
            public static GUIContent PreloadAudioData = EditorGUIUtility.TrTextContent("Preload Audio Data*");
            public static GUIContent CompressionFormat = EditorGUIUtility.TrTextContent("Compression Format");
            public static GUIContent Quality = EditorGUIUtility.TrTextContent("Quality");
            public static GUIContent SampleRateSetting = EditorGUIUtility.TrTextContent("Sample Rate Setting");
            public static GUIContent SampleRate = EditorGUIUtility.TrTextContent("Sample Rate");
            public static GUIContent DefaultPlatform = EditorGUIUtility.TrTextContent("Default");
            public static GUIContent SharedSettingInformation = EditorGUIUtility.TrTextContent("* Shared setting between multiple platforms.");
        }

        public SerializedProperty m_ForceToMono;
        public SerializedProperty m_Normalize;
        public SerializedProperty m_PreloadAudioData;
        public SerializedProperty m_Ambisonic;
        public SerializedProperty m_LoadInBackground;
        public SerializedProperty m_OrigSize;
        public SerializedProperty m_CompSize;
        public SerializedProperty m_DefaultSampleSettings;

        bool m_SelectionContainsTrackerFile;

        // [Serializable]
        // class AudioImporterPlatformSettings
        // {
        //     public BuildTargetGroup platform;
        //     public bool isOverridden;
        //     public AudioImporterSampleSettings settings;
        // }
        
        private static readonly Type AudioImporterPlatformSettingsType = typeof(AssetImporterEditor).Assembly.GetType("UnityEditor.AudioImporterInspector+AudioImporterPlatformSettings");
        private static readonly FieldInfo AudioImporterPlatformSettingsPlatformField = AudioImporterPlatformSettingsType.GetField("platform",         BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo AudioImporterPlatformSettingsIsOverriddenField = AudioImporterPlatformSettingsType.GetField("isOverridden", BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo AudioImporterPlatformSettingsSettingsField = AudioImporterPlatformSettingsType.GetField("settings",         BindingFlags.Instance | BindingFlags.Public);

        // class PlatformSettings : ScriptableObject
        // {
        //     public List<AudioImporterPlatformSettings> sampleSettingOverrides;
        // }
        
        private static readonly Type PlatformSettingsType = typeof(AssetImporterEditor).Assembly.GetType("UnityEditor.AudioImporterInspector+PlatformSettings");
        private static readonly FieldInfo PlatformSettingsSampleSettingOverridesField = PlatformSettingsType.GetField("sampleSettingOverrides", BindingFlags.Instance | BindingFlags.Public);

        protected override Type extraDataType => PlatformSettingsType;
        
        private static Type m_BuildPlatformType = Assembly.GetAssembly(typeof(BuildPipeline)).GetType("UnityEditor.Build.BuildPlatform");
        private static PropertyInfo m_TargetGroupProperty = m_BuildPlatformType.GetProperty("targetGroup", BindingFlags.Public | BindingFlags.Instance);

        class BuildPlatformGroupComparer : IEqualityComparer<object>
        {
#pragma warning disable CS0108, CS0114
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public bool Equals(object a, object z)
#pragma warning restore CS0108, CS0114
            {
                if (a == null || z == null)
                    return false;
                
                if (a.GetType() != z.GetType())
                    return false;
                
                if (a.GetType() != m_BuildPlatformType)
                    return false;
                
                BuildTargetGroup targetGroupA = (BuildTargetGroup)m_TargetGroupProperty.GetValue(a);
                BuildTargetGroup targetGroupZ = (BuildTargetGroup)m_TargetGroupProperty.GetValue(z);
                return targetGroupA == targetGroupZ;
            }

            public int GetHashCode(object platform)
            {
                return (int)(BuildTargetGroup)m_TargetGroupProperty.GetValue(platform);
            }
        }
        
        static readonly BuildPlatformGroupComparer s_BuildPlatformGroupComparer = new();

        private static readonly Type e_BuildPlatformsPluralType = typeof(BuildPipeline).Assembly.GetType("UnityEditor.Build.BuildPlatforms");
        private static readonly FieldInfo s_BuildPlatformsInstanceField = e_BuildPlatformsPluralType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_GetValidPlatformsMethod = e_BuildPlatformsPluralType.GetMethod("GetValidPlatforms" ,types: new Type[0], modifiers: null, binder: null, 
                                                                                                            bindingAttr: BindingFlags.Instance | BindingFlags.Public);
        
        // Don't add duplicate platform groups even if there are multiple platforms in the group
        // Case UUM-399

        private IEnumerable<object> GetValidPlatforms()
        {
            if (s_BuildPlatformsInstanceField == null)
            {
                Debug.LogWarning("is null");
            }
            
            object instance = s_BuildPlatformsInstanceField.GetValue(null);
            IEnumerable platforms = (IEnumerable)s_GetValidPlatformsMethod.Invoke(instance, null);
            HashSet<object> set = new(s_BuildPlatformGroupComparer);
            foreach (object platform in platforms)
                set.Add(platform);
            
            return set;
        }

        static readonly FieldInfo s_NamedBuildTargetField = m_BuildPlatformType.GetField("namedBuildTarget", BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo s_GetBuildTargetGroupNameMethod = typeof(BuildPipeline).GetMethod("GetBuildTargetGroupName", types: new Type[] {typeof(BuildTargetGroup) },
                                                                                                                           modifiers: null, binder: null, bindingAttr: BindingFlags.NonPublic | BindingFlags.Static);

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            object settings = extraData;
            //PlatformSettings settings = extraData as PlatformSettings;
            var audioImporter = targets[targetIndex] as AudioImporter;
            if (settings != null && audioImporter != null)
            {
                // We need to sort them so every extraDataTarget have them ordered correctly and we can use serializedProperties.
                IOrderedEnumerable<object> validPlatforms = GetValidPlatforms().OrderBy(platform => ((NamedBuildTarget)s_NamedBuildTargetField.GetValue(platform)).TargetName);
                
                object list = Activator.CreateInstance(typeof(List<>).MakeGenericType(AudioImporterPlatformSettingsType));
                MethodInfo addMethod = list.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
                PlatformSettingsSampleSettingOverridesField.SetValue(settings, list);
                
                //PlatformSettingsSampleSettingOverridesField.SetValue(settings, new List<object>(validPlatforms.Count()));

                //settings.sampleSettingOverrides = new List<AudioImporterPlatformSettings>(validPlatforms.Count());
                foreach (object platform in validPlatforms)
                {
                    NamedBuildTarget namedBuildTarget = (NamedBuildTarget)s_NamedBuildTargetField.GetValue(platform);
                    BuildTargetGroup buildTargetGroup = namedBuildTarget.ToBuildTargetGroup();
                    string platformName = (string) s_GetBuildTargetGroupNameMethod.Invoke(obj: null, parameters: new object[] {buildTargetGroup}); // override sample settings are per platform group
                    var sample = audioImporter.GetOverrideSampleSettings(platformName);
                    
                    object audioImporterPlatformSettings = Activator.CreateInstance(AudioImporterPlatformSettingsType);
                    AudioImporterPlatformSettingsPlatformField.SetValue(audioImporterPlatformSettings, buildTargetGroup);
                    AudioImporterPlatformSettingsIsOverriddenField.SetValue(audioImporterPlatformSettings, audioImporter.ContainsSampleSettingsOverride(platformName));
                    AudioImporterPlatformSettingsSettingsField.SetValue(audioImporterPlatformSettings, sample);
                    addMethod.Invoke(list, new object[] {audioImporterPlatformSettings});
                    
                    // settings.sampleSettingOverrides.Add(new AudioImporterPlatformSettings()
                    // {
                    //     platform = buildTargetGroup,
                    //     isOverridden = audioImporter.ContainsSampleSettingsOverride(platformName),
                    //     settings = sample
                    // });
                }
            }
        }

        private IEnumerable<AudioImporter> GetAllAudioImporterTargets()
        {
            foreach (Object importer in targets)
            {
                AudioImporter audioImporter = importer as AudioImporter;
                if (audioImporter != null)
                    yield return audioImporter;
            }
        }

        private void SyncSettingsToBackend()
        {
            for (var index = 0; index < targets.Length; index++)
            {
                var audioImporter = targets[index] as AudioImporter;
                object settings = extraDataTargets[index];
                //var settings = extraDataTargets[index] as PlatformSettings;
                if (settings != null && audioImporter != null)
                {
                    object sampleSettingOverrides = PlatformSettingsSampleSettingOverridesField.GetValue(settings);
                    foreach (object setting in (IEnumerable) sampleSettingOverrides)
                    {
                        bool isOverridden = (bool)AudioImporterPlatformSettingsIsOverriddenField.GetValue(setting);
                        BuildTargetGroup platform = (BuildTargetGroup)AudioImporterPlatformSettingsPlatformField.GetValue(setting);
                        if (isOverridden)
                        {
                            AudioImporterSampleSettings settingsField = (AudioImporterSampleSettings) AudioImporterPlatformSettingsSettingsField.GetValue(setting);
                            audioImporter.SetOverrideSampleSettings(platform.ToString(), settingsField);
                        }
                        else if (audioImporter.ContainsSampleSettingsOverride(platform.ToString()))
                        {
                            audioImporter.ClearSampleSettingOverride(platform.ToString());
                        }
                    }
                }
            }
        }
        
        private static readonly MethodInfo s_Internal_ContainsSampleSettingsOverrideMethod = typeof(AudioImporter).GetMethod("Internal_ContainsSampleSettingsOverride", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_Internal_GetOverrideSampleSettingsMethod = typeof(AudioImporter).GetMethod("Internal_GetOverrideSampleSettings", BindingFlags.Instance | BindingFlags.NonPublic);

        public bool CurrentPlatformHasAutoTranslatedCompression()
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                AudioCompressionFormat defaultCompressionFormat = importer.defaultSampleSettings.compressionFormat;
                // Because we only want to query if the importer does not have an override.
                if ((bool)s_Internal_ContainsSampleSettingsOverrideMethod.Invoke(importer, new object[]{targetGroup}) == false)
                {
                    AudioImporterSampleSettings overrideSettings = (AudioImporterSampleSettings)s_Internal_GetOverrideSampleSettingsMethod.Invoke(importer, new object[]{targetGroup});
                    AudioCompressionFormat overrideCompressionFormat = overrideSettings.compressionFormat;

                    // If we dont have an override, but the translated compression format is different,
                    // this means we have audio translate happening.
                    if (defaultCompressionFormat != overrideCompressionFormat)
                        return true;
                }
            }

            return false;
        }

        public bool IsHardwareSound(AudioCompressionFormat format)
        {
            switch (format)
            {
                case AudioCompressionFormat.HEVAG:
                case AudioCompressionFormat.VAG:
                case AudioCompressionFormat.XMA:
                case AudioCompressionFormat.GCADPCM:
                    return true;
                default:
                    return false;
            }
        }

        public bool CurrentSelectionContainsHardwareSounds()
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                AudioImporterSampleSettings overrideSettings = (AudioImporterSampleSettings)s_Internal_GetOverrideSampleSettingsMethod.Invoke(importer, new object[]{targetGroup});
                if (IsHardwareSound(overrideSettings.compressionFormat))
                    return true;
            }

            return false;
        }
        
        private static readonly MethodInfo s_GetPathExtensionMethod = typeof(FileUtil).GetMethod("GetPathExtension", BindingFlags.Static | BindingFlags.NonPublic);

        public override void OnEnable()
        {
            base.OnEnable();

            m_ForceToMono = serializedObject.FindProperty("m_ForceToMono");
            m_Normalize = serializedObject.FindProperty("m_Normalize");
            m_PreloadAudioData = serializedObject.FindProperty("m_PreloadAudioData");
            m_Ambisonic = serializedObject.FindProperty("m_Ambisonic");
            m_LoadInBackground = serializedObject.FindProperty("m_LoadInBackground");
            m_OrigSize = serializedObject.FindProperty("m_PreviewData.m_OrigSize");
            m_CompSize = serializedObject.FindProperty("m_PreviewData.m_CompSize");

            m_DefaultSampleSettings = serializedObject.FindProperty("m_DefaultSettings");

            m_SelectionContainsTrackerFile = false;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                string assetPath = importer.assetPath;
                string ext = ((string)s_GetPathExtensionMethod.Invoke(obj: null, parameters: new object[] { assetPath })).ToLowerInvariant();
                if (ext == "mod" || ext == "it" || ext == "s3m" || ext == "xm")
                {
                    m_SelectionContainsTrackerFile = true;
                    break;
                }
            }
        }
        
        private static readonly PropertyInfo s_origSizeProperty = typeof(AudioImporter).GetProperty("origSize", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo s_compSizeProperty = typeof(AudioImporter).GetProperty("compSize", BindingFlags.Instance | BindingFlags.NonPublic);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            extraDataSerializedObject.Update();

            OnAudioImporterGUI(m_SelectionContainsTrackerFile);

            int origSize = 0, compSize = 0;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                origSize += (int)s_origSizeProperty.GetValue(importer, null);
                compSize += (int)s_compSizeProperty.GetValue(importer, null);
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Original Size: \t" + EditorUtility.FormatBytes(origSize) + "\nImported Size: \t" + EditorUtility.FormatBytes(compSize) + "\n" +
                "Ratio: \t\t" + (100.0f * (float)compSize / (float)origSize).ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + "%", MessageType.Info);

            if (CurrentPlatformHasAutoTranslatedCompression())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("The selection contains different compression formats to the default settings for the current build platform.", MessageType.Info);
            }

            if (CurrentSelectionContainsHardwareSounds())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("The selection contains sounds that are decompressed in hardware. Advanced mixing is not available for these sounds.", MessageType.Info);
            }

            extraDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        private List<AudioCompressionFormat> GetFormatsForPlatform(BuildTargetGroup platform)
        {
            List<AudioCompressionFormat> allowedFormats = new List<AudioCompressionFormat>();

            //WebGL only supports AAC currently.
            if (platform == BuildTargetGroup.WebGL)
            {
                allowedFormats.Add(AudioCompressionFormat.AAC);
                return allowedFormats;
            }

            allowedFormats.Add(AudioCompressionFormat.PCM);

            allowedFormats.Add(AudioCompressionFormat.Vorbis);

            allowedFormats.Add(AudioCompressionFormat.ADPCM);

            if (platform != BuildTargetGroup.Standalone &&
                platform != BuildTargetGroup.WSA &&
                platform != BuildTargetGroup.XboxOne &&
                platform != BuildTargetGroup.Unknown)
            {
                allowedFormats.Add(AudioCompressionFormat.MP3);
            }

            if (platform == BuildTargetGroup.PS4 || platform == BuildTargetGroup.PS5)
            {
                allowedFormats.Add(AudioCompressionFormat.ATRAC9);
            }

            if (platform == BuildTargetGroup.XboxOne || platform == BuildTargetGroup.GameCoreXboxSeries || platform == BuildTargetGroup.GameCoreXboxOne)
            {
                allowedFormats.Add(AudioCompressionFormat.XMA);
            }

            return allowedFormats;
        }

        private bool CompressionFormatHasQuality(AudioCompressionFormat format)
        {
            switch (format)
            {
                case AudioCompressionFormat.Vorbis:
                case AudioCompressionFormat.MP3:
                case AudioCompressionFormat.XMA:
                case AudioCompressionFormat.AAC:
                case AudioCompressionFormat.ATRAC9:
                    return true;
                default:
                    return false;
            }
        }

        private void OnSampleSettingGUI(object platform, SerializedProperty audioImporterSampleSettings, bool selectionContainsTrackerFile)
        {
            //Load Type
            var loadTypeProperty = audioImporterSampleSettings.FindPropertyRelative("loadType");
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.LoadType, loadTypeProperty))
                {
                    EditorGUI.showMixedValue = loadTypeProperty.hasMultipleDifferentValues;
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        var newValue = (AudioClipLoadType)EditorGUILayout.EnumPopup(propertyScope.content, (AudioClipLoadType)loadTypeProperty.intValue);
                        if (changed.changed)
                        {
                            loadTypeProperty.intValue = (int)newValue;
                        }
                    }

                    EditorGUI.showMixedValue = false;
                }
            }

            //Preload Audio Data
            // If the loadtype is streaming on the selected platform, gray out the "Preload Audio Data" option and show the checkbox as unchecked.
            bool disablePreloadAudioDataOption = (AudioClipLoadType)loadTypeProperty.intValue == AudioClipLoadType.Streaming;
            using (new EditorGUI.DisabledScope(disablePreloadAudioDataOption))
            {
                if (disablePreloadAudioDataOption)
                    EditorGUILayout.Toggle("Preload Audio Data", false);
                else
                    EditorGUILayout.PropertyField(m_PreloadAudioData, Style.PreloadAudioData);
            }

            if (!selectionContainsTrackerFile)
            {
                //Compression format
                var compressionFormatProperty = audioImporterSampleSettings.FindPropertyRelative("compressionFormat");
                NamedBuildTarget namedBuildTarget = (NamedBuildTarget) s_NamedBuildTargetField.GetValue(platform);
                var allowedFormats = GetFormatsForPlatform(namedBuildTarget.ToBuildTargetGroup());
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.CompressionFormat, compressionFormatProperty))
                    {
                        EditorGUI.showMixedValue = compressionFormatProperty.hasMultipleDifferentValues;
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            var newValue = (AudioCompressionFormat)EditorGUILayout.IntPopup(
                                propertyScope.content,
                                compressionFormatProperty.intValue,
                                allowedFormats.Select(a => new GUIContent(a.ToString())).ToArray(),
                                allowedFormats.Select(a => (int)a).ToArray());
                            if (changed.changed)
                            {
                                compressionFormatProperty.intValue = (int)newValue;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }
                }

                //Quality
                if (!compressionFormatProperty.hasMultipleDifferentValues && CompressionFormatHasQuality((AudioCompressionFormat)compressionFormatProperty.intValue))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        var property = audioImporterSampleSettings.FindPropertyRelative("quality");
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.Quality, property))
                        {
                            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                            using (var changed = new EditorGUI.ChangeCheckScope())
                            {
                                var newValue = EditorGUILayout.IntSlider(propertyScope.content, (int)Mathf.Clamp(property.floatValue * 100.0f + 0.5f, 1.0f, 100.0f), 1, 100);
                                if (changed.changed)
                                {
                                    property.floatValue = 0.01f * newValue;
                                }
                            }

                            EditorGUI.showMixedValue = false;
                        }
                    }
                }

                if (namedBuildTarget != NamedBuildTarget.WebGL)
                {
                    //Sample rate settings
                    var sampleRateSettingProperty = audioImporterSampleSettings.FindPropertyRelative("sampleRateSetting");
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.SampleRateSetting, sampleRateSettingProperty))
                        {
                            EditorGUI.showMixedValue = sampleRateSettingProperty.hasMultipleDifferentValues;
                            using (var changed = new EditorGUI.ChangeCheckScope())
                            {
                                var newValue = (AudioSampleRateSetting)EditorGUILayout.EnumPopup(propertyScope.content, (AudioSampleRateSetting)sampleRateSettingProperty.intValue);
                                if (changed.changed)
                                {
                                    sampleRateSettingProperty.intValue = (int)newValue;
                                }
                            }

                            EditorGUI.showMixedValue = false;
                        }
                    }

                    //Sample rate override settings
                    if (!sampleRateSettingProperty.hasMultipleDifferentValues && (AudioSampleRateSetting)sampleRateSettingProperty.intValue == AudioSampleRateSetting.OverrideSampleRate)
                    {
                        using (var horizontal = new EditorGUILayout.HorizontalScope())
                        {
                            var property = audioImporterSampleSettings.FindPropertyRelative("sampleRateOverride");
                            using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.SampleRate, property))
                            {
                                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                                using (var changed = new EditorGUI.ChangeCheckScope())
                                {
                                    var newValue = EditorGUILayout.IntPopup(propertyScope.content, property.intValue,
                                        Style.kSampleRateStrings, Style.kSampleRateValues);
                                    if (changed.changed)
                                    {
                                        property.intValue = newValue;
                                    }
                                }

                                EditorGUI.showMixedValue = false;
                            }
                        }
                    }
                }

                //TODO include the settings for things like HEVAG

                EditorGUILayout.LabelField(Style.SharedSettingInformation, EditorStyles.miniLabel);
            }
        }
        
        private static readonly MethodInfo s_BeginPlatformGrouping = typeof(EditorGUILayout).GetMethod("BeginPlatformGrouping", types: new []{ m_BuildPlatformType.MakeArrayType(), typeof(GUIContent) }, modifiers: null, 
                                                                                                       binder: null, bindingAttr: BindingFlags.Static | BindingFlags.NonPublic);
        
        private static readonly ConstructorInfo s_BuildPlatformConstructor = m_BuildPlatformType.GetConstructor(types: new[] { typeof(string), typeof(string), typeof(NamedBuildTarget), typeof(BuildTarget), typeof(bool) });
        private static readonly PropertyInfo s_BuildPlatformTitleProperty = m_BuildPlatformType.GetProperty("title", bindingAttr: BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo s_EndPlatformGrouping = typeof(EditorGUILayout).GetMethod("EndPlatformGrouping", BindingFlags.Static | BindingFlags.NonPublic);
        
        private void OnAudioImporterGUI(bool selectionContainsTrackerFile)
        {
            if (!selectionContainsTrackerFile)
            {
                EditorGUILayout.PropertyField(m_ForceToMono);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!m_ForceToMono.boolValue))
                {
                    EditorGUILayout.PropertyField(m_Normalize);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(m_LoadInBackground);
                EditorGUILayout.PropertyField(m_Ambisonic);
            }
            
            
            // We need to sort them so every extraDataTarget have them ordered correctly and we can use serializedProperties.
            object[] validPlatforms = GetValidPlatforms().OrderBy(platform => ((NamedBuildTarget)s_NamedBuildTargetField.GetValue(platform)).TargetName).ToArray();
            
            Array reflectedValidPlatforms = Array.CreateInstance(m_BuildPlatformType, validPlatforms.Length);
            for (int i = 0; i < validPlatforms.Length; i++)
            {
                reflectedValidPlatforms.SetValue(validPlatforms[i], i);
            }
            
            GUILayout.Space(10);
            int shownSettingsPage = (int)s_BeginPlatformGrouping.Invoke(obj: null, parameters: new object[] { reflectedValidPlatforms, Style.DefaultPlatform });

            if (shownSettingsPage == -1)
            {
                object platform = s_BuildPlatformConstructor.Invoke(new object[] { "", "", NamedBuildTarget.Unknown, BuildTarget.NoTarget, true });
                OnSampleSettingGUI(platform, m_DefaultSampleSettings, selectionContainsTrackerFile);
            }
            else
            {
                object platform = validPlatforms[shownSettingsPage];
                SerializedProperty platformProperty = extraDataSerializedObject.FindProperty($"sampleSettingOverrides.Array.data[{shownSettingsPage}]");
                var isOverriddenProperty = platformProperty.FindPropertyRelative("isOverridden");

                // Define the UI state of the override here.
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    GUIContent title = (GUIContent)s_BuildPlatformTitleProperty.GetValue(platform);
                    
                    var label = EditorGUIUtility.TrTempContent("Override for " + title.text);
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, label, isOverriddenProperty))
                    {
                        EditorGUI.showMixedValue = isOverriddenProperty.hasMultipleDifferentValues;
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            var newValue = EditorGUILayout.ToggleLeft(propertyScope.content, isOverriddenProperty.boolValue);
                            if (changed.changed)
                            {
                                isOverriddenProperty.boolValue = newValue;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }
                }

                using (new EditorGUI.DisabledScope(isOverriddenProperty.hasMultipleDifferentValues || !isOverriddenProperty.boolValue))
                {
                    OnSampleSettingGUI(platform, platformProperty.FindPropertyRelative("settings"), selectionContainsTrackerFile);
                }
            }

            s_EndPlatformGrouping.Invoke(obj: null, parameters: null);
        }
        
        private static readonly Type s_ProjectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        private static readonly MethodInfo s_GetAllProjectBrowsers = s_ProjectBrowserType.GetMethod("GetAllProjectBrowsers", BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo s_Repaint = s_ProjectBrowserType.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public);

        protected override void Apply()
        {
            base.Apply();

            SyncSettingsToBackend();
            
            // This is necessary to enforce redrawing the static preview icons in the project browser, as properties like ForceToMono
            // may have changed the preview completely.
            foreach (object pb in (IEnumerable)s_GetAllProjectBrowsers.Invoke(obj: null, parameters: null))
                s_Repaint.Invoke(pb, parameters: null);
        }

        public void OnSceneDrag(SceneView sceneView, int index)
        {
            Debug.Log("called");
            Event e = Event.current;
            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                e.Use();
            }
            else if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                e.Use();
                
                AudioImporter audioImporter = DragAndDrop.objectReferences[0] as AudioImporter;
                
                if (audioImporter == null)
                    return;
                
                AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
                 
                GameObject closest = HandleUtility.PickGameObject(e.mousePosition, false);
                Transform parent = closest != null ? closest.transform.parent : null;
                
                GameObject createdObj = new(audioClip!.name);
                AudioSource audioSource = createdObj.AddComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.playOnAwake = false;
                createdObj.transform.SetParent(parent);
            }
            
            ApplyRevertGUI();
        }
    }
}*/