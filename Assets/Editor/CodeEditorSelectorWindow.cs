#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class CodeEditorSelectorWindow : EditorWindow
{
    private string currentEditorPath;
    private const string PrefsKey = "kScriptsDefaultApp";

    [MenuItem("Tools/Code Editor Selector")]
    public static void ShowWindow()
    {
        GetWindow<CodeEditorSelectorWindow>("Editor Selector");
    }

    private void OnEnable()
    {
        currentEditorPath = EditorPrefs.GetString(PrefsKey, "Not Set");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Select Default Code Editor", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Current Editor Path:", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(currentEditorPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        if (GUILayout.Button("Browse for Editor (.exe)", GUILayout.Height(30)))
        {
            BrowseForEditor();
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Quick Select:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("VS Code"))
        {
            TrySetQuickEditor("VS Code", new[] {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Programs/Microsoft VS Code/Code.exe"),
                @"C:\Program Files\Microsoft VS Code\Code.exe"
            });
        }
        
        if (GUILayout.Button("Visual Studio"))
        {
             // This is a broad guess, ideally we'd use vswhere
            TrySetQuickEditor("Visual Studio", new[] {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
            });
        }

        if (GUILayout.Button("Rider"))
        {
            // Rider paths often include version numbers, so this is just an example
            BrowseForEditor(); // Better to browse for Rider as it moves around
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        if (GUILayout.Button("Force Refresh Unity Preferences"))
        {
             // Unity might not immediately pick up EditorPrefs changes in the Preferences window UI
             // But it usually works for the next "Open C# Project" call
             EditorUtility.RequestScriptReload();
             Debug.Log("Editor preference updated. You may need to restart Unity if the Preferences window doesn't update immediately.");
        }
    }

    private void BrowseForEditor()
    {
        string path = EditorUtility.OpenFilePanel("Select Editor Executable", "", "exe");
        if (!string.IsNullOrEmpty(path))
        {
            SetEditor(path);
        }
    }

    private void TrySetQuickEditor(string name, string[] commonPaths)
    {
        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                SetEditor(path);
                return;
            }
        }
        
        if (EditorUtility.DisplayDialog("Editor Not Found", $"Could not find {name} at common locations. Would you like to browse manually?", "Yes", "No"))
        {
            BrowseForEditor();
        }
    }

    private void SetEditor(string path)
    {
        EditorPrefs.SetString(PrefsKey, path);
        currentEditorPath = path;
        Debug.Log($"Default code editor set to: {path}");
        
        // Some versions of Unity might need a refresh or use specialized logic for certain editors
        // But kScriptsDefaultApp is the primary one.
    }
}
#endif

