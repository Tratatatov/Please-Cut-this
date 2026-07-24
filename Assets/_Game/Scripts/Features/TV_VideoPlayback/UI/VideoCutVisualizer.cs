using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoCutVisualizer : IInitializable, IDisposableService
{
    private VideoCutService _videoCutManager;
    private VideoPlayerService _videoPlayerManager;

    private RectTransform _containerRect;
    private RectTransform _cutMarkerPrefab;
    private Button _deleteSelectedCutButton;

    private List<GameObject> _spawnedMarkers = new List<GameObject>();
    private CutMarkerInteractable _selectedMarker;

    public VideoCutVisualizer(RectTransform container, RectTransform prefab, Button deleteButton)
    {
        _containerRect = container;
        _cutMarkerPrefab = prefab;
        _deleteSelectedCutButton = deleteButton;
    }

    public void Initialize()
    {
        _videoCutManager = ServiceLocator.Get<VideoCutService>();
        _videoPlayerManager = ServiceLocator.Get<VideoPlayerService>();

        if (_videoCutManager != null)
        {
            _videoCutManager.OnIntervalsChanged += UpdateVisuals;
        }
        else
        {
            Debug.LogWarning("VideoCutVisualizer: VideoCutService не найден!");
        }

        if (_videoPlayerManager == null)
        {
            Debug.LogWarning("VideoCutVisualizer: VideoPlayerService не найден!");
        }

        if (_deleteSelectedCutButton != null)
        {
            _deleteSelectedCutButton.onClick.AddListener(DeleteSelectedMarker);
            _deleteSelectedCutButton.interactable = false; 
        }
    }

    public void Dispose()
    {
        if (_videoCutManager != null)
        {
            _videoCutManager.OnIntervalsChanged -= UpdateVisuals;
        }

        if (_deleteSelectedCutButton != null)
        {
            _deleteSelectedCutButton.onClick.RemoveListener(DeleteSelectedMarker);
        }
    }

    public void UpdateVisuals()
    {
        if (_videoCutManager == null || _videoPlayerManager == null || _containerRect == null || _cutMarkerPrefab == null)
            return;

        double duration = _videoPlayerManager.Duration;

        if (duration <= 0)
            return;

        SelectMarker(null); 

        foreach (var marker in _spawnedMarkers)
        {
            if (marker != null)
            {
                Object.Destroy(marker);
            }
        }
        _spawnedMarkers.Clear();

        foreach (var interval in _videoCutManager.intervalsToSkip)
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

        if (_deleteSelectedCutButton != null)
        {
            _deleteSelectedCutButton.interactable = (_selectedMarker != null);
        }
    }

    public void DeleteSelectedMarker()
    {
        if (_selectedMarker != null && _videoCutManager != null)
        {
            _videoCutManager.RemoveCutInterval(_selectedMarker.interval);
        }
    }

    private void CreateMarker(SkipInterval interval, double totalDuration)
    {
        double startTime = interval.startTime;
        double endTime = interval.endTime;

        float startFraction = (float)(startTime / totalDuration);
        float endFraction = (float)(endTime / totalDuration);

        startFraction = Mathf.Clamp01(startFraction);
        endFraction = Mathf.Clamp01(endFraction);

        RectTransform newMarker = Object.Instantiate(_cutMarkerPrefab, _containerRect);
        newMarker.gameObject.SetActive(true);
        _spawnedMarkers.Add(newMarker.gameObject);

        newMarker.anchorMin = new Vector2(startFraction, 0f);
        newMarker.anchorMax = new Vector2(endFraction, 1f);

        newMarker.offsetMin = Vector2.zero;
        newMarker.offsetMax = Vector2.zero;
        newMarker.localScale = Vector3.one;

        CutMarkerInteractable interactable = newMarker.GetComponent<CutMarkerInteractable>();
        if (interactable == null)
        {
            interactable = newMarker.gameObject.AddComponent<CutMarkerInteractable>();
        }
        interactable.Initialize(interval, this);
    }
}
