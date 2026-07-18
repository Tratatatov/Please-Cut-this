using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoCutManager : MonoBehaviour
{
    [Header("Настройки плеера")]
    [Tooltip("Ссылка на VideoPlayerManager (рекомендуется)")]
    public VideoPlayerManager videoPlayerManager;

    [Tooltip("Ссылка на компонент VideoPlayer (устарело, используется если VideoPlayerManager не назначен)")]
    public VideoPlayer videoPlayer;

    [Header("Список вырезанных фрагментов")]
    [Tooltip("Добавьте сюда интервалы, которые нужно игнорировать при просмотре")]
    public List<SkipInterval> intervalsToSkip = new List<SkipInterval>();

    [Tooltip("Небольшое смещение (в секундах) после конца интервала при прыжке, чтобы избежать зацикливания из-за погрешности точности кадров видео.")]
    public float skipBufferTime = 0.05f;

    /// <summary>
    /// Событие, вызываемое при изменении списка интервалов (добавление, удаление, очистка)
    /// </summary>
    public event Action OnIntervalsChanged;

    private void Start()
    {
        // Инициализация VideoPlayerManager, если он не назначен вручную, но назначен videoPlayer
        if (videoPlayerManager == null && videoPlayer != null)
        {
            videoPlayerManager = videoPlayer.GetComponent<VideoPlayerManager>();
            if (videoPlayerManager == null)
            {
                videoPlayerManager = videoPlayer.gameObject.AddComponent<VideoPlayerManager>();
                videoPlayerManager.videoPlayer = videoPlayer;
            }
        }
        else if (videoPlayerManager != null && videoPlayer == null)
        {
            videoPlayer = videoPlayerManager.VideoPlayer;
        }
    }

    private void Update()
    {
        // Если менеджер не назначен/не инициализирован, видео не воспроизводится или идет перемотка, ничего не делаем
        if (videoPlayerManager == null || !videoPlayerManager.IsPlaying || videoPlayerManager.IsSeeking)
            return;

        double currentTime = videoPlayerManager.CurrentTime;

        // Проверяем все интервалы
        for (int i = 0; i < intervalsToSkip.Count; i++)
        {
            var interval = intervalsToSkip[i];
            // Если текущее время попало внутрь интервала, который нужно пропустить
            if (currentTime >= interval.startTime && currentTime < interval.endTime)
            {
                Debug.Log($"Пропуск фрагмента с {interval.startTime} сек по {interval.endTime} сек. (ТЕСТ: Переход в середину видео)");

                // ТЕСТ: телепортируем в середину видео вместо конца отрезка
                float middleTime = (float)(videoPlayerManager.Duration / 2.0);
                videoPlayerManager.JumpToTime(interval.endTime + 0.02f);

                // Прерываем цикл, так как перемотка уже инициирована
                break;
            }
        }
    }

    private bool _isWaitingForEnd = false;
    private double _pendingStartTime = 0;

    /// <summary>
    /// Метод для одной кнопки: первое нажатие ставит первую точку, второе — вторую (и автоматически меняет их местами, если нужно).
    /// </summary>
    public void ToggleIntervalPoint()
    {
        if (videoPlayerManager != null)
        {
            ToggleIntervalPoint(videoPlayerManager.CurrentTime);
        }
    }

    /// <summary>
    /// Метод для одной кнопки по указанному времени.
    /// </summary>
    public void ToggleIntervalPoint(double time)
    {
        if (!_isWaitingForEnd)
        {
            // Ставим начало
            _pendingStartTime = time;
            _isWaitingForEnd = true;
            Debug.Log($"Cat-интервал: задана первая точка на {time} сек. Ожидание второй точки...");
        }
        else
        {
            // Ставим конец
            double startTime = _pendingStartTime;
            double endTime = time;

            // Если конец меньше начала, меняем их местами
            if (endTime < startTime)
            {
                startTime = time;
                endTime = _pendingStartTime;
                Debug.Log($"Cat-интервал: время перепутано ({endTime} < {startTime}), меняем местами.");
            }

            if (startTime == endTime)
            {
                Debug.LogWarning("Cat-интервал: начало и конец совпадают. Интервал не добавлен.");
                _isWaitingForEnd = false;
                return;
            }

            AddCutInterval(startTime, endTime);
            _isWaitingForEnd = false;
            Debug.Log($"Cat-интервал: добавлен фрагмент с {startTime} сек по {endTime} сек.");
        }
    }

    /// <summary>
    /// Задать начало вырезаемого интервала (используется текущее время видео).
    /// </summary>
    public void SetIntervalStart()
    {
        if (videoPlayerManager != null)
        {
            SetIntervalStart(videoPlayerManager.CurrentTime);
        }
    }

    /// <summary>
    /// Задать начало вырезаемого интервала (по указанному времени).
    /// </summary>
    public void SetIntervalStart(double time)
    {
        _pendingStartTime = time;
        _isWaitingForEnd = true;
        Debug.Log($"Cat-интервал: задано начало на {time} сек. Ожидание конца интервала...");
    }

    /// <summary>
    /// Задать конец вырезаемого интервала (текущее время видео) и автоматически добавить его.
    /// </summary>
    public void SetIntervalEnd()
    {
        if (videoPlayerManager != null)
        {
            SetIntervalEnd(videoPlayerManager.CurrentTime);
        }
    }

    /// <summary>
    /// Задать конец вырезаемого интервала (по указанному времени) и автоматически добавить его.
    /// </summary>
    public void SetIntervalEnd(double time)
    {
        if (!_isWaitingForEnd)
        {
            Debug.LogWarning("Cat-интервал: Сначала необходимо задать начало интервала (SetIntervalStart)!");
            return;
        }

        double startTime = _pendingStartTime;
        double endTime = time;

        if (endTime < startTime)
        {
            startTime = time;
            endTime = _pendingStartTime;
            Debug.Log($"Cat-интервал: время перепутано, меняем местами.");
        }

        if (startTime == endTime)
        {
            Debug.LogWarning("Cat-интервал: начало и конец совпадают.");
            _isWaitingForEnd = false;
            return;
        }

        AddCutInterval(startTime, endTime);
        _isWaitingForEnd = false;
        Debug.Log($"Cat-интервал: добавлен фрагмент с {startTime} сек по {endTime} сек.");
    }

    /// <summary>
    /// Метод для динамического добавления вырезанного куска (например, по нажатию кнопки в UI)
    /// </summary>
    public void AddCutInterval(double startSeconds, double endSeconds)
    {
        if (startSeconds >= endSeconds)
        {
            Debug.LogWarning("VideoCutManager: Время начала выреза должно быть меньше времени конца!");
            return;
        }
        intervalsToSkip.Add(new SkipInterval
        {
            startTime = startSeconds,
            endTime = endSeconds
        });
        // Желательно отсортировать список по времени начала, чтобы интервалы шли по порядку
        intervalsToSkip.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        OnIntervalsChanged?.Invoke();
    }

    /// <summary>
    /// Метод для удаления интервала (отмена вырезания)
    /// </summary>
    public void RemoveCutInterval(SkipInterval intervalToRemove)
    {
        if (intervalsToSkip.Remove(intervalToRemove))
        {
            OnIntervalsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Очистить все вырезы (вернуть исходное видео)
    /// </summary>
    public void ClearAllCuts()
    {
        intervalsToSkip.Clear();
        OnIntervalsChanged?.Invoke();
    }
}

