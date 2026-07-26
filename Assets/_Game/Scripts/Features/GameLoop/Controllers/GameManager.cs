using Core.Services;
using GamePlay.Data;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.Controllers
{
    /// <summary>
    /// Главный менеджер сцены (MonoBehaviour) для управления игровым процессом и параметрами приложения.
    /// Перенаправляет вызовы и конфигурации в C#-сервис GameLoopController.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Основные конфигурации и ссылки")]
        [SerializeField] private DayScheduleConfig _schedule;
        [SerializeField] private GameControlsConfig _controlsConfig;
        [SerializeField] private TV _tv;
        [SerializeField] private Material _tvOnMaterial;
        [SerializeField] private Material _tvReverseOnMaterial;

        [Header("[DEBUG] Настройки отладки")]
        [SerializeField] private bool _isDebugMode = false;
        [Range(0.1f, 10f)]
        [SerializeField] private float _timeScale = 1.0f;
        [SerializeField] private VideotapeConfig _debugVideotapeConfig;

        public bool IsDebugMode => _isDebugMode;
        public float TimeScale => _timeScale;
        public DayScheduleConfig Schedule => _schedule;
        public VideotapeConfig DebugVideotapeConfig => _isDebugMode ? _debugVideotapeConfig : null;
        public TV Tv => _tv;
        public Material TvOnMaterial => _tvOnMaterial;
        public Material TvReverseOnMaterial => _tvReverseOnMaterial;
        public GameControlsConfig ControlsConfig => _controlsConfig;

        public ClientDataConfig CurrentClient => GameLoopController?.CurrentClient;
        public bool IsGameStarted => GameLoopController != null && GameLoopController.IsGameStarted;
        public CassetteState CurrentCassetteState => GameLoopController != null ? GameLoopController.CurrentCassetteState : CassetteState.None;

        private GameLoopController GameLoopController => ServiceLocator.Get<GameLoopController>();

        public void Initialize()
        {
        }

        private void OnEnable()
        {
            if (_isDebugMode)
            {
                Time.timeScale = _timeScale;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1.0f;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _isDebugMode)
            {
                Time.timeScale = _timeScale;
            }
        }

        /// <summary>
        /// Изменение скорости работы проекта (Time.scale).
        /// </summary>
        public void SetTimeScale(float scale)
        {
            _timeScale = Mathf.Max(0f, scale);
            if (_isDebugMode)
            {
                Time.timeScale = _timeScale;
            }
        }

        public void CallClient()
        {
            GameLoopController?.CallClient();
        }

        public void StartGame()
        {
            GameLoopController?.StartGame();
        }

        public void SpawnNextClient()
        {
            GameLoopController?.SpawnNextClient();
        }

        public void HandoverTapeToClient()
        {
            GameLoopController?.HandoverTapeToClient();
        }

        public void DismissCurrentClient()
        {
            GameLoopController?.DismissCurrentClient();
        }

        public void LaunchDebugVideotape(VideotapeConfig videotapeConfig)
        {
            GameLoopController?.LaunchDebugVideotape(videotapeConfig);
        }

        public void EjectCassette()
        {
            GameLoopController?.EjectCassette();
        }
    }
}
