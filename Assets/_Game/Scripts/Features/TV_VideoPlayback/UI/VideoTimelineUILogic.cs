using UnityEngine;
using System;
using GamePlay.View;

public class VideoTimelineUILogic : IInitializable, IUpdatable, IDisposableService
{
    private readonly VideoPlayerControlsUIView _view;
    private readonly VideoPlayerService _playerManager;
    private readonly VideoCutService _cutManager;

    private float _speedBeforeHold = 0f;
    private bool _isHoldingButton = false;
    private bool _isHoldingRewind = false;
    private bool _isHoldingForward = false;
    private float _holdStartTime = 0f;

    public event Action OnFinishEditingClicked;

    public VideoTimelineUILogic(VideoPlayerControlsUIView view, VideoPlayerService playerManager, VideoCutService cutManager)
    {
        _view = view;
        _playerManager = playerManager;
        _cutManager = cutManager;
    }

    public void Initialize()
    {
        if (_view == null) return;

        _view.Initialize();
        _view.OnPlayPauseClicked += HandlePlayPauseClicked;
        _view.OnSetCutIntervalClicked += HandleSetCutIntervalClicked;
        _view.OnClearAllCutsClicked += HandleClearAllCutsClicked;
        _view.OnFinishEditingClicked += HandleFinishEditingClicked;
        _view.OnSpeedSliderValueChangedEvent += HandleSpeedSliderValueChanged;
        
        _view.OnRewindPointerDown += HandleRewindPointerDown;
        _view.OnForwardPointerDown += HandleForwardPointerDown;
        _view.OnHoldPointerUp += HandleHoldPointerUp;

        if (_playerManager != null)
        {
            _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
        }
    }

    public void Dispose()
    {
        if (_view == null) return;

        _view.OnPlayPauseClicked -= HandlePlayPauseClicked;
        _view.OnSetCutIntervalClicked -= HandleSetCutIntervalClicked;
        _view.OnClearAllCutsClicked -= HandleClearAllCutsClicked;
        _view.OnFinishEditingClicked -= HandleFinishEditingClicked;
        _view.OnSpeedSliderValueChangedEvent -= HandleSpeedSliderValueChanged;

        _view.OnRewindPointerDown -= HandleRewindPointerDown;
        _view.OnForwardPointerDown -= HandleForwardPointerDown;
        _view.OnHoldPointerUp -= HandleHoldPointerUp;
    }

    public void Update()
    {
        if (_playerManager == null || _view == null)
            return;

        // Обработка ускорения при удержании кнопок
        if (_isHoldingButton && _view.enableHoldAcceleration && _view.accelerationDuration > 0f)
        {
            float elapsed = Time.unscaledTime - _holdStartTime;
            float t = Mathf.Clamp01(elapsed / _view.accelerationDuration);

            if (_isHoldingRewind)
            {
                _playerManager.PlaybackSpeed = Mathf.Lerp(_view.rewindStartSpeed, _view.rewindHoldSpeed, t);
                _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
            }
            else if (_isHoldingForward)
            {
                _playerManager.PlaybackSpeed = Mathf.Lerp(_view.forwardStartSpeed, _view.forwardHoldSpeed, t);
                _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
            }
        }

        // Обновляем значение слайдера в соответствии с видео
        if (_playerManager.IsPrepared && _playerManager.Duration > 0)
        {
            float progress = (float)(_playerManager.CurrentTime / _playerManager.Duration);
            _view.UpdateProgressSlider(progress);
        }

        // Обновляем текстовое отображение времени
        _view.UpdateTimeText(_playerManager.CurrentTime, _playerManager.Duration);

        // Обновляем состояние кнопки паузы/воспроизведения
        bool isPlaying = _playerManager.PlaybackSpeed != 0f;
        _view.UpdatePlayPauseButtonVisual(isPlaying);
    }

    private void HandleSpeedSliderValueChanged(float value)
    {
        if (_playerManager != null)
        {
            _playerManager.PlaybackSpeed = value;
            _view.UpdateSpeedUI(value);
        }
    }

    private void HandleRewindPointerDown()
    {
        if (_playerManager != null)
        {
            if (!_isHoldingButton)
            {
                _speedBeforeHold = _playerManager.PlaybackSpeed;
                _isHoldingButton = true;
            }
            _isHoldingRewind = true;
            _isHoldingForward = false;
            _holdStartTime = Time.unscaledTime;

            _playerManager.PlaybackSpeed = _view.enableHoldAcceleration ? _view.rewindStartSpeed : _view.rewindHoldSpeed;
            _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
        }
    }

    private void HandleForwardPointerDown()
    {
        if (_playerManager != null)
        {
            if (!_isHoldingButton)
            {
                _speedBeforeHold = _playerManager.PlaybackSpeed;
                _isHoldingButton = true;
            }
            _isHoldingForward = true;
            _isHoldingRewind = false;
            _holdStartTime = Time.unscaledTime;

            _playerManager.PlaybackSpeed = _view.enableHoldAcceleration ? _view.forwardStartSpeed : _view.forwardHoldSpeed;
            _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
        }
    }

    private void HandleHoldPointerUp()
    {
        if (_playerManager != null && _isHoldingButton)
        {
            _playerManager.PlaybackSpeed = _speedBeforeHold;
            _isHoldingButton = false;
            _isHoldingRewind = false;
            _isHoldingForward = false;
            _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
        }
    }

    private void HandlePlayPauseClicked()
    {
        if (_playerManager == null) return;

        if (_playerManager.PlaybackSpeed != 0f)
        {
            _playerManager.PlaybackSpeed = 0f;
        }
        else
        {
            _playerManager.PlaybackSpeed = 1f;
        }
        _speedBeforeHold = _playerManager.PlaybackSpeed;
        _view.UpdateSpeedUI(_playerManager.PlaybackSpeed);
    }

    private void HandleSetCutIntervalClicked()
    {
        if (_cutManager != null)
        {
            _cutManager.ToggleIntervalPoint();
        }
    }

    private void HandleClearAllCutsClicked()
    {
        if (_cutManager != null)
        {
            _cutManager.ClearAllCuts();
        }
    }

    private void HandleFinishEditingClicked()
    {
        OnFinishEditingClicked?.Invoke();
    }
}
