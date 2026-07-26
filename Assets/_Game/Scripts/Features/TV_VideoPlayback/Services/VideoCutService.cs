using System;
using System.Collections.Generic;
using UnityEngine;

public class VideoCutService : IInitializable, IUpdatable
{
    private VideoPlayerService _videoPlayerManager;

    public List<SkipInterval> intervalsToSkip = new List<SkipInterval>();
    public float skipBufferTime = 0.05f;

    public event Action OnIntervalsChanged;

    public event Action OnPendingCutChanged;

    private bool _isWaitingForEnd = false;
    private double _pendingStartTime = 0;

    public bool IsWaitingForEnd => _isWaitingForEnd;
    public double PendingStartTime => _pendingStartTime;

    public void Initialize()
    {
        _videoPlayerManager = ServiceLocator.Get<VideoPlayerService>();
        if (_videoPlayerManager == null)
        {
            Debug.LogError("<color=orange>[VideoCutService]</color> VideoPlayerService не найден в ServiceLocator!");
        }
    }

    public void Update()
    {
        if (_videoPlayerManager == null || !_videoPlayerManager.IsPlaying || _videoPlayerManager.IsSeeking)
            return;

        double currentTime = _videoPlayerManager.CurrentTime;
        bool isRewinding = _videoPlayerManager.PlaybackSpeed < 0f;

        for (int i = 0; i < intervalsToSkip.Count; i++)
        {
            var interval = intervalsToSkip[i];
            if (currentTime >= interval.startTime && currentTime < interval.endTime)
            {
                if (isRewinding)
                {
                    Debug.Log($"<color=orange>[VideoCutService]</color> Пропуск фрагмента в обратную сторону с {interval.endTime:F2} сек по {interval.startTime:F2} сек.");
                    _videoPlayerManager.JumpToTime(interval.startTime - skipBufferTime);
                }
                else
                {
                    Debug.Log($"<color=orange>[VideoCutService]</color> Пропуск фрагмента с {interval.startTime:F2} сек по {interval.endTime:F2} сек.");
                    _videoPlayerManager.JumpToTime(interval.endTime + skipBufferTime);
                }
                break;
            }
        }
    }

    public void ToggleIntervalPoint()
    {
        if (_videoPlayerManager != null)
        {
            ToggleIntervalPoint(_videoPlayerManager.CurrentTime);
        }
    }

    public void ToggleIntervalPoint(double time)
    {
        if (!_isWaitingForEnd)
        {
            _pendingStartTime = time;
            _isWaitingForEnd = true;
            Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: задана первая точка на {time:F2} сек. Ожидание второй точки...");
            OnPendingCutChanged?.Invoke();
        }
        else
        {
            double startTime = _pendingStartTime;
            double endTime = time;

            if (endTime < startTime)
            {
                startTime = time;
                endTime = _pendingStartTime;
                Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: время перепутано ({endTime:F2} < {startTime:F2}), меняем местами.");
            }

            if (startTime == endTime)
            {
                Debug.LogWarning("<color=orange>[VideoCutService]</color> Cat-интервал: начало и конец совпадают. Интервал не добавлен.");
                _isWaitingForEnd = false;
                OnPendingCutChanged?.Invoke();
                return;
            }

            AddCutInterval(startTime, endTime);
            _isWaitingForEnd = false;
            OnPendingCutChanged?.Invoke();
            Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: добавлен фрагмент с {startTime:F2} сек по {endTime:F2} сек.");
        }
    }

    public void SetIntervalStart()
    {
        if (_videoPlayerManager != null)
        {
            SetIntervalStart(_videoPlayerManager.CurrentTime);
        }
    }

    public void SetIntervalStart(double time)
    {
        _pendingStartTime = time;
        _isWaitingForEnd = true;
        Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: задано начало на {time:F2} сек. Ожидание конца интервала...");
        OnPendingCutChanged?.Invoke();
    }

    public void SetIntervalEnd()
    {
        if (_videoPlayerManager != null)
        {
            SetIntervalEnd(_videoPlayerManager.CurrentTime);
        }
    }

    public void SetIntervalEnd(double time)
    {
        if (!_isWaitingForEnd)
        {
            Debug.LogWarning("<color=orange>[VideoCutService]</color> Cat-интервал: Сначала необходимо задать начало интервала (SetIntervalStart)!");
            return;
        }

        double startTime = _pendingStartTime;
        double endTime = time;

        if (endTime < startTime)
        {
            startTime = time;
            endTime = _pendingStartTime;
            Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: время перепутано, меняем местами.");
        }

        if (startTime == endTime)
        {
            Debug.LogWarning("<color=orange>[VideoCutService]</color> Cat-интервал: начало и конец совпадают.");
            _isWaitingForEnd = false;
            OnPendingCutChanged?.Invoke();
            return;
        }

        AddCutInterval(startTime, endTime);
        _isWaitingForEnd = false;
        OnPendingCutChanged?.Invoke();
        Debug.Log($"<color=orange>[VideoCutService]</color> Cat-интервал: добавлен фрагмент с {startTime:F2} сек по {endTime:F2} сек.");
    }

    public void AddCutInterval(double startSeconds, double endSeconds)
    {
        if (startSeconds >= endSeconds)
        {
            Debug.LogWarning("<color=orange>[VideoCutService]</color> Время начала выреза должно быть меньше времени конца!");
            return;
        }
        intervalsToSkip.Add(new SkipInterval
        {
            startTime = startSeconds,
            endTime = endSeconds
        });
        
        intervalsToSkip.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        OnIntervalsChanged?.Invoke();
    }

    public void RemoveCutInterval(SkipInterval intervalToRemove)
    {
        if (intervalsToSkip.Remove(intervalToRemove))
        {
            OnIntervalsChanged?.Invoke();
        }
    }

    public void ClearAllCuts()
    {
        intervalsToSkip.Clear();
        _isWaitingForEnd = false;
        OnPendingCutChanged?.Invoke();
        OnIntervalsChanged?.Invoke();
    }
}
