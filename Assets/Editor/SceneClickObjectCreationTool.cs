#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SceneClickObjectCreationTool
{
    private const string ToolEnabledKey = "SceneClickObjectCreationTool_Enabled";

    static SceneClickObjectCreationTool()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/Scene Click Create/Enabled")]
    private static void ToggleTool()
    {
        bool enabled = !IsEnabled();
        EditorPrefs.SetBool(ToolEnabledKey, enabled);
    }

    [MenuItem("Tools/Scene Click Create/Enabled", true)]
    private static bool ToggleToolValidate()
    {
        Menu.SetChecked("Tools/Scene Click Create/Enabled", IsEnabled());
        return true;
    }

    // Shortcut: Ctrl+Q (%q). Can be customized by changing the MenuItem path or in Unity's Shortcut Manager (Edit -> Shortcuts)
    [MenuItem("Tools/Scene Click Create/Reset Selected Transform %q")]
    private static void ResetSelectedTransform()
    {
        if (Selection.transforms.Length == 0)
        {
            return;
        }

        Undo.RecordObjects(Selection.transforms, "Reset Selected Transform");
        foreach (Transform t in Selection.transforms)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }
    }

    [MenuItem("Tools/Scene Click Create/Reset Selected Transform %q", true)]
    private static bool ResetSelectedTransformValidate()
    {
        return Selection.transforms.Length > 0;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!IsEnabled())
        {
            return;
        }

        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 1 || !currentEvent.control)
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition(currentEvent.mousePosition);
        ShowCreationMenu(spawnPosition);

        currentEvent.Use();
    }

    private static bool IsEnabled()
    {
        return EditorPrefs.GetBool(ToolEnabledKey, true);
    }

    private static Vector3 GetSpawnPosition(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 5000f))
        {
            return hitInfo.point;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return ray.origin + ray.direction * 10f;
    }

    private static void ShowCreationMenu(Vector3 spawnPosition)
    {
        GenericMenu menu = new GenericMenu();

        AddMenuItem(menu, "Create Empty", spawnPosition, CreateEmptyObject);
        menu.AddSeparator("");
        AddMenuItem(menu, "3D Object/Cube", spawnPosition, position => CreatePrimitive(PrimitiveType.Cube, position));
        AddMenuItem(menu, "3D Object/Sphere", spawnPosition, position => CreatePrimitive(PrimitiveType.Sphere, position));
        AddMenuItem(menu, "3D Object/Capsule", spawnPosition, position => CreatePrimitive(PrimitiveType.Capsule, position));
        AddMenuItem(menu, "3D Object/Cylinder", spawnPosition, position => CreatePrimitive(PrimitiveType.Cylinder, position));
        AddMenuItem(menu, "3D Object/Plane", spawnPosition, position => CreatePrimitive(PrimitiveType.Plane, position));
        AddMenuItem(menu, "3D Object/Quad", spawnPosition, position => CreatePrimitive(PrimitiveType.Quad, position));
        menu.AddSeparator("");
        AddMenuItem(menu, "Light/Directional Light", spawnPosition, CreateDirectionalLight);
        AddMenuItem(menu, "Light/Point Light", spawnPosition, CreatePointLight);
        AddMenuItem(menu, "Camera", spawnPosition, CreateCamera);

        menu.ShowAsContext();
    }

    private static void AddMenuItem(GenericMenu menu, string label, Vector3 spawnPosition, System.Action<Vector3> createAction)
    {
        menu.AddItem(new GUIContent(label), false, () => createAction(spawnPosition));
    }

    private static void CreateEmptyObject(Vector3 position)
    {
        GameObject gameObject = new GameObject("GameObject");
        FinalizeCreatedObject(gameObject, position);
    }

    private static void CreatePrimitive(PrimitiveType primitiveType, Vector3 position)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
        FinalizeCreatedObject(gameObject, position);
    }

    private static void CreateCamera(Vector3 position)
    {
        GameObject gameObject = new GameObject("Camera");
        gameObject.AddComponent<Camera>();
        FinalizeCreatedObject(gameObject, position);
    }

    private static void CreateDirectionalLight(Vector3 position)
    {
        GameObject gameObject = new GameObject("Directional Light");
        Light light = gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        gameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        FinalizeCreatedObject(gameObject, position);
    }

    private static void CreatePointLight(Vector3 position)
    {
        GameObject gameObject = new GameObject("Point Light");
        Light light = gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        FinalizeCreatedObject(gameObject, position);
    }

    private static void FinalizeCreatedObject(GameObject gameObject, Vector3 position)
    {
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {gameObject.name}");
        gameObject.transform.position = position;

        if (Selection.activeTransform != null)
        {
            Undo.SetTransformParent(gameObject.transform, Selection.activeTransform, "Set Parent");
        }

        Selection.activeGameObject = gameObject;
        EditorGUIUtility.PingObject(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
}
#endif
