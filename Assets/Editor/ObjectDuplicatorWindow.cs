#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ObjectDuplicatorWindow : EditorWindow
{
    private GameObject _source;
    private int        _count     = 5;
    private Vector3    _offset    = new Vector3(2f, 0f, 0f);
    private float      _rotMinY    = 0f;
    private float      _rotMaxY    = 360f;
    private float      _scaleMin   = 1f;
    private float      _scaleMax   = 1f;
    private bool       _uniformScale = true;

    [MenuItem("Tools/Object Duplicator")]
    public static void Open() => GetWindow<ObjectDuplicatorWindow>("Object Duplicator");

    private void OnGUI()
    {
        GUILayout.Label("Object Duplicator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _source  = (GameObject)EditorGUILayout.ObjectField("Source Object", _source, typeof(GameObject), true);
        _count   = EditorGUILayout.IntSlider("Count", _count, 1, 500);
        _offset  = EditorGUILayout.Vector3Field("Offset per Copy", _offset);

        EditorGUILayout.Space();
        GUILayout.Label("Random Y Rotation", EditorStyles.boldLabel);
        EditorGUILayout.MinMaxSlider(
            $"Range  [{_rotMinY:F0}°  –  {_rotMaxY:F0}°]",
            ref _rotMinY, ref _rotMaxY, 0f, 360f);
        _rotMinY = EditorGUILayout.FloatField("Min Y", _rotMinY);
        _rotMaxY = EditorGUILayout.FloatField("Max Y", _rotMaxY);
        _rotMinY = Mathf.Clamp(_rotMinY, 0f, _rotMaxY);
        _rotMaxY = Mathf.Clamp(_rotMaxY, _rotMinY, 360f);

        EditorGUILayout.Space();
        GUILayout.Label("Random Scale (multiplier of source)", EditorStyles.boldLabel);
        _uniformScale = EditorGUILayout.Toggle("Uniform", _uniformScale);
        EditorGUILayout.MinMaxSlider(
            $"Range  [{_scaleMin:F2}  –  {_scaleMax:F2}]",
            ref _scaleMin, ref _scaleMax, 0.01f, 5f);
        _scaleMin = EditorGUILayout.FloatField("Min", _scaleMin);
        _scaleMax = EditorGUILayout.FloatField("Max", _scaleMax);
        _scaleMin = Mathf.Clamp(_scaleMin, 0.01f, _scaleMax);
        _scaleMax = Mathf.Clamp(_scaleMax, _scaleMin, 5f);

        EditorGUILayout.Space();

        GUI.enabled = _source != null && _count > 0;
        if (GUILayout.Button("Duplicate", GUILayout.Height(32)))
            Duplicate();
        GUI.enabled = true;

        if (_source == null)
            EditorGUILayout.HelpBox("Assign a Source Object.", MessageType.Warning);
    }

    private void Duplicate()
    {
        Transform parent     = _source.transform.parent;
        Vector3   basePos    = _source.transform.position;
        Quaternion baseRot   = _source.transform.rotation;

        Undo.SetCurrentGroupName("Duplicate Objects");
        int group = Undo.GetCurrentGroup();

        for (int i = 1; i <= _count; i++)
        {
            GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(
                PrefabUtility.IsPartOfAnyPrefab(_source) ? PrefabUtility.GetCorrespondingObjectFromSource(_source) : _source,
                parent);

            if (copy == null)
                copy = Instantiate(_source, parent);

            copy.name = $"{_source.name} ({i})";

            float  randY = Random.Range(_rotMinY, _rotMaxY);
            copy.transform.position = basePos + _offset * i;
            copy.transform.rotation = baseRot * Quaternion.Euler(0f, randY, 0f);

            Vector3 baseScale = _source.transform.localScale;
            if (_uniformScale)
            {
                float s = Random.Range(_scaleMin, _scaleMax);
                copy.transform.localScale = baseScale * s;
            }
            else
            {
                copy.transform.localScale = new Vector3(
                    baseScale.x * Random.Range(_scaleMin, _scaleMax),
                    baseScale.y * Random.Range(_scaleMin, _scaleMax),
                    baseScale.z * Random.Range(_scaleMin, _scaleMax));
            }

            Undo.RegisterCreatedObjectUndo(copy, "Duplicate");
        }

        Undo.CollapseUndoOperations(group);
    }
}
#endif
