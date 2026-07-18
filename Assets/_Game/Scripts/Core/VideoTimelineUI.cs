using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VideoTimelineUI : MonoBehaviour
{
    [Header("Менеджеры")]
    [Tooltip("Ссылка на менеджер воспроизведения видео")]
    public VideoPlayerManager videoPlayerManager;

    [Tooltip("Ссылка на менеджер вырезов (если есть)")]
    public VideoCutManager videoCutManager;

    [Header("UI Компоненты")]
    [Tooltip("Слайдер прогресса воспроизведения (если пустой, попытается найти на этом же GameObject)")]
    public Slider progressSlider;

    [Tooltip("Компонент для отображения текста времени (TextMeshPro)")]
    public TMPro.TMP_Text timeTextTMP;

    [Tooltip("Кнопка воспроизведения/паузы")]
    public Button playPauseButton;

    [Tooltip("Кнопка для установки точек интервала (Cat-интервал)")]
    public Button setCutIntervalButton;

    [Tooltip("Спрайт иконки воспроизведения (Play)")]
    public Sprite playSprite;

    [Tooltip("Спрайт иконки паузы (Pause)")]
    public Sprite pauseSprite;

    private bool _isDraggingSlider = false;
    private bool _wasPlayingBeforeDrag = false;

    private void Start()
    {
        // Настройка дефолтных UI компонентов
        if (progressSlider == null)
        {
            progressSlider = GetComponent<Slider>();
        }

        if (progressSlider != null)
        {
            progressSlider.onValueChanged.AddListener(OnSliderValueChanged);

            // Навешиваем EventTrigger прямо на объект слайдера для гарантированного перехвата событий мыши/тача
            EventTrigger trigger = progressSlider.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = progressSlider.gameObject.AddComponent<EventTrigger>();
            }

            // Добавляем PointerDown триггер
            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { OnSliderPointerDown((PointerEventData)data); });
            trigger.triggers.Add(entryDown);

            // Добавляем PointerUp триггер
            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { OnSliderPointerUp((PointerEventData)data); });
            trigger.triggers.Add(entryUp);
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(OnPlayPauseButtonClicked);
        }

        if (setCutIntervalButton != null)
        {
            setCutIntervalButton.onClick.AddListener(OnSetCutIntervalButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (progressSlider != null)
        {
            progressSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveListener(OnPlayPauseButtonClicked);
        }

        if (setCutIntervalButton != null)
        {
            setCutIntervalButton.onClick.RemoveListener(OnSetCutIntervalButtonClicked);
        }
    }

    private void OnSetCutIntervalButtonClicked()
    {
        if (videoCutManager != null)
        {
            videoCutManager.ToggleIntervalPoint();
        }
    }

    private void Update()
    {
        if (videoPlayerManager == null) 
            return;

        // Обновляем значение слайдера в соответствии с видео, если пользователь его не перетаскивает
        if (!_isDraggingSlider && progressSlider != null && videoPlayerManager.IsPrepared && videoPlayerManager.Duration > 0)
        {
            float progress = (float)(videoPlayerManager.CurrentTime / videoPlayerManager.Duration);
            progressSlider.SetValueWithoutNotify(progress);
        }

        // Обновляем текстовое отображение времени
        UpdateTimeText();

        // Обновляем состояние кнопки паузы/воспроизведения
        UpdatePlayPauseButtonVisual();
    }

    private void OnPlayPauseButtonClicked()
    {
        if (videoPlayerManager == null) 
            return;

        if (videoPlayerManager.IsPlaying)
        {
            videoPlayerManager.Pause();
        }
        else
        {
            videoPlayerManager.Play();
        }
    }

    private void UpdatePlayPauseButtonVisual()
    {
        if (playPauseButton == null) 
            return;

        Image btnImage = playPauseButton.image;
        if (btnImage == null) 
            return;

        bool isPlaying = videoPlayerManager != null && videoPlayerManager.IsPlaying;
        Sprite targetSprite = isPlaying ? pauseSprite : playSprite;

        if (btnImage.sprite != targetSprite)
        {
            btnImage.sprite = targetSprite;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        // Перематываем только во время перетаскивания пользователем
        if (_isDraggingSlider && videoPlayerManager != null && videoPlayerManager.IsPrepared && videoPlayerManager.Duration > 0)
        {
            double targetTime = value * videoPlayerManager.Duration;
            videoPlayerManager.Seek(targetTime);
        }
    }

    private void OnSliderPointerDown(PointerEventData eventData)
    {
        _isDraggingSlider = true;
        if (videoPlayerManager != null)
        {
            _wasPlayingBeforeDrag = videoPlayerManager.IsPlaying;
            if (_wasPlayingBeforeDrag)
            {
                videoPlayerManager.Pause();
            }
        }
    }

    private void OnSliderPointerUp(PointerEventData eventData)
    {
        _isDraggingSlider = false;
        if (videoPlayerManager != null && videoPlayerManager.IsPrepared && videoPlayerManager.Duration > 0 && progressSlider != null)
        {
            double targetTime = progressSlider.value * videoPlayerManager.Duration;
            videoPlayerManager.Seek(targetTime);

            if (_wasPlayingBeforeDrag)
            {
                videoPlayerManager.Play();
            }
        }
    }

    private void UpdateTimeText()
    {
        if (timeTextTMP == null)
            return;

        if (videoPlayerManager == null || !videoPlayerManager.IsPrepared || videoPlayerManager.Duration <= 0)
        {
            timeTextTMP.text = "00:00 / 00:00";
            return;
        }

        // Если пользователь тянет слайдер, показываем время в точке перетаскивания, иначе - текущее время видео
        double displayTime = (_isDraggingSlider && progressSlider != null) 
            ? progressSlider.value * videoPlayerManager.Duration 
            : videoPlayerManager.CurrentTime;

        string currentStr = FormatTime(displayTime);
        string durationStr = FormatTime(videoPlayerManager.Duration);
        timeTextTMP.text = $"{currentStr} / {durationStr}";
    }

    private string FormatTime(double seconds)
    {
        System.TimeSpan time = System.TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)time.TotalHours, time.Minutes, time.Seconds);
        }
        return string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
    }
}
