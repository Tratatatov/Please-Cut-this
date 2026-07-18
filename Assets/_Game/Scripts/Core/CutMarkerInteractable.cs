using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Компонент, который вешается на префаб маркера выреза.
/// Обрабатывает клики и визуально выделяет маркер.
/// </summary>
public class CutMarkerInteractable : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Объект-подсветка (например, рамка или просто увеличенный фон), который включается при выборе")]
    public GameObject highlightObject;

    [Tooltip("Увеличение маркера при выделении в пикселях по X и Y (если highlightObject не задан)")]
    public Vector2 selectionPadding = new Vector2(5f, 5f);

    private RectTransform _rectTransform;

    public SkipInterval interval { get; private set; }
    private VideoCutVisualizer _visualizer;
    private bool _isSelected = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }

    public void Initialize(SkipInterval skipInterval, VideoCutVisualizer visualizer)
    {
        interval = skipInterval;
        _visualizer = visualizer;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_visualizer != null)
        {
            _visualizer.SelectMarker(this);
        }
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;

        // Выводим в консоль статус выбора
        if (_isSelected)
        {
            Debug.Log($"CutMarkerInteractable: Отрезок с {interval.startTime:F2} по {interval.endTime:F2} ВЫБРАН.");
        }
        else
        {
            Debug.Log($"CutMarkerInteractable: Выделение снято с отрезка {interval.startTime:F2} - {interval.endTime:F2}.");
        }

        if (highlightObject != null)
        {
            highlightObject.SetActive(_isSelected);
        }
        else if (_rectTransform != null)
        {
            // Если отдельного объекта для подсветки нет, увеличиваем отступы от центра (RectTransform)
            if (_isSelected)
            {
                _rectTransform.offsetMin = new Vector2(-selectionPadding.x, -selectionPadding.y);
                _rectTransform.offsetMax = new Vector2(selectionPadding.x, selectionPadding.y);
            }
            else
            {
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
