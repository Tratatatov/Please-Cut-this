#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MissingScriptsFinder : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MissingScriptInfo> missingScripts = new List<MissingScriptInfo>();
    private bool searchInScene = true;
    private bool searchInProject = false;
    private bool autoRefresh = false;
    private double lastRefreshTime = 0;
    private const double REFRESH_INTERVAL = 3.0;

    private struct MissingScriptInfo
    {
        public GameObject gameObject;
        public int missingCount;
        public string path;
    }

    [MenuItem("Window/Missing Scripts Finder")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptsFinder>("Missing Scripts");
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawOptions();
        DrawButtons();
        DrawResults();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Missing Scripts Finder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Поиск объектов со сломанными скриптами", MessageType.Info);
        EditorGUILayout.Space();
    }

    private void DrawOptions()
    {
        EditorGUILayout.LabelField("Параметры поиска:", EditorStyles.boldLabel);

        searchInScene = EditorGUILayout.Toggle("Искать в текущей сцене", searchInScene);
        searchInProject = EditorGUILayout.Toggle("Искать во всех префабах проекта", searchInProject);
        autoRefresh = EditorGUILayout.Toggle("Автоматическое обновление", autoRefresh);

        EditorGUILayout.Space();
    }

    private void DrawButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Поиск", GUILayout.Height(30)))
        {
            FindMissingScripts();
        }

        if (GUILayout.Button("Очистить", GUILayout.Height(30)))
        {
            missingScripts.Clear();
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = missingScripts.Count > 0;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button($"Удалить все сломанные скрипты ({missingScripts.Count})", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Подтверждение",
                $"Удалить все сломанные скрипты из {missingScripts.Count} объектов?",
                "Да, удалить все", "Отмена"))
            {
                RemoveAllMissingScripts();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space();
    }

    private void DrawResults()
    {
        EditorGUILayout.LabelField($"Найдено: {missingScripts.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (missingScripts.Count == 0)
        {
            // EditorGUILayout.HelpBox("Объектов со сломанными скриптами не найдено!", MessageType.Success);
        }
        else
        {
            for (int i = 0; i < missingScripts.Count; i++)
            {
                DrawMissingScriptItem(missingScripts[i], i);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMissingScriptItem(MissingScriptInfo info, int index)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{info.gameObject.name}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField($"({info.missingCount})", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Путь: {info.path}", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Выделить", GUILayout.Width(80)))
        {
            SelectGameObject(info.gameObject);
        }

        if (GUILayout.Button("Удалить скрипты", GUILayout.Width(120)))
        {
            if (EditorUtility.DisplayDialog("Подтверждение",
                $"Удалить все сломанные скрипты из {info.gameObject.name}?",
                "Да", "Нет"))
            {
                RemoveMissingScripts(info.gameObject);
                FindMissingScripts();
            }
        }

        if (GUILayout.Button("Пинг", GUILayout.Width(50)))
        {
            EditorGUIUtility.PingObject(info.gameObject);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void FindMissingScripts()
    {
        missingScripts.Clear();

        if (searchInScene)
        {
            FindMissingInScene();
        }

        if (searchInProject)
        {
            FindMissingInProject();
        }

        lastRefreshTime = EditorApplication.timeSinceStartup;
    }

    private void FindMissingInScene()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            int missingCount = CountMissingScripts(obj);
            if (missingCount > 0)
            {
                missingScripts.Add(new MissingScriptInfo
                {
                    gameObject = obj,
                    missingCount = missingCount,
                    path = GetGameObjectPath(obj)
                });
            }
        }
    }

    private void FindMissingInProject()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                int missingCount = CountMissingScripts(prefab);
                if (missingCount > 0)
                {
                    missingScripts.Add(new MissingScriptInfo
                    {
                        gameObject = prefab,
                        missingCount = missingCount,
                        path = path
                    });
                }
            }
        }
    }

    private int CountMissingScripts(GameObject obj)
    {
        int count = 0;
        Component[] components = obj.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
            {
                count++;
            }
        }

        return count;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.gameObject.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void SelectGameObject(GameObject obj)
    {
        Selection.activeGameObject = obj;
        EditorGUIUtility.PingObject(obj);
    }

    private void RemoveAllMissingScripts()
    {
        int totalRemoved = 0;

        foreach (MissingScriptInfo info in missingScripts)
        {
            if (info.gameObject != null)
            {
                totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(info.gameObject);
                EditorUtility.SetDirty(info.gameObject);
            }
        }

        AssetDatabase.SaveAssets();
        FindMissingScripts();
        Debug.Log($"[MissingScriptsFinder] Удалено сломанных скриптов: {totalRemoved}");
    }

    private void RemoveMissingScripts(GameObject obj)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();
    }

    private void OnInspectorUpdate()
    {
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
        {
            FindMissingScripts();
            Repaint();
        }
    }
}
#endif