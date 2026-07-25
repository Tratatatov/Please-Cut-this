#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Tools.Prefabs
{
    public class PrefabChildPlacerWindow : EditorWindow
    {
        [SerializeField]
        private GameObject prefabToAdd;

        [SerializeField]
        private List<Transform> parentObjects = new List<Transform>();

        [MenuItem("Tools/Prefab Child Placer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabChildPlacerWindow>("Prefab Child Placer");
            window.minSize = new Vector2(360f, 160f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorStyles.label.wordWrap = true;

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            GUILayout.Label("Prefab Child Placer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            prefabToAdd = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToAdd, typeof(GameObject), false);

            EditorGUILayout.Space();
            GUILayout.Label("Parent Objects", EditorStyles.boldLabel);

            for (int i = 0; i < parentObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                parentObjects[i] = (Transform)EditorGUILayout.ObjectField($"Parent {i + 1}", parentObjects[i], typeof(Transform), true);
                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    parentObjects.RemoveAt(i);
                    GUI.FocusControl(null);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Parent"))
            {
                parentObjects.Add(Selection.activeTransform);
            }

            if (GUILayout.Button("Use Selection"))
            {
                CollectSelectedParents();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(prefabToAdd == null || parentObjects.Count == 0))
            {
                if (GUILayout.Button("Add Prefab As Children", GUILayout.Height(32f)))
                {
                    AddPrefabAsChild();
                }
            }

            EditorGUILayout.HelpBox(
                "Adds the selected prefab under each chosen parent and resets local position to zero.",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void AddPrefabAsChild()
        {
            if (prefabToAdd == null || parentObjects.Count == 0)
            {
                return;
            }

            foreach (Transform parent in parentObjects)
            {
                if (parent == null)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToAdd, parent);
                if (instance == null)
                {
                    instance = Instantiate(prefabToAdd, parent);
                }

                Undo.RegisterCreatedObjectUndo(instance, "Add Prefab As Child");
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                Selection.activeGameObject = instance;
                EditorGUIUtility.PingObject(instance);
            }
        }

        private void CollectSelectedParents()
        {
            parentObjects.Clear();

            Transform[] selection = Selection.transforms;
            if (selection != null && selection.Length > 0)
            {
                parentObjects.AddRange(selection);
                return;
            }

            if (Selection.activeTransform != null)
            {
                parentObjects.Add(Selection.activeTransform);
            }
        }
    }
}
#endif
