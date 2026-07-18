using UnityEngine;
using UnityEngine.UI;

public class VideoTimelineUI : MonoBehaviour
{
    private VideoPlayerManager videoPlayerManager;
    private VideoCutManager videoCutManager;

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

    [Header("Управление воспроизведением")]
    [Tooltip("Кнопка перемотки назад (меняет скорость на отрицательную)")]
    public Button rewindBackButton;

    [Tooltip("Список доступных скоростей отмотки назад (отрицательные значения)")]
    public float[] rewindSpeeds = new float[] { -1f, -2f, -4f };

    [Tooltip("Кнопка изменения скорости воспроизведения (перемотки вперед)")]
    public Button changeSpeedButton;

    [Tooltip("Список доступных скоростей перемотки вперед (положительные значения)")]
    public float[] forwardSpeeds = new float[] { 1f, 1.5f, 2f, 3f };

    [Tooltip("Текстовый компонент для отображения текущей скорости (необязательно)")]
    public TMPro.TMP_Text speedTextTMP;

    private int _currentRewindSpeedIndex = 0;
    private int _currentForwardSpeedIndex = 0;

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
            rewindBackButton.onClick.AddListener(OnRewindBackButtonClicked);
        }

        if (changeSpeedButton != null)
        {
            changeSpeedButton.onClick.AddListener(OnChangeSpeedButtonClicked);
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

        if (rewindBackButton != null)
        {
            rewindBackButton.onClick.RemoveListener(OnRewindBackButtonClicked);
        }

        if (changeSpeedButton != null)
        {
            changeSpeedButton.onClick.RemoveListener(OnChangeSpeedButtonClicked);
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
            _currentForwardSpeedIndex = 0;
        }
        UpdateSpeedUI();
    }

    private void OnRewindBackButtonClicked()
    {
        if (videoPlayerManager != null && rewindSpeeds != null && rewindSpeeds.Length > 0)
        {
            if (videoPlayerManager.PlaybackSpeed >= 0f)
            {
                _currentRewindSpeedIndex = 0;
            }
            else
            {
                _currentRewindSpeedIndex = (_currentRewindSpeedIndex + 1) % rewindSpeeds.Length;
            }

            float targetSpeed = rewindSpeeds[_currentRewindSpeedIndex];
            videoPlayerManager.PlaybackSpeed = targetSpeed;
            UpdateSpeedUI();
        }
    }

    private void OnChangeSpeedButtonClicked()
    {
        if (videoPlayerManager != null && forwardSpeeds != null && forwardSpeeds.Length > 0)
        {
            if (videoPlayerManager.PlaybackSpeed <= 0f)
            {
                _currentForwardSpeedIndex = 0;
            }
            else
            {
                _currentForwardSpeedIndex = (_currentForwardSpeedIndex + 1) % forwardSpeeds.Length;
            }

            float targetSpeed = forwardSpeeds[_currentForwardSpeedIndex];
            videoPlayerManager.PlaybackSpeed = targetSpeed;
            UpdateSpeedUI();
        }
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
