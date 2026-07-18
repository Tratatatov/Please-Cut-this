using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerManager : MonoBehaviour
{
    [Tooltip("Ссылка на компонент VideoPlayer. Если не назначен, попытается найти его на этом же GameObject.")]
    public VideoPlayer videoPlayer;

    /// <summary>
    /// Current playback time in seconds.
    /// </summary>
    public double CurrentTime
    {
        get => videoPlayer != null ? videoPlayer.time : 0.0;
        set
        {
            if (videoPlayer != null)
            {
                videoPlayer.time = value;
            }
        }
    }

    /// <summary>
    /// Total duration of the video in seconds.
    /// </summary>
    public double Duration => videoPlayer != null ? videoPlayer.length : 0.0;

    /// <summary>
    /// Is the video currently playing.
    /// </summary>
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

    /// <summary>
    /// Has the video player successfully prepared the video source.
    /// </summary>
    public bool IsPrepared => videoPlayer != null && videoPlayer.isPrepared;

    /// <summary>
    /// Direct access to the underlying VideoPlayer component.
    /// </summary>
    public VideoPlayer VideoPlayer => videoPlayer;

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

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnVideoPlayerPrepared;
            videoPlayer.seekCompleted += OnVideoSeekCompleted;
        }
        else
        {
            Debug.LogWarning("VideoPlayerManager: Компонент VideoPlayer не назначен и не найден на этом же GameObject! Пожалуйста, назначьте его в инспекторе.", this);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPlayerPrepared;
            videoPlayer.seekCompleted -= OnVideoSeekCompleted;
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

    private void Update()
    {
        if (IsSeeking && videoPlayer != null && videoPlayer.isPrepared)
        {
            // Мы завершаем состояние перемотки только тогда, когда:
            // 1. Событие seekCompleted было вызвано и время сдвинулось с места начала
            // ИЛИ 2. Текущее время уже очень близко к цели (в пределах 1 секунды)
            bool timeHasMoved = Math.Abs(videoPlayer.time - _preSeekTime) > 0.1;
            bool reachedTarget = Math.Abs(videoPlayer.time - _targetSeekTime) < 1.0;

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

    /// <summary>
    /// Перепрыгнуть на указанное время в секундах (с возможностью дробной части)
    /// </summary>
    /// <param name="timeInSeconds">Время в секундах</param>
    public void JumpToTime(double timeInSeconds)
    {
        Seek(timeInSeconds);
        Debug.Log($"[VideoPlayerManager] Переход на время: {timeInSeconds} сек.");
    }

    /// <summary>
    /// Play/resume the video.
    /// </summary>
    public void Play()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    /// <summary>
    /// Pause the video.
    /// </summary>
    public void Pause()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }
    }

    /// <summary>
    /// Seek to a specific time in seconds.
    /// </summary>
    public void Seek(double seconds)
    {
        if (videoPlayer != null)
        {
            if (!IsSeeking)
            {
                _wasPlayingBeforeSeek = IsPlaying;
                _preSeekTime = videoPlayer.time;
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
            videoPlayer.time = targetTime;
        }
    }
}
