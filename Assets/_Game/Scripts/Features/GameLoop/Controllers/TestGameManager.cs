using System.Collections.Generic;
using Core.Services;
using GamePlay.Data;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.Controllers
{
    public enum CassetteState
    {
        None,
        ClientGaveCassette,
        InsertingCassette,
        TapeInserted,
        EjectingCassette,
        TapeReadyToReturn
    }

    /// <summary>
    /// Автономный отладочный менеджер (MonoBehaviour) для тестирования игрового процесса.
    /// Позволяет управлять скоростью времени (Time.scale), вручную вызывать клиентов и тестировать кассету.
    /// Никакой основной функционал игры от него не зависит.
    /// </summary>
    public class TestGameManager : MonoBehaviour
    {
        [Header("Скорость игры")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _timeScale = 1.0f;

        [Header("Конфигурации и ссылки")]
        [SerializeField] private DayScheduleConfig _schedule;
        [SerializeField] private VideotapeConfig _debugVideotapeConfig;
        [SerializeField] private TV _tv;
        [SerializeField] private Material _tvOnMaterial;
        [SerializeField] private Material _tvReverseOnMaterial;
        [SerializeField] private GameControlsConfig _controlsConfig;

        private Queue<ClientDataConfig> _clientQueue;
        private ClientDataConfig _currentClient;
        private bool _isGameStarted;
        private CassetteState _cassetteState = CassetteState.None;

        public ClientDataConfig CurrentClient => _currentClient;
        public bool IsGameStarted => _isGameStarted;
        public CassetteState CurrentCassetteState => _cassetteState;

        private ClientsController ClientsController => ServiceLocator.Get<ClientsController>();
        private PlayerViewController PlayerViewController => ServiceLocator.Get<PlayerViewController>();

        public void Initialize()
        {
            _isGameStarted = false;
            _currentClient = null;
            _cassetteState = CassetteState.None;

            EnsureScheduleLoaded();

            KeyCode interactKey = _controlsConfig != null ? _controlsConfig.InteractCassetteKey : KeyCode.E;
            KeyCode ejectKey = _controlsConfig != null ? _controlsConfig.EjectCassetteKey : KeyCode.F;
            KeyCode toggleKey = _controlsConfig != null ? _controlsConfig.ToggleCameraKey : KeyCode.Q;
            KeyCode nextKey = _controlsConfig != null ? _controlsConfig.NextClientKey : KeyCode.N;
            KeyCode dismissKey = _controlsConfig != null ? _controlsConfig.DismissClientKey : KeyCode.X;

            Debug.Log("<color=white>[TestGameManager]</color> Инициализирован. Управление:\n" +
                      $" - Нажмите [{interactKey}]: Взять кассету у клиента / Передать готовую кассету\n" +
                      $" - Нажмите [{ejectKey}]: Извлечь кассету из плеера\n" +
                      $" - Нажмите [Space] или [{nextKey}]: Приход нового клиента\n" +
                      $" - Нажмите [{dismissKey}]: Уход текущего клиента из комнаты\n" +
                      $" - Нажмите [{toggleKey}]: Переключение видов камеры (Комната / ТВ)");

            if (_debugVideotapeConfig != null)
            {
                Debug.Log("<color=white>[TestGameManager]</color> Запуск отладочной кассеты...");
                LaunchDebugVideotape(_debugVideotapeConfig);
            }

            var timelineLogic = ServiceLocator.Get<VideoTimelineUILogic>();
            if (timelineLogic != null)
            {
                timelineLogic.OnFinishEditingClicked -= EjectCassette;
                timelineLogic.OnFinishEditingClicked += EjectCassette;
            }
        }

        private void OnDestroy()
        {
            var timelineLogic = ServiceLocator.Get<VideoTimelineUILogic>();
            if (timelineLogic != null)
            {
                timelineLogic.OnFinishEditingClicked -= EjectCassette;
            }
        }

        private void OnEnable()
        {
            Time.timeScale = _timeScale;
        }

        private void OnDisable()
        {
            Time.timeScale = 1.0f;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
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
            Time.timeScale = _timeScale;
        }

        /// <summary>
        /// Публичный метод для вызова очередного клиента к стойке.
        /// </summary>
        public void CallClient()
        {
            SpawnNextClient();
        }

        public void StartGame()
        {
            _isGameStarted = true;
            Debug.Log("<color=white>[TestGameManager]</color> Старт игры!");
            SpawnNextClient();
        }

        public void SpawnNextClient()
        {
            EnsureScheduleLoaded();

            if (_clientQueue == null || _clientQueue.Count == 0)
            {
                Debug.Log("<color=white>[TestGameManager]</color> Все клиенты из расписания обслужены или расписание не назначено!");
                return;
            }

            _currentClient = _clientQueue.Dequeue();
            _cassetteState = CassetteState.None;

            Debug.Log($"<color=white>[TestGameManager]</color> Приход клиента: {_currentClient.ClientName} (Тип: {_currentClient.ModelType}). Идет к стойке...");

            var playerVC = PlayerViewController;
            playerVC?.SwitchToRoomView();

            var clientsCtrl = ClientsController;
            clientsCtrl?.StartNewClient(_currentClient, () =>
            {
                Debug.Log($"<color=white>[TestGameManager]</color> Клиент {_currentClient.ClientName} подошел к стойке! Включаем ClientCamera и запускаем диалог.");
                PlayerViewController?.SwitchToClientDialogueView(_currentClient);
                _cassetteState = CassetteState.ClientGaveCassette;
            });
        }

        public void HandoverTapeToClient()
        {
            if (_currentClient == null)
            {
                Debug.LogWarning("<color=white>[TestGameManager]</color> Некому передавать кассету: _currentClient не установлен!");
                return;
            }

            Debug.Log($"<color=white>[TestGameManager]</color> Передача кассеты клиенту {_currentClient.ClientName}. Запуск анимации передачи и финального диалога...");

            ClientsController?.ClientView?.PlayTakeAnimation();

            string phraseText = !string.IsNullOrEmpty(_currentClient.SuccessPhrase) 
                ? _currentClient.SuccessPhrase 
                : "Отличная работа, спасибо за готовую кассету!";

            List<Core.Data.DialoguePhrase> returnPhrases = new List<Core.Data.DialoguePhrase>
            {
                new Core.Data.DialoguePhrase(phraseText)
            };

            var dialogueService = ServiceLocator.Get<DialogueService>();
            if (dialogueService != null)
            {
                System.Action onReturnDialogueFinished = null;
                onReturnDialogueFinished = () =>
                {
                    dialogueService.OnDialogueCompleted -= onReturnDialogueFinished;
                    Debug.Log($"<color=white>[TestGameManager]</color> Клиент {_currentClient.ClientName} произнес финальную фразу и направляется к выходу.");
                    DismissCurrentClient();
                };

                dialogueService.OnDialogueCompleted += onReturnDialogueFinished;

                PlayerViewController?.SwitchToClientDialogueView(_currentClient);
                dialogueService.PlayDialogue(_currentClient.ClientName, returnPhrases, _currentClient.PhraseDelay);
            }
            else
            {
                DismissCurrentClient();
            }
        }

        public void DismissCurrentClient()
        {
            if (_currentClient == null)
            {
                Debug.LogWarning("<color=white>[TestGameManager]</color> Нет активного клиента у стойки.");
                return;
            }

            Debug.Log($"<color=white>[TestGameManager]</color> Клиент {_currentClient.ClientName} направляется к выходу...");

            ClientsController?.DismissCurrentClient(() =>
            {
                Debug.Log($"<color=white>[TestGameManager]</color> Клиент {_currentClient.ClientName} покинул комнату.");
                _currentClient = null;
                _cassetteState = CassetteState.None;

                PlayerViewController?.SwitchToRoomView();
            });
        }

        public void LaunchDebugVideotape(VideotapeConfig videotapeConfig)
        {
            if (videotapeConfig == null)
            {
                Debug.LogWarning("<color=white>[TestGameManager]</color> Debug VideotapeConfig не назначен!");
                return;
            }

            var playerVC = PlayerViewController;
            if (playerVC != null)
            {
                playerVC.SwitchToCassetteInsertingView(() =>
                {
                    TVRendererService tvService = _tv != null ? _tv.TVRendererService : null;
                    if (tvService != null && _tvOnMaterial != null)
                    {
                        tvService.SetScreenMaterial(_tvOnMaterial, _tvReverseOnMaterial);
                    }
                    else if (_tv != null && _tvOnMaterial != null)
                    {
                        _tv.SetScreenMaterial(_tvOnMaterial, _tvReverseOnMaterial);
                    }

                    var levelMediator = ServiceLocator.Get<CutLevelMediator>();
                    levelMediator?.LoadLevel(videotapeConfig);

                    var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
                    videoPlayerService?.Play();

                    var gameStateManager = ServiceLocator.Get<GameStateManager>();
                    gameStateManager?.SwitchState<MontageGameState>();

                    _cassetteState = CassetteState.TapeInserted;
                    Debug.Log($"<color=white>[TestGameManager]</color> Имитация вставки завершена: кассета '{videotapeConfig.name}' запущена на ТВ.");
                }, 2.0f);
            }
            else
            {
                Debug.LogError("<color=white>[TestGameManager]</color> Cannot switch to CassetteInsertingView: PlayerViewController is NULL!");
            }
        }

        private void Update()
        {
            if (Mathf.Abs(Time.timeScale - _timeScale) > 0.001f)
            {
                Time.timeScale = _timeScale;
            }

            KeyCode interactKey = _controlsConfig != null ? _controlsConfig.InteractCassetteKey : KeyCode.E;
            KeyCode ejectKey = _controlsConfig != null ? _controlsConfig.EjectCassetteKey : KeyCode.F;
            KeyCode nextKey = _controlsConfig != null ? _controlsConfig.NextClientKey : KeyCode.N;
            KeyCode dismissKey = _controlsConfig != null ? _controlsConfig.DismissClientKey : KeyCode.X;

            var playerVC = PlayerViewController;
            if (playerVC != null && playerVC.IsControlsLocked)
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                if (_cassetteState == CassetteState.ClientGaveCassette)
                {
                    Debug.Log($"<color=white>[TestGameManager]</color> Нажата клавиша [{interactKey}]: Берем кассету у клиента и запускаем анимацию вставки в плеер...");
                    _cassetteState = CassetteState.InsertingCassette;

                    PlayerViewController?.SwitchToCassetteInsertingView(() =>
                    {
                        _cassetteState = CassetteState.TapeInserted;
                        Debug.Log("<color=white>[TestGameManager]</color> Анимация вставки кассеты завершена. Включаем TV и режим монтажа.");

                        if (_tvOnMaterial != null && _tv != null)
                        {
                            _tv.SetScreenMaterial(_tvOnMaterial, _tvReverseOnMaterial);
                            
                            var levelMediator = ServiceLocator.Get<CutLevelMediator>();
                            if (_currentClient != null && _currentClient.LevelData != null)
                            {
                                levelMediator?.LoadLevel(_currentClient.LevelData);
                            }
                            else if (_debugVideotapeConfig != null)
                            {
                                levelMediator?.LoadLevel(_debugVideotapeConfig);
                            }

                            var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
                            videoPlayerService?.Play();
                        }

                        var gameStateManager = ServiceLocator.Get<GameStateManager>();
                        gameStateManager?.SwitchState<MontageGameState>();
                    }, 2.0f);
                }
                else if (_cassetteState == CassetteState.TapeReadyToReturn)
                {
                    Debug.Log($"<color=white>[TestGameManager]</color> Нажата клавиша [{interactKey}]: Передаем готовую кассету клиенту.");
                    HandoverTapeToClient();
                    _cassetteState = CassetteState.None;
                }
                else if (_currentClient != null && _cassetteState == CassetteState.None)
                {
                    Debug.Log($"<color=white>[TestGameManager]</color> Нажата клавиша [{interactKey}]: Берем кассету у клиента...");
                    _cassetteState = CassetteState.ClientGaveCassette;
                }
            }

            if (Input.GetKeyDown(ejectKey))
            {
                EjectCassette();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(nextKey))
            {
                if (!_isGameStarted)
                {
                    StartGame();
                }
                else if (_currentClient == null)
                {
                    SpawnNextClient();
                }
                else
                {
                    Debug.Log($"<color=white>[TestGameManager]</color> Клиент уже у стойки! Состояние кассеты: {_cassetteState}.");
                }
            }

            if (Input.GetKeyDown(dismissKey))
            {
                if (_currentClient != null)
                {
                    DismissCurrentClient();
                }
            }
        }

        private void EnsureScheduleLoaded()
        {
            if (_clientQueue != null) return;

            if (_schedule != null && _schedule.Clients != null)
            {
                _clientQueue = new Queue<ClientDataConfig>(_schedule.Clients);
            }
            else
            {
                _clientQueue = new Queue<ClientDataConfig>();
            }
        }

        public void EjectCassette()
        {
            if (_cassetteState == CassetteState.TapeInserted)
            {
                Debug.Log("<color=white>[TestGameManager]</color> Извлекаем кассету из плеера (по клавише или кнопке UI)...");
                _cassetteState = CassetteState.EjectingCassette;

                if (_tv != null)
                {
                    _tv.ResetToDefaultMaterial();
                }

                PlayerViewController?.SwitchToCassetteEjectingView(() =>
                {
                    _cassetteState = CassetteState.TapeReadyToReturn;
                    Debug.Log("<color=white>[TestGameManager]</color> Кассета извлечена. Возврат к виду комнаты. Нажмите E для передачи клиенту.");
                    PlayerViewController?.SwitchToRoomView();
                }, 2.0f);
            }
        }
    }
}
