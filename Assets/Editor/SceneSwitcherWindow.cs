#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;
using Unity.CodeEditor;

[InitializeOnLoad]
public class SceneSwitcherWindow : EditorWindow
{
    static SceneSwitcherWindow()
    {
        EditorApplication.delayCall += () =>
        {
            ApplyPlayModeStartSceneFromPrefs();
        };
    }

    // Ключи для EditorPrefs
    private const string AddedScenePathsKey = "SceneSwitcher_AddedScenePaths";
    private const string AddedSceneNamesKey = "SceneSwitcher_AddedSceneNames";
    private const string PlayModeStartEnabledKey = "SceneSwitcher_PlayModeStartEnabled";
    private const string PlayModeStartScenePathKey = "SceneSwitcher_PlayModeStartScenePath";

    // Список добавленных сцен
    private List<string> addedScenePaths = new List<string>();
    private List<string> addedSceneNames = new List<string>();

    private bool playModeStartEnabled;
    private string playModeStartScenePath;
    private SceneAsset playModeStartSceneAsset;

    // Сцены из Build Settings (по умолчанию)
    private List<string> buildScenePaths = new List<string>();
    private List<string> buildSceneNames = new List<string>();

    // Все сцены в проекте
    private List<string> allScenePaths = new List<string>();
    private List<string> allSceneNames = new List<string>();

    // Поиск
    private string searchFilter = "";
    private Vector2 scrollPosition;
    private Vector2 mainScrollPosition;

    // Текущая вкладка
    private int selectedTab = 0;

    [MenuItem("Tools/Scene Switcher")]
    static void Init()
    {
        SceneSwitcherWindow window = (SceneSwitcherWindow)EditorWindow.GetWindow(typeof(SceneSwitcherWindow));
        window.titleContent = new GUIContent("Scene Switcher");
        window.Show();
    }

    void OnEnable()
    {
        LoadAddedScenes();
        LoadStartupSceneSettings();
        RefreshBuildScenes();
        RefreshAllScenes();
        ApplyPlayModeStartScene();
    }

    void OnDisable()
    {
        SaveAddedScenes();
    }

    void SaveAddedScenes()
    {
        // Сохраняем пути сцен
        string pathsJson = string.Join("|", addedScenePaths.ToArray());
        EditorPrefs.SetString(AddedScenePathsKey, pathsJson);

        // Сохраняем имена сцен
        string namesJson = string.Join("|", addedSceneNames.ToArray());
        EditorPrefs.SetString(AddedSceneNamesKey, namesJson);
    }

    void LoadAddedScenes()
    {
        addedScenePaths.Clear();
        addedSceneNames.Clear();

        // Загружаем пути сцен
        string savedPaths = EditorPrefs.GetString(AddedScenePathsKey, "");
        if (!string.IsNullOrEmpty(savedPaths))
        {
            string[] paths = savedPaths.Split('|');
            addedScenePaths.AddRange(paths);
        }

        // Загружаем имена сцен
        string savedNames = EditorPrefs.GetString(AddedSceneNamesKey, "");
        if (!string.IsNullOrEmpty(savedNames))
        {
            string[] names = savedNames.Split('|');
            addedSceneNames.AddRange(names);
        }
    }

    void RefreshAllScenes()
    {
        allScenePaths.Clear();
        allSceneNames.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allScenePaths.Add(path);
            allSceneNames.Add(Path.GetFileNameWithoutExtension(path));
        }

        // Сортируем по имени
        var sortedPairs = allSceneNames.Select((name, index) => new { name, index })
            .OrderBy(x => x.name)
            .ToList();

