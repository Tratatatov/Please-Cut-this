using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VideoTimelineUI : MonoBehaviour
{
    private VideoPlayerManager videoPlayerManager;
    private VideoCutManager videoCutManager;

    [Header("UI Компоненты")]
    [Tooltip("Слайдер прогресса воспроизведения (если пустой, попытается найти на этом же GameObject)")]
    public Slider progressSlider;

    [Tooltip("Компонент для отображения текста времени (TextMeshPro)")]
    public TMP_Text timeTextTMP;

    [Tooltip("Кнопка воспроизведения/паузы")]
    public Button playPauseButton;

    [Tooltip("Кнопка для установки точек интервала (Cat-интервал)")]
    public Button setCutIntervalButton;

    [Tooltip("Спрайт иконки воспроизведения (Play)")]
    public Sprite playSprite;

    [Tooltip("Спрайт иконки паузы (Pause)")]
    public Sprite pauseSprite;

    [Header("Управление воспроизведением")]
    [Tooltip("Кнопка перемотки назад (зажмите для отмотки)")]
    public Button rewindBackButton;

    [Tooltip("Скорость отмотки назад (отрицательное значение, например -3)")]
    public float rewindHoldSpeed = -3f;

    [Tooltip("Кнопка быстрой перемотки вперед (зажмите для ускорения)")]
    public Button changeSpeedButton;

    [Tooltip("Скорость быстрой перемотки вперед (положительное значение, например 3)")]
    public float forwardHoldSpeed = 3f;

    [Tooltip("Текстовый компонент для отображения текущей скорости (необязательно)")]
    public TMPro.TMP_Text speedTextTMP;

    private float _speedBeforeHold = 0f;
    private bool _isHoldingButton = false;

    private void Start()
    {
        // Получаем зависимости через ServiceLocator
        videoPlayerManager = ServiceLocator.Get<VideoPlayerManager>();
        videoCutManager = ServiceLocator.Get<VideoCutManager>();

        // Настройка дефолтных UI компонентов
        if (progressSlider == null)
        {
            progressSlider = GetComponent<Slider>();
        }

        if (progressSlider != null)
        {
            // Отключаем интерактивность слайдера, делая его read-only индикатором
            progressSlider.interactable = false;
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(OnPlayPauseButtonClicked);
        }

        if (setCutIntervalButton != null)
        {
            setCutIntervalButton.onClick.AddListener(OnSetCutIntervalButtonClicked);
        }

        if (rewindBackButton != null)
        {
            SetupButtonHold(rewindBackButton, OnRewindPointerDown, OnHoldPointerUp);
        }

        if (changeSpeedButton != null)
        {
            SetupButtonHold(changeSpeedButton, OnForwardPointerDown, OnHoldPointerUp);
        }

        UpdateSpeedUI();
    }

    private void OnDestroy()
    {
        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveListener(OnPlayPauseButtonClicked);
        }

        if (setCutIntervalButton != null)
        {
            setCutIntervalButton.onClick.RemoveListener(OnSetCutIntervalButtonClicked);
        }
    }

    private void SetupButtonHold(Button button, System.Action onDown, System.Action onUp)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => { onDown(); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((data) => { onUp(); });
        trigger.triggers.Add(entryUp);
    }

    private void OnRewindPointerDown()
    {
        if (videoPlayerManager != null)
        {
            if (!_isHoldingButton)
            {
                _speedBeforeHold = videoPlayerManager.PlaybackSpeed;
                _isHoldingButton = true;
            }
            videoPlayerManager.PlaybackSpeed = rewindHoldSpeed;
            UpdateSpeedUI();
        }
    }

    private void OnForwardPointerDown()
    {
        if (videoPlayerManager != null)
        {
            if (!_isHoldingButton)
            {
                _speedBeforeHold = videoPlayerManager.PlaybackSpeed;
                _isHoldingButton = true;
            }
            videoPlayerManager.PlaybackSpeed = forwardHoldSpeed;
            UpdateSpeedUI();
        }
    }

    private void OnHoldPointerUp()
    {
        if (videoPlayerManager != null && _isHoldingButton)
        {
            videoPlayerManager.PlaybackSpeed = _speedBeforeHold;
            _isHoldingButton = false;
            UpdateSpeedUI();
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

        // Обновляем значение слайдера в соответствии с видео
        if (progressSlider != null && videoPlayerManager.IsPrepared && videoPlayerManager.Duration > 0)
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

        if (videoPlayerManager.PlaybackSpeed != 0f)
        {
            videoPlayerManager.PlaybackSpeed = 0f;
        }
        else
        {
            videoPlayerManager.PlaybackSpeed = 1f;
        }
        _speedBeforeHold = videoPlayerManager.PlaybackSpeed;
        UpdateSpeedUI();
    }

    private void UpdateSpeedUI()
    {
        if (speedTextTMP != null && videoPlayerManager != null)
        {
            speedTextTMP.text = $"{videoPlayerManager.PlaybackSpeed}x";
        }
    }

    private void UpdatePlayPauseButtonVisual()
    {
        if (playPauseButton == null) 
            return;

        Image btnImage = playPauseButton.image;
        if (btnImage == null) 
            return;

        bool isPlaying = videoPlayerManager != null && videoPlayerManager.PlaybackSpeed != 0f;
        Sprite targetSprite = isPlaying ? pauseSprite : playSprite;

        if (btnImage.sprite != targetSprite)
        {
            btnImage.sprite = targetSprite;
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

        double displayTime = videoPlayerManager.CurrentTime;

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
