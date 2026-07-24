#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Tools.Lighting
{
    public class LightingPresetManagerWindow : EditorWindow
    {
        private string presetFolder = "Assets/LightingPresets";
        private Vector2 scrollPosition;

        // Preset Parameter Structures
        private struct PresetConfig
        {
            public string name;
            public float resolution;
            public int padding;
            public int maxSize;
            public int directSamples;
            public int indirectSamples;
            public int environmentSamples;
            public int bounces;
            public bool enableAO;
            public float aoMaxDist;
            public float aoIndirect;
            public float aoDirect;
            public bool enableBakedGI;
            public bool enableRealtimeGI;
            public string description;
        }

        private readonly PresetConfig testPreset = new PresetConfig
        {
            name = "Test_Draft_Preset",
            description = "Fast bakes for rapid testing. Lower sample counts, minimal bounces, and low lightmap resolution.",
            resolution = 10f,
            padding = 2,
            maxSize = 512,
            directSamples = 16,
            indirectSamples = 128,
            environmentSamples = 128,
            bounces = 1,
            enableAO = false,
            aoMaxDist = 1.0f,
            aoIndirect = 1.0f,
            aoDirect = 1.0f,
            enableBakedGI = true,
            enableRealtimeGI = false
        };

        private readonly PresetConfig mediumPreset = new PresetConfig
        {
            name = "Medium_Quality_Preset",
            description = "Standard balance between baking speed and lighting fidelity. Suitable for daily production development.",
            resolution = 20f,
            padding = 2,
            maxSize = 1024,
            directSamples = 32,
            indirectSamples = 256,
            environmentSamples = 256,
            bounces = 2,
            enableAO = true,
            aoMaxDist = 2.5f,
            aoIndirect = 1.9f,
            aoDirect = 1.16f,
            enableBakedGI = true,
            enableRealtimeGI = false
        };

        private readonly PresetConfig highPreset = new PresetConfig
        {
            name = "High_Quality_Preset",
            description = "High-fidelity settings for production-grade final bakes. High resolution, high sample counts, and maximum bouncing details.",
            resolution = 40f,
            padding = 4,
            maxSize = 2048,
            directSamples = 64,
            indirectSamples = 512,
            environmentSamples = 512,
            bounces = 3,
            enableAO = true,
            aoMaxDist = 3.0f,
            aoIndirect = 1.9f,
            aoDirect = 1.16f,
            enableBakedGI = true,
            enableRealtimeGI = false
        };

        [MenuItem("Tools/Lighting Preset Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<LightingPresetManagerWindow>("Lighting Presets");
            window.minSize = new Vector2(400f, 600f);
            window.Show();
        }

        private void OnGUI()
        {
            // Custom UI Styles
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            GUIStyle sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 5, 5)
            };

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 5, 10)
            };

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 15, 15, 15) });

            // Title Banner
            EditorGUILayout.LabelField("Lighting Preset Manager", headerStyle);
            EditorGUILayout.LabelField("Easily generate and apply lighting settings presets for test, medium, and high quality bakes.", subHeaderStyle);
            GUILayout.Space(10);

            // Active Scene Lighting Info
            DrawActiveSceneSection(boxStyle, sectionTitleStyle);

            // Settings & Path Configuration
            DrawPathConfigurationSection(boxStyle, sectionTitleStyle);

            // Preset Columns/Blocks
            DrawPresetSection("Test / Draft Preset", testPreset, boxStyle, sectionTitleStyle, new Color(0.9f, 0.9f, 0.9f));
            DrawPresetSection("Medium Quality Preset", mediumPreset, boxStyle, sectionTitleStyle, new Color(0.85f, 0.95f, 0.85f));
            DrawPresetSection("High Quality Preset", highPreset, boxStyle, sectionTitleStyle, new Color(0.85f, 0.9f, 0.95f));

            // Global Actions
            DrawGlobalActions();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveSceneSection(GUIStyle boxStyle, GUIStyle titleStyle)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Active Scene Setup", titleStyle);

            LightingSettings activeSettings = Lightmapping.lightingSettings;

            if (activeSettings != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Settings Asset:", GUILayout.Width(150));
                EditorGUILayout.ObjectField(activeSettings, typeof(LightingSettings), false);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"• Lightmapper: {activeSettings.lightmapper}");
                EditorGUILayout.LabelField($"• Resolution: {activeSettings.lightmapResolution} texels/unit");
                EditorGUILayout.LabelField($"• Max Bounces: {activeSettings.maxBounces}");
                
                // Read serialized details safely
                SerializedObject so = new SerializedObject(activeSettings);
                int maxSize = GetSerializedPropertyInt(so, "m_LightmapMaxSize", 1024);
                bool aoEnabled = GetSerializedPropertyBool(so, "m_AO", false);
                EditorGUILayout.LabelField($"• Max Size: {maxSize}");
                EditorGUILayout.LabelField($"• Ambient Occlusion: {(aoEnabled ? "Enabled" : "Disabled")}");

                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping Active Asset", GUILayout.Height(24)))
                {
                    EditorGUIUtility.PingObject(activeSettings);
                }

                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Reset Scene Settings", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Reset Lighting Settings", "Are you sure you want to clear the active lighting settings for this scene? This will reset them to Unity defaults.", "Yes", "No"))
                    {
                        Lightmapping.lightingSettings = null;
                        Debug.Log("[LightingPresetManager] Reset active scene lighting settings to default.");
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No active LightingSettings asset is assigned to this scene. Scene is using Unity's default auto-baking parameters.", MessageType.Warning);
                if (GUILayout.Button("Create & Assign New Settings", GUILayout.Height(26)))
                {
                    LightingSettings newSettings = new LightingSettings();
                    newSettings.name = "SceneDefaultSettings";
                    Lightmapping.lightingSettings = newSettings;
                    Debug.Log("[LightingPresetManager] Created and assigned a default LightingSettings asset to the scene.");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPathConfigurationSection(GUIStyle boxStyle, GUIStyle titleStyle)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Preset Path Configuration", titleStyle);

            EditorGUILayout.BeginHorizontal();
            presetFolder = EditorGUILayout.TextField("Export Folder", presetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string absolutePath = EditorUtility.OpenFolderPanel("Select Export Directory", presetFolder, "");
                if (!string.IsNullOrEmpty(absolutePath))
                {
                    if (absolutePath.Contains(Application.dataPath))
                    {
                        presetFolder = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Directory", "Please select a directory inside your Assets folder.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPresetSection(string title, PresetConfig config, GUIStyle boxStyle, GUIStyle titleStyle, Color colorTint)
        {
            var origColor = GUI.backgroundColor;
            GUI.backgroundColor = colorTint;
            EditorGUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = origColor;

            EditorGUILayout.LabelField(title, titleStyle);
            EditorGUILayout.LabelField(config.description, EditorStyles.miniLabel);
            GUILayout.Space(5);

            // Display key parameters
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            EditorGUILayout.LabelField($"• Resolution: {config.resolution} tx/unit");
            EditorGUILayout.LabelField($"• Max Size: {config.maxSize}px");
            EditorGUILayout.LabelField($"• Bounces: {config.bounces}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"• Direct/Ind. Samples: {config.directSamples} / {config.indirectSamples}");
            EditorGUILayout.LabelField($"• Env Samples: {config.environmentSamples}");
            EditorGUILayout.LabelField($"• Ambient Occlusion: {(config.enableAO ? "On" : "Off")}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Asset", GUILayout.Height(24)))
            {
                CreatePresetAsset(config);
            }

            if (GUILayout.Button("Apply To Scene", GUILayout.Height(24)))
            {
                ApplyPresetToScene(config);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawGlobalActions()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate All 3 Presets", GUILayout.Height(36)))
            {
                CreatePresetAsset(testPreset);
                CreatePresetAsset(mediumPreset);
                CreatePresetAsset(highPreset);
                EditorUtility.DisplayDialog("Bake Presets Created", $"All presets successfully generated in {presetFolder}.", "OK");
            }

            if (GUILayout.Button("Open Lighting Window", GUILayout.Height(36)))
            {
                EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void CreatePresetAsset(PresetConfig config)
        {
            // Ensure target directory exists
            if (!Directory.Exists(presetFolder))
            {
                Directory.CreateDirectory(presetFolder);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(presetFolder, $"{config.name}.lighting");

            // Instantiate and configure
            LightingSettings settings = new LightingSettings();
            settings.name = config.name;
            ConfigureSettings(settings, config);

            // Save Asset
            AssetDatabase.CreateAsset(settings, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LightingPresetManager] Created preset asset at: {fullPath}");
        }

        private void ApplyPresetToScene(PresetConfig config)
        {
            LightingSettings settings = Lightmapping.lightingSettings;
            if (settings == null)
            {
                settings = new LightingSettings();
                settings.name = "Scene_" + config.name;
                Lightmapping.lightingSettings = settings;
            }

            Undo.RecordObject(settings, "Apply Lighting Preset");
            ConfigureSettings(settings, config);
            
            // Re-apply to force Unity to recognize the changes
            Lightmapping.lightingSettings = settings;
            
            Debug.Log($"[LightingPresetManager] Successfully applied preset '{config.name}' to the active scene.");
            EditorUtility.DisplayDialog("Preset Applied", $"Lighting Preset '{config.name}' has been successfully configured and applied to the current scene.", "OK");
        }

        private void ConfigureSettings(LightingSettings settings, PresetConfig config)
        {
            // Configure C# public properties directly
            settings.lightmapResolution = config.resolution;
            settings.lightmapPadding = config.padding;
            settings.maxBounces = config.bounces;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.filteringMode = LightingSettings.FilterMode.Auto;

            // Configure via SerializedObject for maximum compatibility with all versions
            SerializedObject so = new SerializedObject(settings);

            SetSerializedProperty(so, "m_LightmapMaxSize", config.maxSize);
            SetSerializedProperty(so, "m_DirectSampleCount", config.directSamples);
            SetSerializedProperty(so, "m_IndirectSampleCount", config.indirectSamples);
            SetSerializedProperty(so, "m_EnvironmentSampleCount", config.environmentSamples);
            
            SetSerializedProperty(so, "m_AO", config.enableAO);
            SetSerializedProperty(so, "m_AOMaxDistance", config.aoMaxDist);
            SetSerializedProperty(so, "m_CompAOExponent", config.aoIndirect);
            SetSerializedProperty(so, "m_CompAOExponentDirect", config.aoDirect);

            SetSerializedProperty(so, "m_EnableBakedLightmaps", config.enableBakedGI);
            SetSerializedProperty(so, "m_EnableRealtimeLightmaps", config.enableRealtimeGI);

            so.ApplyModifiedProperties();
        }

        // SerializedProperty Helpers
        private static void SetSerializedProperty(SerializedObject so, string propertyName, object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                if (value is bool boolVal)
                    prop.boolValue = boolVal;
                else if (value is int intVal)
                    prop.intValue = intVal;
                else if (value is float floatVal)
                    prop.floatValue = floatVal;
                else if (value is string stringVal)
                    prop.stringValue = stringVal;
            }
        }

        private static int GetSerializedPropertyInt(SerializedObject so, string propertyName, int defaultValue)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            return prop != null ? prop.intValue : defaultValue;
        }

        private static bool GetSerializedPropertyBool(SerializedObject so, string propertyName, bool defaultValue)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            return prop != null ? prop.boolValue : defaultValue;
        }
    }
}
#endif
