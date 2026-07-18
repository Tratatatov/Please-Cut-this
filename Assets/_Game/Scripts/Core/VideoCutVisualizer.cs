using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Класс для визуализации вырезанных интервалов (Cat-интервалов) на таймлайне.
/// </summary>
public class VideoCutVisualizer : MonoBehaviour
{
    [Header("Менеджеры")]
    [Tooltip("Ссылка на менеджер вырезов для получения списка интервалов и подписки на изменения")]
    public VideoCutManager videoCutManager;

    [Tooltip("Ссылка на менеджер плеера для получения общей длительности видео")]
    public VideoPlayerManager videoPlayerManager;

    [Header("UI Настройки")]
    [Tooltip("Контейнер (RectTransform), внутри которого будут создаваться полоски. Обычно это область слайдера.")]
    public RectTransform containerRect;

    [Tooltip("Префаб полоски выреза (UI Image с нужным цветом)")]
    public RectTransform cutMarkerPrefab;

    [Tooltip("Кнопка для удаления выбранного интервала")]
    public Button deleteSelectedCutButton;

    // Храним созданные маркеры, чтобы удалять их при обновлении
    private List<GameObject> _spawnedMarkers = new List<GameObject>();
    private CutMarkerInteractable _selectedMarker;

    private void Start()
    {
        if (videoCutManager != null)
        {
            // Подписываемся на изменения в списке интервалов
            videoCutManager.OnIntervalsChanged += UpdateVisuals;
        }
        else
        {
            Debug.LogWarning("VideoCutVisualizer: Не назначен VideoCutManager!");
        }

        if (videoPlayerManager == null)
        {
            Debug.LogWarning("VideoCutVisualizer: Не назначен VideoPlayerManager!");
        }

        if (containerRect == null)
        {
            Debug.LogWarning("VideoCutVisualizer: Не назначен контейнер для маркеров (containerRect)!");
        }

        if (cutMarkerPrefab == null)
        {
            Debug.LogWarning("VideoCutVisualizer: Не назначен префаб маркера (cutMarkerPrefab)!");
        }

        if (deleteSelectedCutButton != null)
        {
            deleteSelectedCutButton.onClick.AddListener(DeleteSelectedMarker);
            deleteSelectedCutButton.interactable = false; // Изначально неактивна
        }
    }

    private void OnDestroy()
    {
        if (videoCutManager != null)
        {
            // Отписываемся от события при уничтожении объекта
            videoCutManager.OnIntervalsChanged -= UpdateVisuals;
        }

        if (deleteSelectedCutButton != null)
        {
            deleteSelectedCutButton.onClick.RemoveListener(DeleteSelectedMarker);
        }
    }

    /// <summary>
    /// Полностью перерисовывает все полоски вырезов
    /// </summary>
    public void UpdateVisuals()
    {
        if (videoCutManager == null || videoPlayerManager == null || containerRect == null || cutMarkerPrefab == null)
            return;

        double duration = videoPlayerManager.Duration;

        // Если длительность видео еще не известна или равна нулю, рисовать нечего
        if (duration <= 0)
            return;

        SelectMarker(null); // Сбрасываем выбор перед перерисовкой

        // 1. Очищаем старые маркеры
        foreach (var marker in _spawnedMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        _spawnedMarkers.Clear();

        // 2. Создаем новые маркеры на основе текущих интервалов
        foreach (var interval in videoCutManager.intervalsToSkip)
        {
            CreateMarker(interval, duration);
        }
    }

    public void SelectMarker(CutMarkerInteractable marker)
    {
        if (_selectedMarker != null)
        {
            _selectedMarker.SetSelected(false);
        }

        // Если кликнули по тому же маркеру, снимаем выделение
        if (_selectedMarker == marker)
        {
            _selectedMarker = null;
        }
        else
        {
            _selectedMarker = marker;
            if (_selectedMarker != null)
            {
                _selectedMarker.SetSelected(true);
            }
        }

        if (deleteSelectedCutButton != null)
        {
            deleteSelectedCutButton.interactable = (_selectedMarker != null);
        }
    }

    public void DeleteSelectedMarker()
    {
        if (_selectedMarker != null && videoCutManager != null)
        {
            videoCutManager.RemoveCutInterval(_selectedMarker.interval);
        }
    }

    /// <summary>
    /// Создает один маркер на таймлайне и позиционирует его
    /// </summary>
    private void CreateMarker(SkipInterval interval, double totalDuration)
    {
        double startTime = interval.startTime;
        double endTime = interval.endTime;

        // Вычисляем проценты для позиционирования
        float startFraction = (float)(startTime / totalDuration);
        float endFraction = (float)(endTime / totalDuration);

        // Ограничиваем значения в пределах от 0 до 1
        startFraction = Mathf.Clamp01(startFraction);
        endFraction = Mathf.Clamp01(endFraction);

        // Инстанцируем префаб в контейнер
        RectTransform newMarker = Instantiate(cutMarkerPrefab, containerRect);
        newMarker.gameObject.SetActive(true);
        _spawnedMarkers.Add(newMarker.gameObject);

        // Настраиваем Anchors для автоматического растяжения маркера относительно родителя.
        // AnchorMin.x — это начало интервала, AnchorMax.x — конец.
        // Y остается от 0 до 1, чтобы маркер заполнял всю высоту контейнера.
        newMarker.anchorMin = new Vector2(startFraction, 0f);
        newMarker.anchorMax = new Vector2(endFraction, 1f);

        // Сбрасываем смещения, чтобы маркер точно совпадал со своими якорями
        newMarker.offsetMin = Vector2.zero;
        newMarker.offsetMax = Vector2.zero;
        newMarker.localScale = Vector3.one;

        // Настраиваем интерактивность
        CutMarkerInteractable interactable = newMarker.GetComponent<CutMarkerInteractable>();
        if (interactable == null)
        {
            interactable = newMarker.gameObject.AddComponent<CutMarkerInteractable>();
        }
        interactable.Initialize(interval, this);
    }
}
