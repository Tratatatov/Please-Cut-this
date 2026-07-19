using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class VideoTimelineUIView : MonoBehaviour
{
    [Header("UI Компоненты")]
    [Tooltip("Слайдер прогресса воспроизведения")]
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

    [Header("Настройки ускорения при удержании")]
    [Tooltip("Включить плавное ускорение при удержании кнопок перемотки")]
    public bool enableHoldAcceleration = false;

    [Tooltip("Начальная скорость при удержании кнопки перемотки назад (отрицательное значение, например -1)")]
    public float rewindStartSpeed = -1f;

    [Tooltip("Начальная скорость при удержании кнопки перемотки вперед (положительное значение, например 1)")]
    public float forwardStartSpeed = 1f;

    [Tooltip("Время разгона до максимальной скорости (в секундах)")]
    public float accelerationDuration = 2f;

    [Tooltip("Слайдер для изменения скорости воспроизведения (например, от -3 до 3)")]
    public Slider speedSlider;

    [Tooltip("Текстовый компонент для отображения текущей скорости (необязательно)")]
    public TMPro.TMP_Text speedTextTMP;

    public event Action OnPlayPauseClicked;
    public event Action OnSetCutIntervalClicked;
    public event Action<float> OnSpeedSliderValueChangedEvent;
    
    public event Action OnRewindPointerDown;
    public event Action OnForwardPointerDown;
    public event Action OnHoldPointerUp;

    private void Start()
    {
        if (progressSlider == null)
        {
            progressSlider = GetComponent<Slider>();
        }

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
        }

        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener((val) => OnSpeedSliderValueChangedEvent?.Invoke(val));
        }

        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(() => OnPlayPauseClicked?.Invoke());
        }

        if (setCutIntervalButton != null)
        {
            setCutIntervalButton.onClick.AddListener(() => OnSetCutIntervalClicked?.Invoke());
        }

        if (rewindBackButton != null)
        {
            SetupButtonHold(rewindBackButton, () => OnRewindPointerDown?.Invoke(), () => OnHoldPointerUp?.Invoke());
        }

        if (changeSpeedButton != null)
        {
            SetupButtonHold(changeSpeedButton, () => OnForwardPointerDown?.Invoke(), () => OnHoldPointerUp?.Invoke());
        }
    }

    private void SetupButtonHold(Button button, Action onDown, Action onUp)
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

    public void UpdateSpeedUI(float speed)
    {
        if (speedTextTMP != null)
        {
            speedTextTMP.text = $"{speed:0.##}x";
        }

        if (speedSlider != null)
        {
            speedSlider.SetValueWithoutNotify(speed);
        }
    }

    public void UpdatePlayPauseButtonVisual(bool isPlaying)
    {
        if (playPauseButton == null) return;

        Image btnImage = playPauseButton.image;
        if (btnImage == null) return;

        Sprite targetSprite = isPlaying ? pauseSprite : playSprite;

        if (btnImage.sprite != targetSprite)
        {
            btnImage.sprite = targetSprite;
        }
    }

    public void UpdateTimeText(double currentTime, double duration)
    {
        if (timeTextTMP == null) return;

        if (duration <= 0)
        {
            timeTextTMP.text = "00:00 / 00:00";
            return;
        }

        string currentStr = FormatTime(currentTime);
        string durationStr = FormatTime(duration);
        timeTextTMP.text = $"{currentStr} / {durationStr}";
    }

    public void UpdateProgressSlider(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(progress);
        }
    }

    private string FormatTime(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)time.TotalHours, time.Minutes, time.Seconds);
        }
        return string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
    }
}
