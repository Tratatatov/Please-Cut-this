using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamePlay.View
{
    /// <summary>
    /// View-компонент кнопок управления видеоплеером, слайдера времени и маркеров выреза.
    /// </summary>
    public class VideoPlayerControlsUIView : MonoBehaviour
    {
        [Header("Отображение времени и слайдер")]
        [Tooltip("Слайдер прогресса воспроизведения")]
        public Slider progressSlider;

        [Tooltip("Компонент для отображения текста времени (TextMeshPro)")]
        public TMP_Text timeTextTMP;

        [Header("Кнопки управления")]
        [Tooltip("Кнопка воспроизведения/паузы")]
        public Button playPauseButton;

        [Tooltip("Кнопка для установки точек интервала (Cat-интервал)")]
        public Button setCutIntervalButton;

        [Tooltip("Кнопка завершения редактирования / извлечения кассеты")]
        public Button finishEditingButton;

        [Tooltip("Спрайт иконки воспроизведения (Play)")]
        public Sprite playSprite;

        [Tooltip("Спрайт иконки паузы (Pause)")]
        public Sprite pauseSprite;

        [Header("Перемотка и Скорость")]
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
        public TMP_Text speedTextTMP;

        [Header("Маркеры вырезов")]
        [Tooltip("Контейнер (RectTransform) для спавна элементов маркеров выреза")]
        public RectTransform markerContainer;

        [Tooltip("Префаб маркера выреза")]
        public RectTransform markerPrefab;

        [Tooltip("Кнопка удаления выделенного маркера выреза")]
        public Button deleteSelectedCutButton;

        [Tooltip("Кнопка очистки всех вырезанных отрезков (необязательно)")]
        public Button clearAllCutsButton;

        [Header("Canvas")]
        [Tooltip("Ссылка на Canvas контроля видеоплеера (если не указана, берется с данного объекта или родителя)")]
        [SerializeField] private Canvas _canvas;

        public Canvas Canvas => _canvas;

        public event Action OnPlayPauseClicked;
        public event Action OnSetCutIntervalClicked;
        public event Action OnClearAllCutsClicked;
        public event Action OnFinishEditingClicked;
        public event Action<float> OnSpeedSliderValueChangedEvent;
        
        public event Action OnRewindPointerDown;
        public event Action OnForwardPointerDown;
        public event Action OnHoldPointerUp;

        public void Initialize()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>();
                }
            }

            SetCanvasActive(false);

            if (progressSlider == null)
            {
                progressSlider = GetComponent<Slider>();
            }

            ResetProgressSlider();

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

            if (clearAllCutsButton != null)
            {
                clearAllCutsButton.onClick.AddListener(() => OnClearAllCutsClicked?.Invoke());
            }

            if (finishEditingButton != null)
            {
                finishEditingButton.onClick.AddListener(() => OnFinishEditingClicked?.Invoke());
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

        public void ResetProgressSlider()
        {
            if (progressSlider != null)
            {
                progressSlider.SetValueWithoutNotify(progressSlider.minValue);
            }
        }

        public void SetCanvasActive(bool active)
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(active);
            }
            else
            {
                gameObject.SetActive(active);
            }
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
}