        allSceneNames = sortedPairs.Select(x => x.name).ToList();
        allScenePaths = sortedPairs.Select(x => allScenePaths[x.index]).ToList();
    }

    void RefreshBuildScenes()
    {
        buildScenePaths.Clear();
        buildSceneNames.Clear();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene == null || string.IsNullOrEmpty(scene.path))
            {
                continue;
            }

            buildScenePaths.Add(scene.path);
            buildSceneNames.Add(Path.GetFileNameWithoutExtension(scene.path));
        }
    }

    bool IsBuildScene(string path)
    {
        return buildScenePaths.Contains(path);
    }

    void GetMyScenes(out List<string> scenePaths, out List<string> sceneNames)
    {
        scenePaths = new List<string>();
        sceneNames = new List<string>();

        // Сначала сцены из билда
        for (int i = 0; i < buildScenePaths.Count; i++)
        {
            scenePaths.Add(buildScenePaths[i]);
            sceneNames.Add(buildSceneNames[i]);
        }

        // Затем дополнительные сцены пользователя
        for (int i = 0; i < addedScenePaths.Count; i++)
        {
            string path = addedScenePaths[i];
            if (scenePaths.Contains(path))
            {
                continue;
            }

            scenePaths.Add(path);
            sceneNames.Add(addedSceneNames[i]);
        }
    }

    void OnGUI()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

        GUILayout.Label("Scene Switcher", EditorStyles.boldLabel);

        // === Вкладки ===
        selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "My Scenes", "All Scenes" });

        GUILayout.Space(10);

        if (selectedTab == 0)
        {
            DrawMyScenesTab();
        }
        else
        {
            DrawAllScenesTab();
        }

        GUILayout.Space(20);
        GUILayout.Label("Settings & Tools", EditorStyles.boldLabel);

        DrawStartupSceneSection();
        DrawAntigravitySection();
        DrawIDESelectionSection();

        EditorGUILayout.EndScrollView();
    }

    void DrawMyScenesTab()
    {
        GUILayout.Label("My Scenes:", EditorStyles.boldLabel);

        GetMyScenes(out List<string> myScenePaths, out List<string> mySceneNames);

        if (mySceneNames.Count == 0)
        {
            GUILayout.Label("No scenes in Build Settings and no additional scenes added.", EditorStyles.helpBox);
        }
        else
        {
            for (int i = 0; i < mySceneNames.Count; i++)
            {
                string scenePath = myScenePaths[i];
                bool isBuildScene = IsBuildScene(scenePath);

                GUILayout.BeginHorizontal();

                // Кнопка для загрузки сцены
                if (GUILayout.Button(mySceneNames[i]))
                {
                    LoadScene(scenePath);
                }

                if (isBuildScene)
                {
                    GUILayout.Label("Build", EditorStyles.miniLabel, GUILayout.Width(35));
                }
                else
                {
                    // Кнопка для удаления только доп. сцен
                    GUIStyle removeStyle = new GUIStyle(EditorStyles.miniButton);
                    removeStyle.normal.textColor = Color.red;
                    if (GUILayout.Button("X", removeStyle, GUILayout.Width(25)))
                    {
                        int indexToRemove = addedScenePaths.IndexOf(scenePath);
                        if (indexToRemove >= 0)
                        {
                            addedScenePaths.RemoveAt(indexToRemove);
                            addedSceneNames.RemoveAt(indexToRemove);
                            SaveAddedScenes();
                        }

                        GUI.FocusControl(null);
                        break;
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);

        // Кнопка очистки всех сцен
        if (addedSceneNames.Count > 0)
        {
            if (GUILayout.Button("Clear All", EditorStyles.miniButtonRight))
            {
                if (EditorUtility.DisplayDialog("Clear Added Scenes", "Are you sure you want to remove all additional scenes?", "Yes", "No"))
                {
                    addedScenePaths.Clear();
                    addedSceneNames.Clear();
                    SaveAddedScenes();
                }
            }
        }
    }

    void DrawAllScenesTab()
    {
        GUILayout.Label("All Scenes in Project:", EditorStyles.boldLabel);

        // Поле поиска
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(60));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            RefreshBuildScenes();
            RefreshAllScenes();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Список всех сцен с фильтром

        for (int i = 0; i < allSceneNames.Count; i++)
        {
            string sceneName = allSceneNames[i];
            string scenePath = allScenePaths[i];

            // Применяем фильтр поиска
            if (!string.IsNullOrEmpty(searchFilter) &&
                !sceneName.ToLower().Contains(searchFilter.ToLower()))
            {
                continue;
            }

            bool isBuildScene = IsBuildScene(scenePath);
            bool isAddedExtra = addedScenePaths.Contains(scenePath);
            bool isInMyScenes = isBuildScene || isAddedExtra;

            GUILayout.BeginHorizontal();

            // Показываем название сцены
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            if (isInMyScenes)
            {
                labelStyle.normal.textColor = Color.green;
                string badge = isBuildScene ? "[Build]" : "[Added]";
                GUILayout.Label($"✓ {sceneName} {badge}", labelStyle);
            }
            else
            {
                GUILayout.Label(sceneName, labelStyle);
            }

            GUILayout.FlexibleSpace();

            // Кнопка добавления/удаления
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            if (isBuildScene)
            {
                GUI.enabled = false;
                GUILayout.Button("Build", buttonStyle, GUILayout.Width(70));
                GUI.enabled = true;
            }
            else if (isAddedExtra)
            {
                buttonStyle.normal.textColor = Color.red;
                if (GUILayout.Button("Remove", buttonStyle, GUILayout.Width(70)))
                {
                    int indexToRemove = addedScenePaths.IndexOf(scenePath);
                    if (indexToRemove >= 0)
                    {
                        addedScenePaths.RemoveAt(indexToRemove);
                        addedSceneNames.RemoveAt(indexToRemove);
                        SaveAddedScenes();
                    }
                    GUI.FocusControl(null);
                    break;
                }
            }
            else
            {
                if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(60)))
                {
                    addedScenePaths.Add(scenePath);
                    addedSceneNames.Add(sceneName);
                    SaveAddedScenes();
                    GUI.FocusControl(null);
                    break;
                }
            }

            // Кнопка загрузки сцены
            if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(55)))
            {
                LoadScene(scenePath);
            }

            GUILayout.EndHorizontal();
        }
    }

    void LoadScene(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // Проверяем, существует ли сцена
        if (!File.Exists(Path.Combine(Application.dataPath, "..", path)) && !File.Exists(path))
        {
            RefreshAllScenes();
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }

    private void DrawStartupSceneSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();
        playModeStartEnabled = EditorGUILayout.ToggleLeft("Forced Startup Scene", playModeStartEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            SaveStartupSceneSettings();
            ApplyPlayModeStartScene();
        }

        if (playModeStartEnabled)
        {
            EditorGUI.BeginChangeCheck();
            playModeStartSceneAsset = (SceneAsset)EditorGUILayout.ObjectField("Startup Scene", playModeStartSceneAsset, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                playModeStartScenePath = AssetDatabase.GetAssetPath(playModeStartSceneAsset);
                SaveStartupSceneSettings();
                ApplyPlayModeStartScene();
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private void LoadStartupSceneSettings()
    {
        playModeStartEnabled = EditorPrefs.GetBool(PlayModeStartEnabledKey, false);
        playModeStartScenePath = EditorPrefs.GetString(PlayModeStartScenePathKey, "");
        if (!string.IsNullOrEmpty(playModeStartScenePath))
        {
            playModeStartSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(playModeStartScenePath);
        }
    }

    private void SaveStartupSceneSettings()
    {
        EditorPrefs.SetBool(PlayModeStartEnabledKey, playModeStartEnabled);
        EditorPrefs.SetString(PlayModeStartScenePathKey, playModeStartScenePath);
    }

    private void ApplyPlayModeStartScene()
    {
        if (playModeStartEnabled && playModeStartSceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = playModeStartSceneAsset;
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }

    private static void ApplyPlayModeStartSceneFromPrefs()
    {
        bool enabled = EditorPrefs.GetBool(PlayModeStartEnabledKey, false);
        string path = EditorPrefs.GetString(PlayModeStartScenePathKey, "");

        if (enabled && !string.IsNullOrEmpty(path))
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorSceneManager.playModeStartScene = asset;
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }

    private void DrawAntigravitySection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Antigravity IDE", EditorStyles.boldLabel);

        bool isInstalled = IsAntigravityInstalled();

        EditorGUI.BeginDisabledGroup(isInstalled);
        if (GUILayout.Button(isInstalled ? "Antigravity IDE Installed" : "Install / Update Antigravity IDE", GUILayout.Height(30)))
        {
            const string packageUrl = "https://github.com/billythekidz/UnityAntigravityIDE.git";
            UnityEditor.PackageManager.Client.Add(packageUrl);
            Debug.Log($"[Antigravity] Installation of {packageUrl} requested.");
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private void DrawIDESelectionSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("External Script Editor", EditorStyles.boldLabel);

        var installations = CodeEditor.Editor.GetFoundScriptEditorPaths();
        string currentEditorPath = CodeEditor.CurrentEditorInstallation;

        if (installations == null || installations.Count == 0)
        {
            EditorGUILayout.HelpBox("No external editors found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            return;
        }

        string[] editorPaths = installations.Keys.ToArray();
        string[] editorNames = installations.Values.ToArray();
        int currentSelection = -1;

        for (int i = 0; i < editorPaths.Length; i++)
        {
            if (editorPaths[i] == currentEditorPath)
            {
                currentSelection = i;
                break;
            }
        }

        EditorGUI.BeginChangeCheck();
        int newSelection = EditorGUILayout.Popup("Select IDE", currentSelection, editorNames);
        if (EditorGUI.EndChangeCheck() && newSelection >= 0)
        {
            CodeEditor.Editor.SetCodeEditor(editorPaths[newSelection]);
            Debug.Log($"[IDE] External script editor set to: {editorNames[newSelection]}");
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private bool IsAntigravityInstalled()
    {
        string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                string content = File.ReadAllText(manifestPath);
                return content.Contains("com.antigravity.ide");
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
}
#endif
