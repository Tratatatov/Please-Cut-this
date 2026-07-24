using Core.Services;
using GamePlay.Data;
using UnityEngine;
using System.Collections.Generic;

namespace GamePlay.Controllers
{
    /// <summary>
    /// Тестовый менеджер игры для проверки работы и поведения клиентов.
    /// Автоматически управляет очередью клиентов из расписания дня (DayScheduleSO),
    /// запускает движение клиента к стойке, переключает камеры и отправляет клиента к выходу.
    /// </summary>
    public class TestGameManager : IInitializable, IUpdatable
    {
        private readonly DayScheduleSO _schedule;
        private readonly ClientsController _clientsController;
        private readonly PlayerViewController _playerViewController;
        private readonly GameControlsConfigSO _controlsConfig;

        private Queue<ClientDataSO> _clientQueue;
        private ClientDataSO _currentClient;
        private bool _isGameStarted;

        public ClientDataSO CurrentClient => _currentClient;
        public bool IsGameStarted => _isGameStarted;

        public TestGameManager(
            DayScheduleSO schedule,
            ClientsController clientsController,
            PlayerViewController playerViewController,
            GameControlsConfigSO controlsConfig = null)
        {
            _schedule = schedule;
            _clientsController = clientsController;
            _playerViewController = playerViewController;
            _controlsConfig = controlsConfig;
        }

        public void Initialize()
        {
            _isGameStarted = false;
            _currentClient = null;

            if (_schedule != null && _schedule.Clients != null)
            {
                _clientQueue = new Queue<ClientDataSO>(_schedule.Clients);
            }
            else
            {
                _clientQueue = new Queue<ClientDataSO>();
            }

            KeyCode finishKey = _controlsConfig != null ? _controlsConfig.FinishEditingKey : KeyCode.E;
            KeyCode toggleKey = _controlsConfig != null ? _controlsConfig.ToggleCameraKey : KeyCode.Q;
            KeyCode nextKey = _controlsConfig != null ? _controlsConfig.NextClientKey : KeyCode.N;
            KeyCode dismissKey = _controlsConfig != null ? _controlsConfig.DismissClientKey : KeyCode.X;

            Debug.Log("[TestGameManager] Инициализирован. Управление:\n" +
                      $" - Нажмите [{finishKey}]: Завершить работу с кассетой и передать клиенту\n" +
                      $" - Нажмите [Space] или [{nextKey}]: Приход нового клиента\n" +
                      $" - Нажмите [{dismissKey}]: Уход текущего клиента из комнаты\n" +
                      $" - Нажмите [{toggleKey}]: Переключение видов камеры (Комната / ТВ)");
        }

        public void StartGame()
        {
            _isGameStarted = true;
            Debug.Log("[TestGameManager] Старт игры!");
            SpawnNextClient();
        }

        public void SpawnNextClient()
        {
            if (_clientQueue == null || _clientQueue.Count == 0)
            {
                Debug.Log("[TestGameManager] Все клиенты из расписания на сегодня обслужены!");
                return;
            }

            _currentClient = _clientQueue.Dequeue();

            Debug.Log($"[TestGameManager] Приход клиента: {_currentClient.ClientName} (Тип: {_currentClient.ModelType}). Идет к стойке...");

            // Возвращаем камеру в общий вид комнаты перед приходом
            _playerViewController?.SwitchToRoomView();

            // Запускаем физическое движение клиента от двери к стойке
            _clientsController?.StartNewClient(_currentClient, () =>
            {
                Debug.Log($"[TestGameManager] Клиент {_currentClient.ClientName} подошел к стойке! Включаем ClientCamera и запускаем диалог.");
                _playerViewController?.SwitchToClientDialogueView(_currentClient);
            });
        }

        public void HandoverTapeToClient()
        {
            if (_currentClient == null)
            {
                Debug.LogWarning("[TestGameManager] Некому передавать кассету: _currentClient не установлен!");
                return;
            }

            Debug.Log($"[TestGameManager] Передача кассеты клиенту {_currentClient.ClientName}. Запуск анимации передачи и финального диалога...");

            // Запускаем анимацию передачи кассеты (Take trigger)
            _clientsController?.ClientView?.PlayTakeAnimation();

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
                    Debug.Log($"[TestGameManager] Клиент {_currentClient.ClientName} произнес финальную фразу и направляется к выходу.");
                    DismissCurrentClient();
                };

                dialogueService.OnDialogueCompleted += onReturnDialogueFinished;

                _playerViewController?.SwitchToClientDialogueView(_currentClient);
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
                Debug.LogWarning("[TestGameManager] Нет активного клиента у стойки.");
                return;
            }

            Debug.Log($"[TestGameManager] Клиент {_currentClient.ClientName} направляется к выходу...");

            _clientsController?.DismissCurrentClient(() =>
            {
                Debug.Log($"[TestGameManager] Клиент {_currentClient.ClientName} покинул комнату.");
                _currentClient = null;

                // Переключаем камеру обратно на общий вид комнаты
                _playerViewController?.SwitchToRoomView();
            });
        }

        public void Update()
        {
            KeyCode finishKey = _controlsConfig != null ? _controlsConfig.FinishEditingKey : KeyCode.E;
            KeyCode nextKey = _controlsConfig != null ? _controlsConfig.NextClientKey : KeyCode.N;
            KeyCode dismissKey = _controlsConfig != null ? _controlsConfig.DismissClientKey : KeyCode.X;

            // Клавиша E (или настраиваемая): Завершение работы с кассетой и передача клиенту
            if (Input.GetKeyDown(finishKey))
            {
                if (_currentClient != null)
                {
                    HandoverTapeToClient();
                }
                else
                {
                    Debug.LogWarning($"[TestGameManager] Нажата клавиша [{finishKey}], но у стойки нет клиента!");
                }
            }

            // Горячие клавиши для тестирования:
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
                    Debug.Log($"[TestGameManager] Клиент уже у стойки! Нажмите [{finishKey}], чтобы передать кассету, или [{dismissKey}], чтобы отпустить его.");
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
    }
}
