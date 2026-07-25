#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tools.Debugging
{
    [InitializeOnLoad]
    public class PhysicsDebuggerToggleWindow : EditorWindow
    {
        private static readonly string[] WindowTypeNames =
        {
            "UnityEditor.PhysicsDebugWindow",
            "UnityEditor.PhysicsDebuggerWindow",
            "UnityEditor.SceneViewPhysicsDebugWindow"
        };

        private const string PhysicsDebuggerMenuPath = "Window/Analysis/Physics Debugger";
        private const string HotkeyEnabledKey = "PhysicsDebuggerToggle_HotkeyEnabled";
        private const string HotkeyKeyCodeKey = "PhysicsDebuggerToggle_HotkeyKeyCode";
        private const string HotkeyCtrlKey = "PhysicsDebuggerToggle_HotkeyCtrl";
        private const string HotkeyAltKey = "PhysicsDebuggerToggle_HotkeyAlt";
        private const string HotkeyShiftKey = "PhysicsDebuggerToggle_HotkeyShift";

        static PhysicsDebuggerToggleWindow()
        {
            SceneView.duringSceneGui += HandleSceneViewHotkey;
        }

        [MenuItem("Tools/Physics Debugger Toggle")]
        public static void ShowWindow()
        {
            var window = GetWindow<PhysicsDebuggerToggleWindow>("Physics Debugger");
            window.minSize = new Vector2(320f, 220f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorStyles.label.wordWrap = true;

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            GUILayout.Label("Physics Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            bool isOpen = TryGetPhysicsDebuggerWindow(out EditorWindow physicsWindow);

            EditorGUILayout.HelpBox(
                isOpen ? "Physics Debugger window is currently open." : "Physics Debugger window is currently closed.",
                isOpen ? MessageType.Info : MessageType.None);

            if (GUILayout.Button(isOpen ? "Close Physics Debugger" : "Open Physics Debugger", GUILayout.Height(32f)))
            {
                TogglePhysicsDebugger(physicsWindow);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Hotkey", EditorStyles.boldLabel);

            bool hotkeyEnabled = EditorGUILayout.ToggleLeft("Enable hotkey in Scene View", GetHotkeyEnabled());
            if (hotkeyEnabled != GetHotkeyEnabled())
            {
                EditorPrefs.SetBool(HotkeyEnabledKey, hotkeyEnabled);
            }

            EditorGUI.BeginDisabledGroup(!hotkeyEnabled);

            KeyCode keyCode = (KeyCode)EditorGUILayout.EnumPopup("Key", GetHotkeyKeyCode());
            if (keyCode != GetHotkeyKeyCode())
            {
                EditorPrefs.SetInt(HotkeyKeyCodeKey, (int)keyCode);
            }

            bool requireCtrl = EditorGUILayout.ToggleLeft("Ctrl", GetRequireCtrl());
            if (requireCtrl != GetRequireCtrl())
            {
                EditorPrefs.SetBool(HotkeyCtrlKey, requireCtrl);
            }

            bool requireAlt = EditorGUILayout.ToggleLeft("Alt", GetRequireAlt());
            if (requireAlt != GetRequireAlt())
            {
                EditorPrefs.SetBool(HotkeyAltKey, requireAlt);
            }

            bool requireShift = EditorGUILayout.ToggleLeft("Shift", GetRequireShift());
            if (requireShift != GetRequireShift())
            {
                EditorPrefs.SetBool(HotkeyShiftKey, requireShift);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.HelpBox(
                $"Current hotkey: {GetHotkeyLabel()}. Works when Scene View is focused.",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private static void HandleSceneViewHotkey(SceneView sceneView)
        {
            if (!GetHotkeyEnabled())
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (!MatchesHotkey(currentEvent))
            {
                return;
            }

            TryGetPhysicsDebuggerWindow(out EditorWindow physicsWindow);
            TogglePhysicsDebugger(physicsWindow);
            currentEvent.Use();
        }

        private static void TogglePhysicsDebugger(EditorWindow currentWindow)
        {
            if (currentWindow != null)
            {
                currentWindow.Close();
                return;
            }

            if (!EditorApplication.ExecuteMenuItem(PhysicsDebuggerMenuPath))
            {
                EditorUtility.DisplayDialog(
                    "Physics Debugger",
                    $"Could not open '{PhysicsDebuggerMenuPath}' in this Unity version.",
                    "OK");
            }
        }

        private static bool TryGetPhysicsDebuggerWindow(out EditorWindow physicsWindow)
        {
            physicsWindow = null;

            foreach (string typeName in WindowTypeNames)
            {
                Type windowType = GetEditorType(typeName);
                if (windowType == null)
                {
                    continue;
                }

                UnityEngine.Object foundWindow = Resources.FindObjectsOfTypeAll(windowType).FirstOrDefault();
                if (foundWindow is EditorWindow editorWindow)
                {
                    physicsWindow = editorWindow;
                    return true;
                }
            }

            return false;
        }

        private static Type GetEditorType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            return editorAssembly.GetType(typeName);
        }

        private static bool MatchesHotkey(Event currentEvent)
        {
            return currentEvent.keyCode == GetHotkeyKeyCode() &&
                   currentEvent.control == GetRequireCtrl() &&
                   currentEvent.alt == GetRequireAlt() &&
                   currentEvent.shift == GetRequireShift();
        }

        private static bool GetHotkeyEnabled()
        {
            return EditorPrefs.GetBool(HotkeyEnabledKey, true);
        }

        private static KeyCode GetHotkeyKeyCode()
        {
            return (KeyCode)EditorPrefs.GetInt(HotkeyKeyCodeKey, (int)KeyCode.F8);
        }

        private static bool GetRequireCtrl()
        {
            return EditorPrefs.GetBool(HotkeyCtrlKey, false);
        }

        private static bool GetRequireAlt()
        {
            return EditorPrefs.GetBool(HotkeyAltKey, false);
        }

        private static bool GetRequireShift()
        {
            return EditorPrefs.GetBool(HotkeyShiftKey, false);
        }

        private static string GetHotkeyLabel()
        {
            string label = string.Empty;

            if (GetRequireCtrl())
            {
                label += "Ctrl + ";
            }

            if (GetRequireAlt())
            {
                label += "Alt + ";
            }

            if (GetRequireShift())
            {
                label += "Shift + ";
            }

            return label + GetHotkeyKeyCode();
        }
    }
}
#endif
