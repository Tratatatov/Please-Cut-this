using System.Collections.Generic;
using GamePlay.View;
using UnityEngine;
using UnityEngine.UI;
using Core.Services;

public class VideoCutVisualizer : IInitializable, IDisposableService, IUpdatable
{
    private VideoCutService _videoCutManager;
    private VideoPlayerService _videoPlayerManager;

    private RectTransform _containerRect;
    private RectTransform _cutMarkerPrefab;
    private Button _deleteSelectedCutButton;

    private List<GameObject> _spawnedMarkers = new List<GameObject>();
    private CutMarkerInteractable _selectedMarker;

    private GameObject _pendingMarkerObject;
    private RectTransform _pendingMarkerRect;

    private Color _pendingCutColor = new Color(1f, 0f, 0f, 0.5f);
    private Image _blinkCutImage;
    private float _blinkTimer = 0f;
    private bool _isBlinkingOn = false;

    public VideoCutVisualizer(VideoPlayerControlsUIView controlsView)
    {
        if (controlsView != null)
        {
            _containerRect = controlsView.markerContainer;
            _cutMarkerPrefab = controlsView.markerPrefab;
            _deleteSelectedCutButton = controlsView.deleteSelectedCutButton;
            _pendingCutColor = controlsView.pendingCutColor;
            _blinkCutImage = controlsView.blinkCutImage;
        }
    }

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
            _videoCutManager.OnPendingCutChanged += UpdatePendingMarkerVisibility;
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
            _videoCutManager.OnPendingCutChanged -= UpdatePendingMarkerVisibility;
        }

        if (_deleteSelectedCutButton != null)
        {
            _deleteSelectedCutButton.onClick.RemoveListener(DeleteSelectedMarker);
        }
    }

    private void UpdatePendingMarkerVisibility()
    {
        if (_videoCutManager != null && _videoCutManager.IsWaitingForEnd)
        {
            if (_pendingMarkerObject == null && _cutMarkerPrefab != null && _containerRect != null)
            {
                _pendingMarkerObject = Object.Instantiate(_cutMarkerPrefab.gameObject, _containerRect);
                _pendingMarkerRect = _pendingMarkerObject.GetComponent<RectTransform>();
                
                Image img = _pendingMarkerObject.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _pendingCutColor;
                }
                
                CutMarkerInteractable interactable = _pendingMarkerObject.GetComponent<CutMarkerInteractable>();
                if (interactable != null)
                {
                    Object.Destroy(interactable);
                }
            }
            if (_pendingMarkerObject != null)
            {
                _pendingMarkerObject.SetActive(true);
            }
        }
        else
        {
            if (_pendingMarkerObject != null)
            {
                _pendingMarkerObject.SetActive(false);
            }

            if (_blinkCutImage != null)
            {
                var c = _blinkCutImage.color;
                c.a = 1f;
                _blinkCutImage.color = c;
                _blinkTimer = 0f;
                _isBlinkingOn = false;
            }
        }
    }

    public void Update()
    {
        if (_videoCutManager != null && _videoCutManager.IsWaitingForEnd)
        {
            if (_pendingMarkerObject != null && _pendingMarkerObject.activeSelf)
            {
                if (_videoPlayerManager != null)
                {
                    double duration = _videoPlayerManager.Duration;
                    if (duration > 0)
                    {
                        double startTime = _videoCutManager.PendingStartTime;
                        double endTime = _videoPlayerManager.CurrentTime;

                        if (endTime < startTime)
                        {
                            double temp = startTime;
                            startTime = endTime;
                            endTime = temp;
                        }

                        float startFraction = Mathf.Clamp01((float)(startTime / duration));
                        float endFraction = Mathf.Clamp01((float)(endTime / duration));

                        _pendingMarkerRect.anchorMin = new Vector2(startFraction, 0f);
                        _pendingMarkerRect.anchorMax = new Vector2(endFraction, 1f);
                        _pendingMarkerRect.offsetMin = Vector2.zero;
                        _pendingMarkerRect.offsetMax = Vector2.zero;
                        _pendingMarkerRect.localScale = Vector3.one;
                    }
                }
            }

            if (_blinkCutImage != null)
            {
                _blinkTimer += Time.deltaTime;
                if (_blinkTimer >= 0.5f) // Blink every 0.5s
                {
                    _blinkTimer = 0f;
                    _isBlinkingOn = !_isBlinkingOn;
                    var c = _blinkCutImage.color;
                    c.a = _isBlinkingOn ? 1f : 0.3f;
                    _blinkCutImage.color = c;
                }
            }
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

        RectTransform newMarker = Object.Instantiate(_cutMarkerPrefab.gameObject, _containerRect).GetComponent<RectTransform>();
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
