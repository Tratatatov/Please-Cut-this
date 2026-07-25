#if UNITY_EDITOR
// Поместите этот файл в папку Editor/ вашего Unity-проекта.
// Открыть окно: меню  Tools -> Object Placer

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectPlacerWindow : EditorWindow
{
    // ---------- Данные палитры ----------
    [System.Serializable]
    public class PaletteItem
    {
        public GameObject prefab;

        // Сочетание клавиш для выбора этого слота в Scene View
        public KeyCode hotkey = KeyCode.None;
        public bool ctrl;
        public bool shift;
        public bool alt;

        // Параметры размещения
        public bool alignToNormal = true;          // повернуть Y вверх по нормали поверхности
        public Vector3 rotationOffset = Vector3.zero;
        public float randomYRotation = 0f;          // ±диапазон случайного поворота вокруг Y
        public Vector3 scaleMultiplier = Vector3.one;
    }

    private List<PaletteItem> palette = new List<PaletteItem>();
    private int selectedIndex = -1;

    // ---------- UI-состояние ----------
    private Vector2 scrollPos;
    private bool placementMode = false;
    private Transform parentForPlaced;
    private float iconSize = 56f;

    // Превью для отрисовки в SceneView
    private Vector3 previewPoint;
    private Vector3 previewNormal = Vector3.up;
    private bool hasPreview;

    // Snap-to-grid
    private bool snapEnabled = false;
    private Vector3 gridSize = new Vector3(1f, 1f, 1f);
    private Vector3 gridOrigin = Vector3.zero;
    private bool showGrid = true;
    private bool snapYToHit = true; // если true, Y берётся с поверхности, X/Z снапятся

    // Хоткей переключения Placement Mode
    private KeyCode toggleKey = KeyCode.B;
    private bool toggleCtrl;
    private bool toggleShift;
    private bool toggleAlt;

    // Ручное вращение в Placement Mode (Y-ось / нормаль)
    private float rotateStep = 15f;
    private KeyCode rotateKey = KeyCode.R;          // CW
    private bool rotateCtrl;
    private bool rotateShift;
    private bool rotateAlt;
    private KeyCode rotateKeyCCW = KeyCode.T;       // CCW
    private bool rotateCCWCtrl;
    private bool rotateCCWShift;
    private bool rotateCCWAlt;
    private float manualRotation = 0f;

    // Фиксированный offset по Y при размещении
    private float placementYOffset = 0f;

    // Кеш локальных bounds префабов (для отрисовки куба предпросмотра)
    private readonly Dictionary<GameObject, Bounds> boundsCache = new Dictionary<GameObject, Bounds>();

    // Предпросмотр: куб или полупрозрачная копия модели
    private bool useGhostPreview = false;
    private Color ghostColor = new Color(0.3f, 1f, 0.4f, 1f);
    private float ghostOpacity = 0.45f;
    private Material ghostMaterial;

    private const string PrefsKey = "ObjectPlacerWindow.Palette.v1";
    private const string SnapPrefsKey = "ObjectPlacerWindow.Snap.v1";

    // ---------- Открытие окна ----------
    [MenuItem("Tools/Object Placer")]
    public static void Open()
    {
        var w = GetWindow<ObjectPlacerWindow>("Object Placer");
        w.minSize = new Vector2(280, 360);
    }

    private void OnEnable()
    {
        LoadPalette();
        LoadSnapSettings();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SavePalette();
        SaveSnapSettings();
        DestroyGhostMaterial();
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // ---------- GUI окна ----------
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Object Placer", EditorStyles.boldLabel);

        // Кнопка-переключатель режима размещения
        var prevColor = GUI.backgroundColor;
        GUI.backgroundColor = placementMode ? new Color(0.4f, 1f, 0.4f) : prevColor;
        if (GUILayout.Button(placementMode
                ? "Placement Mode: ON  (click in Scene, Esc to exit)"
                : "Placement Mode: OFF",
                GUILayout.Height(28)))
        {
            placementMode = !placementMode;
            if (placementMode)
                SceneView.lastActiveSceneView?.Focus();
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prevColor;

        // Хоткей для самой кнопки Placement Mode
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Toggle Hotkey", GUILayout.Width(95));
        toggleCtrl = GUILayout.Toggle(toggleCtrl, "Ctrl", "Button", GUILayout.Width(40));
        toggleShift = GUILayout.Toggle(toggleShift, "Shift", "Button", GUILayout.Width(46));
        toggleAlt = GUILayout.Toggle(toggleAlt, "Alt", "Button", GUILayout.Width(40));
        toggleKey = (KeyCode)EditorGUILayout.EnumPopup(toggleKey);
        EditorGUILayout.EndHorizontal();

        // ---------- Preview ----------
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel, GUILayout.Width(95));
        if (GUILayout.Toggle(!useGhostPreview, "Cube", "Button", GUILayout.Width(60))) useGhostPreview = false;
        if (GUILayout.Toggle(useGhostPreview, "Ghost", "Button", GUILayout.Width(60))) useGhostPreview = true;
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!useGhostPreview))
        {
            EditorGUI.BeginChangeCheck();
            ghostColor = EditorGUILayout.ColorField("Ghost Color", ghostColor);
            ghostOpacity = EditorGUILayout.Slider("Opacity", ghostOpacity, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) UpdateGhostMaterial();
        }
        EditorGUILayout.EndVertical();

        // ---------- Ручное вращение ----------
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("↻ CW Hotkey", GUILayout.Width(95));
        rotateCtrl = GUILayout.Toggle(rotateCtrl, "Ctrl", "Button", GUILayout.Width(40));
        rotateShift = GUILayout.Toggle(rotateShift, "Shift", "Button", GUILayout.Width(46));
        rotateAlt = GUILayout.Toggle(rotateAlt, "Alt", "Button", GUILayout.Width(40));
        rotateKey = (KeyCode)EditorGUILayout.EnumPopup(rotateKey);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("↺ CCW Hotkey", GUILayout.Width(95));
        rotateCCWCtrl = GUILayout.Toggle(rotateCCWCtrl, "Ctrl", "Button", GUILayout.Width(40));
        rotateCCWShift = GUILayout.Toggle(rotateCCWShift, "Shift", "Button", GUILayout.Width(46));
        rotateCCWAlt = GUILayout.Toggle(rotateCCWAlt, "Alt", "Button", GUILayout.Width(40));
        rotateKeyCCW = (KeyCode)EditorGUILayout.EnumPopup(rotateKeyCCW);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Step (°)", GUILayout.Width(95));
        rotateStep = EditorGUILayout.FloatField(rotateStep, GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Current: {manualRotation:0.#}°", GUILayout.Width(120));
        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            manualRotation = 0f;
            SceneView.RepaintAll();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Y Offset", GUILayout.Width(95));
        placementYOffset = EditorGUILayout.FloatField(placementYOffset);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        parentForPlaced = (Transform)EditorGUILayout.ObjectField(
            "Parent (optional)", parentForPlaced, typeof(Transform), true);

        // ---------- Snap to Grid ----------
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        snapEnabled = EditorGUILayout.ToggleLeft("Snap to Grid", snapEnabled, EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.FlexibleSpace();
        using (new EditorGUI.DisabledScope(!snapEnabled))
            showGrid = EditorGUILayout.ToggleLeft("Show grid", showGrid, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!snapEnabled))
        {
            gridSize = EditorGUILayout.Vector3Field("Cell Size", gridSize);
            gridOrigin = EditorGUILayout.Vector3Field("Origin", gridOrigin);
            snapYToHit = EditorGUILayout.Toggle(new GUIContent("Y from surface",
                "Если включено, Y берётся с точки попадания, а снапятся только X и Z (удобно для рельефа)."),
                snapYToHit);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Size", GUILayout.Width(30));
        iconSize = GUILayout.HorizontalSlider(iconSize, 32f, 96f, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        DrawPaletteGrid();

        EditorGUILayout.Space();
        if (selectedIndex >= 0 && selectedIndex < palette.Count)
        {
            EditorGUILayout.LabelField($"Slot {selectedIndex + 1} settings", EditorStyles.boldLabel);
            DrawItemSettings(palette[selectedIndex]);
        }
        else
        {
            EditorGUILayout.HelpBox("Click a tile to edit its settings, or drag a prefab onto the [+] tile.", MessageType.None);
        }

        EditorGUILayout.HelpBox(
            "ЛКМ в Scene — поставить. Esc — выйти. Хоткей — выбрать слот и сразу включить режим.\n" +
            "Можно перетаскивать префаб(ы) из Project прямо на плитку [+] или поверх существующей.",
            MessageType.None);
    }

    // ---------- Сетка иконок ----------
    private void DrawPaletteGrid()
    {
        float padding = 4f;
        float cell = iconSize + padding * 2;
        float availableWidth = position.width - 24f;
        int cols = Mathf.Max(1, Mathf.FloorToInt(availableWidth / cell));

        int total = palette.Count + 1; // +1 для плитки добавления
        int rows = Mathf.CeilToInt((float)total / cols);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(cell * 4 + 12));

        for (int r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx < palette.Count) DrawIconTile(idx);
                else if (idx == palette.Count) DrawAddTile();
                else GUILayout.Space(cell);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawIconTile(int idx)
    {
        var item = palette[idx];
        bool isSelected = selectedIndex == idx;

        Rect cell = GUILayoutUtility.GetRect(iconSize, iconSize,
            GUILayout.Width(iconSize), GUILayout.Height(iconSize));

        // Подложка / выделение
        EditorGUI.DrawRect(cell, new Color(0.18f, 0.18f, 0.18f));
        if (isSelected)
        {
            // Рамка-выделение
            EditorGUI.DrawRect(new Rect(cell.x - 2, cell.y - 2, cell.width + 4, 2), new Color(0.3f, 0.7f, 1f));
            EditorGUI.DrawRect(new Rect(cell.x - 2, cell.yMax, cell.width + 4, 2), new Color(0.3f, 0.7f, 1f));
            EditorGUI.DrawRect(new Rect(cell.x - 2, cell.y - 2, 2, cell.height + 4), new Color(0.3f, 0.7f, 1f));
            EditorGUI.DrawRect(new Rect(cell.xMax, cell.y - 2, 2, cell.height + 4), new Color(0.3f, 0.7f, 1f));
        }

        // Иконка
        if (item.prefab != null)
        {
            Texture2D preview = AssetPreview.GetAssetPreview(item.prefab);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(item.prefab);
                if (AssetPreview.IsLoadingAssetPreview(item.prefab.GetInstanceID()))
                    Repaint();
            }
            if (preview != null)
                GUI.DrawTexture(cell, preview, ScaleMode.ScaleToFit);
        }
        else
        {
            var s = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUI.Label(cell, "(empty)", s);
        }

        // Бейдж с хоткеем
        string hk = FormatHotkey(item);
        if (!string.IsNullOrEmpty(hk))
        {
            var badge = new Rect(cell.x, cell.y, cell.width, 14);
            EditorGUI.DrawRect(badge, new Color(0f, 0f, 0f, 0.65f));
            var bs = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                fontSize = 9,
                padding = new RectOffset(4, 0, 0, 0),
            };
            GUI.Label(badge, hk, bs);
        }

        // Номер слота снизу
        var num = new Rect(cell.xMax - 18, cell.yMax - 14, 18, 14);
        var ns = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(1f, 1f, 1f, 0.6f) },
            fontSize = 9,
        };
        GUI.Label(num, (idx + 1).ToString(), ns);

        HandleTileEvents(cell, idx, false);
    }

    private void DrawAddTile()
    {
        Rect cell = GUILayoutUtility.GetRect(iconSize, iconSize,
            GUILayout.Width(iconSize), GUILayout.Height(iconSize));

        EditorGUI.DrawRect(cell, new Color(0.13f, 0.13f, 0.13f));

        var s = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(iconSize * 0.5f),
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };
        GUI.Label(cell, "+", s);

        HandleTileEvents(cell, -1, true);
    }

    private void HandleTileEvents(Rect cell, int idx, bool isAddTile)
    {
        Event e = Event.current;
        if (!cell.Contains(e.mousePosition)) return;

        // Клик ЛКМ — выбрать слот / создать новый
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (isAddTile)
            {
                palette.Add(new PaletteItem());
                selectedIndex = palette.Count - 1;
                SavePalette();
            }
            else
            {
                selectedIndex = idx;
            }
            GUI.FocusControl(null);
            Repaint();
            e.Use();
            return;
        }

        // ПКМ — контекстное меню «удалить» для существующих слотов
        if (!isAddTile && e.type == EventType.ContextClick)
        {
            int captured = idx;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete slot"), false, () =>
            {
                palette.RemoveAt(captured);
                if (selectedIndex >= palette.Count) selectedIndex = palette.Count - 1;
                SavePalette();
                Repaint();
            });
            menu.ShowAsContext();
            e.Use();
            return;
        }

        // Drag-and-drop префабов
        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = isAddTile
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Link;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (isAddTile)
                {
                    foreach (var obj in DragAndDrop.objectReferences)
                        if (obj is GameObject go) palette.Add(new PaletteItem { prefab = go });
                    selectedIndex = palette.Count - 1;
                }
                else
                {
                    var first = DragAndDrop.objectReferences.Length > 0
                        ? DragAndDrop.objectReferences[0] as GameObject
                        : null;
                    if (first != null) palette[idx].prefab = first;
                    selectedIndex = idx;
                }
                SavePalette();
                Repaint();
            }
            e.Use();
        }
    }

    private static string FormatHotkey(PaletteItem item)
    {
        if (item.hotkey == KeyCode.None) return "";
        string s = "";
        if (item.ctrl) s += "C+";
        if (item.shift) s += "S+";
        if (item.alt) s += "A+";
        return s + item.hotkey;
    }

    // ---------- Панель настроек выбранного слота ----------
    private void DrawItemSettings(PaletteItem item)
    {
        EditorGUILayout.BeginVertical("box");

        item.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", item.prefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hotkey", GUILayout.Width(50));
        item.ctrl = GUILayout.Toggle(item.ctrl, "Ctrl", "Button", GUILayout.Width(40));
        item.shift = GUILayout.Toggle(item.shift, "Shift", "Button", GUILayout.Width(46));
        item.alt = GUILayout.Toggle(item.alt, "Alt", "Button", GUILayout.Width(40));
        item.hotkey = (KeyCode)EditorGUILayout.EnumPopup(item.hotkey);
        EditorGUILayout.EndHorizontal();

        item.alignToNormal = EditorGUILayout.Toggle("Align to Normal", item.alignToNormal);
        item.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", item.rotationOffset);
        item.randomYRotation = EditorGUILayout.Slider("Random Y ±°", item.randomYRotation, 0f, 180f);
        item.scaleMultiplier = EditorGUILayout.Vector3Field("Scale Mult", item.scaleMultiplier);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) SavePalette();
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("Delete Slot"))
        {
            palette.RemoveAt(selectedIndex);
            if (selectedIndex >= palette.Count) selectedIndex = palette.Count - 1;
            SavePalette();
        }
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }


    // ---------- Работа в Scene View ----------
    private void OnSceneGUI(SceneView sv)
    {
        Event e = Event.current;

        // 0) Глобальный хоткей: переключить Placement Mode
        if (e.type == EventType.KeyDown && toggleKey != KeyCode.None
            && e.keyCode == toggleKey
            && e.control == toggleCtrl && e.shift == toggleShift && e.alt == toggleAlt)
        {
            placementMode = !placementMode;
            if (!placementMode) hasPreview = false;
            else sv.Focus();
            Repaint();
            sv.Repaint();
            e.Use();
            return;
        }

        // 0.5) Хоткеи вращения превью (только в Placement Mode)
        // CW при взгляде сверху = поворот вокруг -Y, поэтому знак отрицательный.
        if (placementMode && e.type == EventType.KeyDown && rotateKey != KeyCode.None
            && e.keyCode == rotateKey
            && e.control == rotateCtrl && e.shift == rotateShift && e.alt == rotateAlt)
        {
            manualRotation -= rotateStep;
            manualRotation %= 360f;
            Repaint();
            sv.Repaint();
            e.Use();
            return;
        }

        if (placementMode && e.type == EventType.KeyDown && rotateKeyCCW != KeyCode.None
            && e.keyCode == rotateKeyCCW
            && e.control == rotateCCWCtrl && e.shift == rotateCCWShift && e.alt == rotateCCWAlt)
        {
            manualRotation += rotateStep;
            manualRotation %= 360f;
            Repaint();
            sv.Repaint();
            e.Use();
            return;
        }

        // 1) Хоткеи слотов — работают всегда, пока окно открыто
        if (e.type == EventType.KeyDown && palette.Count > 0)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                var it = palette[i];
                if (it.hotkey == KeyCode.None) continue;
                if (e.keyCode != it.hotkey) continue;
                if (e.control != it.ctrl || e.shift != it.shift || e.alt != it.alt) continue;

                selectedIndex = i;
                placementMode = true;
                Repaint();
                sv.Repaint();
                e.Use();
                return;
            }
        }

        // Esc — выход из режима
        if (placementMode && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            placementMode = false;
            hasPreview = false;
            Repaint();
            sv.Repaint();
            e.Use();
            return;
        }

        if (!placementMode) return;
        if (selectedIndex < 0 || selectedIndex >= palette.Count) return;
        var item = palette[selectedIndex];
        if (item.prefab == null) return;

        // Перехватываем стандартный клик-выбор объектов в Scene
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        // 2) Луч под курсором -> поиск ближайшего меша
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        hasPreview = TryRaycastMesh(ray, out previewPoint, out previewNormal, out _);

        // 2.5) Snap-to-grid
        if (hasPreview && snapEnabled)
            previewPoint = SnapToGrid(previewPoint);

        // 2.6) Y offset
        if (hasPreview && placementYOffset != 0f)
            previewPoint.y += placementYOffset;

        // 3) Превью: маркер нормали + куб габаритов префаба + (опционально) сетка
        if (hasPreview)
        {
            float size = HandleUtility.GetHandleSize(previewPoint);
            Handles.color = new Color(0.3f, 1f, 0.4f, 1f);
            Handles.DrawWireDisc(previewPoint, previewNormal, size * 0.15f);
            Handles.DrawLine(previewPoint, previewPoint + previewNormal * size * 0.5f);

            if (useGhostPreview)
                DrawPreviewGhost(item, previewPoint, previewNormal, sv);
            else
                DrawPreviewCube(item, previewPoint, previewNormal);

            // Подпись с текущим накопленным углом
            if (Mathf.Abs(manualRotation) > 0.01f)
            {
                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.6f, 1f, 0.7f) },
                    fontSize = 11,
                };
                float s = HandleUtility.GetHandleSize(previewPoint);
                Handles.Label(previewPoint + previewNormal * s * 0.7f,
                    $"{manualRotation:0.#}°", labelStyle);
            }

            if (snapEnabled && showGrid)
                DrawGridGizmo(previewPoint);
        }

        // 4) Клик — поставить объект
        if (hasPreview && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PlaceObject(item, previewPoint, previewNormal);
            e.Use();
        }

        // Перерисовка для плавного превью
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            sv.Repaint();

        // В ghost-режиме Graphics.DrawMesh держит меш только на 1 кадр —
        // поэтому пока есть активное превью, дергаем Repaint постоянно.
        if (placementMode && useGhostPreview && hasPreview && e.type == EventType.Repaint)
            sv.Repaint();
    }

    // ---------- Снап в сетку ----------
    private Vector3 SnapToGrid(Vector3 p)
    {
        Vector3 q = p - gridOrigin;
        if (gridSize.x > 1e-4f) q.x = Mathf.Round(q.x / gridSize.x) * gridSize.x;
        if (!snapYToHit && gridSize.y > 1e-4f)
            q.y = Mathf.Round(q.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 1e-4f) q.z = Mathf.Round(q.z / gridSize.z) * gridSize.z;
        Vector3 result = q + gridOrigin;
        if (snapYToHit) result.y = p.y; // оставить высоту с поверхности
        return result;
    }

    // ---------- Куб предпросмотра ----------
    private void DrawPreviewCube(PaletteItem item, Vector3 pos, Vector3 normal)
    {
        Bounds local = GetPrefabLocalBounds(item.prefab);

        Quaternion rot = item.alignToNormal
            ? Quaternion.FromToRotation(Vector3.up, normal)
            : Quaternion.identity;
        rot *= Quaternion.Euler(item.rotationOffset);
        rot *= Quaternion.Euler(0f, manualRotation, 0f);
        // Случайный поворот не превьюим — он не детерминирован.

        Vector3 baseScale = item.prefab != null ? item.prefab.transform.localScale : Vector3.one;
        Vector3 scale = Vector3.Scale(baseScale, item.scaleMultiplier);

        Matrix4x4 trs = Matrix4x4.TRS(pos, rot, scale);
        Matrix4x4 prev = Handles.matrix;

        Handles.matrix = trs;

        // Заливка граней (полупрозрачная)
        DrawSolidBoxFaces(local.center, local.size, new Color(0.3f, 1f, 0.4f, 0.07f));

        // Каркас
        Handles.color = new Color(0.3f, 1f, 0.4f, 0.95f);
        Handles.DrawWireCube(local.center, local.size);

        // Маленькая ось «вверх» внутри куба
        Vector3 c = local.center;
        Handles.color = new Color(0.6f, 1f, 0.7f, 0.8f);
        Handles.DrawLine(c, c + Vector3.up * local.size.y * 0.5f);

        Handles.matrix = prev;
    }

    // Полупрозрачная заливка 6 граней AABB (в текущем Handles.matrix)
    private static void DrawSolidBoxFaces(Vector3 center, Vector3 size, Color faceColor)
    {
        Vector3 e = size * 0.5f;
        Vector3[] c = new Vector3[8];
        for (int i = 0; i < 8; i++)
            c[i] = center + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);

        // Грани: пары противоположных по каждой оси
        DrawQuad(c[0], c[1], c[3], c[2], faceColor); // -Z
        DrawQuad(c[4], c[5], c[7], c[6], faceColor); // +Z
        DrawQuad(c[0], c[1], c[5], c[4], faceColor); // -Y
        DrawQuad(c[2], c[3], c[7], c[6], faceColor); // +Y
        DrawQuad(c[0], c[2], c[6], c[4], faceColor); // -X
        DrawQuad(c[1], c[3], c[7], c[5], faceColor); // +X
    }

    private static void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color faceColor)
    {
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] { a, b, c, d },
            faceColor,
            new Color(0, 0, 0, 0));
    }

    // ---------- Ghost-предпросмотр (полупрозрачная копия модели) ----------
    private void DrawPreviewGhost(PaletteItem item, Vector3 pos, Vector3 normal, SceneView sv)
    {
        if (item.prefab == null) return;
        EnsureGhostMaterial();
        if (ghostMaterial == null) return;

        Quaternion rot = item.alignToNormal
            ? Quaternion.FromToRotation(Vector3.up, normal)
            : Quaternion.identity;
        rot *= Quaternion.Euler(item.rotationOffset);
        rot *= Quaternion.Euler(0f, manualRotation, 0f);

        Vector3 baseScale = item.prefab.transform.localScale;
        Vector3 finalScale = Vector3.Scale(baseScale, item.scaleMultiplier);
        Matrix4x4 rootMatrix = Matrix4x4.TRS(pos, rot, finalScale);

        // Корень префаба = identity-эквивалент: матрицы детей берём в системе корня.
        Matrix4x4 prefabRootInv = item.prefab.transform.worldToLocalMatrix;

        Camera cam = sv != null ? sv.camera : null;

        // MeshFilter
        var filters = item.prefab.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in filters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>();
            if (mr != null && !mr.enabled) continue;
            Matrix4x4 relative = prefabRootInv * mf.transform.localToWorldMatrix;
            Matrix4x4 m = rootMatrix * relative;
            DrawMeshAllSubmeshes(mf.sharedMesh, m, cam);
        }

        // SkinnedMeshRenderer (рисуем bind-pose)
        var skinned = item.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinned)
        {
            if (smr == null || smr.sharedMesh == null) continue;
            if (!smr.enabled) continue;
            Matrix4x4 relative = prefabRootInv * smr.transform.localToWorldMatrix;
            Matrix4x4 m = rootMatrix * relative;
            DrawMeshAllSubmeshes(smr.sharedMesh, m, cam);
        }
    }

    private void DrawMeshAllSubmeshes(Mesh mesh, Matrix4x4 m, Camera cam)
    {
        int sub = Mathf.Max(1, mesh.subMeshCount);
        for (int i = 0; i < sub; i++)
        {
            // layer 0; null property block; receiveShadows=false; castShadows=Off
            Graphics.DrawMesh(mesh, m, ghostMaterial, 0, cam, i,
                null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
    }

    private void EnsureGhostMaterial()
    {
        if (ghostMaterial != null)
        {
            UpdateGhostMaterial();
            return;
        }

        // Пробуем последовательно: URP Unlit -> Built-in Standard -> Hidden/Internal-Colored.
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Hidden/Internal-Colored");
        if (sh == null) return;

        ghostMaterial = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        ConfigureMaterialForTransparency(ghostMaterial);
        UpdateGhostMaterial();
    }

    private static void ConfigureMaterialForTransparency(Material m)
    {
        // Built-in Standard: переключаем в Transparent rendering mode
        if (m.HasProperty("_Mode"))
        {
            m.SetFloat("_Mode", 3f); // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        // URP Unlit / общий путь
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);   // Alpha
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
    }

    private void UpdateGhostMaterial()
    {
        if (ghostMaterial == null) return;
        Color c = new Color(ghostColor.r, ghostColor.g, ghostColor.b, ghostOpacity);
        if (ghostMaterial.HasProperty("_BaseColor")) ghostMaterial.SetColor("_BaseColor", c);
        if (ghostMaterial.HasProperty("_Color")) ghostMaterial.SetColor("_Color", c);
        ghostMaterial.color = c;
    }

    private void DestroyGhostMaterial()
    {
        if (ghostMaterial != null)
        {
            DestroyImmediate(ghostMaterial);
            ghostMaterial = null;
        }
    }

    // ---------- Bounds префаба (с кешем) ----------
    private Bounds GetPrefabLocalBounds(GameObject prefab)
    {
        if (prefab == null) return new Bounds(Vector3.zero, Vector3.one * 0.5f);
        if (boundsCache.TryGetValue(prefab, out var cached)) return cached;

        bool any = false;
        Bounds total = new Bounds();
        Matrix4x4 rootInv = prefab.transform.worldToLocalMatrix;

        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in filters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            EncapsulateTransformed(ref total, ref any, mf.sharedMesh.bounds,
                rootInv * mf.transform.localToWorldMatrix);
        }
        var skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinned)
        {
            if (smr == null || smr.sharedMesh == null) continue;
            EncapsulateTransformed(ref total, ref any, smr.sharedMesh.bounds,
                rootInv * smr.transform.localToWorldMatrix);
        }

        if (!any) total = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        boundsCache[prefab] = total;
        return total;
    }

    private static void EncapsulateTransformed(ref Bounds total, ref bool any, Bounds local, Matrix4x4 m)
    {
        Vector3 c = local.center, ext = local.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3(
                (i & 1) == 0 ? -ext.x : ext.x,
                (i & 2) == 0 ? -ext.y : ext.y,
                (i & 4) == 0 ? -ext.z : ext.z);
            Vector3 w = m.MultiplyPoint3x4(corner);
            if (!any) { total = new Bounds(w, Vector3.zero); any = true; }
            else total.Encapsulate(w);
        }
    }

    // ---------- Сетка вокруг точки ----------
    private void DrawGridGizmo(Vector3 center)
    {
        const int half = 4; // 9x9 ячеек
        float sx = gridSize.x > 1e-4f ? gridSize.x : 1f;
        float sz = gridSize.z > 1e-4f ? gridSize.z : 1f;
        float y = center.y + 0.001f;

        // Снапим базу
        float bx = Mathf.Round((center.x - gridOrigin.x) / sx) * sx + gridOrigin.x;
        float bz = Mathf.Round((center.z - gridOrigin.z) / sz) * sz + gridOrigin.z;

        Handles.color = new Color(0.5f, 0.9f, 1f, 0.35f);
        for (int i = -half; i <= half; i++)
        {
            Vector3 a = new Vector3(bx + i * sx, y, bz - half * sz);
            Vector3 b = new Vector3(bx + i * sx, y, bz + half * sz);
            Handles.DrawLine(a, b);

            Vector3 c1 = new Vector3(bx - half * sx, y, bz + i * sz);
            Vector3 c2 = new Vector3(bx + half * sx, y, bz + i * sz);
            Handles.DrawLine(c1, c2);
        }
    }

    // ---------- Reflection-обёртка над internal HandleUtility.IntersectRayMesh ----------
    private static System.Reflection.MethodInfo s_IntersectRayMesh;

    private static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
    {
        if (s_IntersectRayMesh == null)
        {
            s_IntersectRayMesh = typeof(HandleUtility).GetMethod(
                "IntersectRayMesh",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                null,
                new System.Type[] { typeof(Ray), typeof(Mesh), typeof(Matrix4x4), typeof(RaycastHit).MakeByRefType() },
                null);
        }

        if (s_IntersectRayMesh == null)
        {
            hit = default;
            return false;
        }

        object[] args = { ray, mesh, matrix, null };
        bool result = (bool)s_IntersectRayMesh.Invoke(null, args);
        hit = result ? (RaycastHit)args[3] : default;
        return result;
    }

    // ---------- Raycast по мешам без коллайдеров ----------
    private bool TryRaycastMesh(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal, out GameObject hitGO)
    {
        hitPoint = Vector3.zero;
        hitNormal = Vector3.up;
        hitGO = null;

        float closest = float.MaxValue;
        bool found = false;

        // MeshFilter — статика и обычные меши
#if UNITY_2022_2_OR_NEWER
        var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
#else
        var filters = Object.FindObjectsOfType<MeshFilter>();
#endif
        foreach (var mf in filters)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            if (!mf.gameObject.activeInHierarchy) continue;

            if (IntersectRayMesh(ray, mf.sharedMesh, mf.transform.localToWorldMatrix, out RaycastHit h))
            {
                if (h.distance < closest)
                {
                    closest = h.distance;
                    hitPoint = h.point;
                    hitNormal = h.normal;
                    hitGO = mf.gameObject;
                    found = true;
                }
            }
        }

        // SkinnedMeshRenderer — скиннинговые модели
