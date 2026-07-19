using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerManager : IInitializable, IUpdatable, IDisposableService
{
    private VideoPlayer _forwardPlayer;
    private VideoPlayer _reversePlayer;

    private float _customPlaybackSpeed = 1.0f;
    private bool _isReversed = false;

    /// <summary>
    /// Current playback time in seconds (normalized to forward time).
    /// </summary>
    public double CurrentTime
    {
        get
        {
            if (_isReversed && _reversePlayer != null)
            {
                return Math.Clamp(Duration - _reversePlayer.time, 0.0, Duration);
            }
            return _forwardPlayer != null ? _forwardPlayer.time : 0.0;
        }
        set
        {
            double targetTime = Math.Clamp(value, 0.0, Math.Max(0.0, Duration - 0.05));
            if (_isReversed)
            {
                if (_reversePlayer != null) _reversePlayer.time = Math.Clamp(Duration - targetTime, 0.0, Math.Max(0.0, Duration - 0.05));
            }
            else
            {
                if (_forwardPlayer != null) _forwardPlayer.time = targetTime;
            }
        }
    }

    /// <summary>
    /// Gets or sets the playback speed of the video (positive for forward, negative for rewind).
    /// </summary>
    public float PlaybackSpeed
    {
        get => _customPlaybackSpeed;
        set
        {
            float prevSpeed = _customPlaybackSpeed;
            _customPlaybackSpeed = value;
            if (value > 0f)
            {
                bool speedChangedSignificantly = Mathf.Abs(prevSpeed - value) > 0.1f || (prevSpeed <= 0f);
                if (_isReversed)
                {
                    _isReversed = false;
                    SwitchToForwardPlayer();
                    speedChangedSignificantly = true;
                }
                if (_forwardPlayer != null)
                {
                    _forwardPlayer.playbackSpeed = value;
                    _forwardPlayer.Play();
                }
                if (speedChangedSignificantly)
                {
                    Debug.Log($"[VideoPlayerManager] Скорость воспроизведения установлена на: {value}x");
                }
            }
            else if (value < 0f)
            {
                bool speedChangedSignificantly = Mathf.Abs(prevSpeed - value) > 0.1f || (prevSpeed >= 0f);
                if (!_isReversed)
                {
                    _isReversed = true;
                    SwitchToReversePlayer();
                    speedChangedSignificantly = true;
                }
                if (_reversePlayer != null)
                {
                    // Play reverse video forward
                    _reversePlayer.playbackSpeed = -value;
                    _reversePlayer.Play();
                }
                if (speedChangedSignificantly)
                {
                    Debug.Log($"[VideoPlayerManager] Начата отмотка назад со скоростью: {-value}x");
                }
            }
            else
            {
                Pause();
                if (prevSpeed != 0f)
                {
                    Debug.Log($"[VideoPlayerManager] Воспроизведение приостановлено");
                }
            }
        }
    }

    /// <summary>
    /// Total duration of the video in seconds.
    /// </summary>
    public double Duration => _forwardPlayer != null ? _forwardPlayer.length : 0.0;

    /// <summary>
    /// Is the video currently playing.
    /// </summary>
    public bool IsPlaying => (_forwardPlayer != null && _forwardPlayer.isPlaying) || (_reversePlayer != null && _reversePlayer.isPlaying);

    /// <summary>
    /// Has the video player successfully prepared the video source.
    /// </summary>
    public bool IsPrepared => _forwardPlayer != null && _forwardPlayer.isPrepared && (_reversePlayer == null || _reversePlayer.isPrepared);

    /// <summary>
    /// Direct access to the underlying VideoPlayer component.
    /// </summary>
    public VideoPlayer VideoPlayer => _forwardPlayer;

    /// <summary>
    /// Fired when both VideoPlayers have finished preparing.
    /// </summary>
    public event Action OnPrepared;

    /// <summary>
    /// Is the video player currently in the middle of a seek operation.
    /// </summary>
    public bool IsSeeking { get; private set; }

    private bool _wasPlayingBeforeSeek = false;
    private double _preSeekTime = -1.0;
    private double _targetSeekTime = -1.0;
    private bool _seekCompletedEventFired = false;

    private VideoPlayer _playerToHideAfterSeek;
    private VideoPlayer _playerToShowAfterSeek;
    private bool _isWaitingForFrame = false;

    private int _preparedCount = 0;
    private int _targetPrepareCount = 1;

    private RenderTexture _forwardTexture;
    private RenderTexture _reverseTexture;

    private UnityEngine.UI.RawImage _displayImage;
    private Renderer _displayRenderer;
    private string _materialTextureProperty;

    public VideoPlayerManager(
        VideoPlayer forwardPlayer, 
        VideoPlayer reversePlayer,
        UnityEngine.UI.RawImage displayImage = null,
        Renderer displayRenderer = null,
        string materialTextureProperty = "_MainTex"
    )
    {
        _forwardPlayer = forwardPlayer;
        _reversePlayer = reversePlayer;
        _displayImage = displayImage;
        _displayRenderer = displayRenderer;
        _materialTextureProperty = materialTextureProperty;
        
        Debug.Log($"[VideoPlayerManager] Constructor. forwardPlayer: {forwardPlayer}, reversePlayer: {reversePlayer}, displayImage: {displayImage}, displayRenderer: {displayRenderer}");
        if (forwardPlayer != null)
        {
            Debug.Log($"[VideoPlayerManager] forwardPlayer name: {forwardPlayer.gameObject.name}, renderMode: {forwardPlayer.renderMode}, targetTexture: {forwardPlayer.targetTexture}");
        }
        if (reversePlayer != null)
        {
            Debug.Log($"[VideoPlayerManager] reversePlayer name: {reversePlayer.gameObject.name}, renderMode: {reversePlayer.renderMode}, targetTexture: {reversePlayer.targetTexture}");
        }

        if (_forwardPlayer != null)
        {
            _customPlaybackSpeed = _forwardPlayer.playbackSpeed;
            _forwardTexture = _forwardPlayer.targetTexture;
        }
        if (_reversePlayer != null)
        {
            _reverseTexture = _reversePlayer.targetTexture;
            if (_reverseTexture == null && _forwardTexture != null)
            {
                _reverseTexture = _forwardTexture;
            }
            SetupReversePlayer();
        }

        // Explicitly set initial states
        SetPlayerVisibility(_forwardPlayer, true);
        SetPlayerVisibility(_reversePlayer, false);
    }

    private void SetupReversePlayer()
    {
        if (_reversePlayer == null) return;
        
        _reversePlayer.playOnAwake = false;
        _reversePlayer.renderMode = _forwardPlayer.renderMode;
        _reversePlayer.targetCamera = _forwardPlayer.targetCamera;
        
        bool isSharedTexture = (_forwardTexture == _reverseTexture);
        _reversePlayer.targetTexture = isSharedTexture ? null : _reverseTexture;
        
        _reversePlayer.aspectRatio = _forwardPlayer.aspectRatio;
        _reversePlayer.audioOutputMode = _forwardPlayer.audioOutputMode;
        _reversePlayer.targetMaterialRenderer = _forwardPlayer.targetMaterialRenderer;
        _reversePlayer.targetMaterialProperty = _forwardPlayer.targetMaterialProperty;
    }

    private void SetPlayerVisibility(VideoPlayer vp, bool visible)
    {
        if (vp == null) return;
        Debug.Log($"[VideoPlayerManager] SetPlayerVisibility for {vp.gameObject.name} to {visible}");
        if (vp.renderMode == VideoRenderMode.CameraNearPlane || vp.renderMode == VideoRenderMode.CameraFarPlane)
        {
            vp.targetCameraAlpha = visible ? 1f : 0f;
        }
        else if (vp.renderMode == VideoRenderMode.RenderTexture)
        {
            RenderTexture target = (vp == _forwardPlayer) ? _forwardTexture : _reverseTexture;
            bool isSharedTexture = (_forwardTexture == _reverseTexture);
            
            if (isSharedTexture)
            {
                vp.targetTexture = visible ? target : null;
                Debug.Log($"[VideoPlayerManager] Set {vp.gameObject.name} targetTexture (shared) to {(vp.targetTexture != null ? vp.targetTexture.name : "null")}");
            }
            else
            {
                if (vp.targetTexture != target)
                {
                    vp.targetTexture = target;
                    Debug.Log($"[VideoPlayerManager] Set {vp.gameObject.name} targetTexture (separate) to {(target != null ? target.name : "null")}");
                }
            }

            if (visible && target != null)
            {
                UpdateDisplayTexture(target);
            }
        }
        vp.SetDirectAudioVolume(0, visible ? 1f : 0f);
    }

    private void UpdateDisplayTexture(RenderTexture texture)
    {
        if (texture == null) return;
        Debug.Log($"[VideoPlayerManager] UpdateDisplayTexture to {texture.name}");
        if (_displayImage != null)
        {
            _displayImage.texture = texture;
        }
        if (_displayRenderer != null)
        {
            _displayRenderer.material.SetTexture(_materialTextureProperty, texture);
        }
    }

    private void SwitchToForwardPlayer()
    {
        if (_reversePlayer != null && _forwardPlayer != null)
        {
            _reversePlayer.Pause();
            _playerToHideAfterSeek = _reversePlayer;
            _playerToShowAfterSeek = _forwardPlayer;
            _isWaitingForFrame = true;

            _forwardPlayer.time = Math.Clamp(Duration - _reversePlayer.time, 0.0, Math.Max(0.0, Duration - 0.05));
        }
    }

    private void SwitchToReversePlayer()
    {
        if (_forwardPlayer != null && _reversePlayer != null)
        {
            _forwardPlayer.Pause();
            _playerToHideAfterSeek = _forwardPlayer;
            _playerToShowAfterSeek = _reversePlayer;
            _isWaitingForFrame = true;

            _reversePlayer.time = Math.Clamp(Duration - _forwardPlayer.time, 0.0, Math.Max(0.0, Duration - 0.05));
        }
    }

    public void LoadClips(VideoClip forwardClip, VideoClip reverseClip)
    {
        _preparedCount = 0;
        _targetPrepareCount = 0;

        if (_forwardPlayer != null)
        {
            _forwardPlayer.clip = forwardClip;
            _targetPrepareCount++;
            _forwardPlayer.Prepare();
        }

        if (_reversePlayer != null)
        {
            if (reverseClip != null)
            {
                _reversePlayer.clip = reverseClip;
                _targetPrepareCount++;
                _reversePlayer.Prepare();
            }
            else
            {
                Debug.LogWarning("[VideoPlayerManager] Reverse clip is not assigned for this level!");
            }
        }
    }

    public void Initialize()
    {
        if (_forwardPlayer != null)
        {
            _forwardPlayer.prepareCompleted += OnVideoPlayerPrepared;
            _forwardPlayer.seekCompleted += OnVideoSeekCompleted;
            _forwardPlayer.sendFrameReadyEvents = true;
            _forwardPlayer.frameReady += OnFrameReady;
        }
        if (_reversePlayer != null)
        {
            _reversePlayer.prepareCompleted += OnVideoPlayerPrepared;
            _reversePlayer.seekCompleted += OnVideoSeekCompleted;
            _reversePlayer.sendFrameReadyEvents = true;
            _reversePlayer.frameReady += OnFrameReady;
        }
    }

    public void Dispose()
    {
        if (_forwardPlayer != null)
        {
            _forwardPlayer.prepareCompleted -= OnVideoPlayerPrepared;
            _forwardPlayer.seekCompleted -= OnVideoSeekCompleted;
            _forwardPlayer.frameReady -= OnFrameReady;
        }
        if (_reversePlayer != null)
        {
            _reversePlayer.prepareCompleted -= OnVideoPlayerPrepared;
            _reversePlayer.seekCompleted -= OnVideoSeekCompleted;
            _reversePlayer.frameReady -= OnFrameReady;
        }
    }

    private void OnVideoPlayerPrepared(VideoPlayer source)
    {
        _preparedCount++;
        if (_preparedCount >= _targetPrepareCount)
        {
            if (_forwardPlayer != null) _forwardPlayer.time = 0;
            // Removed initial _reversePlayer.time = Duration to prevent VideoPlayer from entering the "reached end" state

            OnPrepared?.Invoke();
        }
    }

    private void OnVideoSeekCompleted(VideoPlayer source)
    {
        if (source == _forwardPlayer || source == _reversePlayer)
        {
            _seekCompletedEventFired = true;
        }
    }

    private void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        if (_isWaitingForFrame && _playerToShowAfterSeek != null && source == _playerToShowAfterSeek)
        {
            VideoPlayer toShow = _playerToShowAfterSeek;
            VideoPlayer toHide = _playerToHideAfterSeek;

            _isWaitingForFrame = false;
            _playerToShowAfterSeek = null;
            _playerToHideAfterSeek = null;

            SetPlayerVisibility(toShow, true);
            if (toHide != null)
            {
                SetPlayerVisibility(toHide, false);
            }
        }
    }

    public void Update()
    {
        if (_isReversed && _reversePlayer != null && _reversePlayer.isPrepared)
        {
            if (_reversePlayer.time >= _reversePlayer.length - 0.05)
            {
                _reversePlayer.time = _reversePlayer.length;
                _isReversed = false;
                _customPlaybackSpeed = 0f;
                SwitchToForwardPlayer();
                Pause();
                Debug.Log("[VideoPlayerManager] Отмотка завершена: достигнуто начало видео.");
            }
        }

        if (IsSeeking && _forwardPlayer != null && _forwardPlayer.isPrepared)
        {
            VideoPlayer activePlayer = _isReversed ? _reversePlayer : _forwardPlayer;
            if (activePlayer == null) activePlayer = _forwardPlayer;

            bool timeHasMoved = Math.Abs(activePlayer.time - _preSeekTime) > 0.1;
            bool reachedTarget = Math.Abs(activePlayer.time - _targetSeekTime) < 1.0;

            if ((_seekCompletedEventFired && timeHasMoved) || reachedTarget)
            {
                IsSeeking = false;
                _seekCompletedEventFired = false;
                if (_wasPlayingBeforeSeek)
                {
                    _wasPlayingBeforeSeek = false;
                    Play();
                }
            }
        }
    }

    public void JumpToTime(double timeInSeconds)
    {
        Seek(timeInSeconds);
        Debug.Log($"[VideoPlayerManager] Переход на время: {timeInSeconds} сек.");
    }

    public void Play()
    {
        if (_isReversed)
        {
            if (_reversePlayer != null) _reversePlayer.Play();
        }
        else
        {
            if (_forwardPlayer != null) _forwardPlayer.Play();
        }
    }

    public void Pause()
    {
        if (_forwardPlayer != null) _forwardPlayer.Pause();
        if (_reversePlayer != null) _reversePlayer.Pause();
    }

    public void Seek(double seconds)
    {
        VideoPlayer activePlayer = _isReversed ? _reversePlayer : _forwardPlayer;
        if (activePlayer != null)
        {
            if (!IsSeeking)
            {
                _wasPlayingBeforeSeek = IsPlaying;
                _preSeekTime = activePlayer.time;
            }
            IsSeeking = true;
            _targetSeekTime = _isReversed ? Duration - seconds : seconds;
            _seekCompletedEventFired = false;

            double targetTime = seconds;
            if (Duration > 0.0)
            {
                targetTime = Math.Clamp(seconds, 0.0, Duration);
            }
            else
            {
                targetTime = Math.Max(0.0, seconds);
            }

            // Pause the player while seeking to allow faster frame decoding and prevent audio stuttering
            if (_wasPlayingBeforeSeek && activePlayer.isPlaying)
            {
                activePlayer.Pause();
            }

            CurrentTime = targetTime;
        }
    }

    public void SetPlaybackSpeed(float speed)
    {
        PlaybackSpeed = speed;
    }
}
