#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ColliderRemovalTool
{
    [MenuItem("Tools/Remove Colliders From Selection")]
    public static void RemoveCollidersFromSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Remove Colliders", "Select at least one object in the hierarchy.", "OK");
            return;
        }

        int removedCount = 0;

        foreach (GameObject selectedObject in selectedObjects)
        {
            removedCount += RemoveCollidersInChildren(selectedObject);
        }

        Debug.Log($"[ColliderRemovalTool] Removed {removedCount} collider component(s) from selected object(s) and children.");
    }

    [MenuItem("Tools/Remove Colliders From Selection", true)]
    public static bool CanRemoveCollidersFromSelection()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static int RemoveCollidersInChildren(GameObject root)
    {
        int removedCount = 0;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(colliders[i]);
            removedCount++;
        }

        Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = colliders2D.Length - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(colliders2D[i]);
            removedCount++;
        }

        return removedCount;
    }
}
#endif