#if UNITY_2022_2_OR_NEWER
        var skinned = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
#else
        var skinned = Object.FindObjectsOfType<SkinnedMeshRenderer>();
#endif
        foreach (var smr in skinned)
        {
            if (smr == null || smr.sharedMesh == null) continue;
            if (!smr.gameObject.activeInHierarchy) continue;

            if (IntersectRayMesh(ray, smr.sharedMesh, smr.transform.localToWorldMatrix, out RaycastHit h))
            {
                if (h.distance < closest)
                {
                    closest = h.distance;
                    hitPoint = h.point;
                    hitNormal = h.normal;
                    hitGO = smr.gameObject;
                    found = true;
                }
            }
        }

        return found;
    }

    // ---------- Размещение ----------
    private void PlaceObject(PaletteItem item, Vector3 pos, Vector3 normal)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab);
        if (go == null) go = Instantiate(item.prefab); // на случай, если это не prefab-asset

        go.transform.position = pos;

        Quaternion rot = item.alignToNormal
            ? Quaternion.FromToRotation(Vector3.up, normal)
            : Quaternion.identity;

        rot *= Quaternion.Euler(item.rotationOffset);
        rot *= Quaternion.Euler(0f, manualRotation, 0f);

        if (item.randomYRotation > 0f)
            rot *= Quaternion.Euler(0f, Random.Range(-item.randomYRotation, item.randomYRotation), 0f);

        go.transform.rotation = rot;

        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(
            s.x * item.scaleMultiplier.x,
            s.y * item.scaleMultiplier.y,
            s.z * item.scaleMultiplier.z);

        if (parentForPlaced != null)
            go.transform.SetParent(parentForPlaced, true);

        Undo.RegisterCreatedObjectUndo(go, "Place " + item.prefab.name);
        Selection.activeGameObject = go;
    }

    // ---------- Сохранение/загрузка через EditorPrefs ----------
    [System.Serializable] private class SerializableData { public List<SerializableItem> items = new List<SerializableItem>(); }
    [System.Serializable]
    private class SerializableItem
    {
        public string prefabGuid;
        public int hotkey;
        public bool ctrl, shift, alt, alignToNormal;
        public float rotX, rotY, rotZ;
        public float scaleX = 1, scaleY = 1, scaleZ = 1;
        public float randomY;
    }

    private void SavePalette()
    {
        var data = new SerializableData();
        foreach (var item in palette)
        {
            string guid = "";
            if (item.prefab != null)
            {
                string path = AssetDatabase.GetAssetPath(item.prefab);
                if (!string.IsNullOrEmpty(path))
                    guid = AssetDatabase.AssetPathToGUID(path);
            }
            data.items.Add(new SerializableItem
            {
                prefabGuid = guid,
                hotkey = (int)item.hotkey,
                ctrl = item.ctrl,
                shift = item.shift,
                alt = item.alt,
                alignToNormal = item.alignToNormal,
                rotX = item.rotationOffset.x,
                rotY = item.rotationOffset.y,
                rotZ = item.rotationOffset.z,
                scaleX = item.scaleMultiplier.x,
                scaleY = item.scaleMultiplier.y,
                scaleZ = item.scaleMultiplier.z,
                randomY = item.randomYRotation,
            });
        }
        EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
    }

    private void LoadPalette()
    {
        palette.Clear();
        if (!EditorPrefs.HasKey(PrefsKey)) return;

        var data = JsonUtility.FromJson<SerializableData>(EditorPrefs.GetString(PrefsKey));
        if (data == null) return;

        foreach (var s in data.items)
        {
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(s.prefabGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(s.prefabGuid);
                if (!string.IsNullOrEmpty(path))
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            palette.Add(new PaletteItem
            {
                prefab = prefab,
                hotkey = (KeyCode)s.hotkey,
                ctrl = s.ctrl,
                shift = s.shift,
                alt = s.alt,
                alignToNormal = s.alignToNormal,
                rotationOffset = new Vector3(s.rotX, s.rotY, s.rotZ),
                scaleMultiplier = new Vector3(s.scaleX, s.scaleY, s.scaleZ),
                randomYRotation = s.randomY,
            });
        }
    }

    // ---------- Сохранение/загрузка настроек снапа ----------
    [System.Serializable]
    private class SnapData
    {
        public bool snapEnabled;
        public bool showGrid = true;
        public bool snapYToHit = true;
        public float gridX = 1f, gridY = 1f, gridZ = 1f;
        public float originX, originY, originZ;
        public int toggleKey = (int)KeyCode.B;
        public bool toggleCtrl, toggleShift, toggleAlt;
        public int rotateKey = (int)KeyCode.R;
        public bool rotateCtrl, rotateShift, rotateAlt;
        public int rotateKeyCCW = (int)KeyCode.T;
        public bool rotateCCWCtrl, rotateCCWShift, rotateCCWAlt;
        public float rotateStep = 15f;
        public float manualRotation = 0f;
        public bool useGhostPreview = false;
        public float ghostR = 0.3f, ghostG = 1f, ghostB = 0.4f;
        public float ghostOpacity = 0.45f;
        public float placementYOffset = 0f;
    }

    private void SaveSnapSettings()
    {
        var d = new SnapData
        {
            snapEnabled = snapEnabled,
            showGrid = showGrid,
            snapYToHit = snapYToHit,
            gridX = gridSize.x,
            gridY = gridSize.y,
            gridZ = gridSize.z,
            originX = gridOrigin.x,
            originY = gridOrigin.y,
            originZ = gridOrigin.z,
            toggleKey = (int)toggleKey,
            toggleCtrl = toggleCtrl,
            toggleShift = toggleShift,
            toggleAlt = toggleAlt,
            rotateKey = (int)rotateKey,
            rotateCtrl = rotateCtrl,
            rotateShift = rotateShift,
            rotateAlt = rotateAlt,
            rotateKeyCCW = (int)rotateKeyCCW,
            rotateCCWCtrl = rotateCCWCtrl,
            rotateCCWShift = rotateCCWShift,
            rotateCCWAlt = rotateCCWAlt,
            rotateStep = rotateStep,
            manualRotation = manualRotation,
            useGhostPreview = useGhostPreview,
            ghostR = ghostColor.r,
            ghostG = ghostColor.g,
            ghostB = ghostColor.b,
            ghostOpacity = ghostOpacity,
            placementYOffset = placementYOffset,
        };
        EditorPrefs.SetString(SnapPrefsKey, JsonUtility.ToJson(d));
    }

    private void LoadSnapSettings()
    {
        if (!EditorPrefs.HasKey(SnapPrefsKey)) return;
        var d = JsonUtility.FromJson<SnapData>(EditorPrefs.GetString(SnapPrefsKey));
        if (d == null) return;
        snapEnabled = d.snapEnabled;
        showGrid = d.showGrid;
        snapYToHit = d.snapYToHit;
        gridSize = new Vector3(d.gridX, d.gridY, d.gridZ);
        gridOrigin = new Vector3(d.originX, d.originY, d.originZ);
        toggleKey = (KeyCode)d.toggleKey;
        toggleCtrl = d.toggleCtrl;
        toggleShift = d.toggleShift;
        toggleAlt = d.toggleAlt;
        rotateKey = (KeyCode)d.rotateKey;
        rotateCtrl = d.rotateCtrl;
        rotateShift = d.rotateShift;
        rotateAlt = d.rotateAlt;
        rotateKeyCCW = (KeyCode)d.rotateKeyCCW;
        rotateCCWCtrl = d.rotateCCWCtrl;
        rotateCCWShift = d.rotateCCWShift;
        rotateCCWAlt = d.rotateCCWAlt;
        rotateStep = d.rotateStep;
        manualRotation = d.manualRotation;
        useGhostPreview = d.useGhostPreview;
        ghostColor = new Color(d.ghostR, d.ghostG, d.ghostB, 1f);
        ghostOpacity = d.ghostOpacity;
        placementYOffset = d.placementYOffset;
        if (ghostMaterial != null) UpdateGhostMaterial();
    }
}
#endif
