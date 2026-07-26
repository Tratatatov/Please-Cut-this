using System;
using Core.Services;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerService : IInitializable, IUpdatable, IDisposableService
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
                if (_reversePlayer.length > 0)
                {
                    double ratio = _reversePlayer.time / _reversePlayer.length;
                    return Math.Clamp(Duration * (1.0 - ratio), 0.0, Duration);
                }
                return 0.0;
            }
            return _forwardPlayer != null ? _forwardPlayer.time : 0.0;
        }
        set
        {
            double targetTime = Math.Clamp(value, 0.0, Math.Max(0.0, Duration - 0.05));
            if (_isReversed)
            {
                if (_reversePlayer != null)
                {
                    if (Duration > 0)
                    {
                        double ratio = targetTime / Duration;
                        _reversePlayer.time = Math.Clamp(_reversePlayer.length * (1.0 - ratio), 0.0, Math.Max(0.0, _reversePlayer.length - 0.05));
                    }
                }
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
                    if (Mathf.Abs(_forwardPlayer.playbackSpeed - value) > 0.01f || speedChangedSignificantly)
                    {
                        _forwardPlayer.playbackSpeed = value;
                    }
                    if (!_forwardPlayer.isPlaying)
                    {
                        _forwardPlayer.Play();
                    }
                }
                if (speedChangedSignificantly)
                {
                    Debug.Log($"<color=cyan>[VideoPlayerService]</color> Скорость воспроизведения установлена на: {value}x");
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
                    float targetReverseSpeed = -value;
                    if (Mathf.Abs(_reversePlayer.playbackSpeed - targetReverseSpeed) > 0.01f || speedChangedSignificantly)
                    {
                        _reversePlayer.playbackSpeed = targetReverseSpeed;
                    }
                    if (!_reversePlayer.isPlaying)
                    {
                        _reversePlayer.Play();
                    }
                }
                if (speedChangedSignificantly)
                {
                    Debug.Log($"<color=cyan>[VideoPlayerService]</color> Начата отмотка назад со скоростью: {-value}x");
                }
            }
            else
            {
                Pause();
                if (prevSpeed != 0f)
                {
                    Debug.Log($"<color=cyan>[VideoPlayerService]</color> Воспроизведение приостановлено");
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
    private float _waitingForFrameTimer = 0f;

    private int _preparedCount = 0;
    private int _targetPrepareCount = 1;

    private RenderTexture _forwardTexture;
    private RenderTexture _reverseTexture;

    private TVRendererService _tvRendererService;
    private string _materialTextureProperty;

    public VideoPlayerService(
        VideoPlayer forwardPlayer, 
        VideoPlayer reversePlayer,
        string materialTextureProperty = "_MainTex",
        TVRendererService tvRendererService = null
    )
    {
        _forwardPlayer = forwardPlayer;
        _reversePlayer = reversePlayer;
        _materialTextureProperty = materialTextureProperty;
        _tvRendererService = tvRendererService;
        
        Debug.Log($"<color=cyan>[VideoPlayerService]</color> Constructor. forwardPlayer: {forwardPlayer}, reversePlayer: {reversePlayer}, tvRendererService: {tvRendererService}");
        if (forwardPlayer != null)
        {
            Debug.Log($"<color=cyan>[VideoPlayerService]</color> forwardPlayer name: {forwardPlayer.gameObject.name}, renderMode: {forwardPlayer.renderMode}, targetTexture: {forwardPlayer.targetTexture}");
        }
        if (reversePlayer != null)
        {
            Debug.Log($"<color=cyan>[VideoPlayerService]</color> reversePlayer name: {reversePlayer.gameObject.name}, renderMode: {reversePlayer.renderMode}, targetTexture: {reversePlayer.targetTexture}");
        }

        if (_forwardPlayer != null)
        {
            _customPlaybackSpeed = _forwardPlayer.playbackSpeed;
            _forwardTexture = _forwardPlayer.targetTexture;

            if (_forwardTexture == null)
            {
                _forwardTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                _forwardTexture.name = "Dynamic_TV_RenderTexture";
                _forwardTexture.Create();
                _forwardPlayer.targetTexture = _forwardTexture;
                Debug.Log("<color=cyan>[VideoPlayerService]</color> targetTexture не был назначен в Inspector. Автоматически создана динамическая RenderTexture (1920x1080).");
            }
        }
        if (_reversePlayer != null)
        {
            _reverseTexture = _reversePlayer.targetTexture;
            if (_reverseTexture == null && _forwardTexture != null)
            {
                _reverseTexture = _forwardTexture;
                _reversePlayer.targetTexture = _reverseTexture;
            }
            SetupReversePlayer();
        }

        if (_tvRendererService != null)
        {
            if (_forwardTexture != null) _tvRendererService.SetTextureForRenderer(_forwardTexture, _materialTextureProperty, false);
            if (_reverseTexture != null && _reverseTexture != _forwardTexture) _tvRendererService.SetTextureForRenderer(_reverseTexture, _materialTextureProperty, true);
        }

        // Explicitly set initial states
        SetPlayerVisibility(_forwardPlayer, true);
        SetPlayerVisibility(_reversePlayer, false);
    }

    public VideoPlayerService(
        VideoPlayer forwardPlayer, 
        VideoPlayer reversePlayer,
        Renderer displayRenderer,
        string materialTextureProperty,
        TVRendererService tvRendererService
    ) : this(forwardPlayer, reversePlayer, materialTextureProperty, tvRendererService ?? (displayRenderer != null ? new TVRendererService(displayRenderer) : null))
    {
    }

    public void BindTVRendererService(TVRendererService tvRendererService)
    {
        _tvRendererService = tvRendererService;
        if (_tvRendererService != null)
        {
            if (_forwardTexture != null) _tvRendererService.SetTextureForRenderer(_forwardTexture, _materialTextureProperty, false);
            if (_reverseTexture != null && _reverseTexture != _forwardTexture) _tvRendererService.SetTextureForRenderer(_reverseTexture, _materialTextureProperty, true);
        }
        RefreshDisplayTexture();
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

    /// <summary>
    /// Включает или отключает игровой объект для указанного VideoPlayer.
    /// </summary>
    public void SetPlayerGameObjectActive(VideoPlayer vp, bool active)
    {
        if (vp == null || vp.gameObject == null) return;

        bool isSharedGameObject = (_forwardPlayer != null && _reversePlayer != null && _forwardPlayer.gameObject == _reversePlayer.gameObject);
        if (isSharedGameObject && !active)
        {
            bool otherActive = (vp == _forwardPlayer && _isReversed) || (vp == _reversePlayer && !_isReversed);
            if (otherActive) return;
        }

        if (vp.gameObject.activeSelf != active)
        {
            vp.gameObject.SetActive(active);
            Debug.Log($"<color=cyan>[VideoPlayerService]</color> GameObject плеера '{vp.gameObject.name}' -> SetActive({active})");
        }
    }

    public void EnableForwardPlayer()
    {
        if (_forwardPlayer != null)
        {
            SetPlayerGameObjectActive(_forwardPlayer, true);
        }
    }

    /// <summary>
    /// Включает GameObject для реверсного плеера (Reverse).
    /// </summary>
    public void EnableReversePlayer()
    {
        if (_reversePlayer != null)
        {
            SetPlayerGameObjectActive(_reversePlayer, true);
        }
    }

    /// <summary>
    /// Отключает игровые объекты всех видеоплееров.
    /// </summary>
    public void DisableAllPlayers()
    {
        if (_forwardPlayer != null) SetPlayerGameObjectActive(_forwardPlayer, false);
        if (_reversePlayer != null) SetPlayerGameObjectActive(_reversePlayer, false);
    }

    private void SetPlayerVisibility(VideoPlayer vp, bool visible)
    {
        if (vp == null) return;
        Debug.Log($"<color=cyan>[VideoPlayerService]</color> SetPlayerVisibility for {vp.gameObject.name} to {visible}");

        if (visible)
        {
            if (vp == _forwardPlayer) EnableForwardPlayer();
            else if (vp == _reversePlayer) EnableReversePlayer();
            else SetPlayerGameObjectActive(vp, true);
        }
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
                Debug.Log($"<color=cyan>[VideoPlayerService]</color> Set {vp.gameObject.name} targetTexture (shared) to {(vp.targetTexture != null ? vp.targetTexture.name : "null")}");
            }
            else
            {
                if (vp.targetTexture != target)
                {
                    vp.targetTexture = target;
                    Debug.Log($"<color=cyan>[VideoPlayerService]</color> Set {vp.gameObject.name} targetTexture (separate) to {(target != null ? target.name : "null")}");
                }
            }

            if (visible && target != null)
            {
                bool isReversedPlayer = (vp == _reversePlayer);
                UpdateDisplayTexture(target, isReversedPlayer);
            }
        }
        SetPlayerAudioState(vp, visible);
    }

    private void SetPlayerAudioState(VideoPlayer vp, bool active)
    {
        if (vp == null) return;
        if (vp.audioOutputMode == VideoAudioOutputMode.AudioSource && vp.GetTargetAudioSource(0) != null)
        {
            vp.GetTargetAudioSource(0).mute = !active;
            vp.GetTargetAudioSource(0).volume = active ? 1f : 0f;
        }
        else
        {
            ushort trackCount = (vp.audioTrackCount > 0) ? vp.audioTrackCount : (ushort)1;
            for (ushort i = 0; i < trackCount; i++)
            {
                vp.SetDirectAudioMute(i, !active);
                vp.SetDirectAudioVolume(i, active ? 1f : 0f);
            }
        }
    }

    public void RefreshDisplayTexture()
    {
        RenderTexture activeTexture = _isReversed ? _reverseTexture : _forwardTexture;
        if (activeTexture == null) activeTexture = _forwardTexture;
        if (activeTexture != null)
        {
            UpdateDisplayTexture(activeTexture, _isReversed);
        }
    }

    private void UpdateDisplayTexture(RenderTexture texture, bool isReversed)
    {
        if (texture == null) return;
        Debug.Log($"<color=cyan>[VideoPlayerService]</color> UpdateDisplayTexture to {texture.name}, isReversed: {isReversed}");
        _tvRendererService?.SetScreenTexture(texture, _materialTextureProperty, isReversed);
    }

    private void SwitchToForwardPlayer()
    {
        EnableForwardPlayer();
        if (_reversePlayer != null && _forwardPlayer != null)
        {
            Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> SwitchToForwardPlayer: переключение с {_reversePlayer.gameObject.name} на {_forwardPlayer.gameObject.name}");
            _reversePlayer.Pause();
            SetPlayerAudioState(_reversePlayer, false);
            SetPlayerAudioState(_forwardPlayer, true);

            if (_forwardPlayer.renderMode == VideoRenderMode.RenderTexture && _forwardPlayer.targetTexture == null && _forwardTexture != null)
            {
                Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> Назначение targetTexture ({_forwardTexture.name}) для {_forwardPlayer.gameObject.name} перед сменой времени.");
                _forwardPlayer.targetTexture = _forwardTexture;
            }

            _playerToHideAfterSeek = _reversePlayer;
            _playerToShowAfterSeek = _forwardPlayer;
            _isWaitingForFrame = true;
            _waitingForFrameTimer = 0f;

            if (_reversePlayer.length > 0)
            {
                double ratio = _reversePlayer.time / _reversePlayer.length;
                _forwardPlayer.time = Math.Clamp(Duration * (1.0 - ratio), 0.0, Math.Max(0.0, Duration - 0.05));
            }
            else
            {
                _forwardPlayer.time = 0.0;
            }
            Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> Установлено время для forward плеера: {_forwardPlayer.time}, ожидание OnFrameReady...");
        }
    }

    private void SwitchToReversePlayer()
    {
        EnableReversePlayer();
        if (_forwardPlayer != null && _reversePlayer != null)
        {
            Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> SwitchToReversePlayer: переключение с {_forwardPlayer.gameObject.name} на {_reversePlayer.gameObject.name}");
            _forwardPlayer.Pause();
            SetPlayerAudioState(_forwardPlayer, false);
            SetPlayerAudioState(_reversePlayer, true);

            if (_reversePlayer.renderMode == VideoRenderMode.RenderTexture && _reversePlayer.targetTexture == null && _reverseTexture != null)
            {
                Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> Назначение targetTexture ({_reverseTexture.name}) для {_reversePlayer.gameObject.name} перед сменой времени.");
                _reversePlayer.targetTexture = _reverseTexture;
            }

            _playerToHideAfterSeek = _forwardPlayer;
            _playerToShowAfterSeek = _reversePlayer;
            _isWaitingForFrame = true;
            _waitingForFrameTimer = 0f;

            if (Duration > 0)
            {
                double ratio = _forwardPlayer.time / Duration;
                _reversePlayer.time = Math.Clamp(_reversePlayer.length * (1.0 - ratio), 0.0, Math.Max(0.0, _reversePlayer.length - 0.05));
            }
            else
            {
                _reversePlayer.time = 0.0;
            }
            Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> Установлено время для reverse плеера: {_reversePlayer.time}, ожидание OnFrameReady...");
        }
    }

    public void LoadClips(VideoClip forwardClip, VideoClip reverseClip)
    {
        _preparedCount = 0;
        _targetPrepareCount = 0;

        if (_forwardPlayer != null)
        {
            SetPlayerGameObjectActive(_forwardPlayer, true);
            _forwardPlayer.Stop();
            _forwardPlayer.playOnAwake = false;
            _forwardPlayer.clip = forwardClip;
            _targetPrepareCount++;
            _forwardPlayer.Prepare();
        }

        if (_reversePlayer != null)
        {
            _reversePlayer.Stop();
            _reversePlayer.playOnAwake = false;
            if (reverseClip != null)
            {
                SetPlayerGameObjectActive(_reversePlayer, true);
                _reversePlayer.clip = reverseClip;
                _targetPrepareCount++;
                _reversePlayer.Prepare();
            }
            else
            {
                Debug.LogWarning("<color=cyan>[VideoPlayerService]</color> Reverse clip is not assigned for this level!");
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
        Debug.Log($"<color=cyan>[VideoPlayerService]</color> Плеер готов: {source.gameObject.name} ({_preparedCount}/{_targetPrepareCount})");

        if (source == _reversePlayer)
        {
            _reversePlayer.Pause();
            SetPlayerAudioState(_reversePlayer, false);
            SetPlayerVisibility(_reversePlayer, false);
        }
        else if (source == _forwardPlayer && _customPlaybackSpeed == 0f)
        {
            _forwardPlayer.Pause();
        }

        if (_preparedCount >= _targetPrepareCount)
        {
            if (_forwardPlayer != null) _forwardPlayer.time = 0;
            if (_reversePlayer != null) _reversePlayer.time = 0;
            RefreshDisplayTexture();
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
            Debug.Log($"<color=cyan>[TEST VideoPlayerService]</color> OnFrameReady успешно сработал для {source.gameObject.name} (кадр: {frameIdx}). Вызываем SetPlayerVisibility.");
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
        if (_isWaitingForFrame && _playerToShowAfterSeek != null)
        {
            _waitingForFrameTimer += Time.unscaledDeltaTime;
            if (_waitingForFrameTimer > 0.15f)
            {
                Debug.LogWarning($"<color=cyan>[TEST VideoPlayerService WARNING]</color> OnFrameReady не сработал за 0.15 сек для {_playerToShowAfterSeek.gameObject.name}! Принудительно переключаем видимость (SetPlayerVisibility).");
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
        if (_isReversed && _reversePlayer != null && _reversePlayer.isPrepared)
        {
            if (_reversePlayer.time >= _reversePlayer.length - 0.05)
            {
                _reversePlayer.time = _reversePlayer.length;
                _isReversed = false;
                _customPlaybackSpeed = 0f;
                SwitchToForwardPlayer();
                Pause();
                Debug.Log("<color=cyan>[VideoPlayerService]</color> Отмотка завершена: достигнуто начало видео.");
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
        Debug.Log($"<color=cyan>[VideoPlayerService]</color> Переход на время: {timeInSeconds} сек.");
    }

    public void Play()
    {
        if (_isReversed)
        {
            EnableReversePlayer();
            if (_reversePlayer != null && !_reversePlayer.isPlaying) _reversePlayer.Play();
        }
        else
        {
            EnableForwardPlayer();
            if (_forwardPlayer != null && !_forwardPlayer.isPlaying) _forwardPlayer.Play();
        }
    }

    public void Pause()
    {
        if (_forwardPlayer != null) _forwardPlayer.Pause();
        if (_reversePlayer != null) _reversePlayer.Pause();
    }

    public void Stop()
    {
        Pause();
        DisableAllPlayers();
        Debug.Log("<color=cyan>[VideoPlayerService]</color> Видеоплееры остановлены, их GameObject отключены.");
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
            
            double targetTimeForSeeking = seconds;
            if (Duration > 0.0)
            {
                targetTimeForSeeking = Math.Clamp(seconds, 0.0, Duration);
            }
            else
            {
                targetTimeForSeeking = Math.Max(0.0, seconds);
            }
            
            if (_isReversed && Duration > 0)
            {
                double ratio = targetTimeForSeeking / Duration;
                _targetSeekTime = _reversePlayer.length * (1.0 - ratio);
            }
            else
            {
                _targetSeekTime = targetTimeForSeeking;
            }
            
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
