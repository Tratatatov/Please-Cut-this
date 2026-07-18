using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerManager : IInitializable, IUpdatable, IDisposableService
{
    private VideoPlayer _videoPlayer;

    private float _customPlaybackSpeed = 1.0f;
    private bool _isRewinding = false;
    private double _rewindSpeed = 1.0;
    private double _targetRewindTime = 0.0;

    /// <summary>
    /// Current playback time in seconds.
    /// </summary>
    public double CurrentTime
    {
        get => _videoPlayer != null ? _videoPlayer.time : 0.0;
        set
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.time = value;
                if (_isRewinding)
                {
                    _targetRewindTime = value;
                }
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
            _customPlaybackSpeed = value;
            if (value > 0f)
            {
                StopRewinding();
                if (_videoPlayer != null)
                {
                    _videoPlayer.playbackSpeed = value;
                    Play();
                }
                Debug.Log($"[VideoPlayerManager] Скорость воспроизведения установлена на: {value}x");
            }
            else if (value < 0f)
            {
                StartRewinding(-value);
            }
            else
            {
                StopRewinding();
                Pause();
                Debug.Log($"[VideoPlayerManager] Воспроизведение приостановлено");
            }
        }
    }

    /// <summary>
    /// Total duration of the video in seconds.
    /// </summary>
    public double Duration => _videoPlayer != null ? _videoPlayer.length : 0.0;

    /// <summary>
    /// Is the video currently playing.
    /// </summary>
    public bool IsPlaying => (_videoPlayer != null && _videoPlayer.isPlaying) || _isRewinding;

    /// <summary>
    /// Has the video player successfully prepared the video source.
    /// </summary>
    public bool IsPrepared => _videoPlayer != null && _videoPlayer.isPrepared;

    /// <summary>
    /// Direct access to the underlying VideoPlayer component.
    /// </summary>
    public VideoPlayer VideoPlayer => _videoPlayer;

    /// <summary>
    /// Fired when the VideoPlayer has finished preparing the video.
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

    public VideoPlayerManager(VideoPlayer player)
    {
        _videoPlayer = player;
        if (_videoPlayer != null)
        {
            _customPlaybackSpeed = _videoPlayer.playbackSpeed;
        }
    }

    public void Initialize()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted += OnVideoPlayerPrepared;
            _videoPlayer.seekCompleted += OnVideoSeekCompleted;
        }
        else
        {
            Debug.LogWarning("VideoPlayerManager: Компонент VideoPlayer не передан!");
        }
    }

    public void Dispose()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnVideoPlayerPrepared;
            _videoPlayer.seekCompleted -= OnVideoSeekCompleted;
        }
    }

    private void OnVideoPlayerPrepared(VideoPlayer source)
    {
        OnPrepared?.Invoke();
    }

    private void OnVideoSeekCompleted(VideoPlayer source)
    {
        _seekCompletedEventFired = true;
    }

    public void Update()
    {
        if (_isRewinding && _videoPlayer != null && _videoPlayer.isPrepared)
        {
            _targetRewindTime -= Time.deltaTime * _rewindSpeed;
            if (_targetRewindTime <= 0.0)
            {
                _targetRewindTime = 0.0;
                _isRewinding = false;
                _customPlaybackSpeed = 0f;
                Pause();
                Debug.Log("[VideoPlayerManager] Отмотка завершена: достигнуто начало видео.");
            }

            if (!IsSeeking)
            {
                Seek(_targetRewindTime);
            }
        }

        if (IsSeeking && _videoPlayer != null && _videoPlayer.isPrepared)
        {
            bool timeHasMoved = Math.Abs(_videoPlayer.time - _preSeekTime) > 0.1;
            bool reachedTarget = Math.Abs(_videoPlayer.time - _targetSeekTime) < 1.0;

            if ((_seekCompletedEventFired && timeHasMoved) || reachedTarget)
            {
                IsSeeking = false;
                _seekCompletedEventFired = false;
                if (_wasPlayingBeforeSeek && !_isRewinding)
                {
                    _wasPlayingBeforeSeek = false;
                    Play();
                }
            }
        }
    }

    public void JumpToTime(double timeInSeconds)
    {
        if (_isRewinding)
        {
            _targetRewindTime = timeInSeconds;
        }
        Seek(timeInSeconds);
        Debug.Log($"[VideoPlayerManager] Переход на время: {timeInSeconds} сек.");
    }

    public void Play()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Play();
        }
    }

    public void Pause()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Pause();
        }
    }

    public void Seek(double seconds)
    {
        if (_videoPlayer != null)
        {
            if (!IsSeeking)
            {
                _wasPlayingBeforeSeek = IsPlaying;
                _preSeekTime = _videoPlayer.time;
            }
            IsSeeking = true;
            _targetSeekTime = seconds;
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
            _videoPlayer.time = targetTime;
        }
    }

    /// <summary>
    /// Sets the playback speed.
    /// </summary>
    public void SetPlaybackSpeed(float speed)
    {
        PlaybackSpeed = speed;
    }

    private void StartRewinding(double speed)
    {
        if (_videoPlayer == null) return;
        _isRewinding = true;
        _rewindSpeed = speed;
        _targetRewindTime = _videoPlayer.time;
        _videoPlayer.Pause();
        Debug.Log($"[VideoPlayerManager] Начата отмотка назад со скоростью: {speed}x");
    }

    private void StopRewinding()
    {
        _isRewinding = false;
    }
}
