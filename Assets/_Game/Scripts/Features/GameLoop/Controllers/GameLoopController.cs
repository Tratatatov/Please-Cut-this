using System;
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

    public class GameLoopController : IInitializable, IUpdatable, IDisposableService
    {
        private readonly DayScheduleConfig _schedule;
        private readonly VideotapeConfig _debugVideotapeConfig;
        private readonly TV _tv;
        private readonly Material _tvOnMaterial;
        private readonly Material _tvReverseOnMaterial;
        private readonly GameControlsConfig _controlsConfig;
        private readonly ClientView _clientView;
        private readonly VideoPlayerControlsUIView _videoPlayerControlsView;
        private readonly bool _isDebugMode;

        private Queue<ClientDataConfig> _clientQueue;
        private ClientDataConfig _currentClient;
        private bool _isGameStarted;
        private CassetteState _cassetteState = CassetteState.None;

        public bool IsDebugMode => _isDebugMode;
        public ClientDataConfig CurrentClient => _currentClient;
        public bool IsGameStarted => _isGameStarted;
        public CassetteState CurrentCassetteState => _cassetteState;

        private ClientsController ClientsController => ServiceLocator.Get<ClientsController>();
        private PlayerViewController PlayerViewController => ServiceLocator.Get<PlayerViewController>();

        public GameLoopController(
            DayScheduleConfig schedule = null,
            VideotapeConfig debugVideotapeConfig = null,
            TV tv = null,
            Material tvOnMaterial = null,
            Material tvReverseOnMaterial = null,
            GameControlsConfig controlsConfig = null,
            ClientView clientView = null,
            VideoPlayerControlsUIView videoPlayerControlsView = null,
            bool isDebugMode = false
        )
        {
            _schedule = schedule;
            _debugVideotapeConfig = debugVideotapeConfig;
            _tv = tv;
            _tvOnMaterial = tvOnMaterial;
            _tvReverseOnMaterial = tvReverseOnMaterial;
            _controlsConfig = controlsConfig;
            _clientView = clientView;
            _videoPlayerControlsView = videoPlayerControlsView;
            _isDebugMode = isDebugMode;
        }

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

            string modeLabel = _isDebugMode ? "DEBUG" : "Обычный";
            Debug.Log($"<color=white>[GameLoopController]</color> Инициализирован (Режим: {modeLabel}). Управление:\n" +
                      $" - Нажмите [{interactKey}]: Взять кассету у клиента / Передать готовую кассету\n" +
                      $" - Нажмите [{ejectKey}]: Извлечь кассету из плеера\n" +
                      $" - Нажмите [Space] или [{nextKey}]: Приход нового клиента\n" +
                      $" - Нажмите [{dismissKey}]: Уход текущего клиента из комнаты\n" +
                      $" - Нажмите [{toggleKey}]: Переключение видов камеры (Комната / ТВ)");

            if (_isDebugMode && _debugVideotapeConfig != null)
            {
                Debug.Log("<color=white>[GameLoopController] [DEBUG]</color> Авто-запуск отладочной кассеты...");
                LaunchDebugVideotape(_debugVideotapeConfig);
            }

            var timelineLogic = ServiceLocator.Get<VideoTimelineUILogic>();
            if (timelineLogic != null)
            {
                timelineLogic.OnFinishEditingClicked -= EjectCassette;
                timelineLogic.OnFinishEditingClicked += EjectCassette;
            }
        }

        public void Dispose()
        {
            var timelineLogic = ServiceLocator.Get<VideoTimelineUILogic>();
            if (timelineLogic != null)
            {
                timelineLogic.OnFinishEditingClicked -= EjectCassette;
            }
        }

        public void StartGame()
        {
            _isGameStarted = true;
            Debug.Log("<color=white>[GameLoopController]</color> Старт игры!");
            SpawnNextClient();
        }

        public void CallClient()
        {
            SpawnNextClient();
        }

        public void SpawnNextClient()
        {
            EnsureScheduleLoaded();

            if (_clientQueue == null || _clientQueue.Count == 0)
            {
                Debug.Log("<color=white>[GameLoopController]</color> Все клиенты из расписания обслужены или расписание не назначено!");
                return;
            }

            _currentClient = _clientQueue.Dequeue();
            _cassetteState = CassetteState.None;

            Debug.Log($"<color=white>[GameLoopController]</color> Приход клиента: {_currentClient.ClientName} (Тип: {_currentClient.ModelType}). Идет к стойке...");

            var playerVC = PlayerViewController;
            playerVC?.SwitchToRoomView();

            var clientsCtrl = ClientsController;
            clientsCtrl?.StartNewClient(_currentClient, () =>
            {
                Debug.Log($"<color=white>[GameLoopController]</color> Клиент {_currentClient.ClientName} подошел к стойке! Включаем ClientCamera и запускаем диалог.");
                PlayerViewController?.SwitchToClientDialogueView(_currentClient);
                _cassetteState = CassetteState.ClientGaveCassette;
            });
        }

        public void HandoverTapeToClient()
        {
            if (_currentClient == null)
            {
                Debug.LogWarning("<color=white>[GameLoopController]</color> Некому передавать кассету: _currentClient не установлен!");
                return;
            }

            Debug.Log($"<color=white>[GameLoopController]</color> Передача кассеты клиенту {_currentClient.ClientName}. Запуск анимации передачи и финального диалога...");

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
                    Debug.Log($"<color=white>[GameLoopController]</color> Клиент {_currentClient.ClientName} произнес финальную фразу и направляется к выходу.");
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
                Debug.LogWarning("<color=white>[GameLoopController]</color> Нет активного клиента у стойки.");
                return;
            }

            Debug.Log($"<color=white>[GameLoopController]</color> Клиент {_currentClient.ClientName} направляется к выходу...");

            ClientsController?.DismissCurrentClient(() =>
            {
                Debug.Log($"<color=white>[GameLoopController]</color> Клиент {_currentClient.ClientName} покинул комнату.");
                _currentClient = null;
                _cassetteState = CassetteState.None;

                PlayerViewController?.SwitchToRoomView();
            });
        }

        public void LaunchDebugVideotape(VideotapeConfig videotapeConfig)
        {
            if (videotapeConfig == null)
            {
                Debug.LogWarning("<color=white>[GameLoopController]</color> Debug VideotapeConfig не назначен!");
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
                    if (_tv != null && _tv.TVRendererService != null)
                    {
                        _tv.TVRendererService.IsCassetteInserted = true;
                        _tv.TVRendererService.SwitchToForwardState();
                    }
                    Debug.Log($"<color=white>[GameLoopController]</color> Имитация вставки завершена: кассета '{videotapeConfig.name}' запущена на ТВ.");
                }, 2.0f);
            }
            else
            {
                Debug.LogError("<color=white>[GameLoopController]</color> Cannot switch to CassetteInsertingView: PlayerViewController is NULL!");
            }
        }

        public void EjectCassette()
        {
            if (_cassetteState == CassetteState.TapeInserted)
            {
                Debug.Log("<color=white>[GameLoopController]</color> Извлекаем кассету из плеера (по клавише или кнопке UI)...");
                _cassetteState = CassetteState.EjectingCassette;

                var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
                videoPlayerService?.Stop();

                if (_tv != null)
                {
                    if (_tv.TVRendererService != null)
                    {
                        _tv.TVRendererService.IsCassetteInserted = false;
                    }
                    _tv.ResetToDefaultMaterial();
                    _tv.TurnOff();
                }

                PlayerViewController?.SwitchToCassetteEjectingView(() =>
                {
                    _cassetteState = CassetteState.TapeReadyToReturn;
                    Debug.Log("<color=white>[GameLoopController]</color> Кассета извлечена. Возврат к виду комнаты. Нажмите E для передачи клиенту.");
                    PlayerViewController?.SwitchToRoomView();
                }, 2.0f);
            }
        }

        public void Update()
        {
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
                    Debug.Log($"<color=white>[GameLoopController]</color> Нажата клавиша [{interactKey}]: Берем кассету у клиента и запускаем анимацию вставки в плеер...");
                    _cassetteState = CassetteState.InsertingCassette;

                    PlayerViewController?.SwitchToCassetteInsertingView(() =>
                    {
                        _cassetteState = CassetteState.TapeInserted;
                        if (_tv != null && _tv.TVRendererService != null)
                        {
                            _tv.TVRendererService.IsCassetteInserted = true;
                            _tv.TVRendererService.SwitchToForwardState();
                        }
                        Debug.Log("<color=white>[GameLoopController]</color> Анимация вставки кассеты завершена. Включаем TV и режим монтажа.");

                        if (_tvOnMaterial != null && _tv != null)
                        {
                            _tv.SetScreenMaterial(_tvOnMaterial, _tvReverseOnMaterial);
                        }
                        
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

                        var gameStateManager = ServiceLocator.Get<GameStateManager>();
                        gameStateManager?.SwitchState<MontageGameState>();
                    }, 2.0f);
                }
                else if (_cassetteState == CassetteState.TapeReadyToReturn)
                {
                    Debug.Log($"<color=white>[GameLoopController]</color> Нажата клавиша [{interactKey}]: Передаем готовую кассету клиенту.");
                    HandoverTapeToClient();
                    _cassetteState = CassetteState.None;
                }
                else if (_currentClient != null && _cassetteState == CassetteState.None)
                {
                    Debug.Log($"<color=white>[GameLoopController]</color> Нажата клавиша [{interactKey}]: Берем кассету у клиента...");
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
                    Debug.Log($"<color=white>[GameLoopController]</color> Клиент уже у стойки! Состояние кассеты: {_cassetteState}.");
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
    }
}
