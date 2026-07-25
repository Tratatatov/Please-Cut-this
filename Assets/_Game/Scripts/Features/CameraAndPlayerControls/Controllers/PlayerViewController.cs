using Core.StateMachines;
using Core.Services;
using GamePlay.Data;
using GamePlay.States.PlayerView;
using UnityEngine;

namespace GamePlay.Controllers
{
    public class PlayerViewController : IInitializable, IUpdatable
    {
        private readonly StateMachine _stateMachine;
        private readonly RoomViewState _roomViewState;
        private readonly VideoEditingViewState _videoEditingViewState;
        private readonly ClientDialogueViewState _clientDialogueViewState;
        private readonly ClientCutsceneViewState _clientCutsceneViewState;
        private readonly CameraControlService _cameraControlService;
        private readonly GameControlsConfig _controlsConfig;

        private CassetteInsertingViewState _cassetteInsertingViewState;
        private CassetteEjectingViewState _cassetteEjectingViewState;

        public StateMachine StateMachine => _stateMachine;
        public VideoEditingViewState VideoEditingViewState => _videoEditingViewState;

        public bool IsControlsLocked =>
            _stateMachine.CurrentState == _clientDialogueViewState ||
            _stateMachine.CurrentState == _clientCutsceneViewState ||
            (_cassetteInsertingViewState != null && _stateMachine.CurrentState == _cassetteInsertingViewState) ||
            (_cassetteEjectingViewState != null && _stateMachine.CurrentState == _cassetteEjectingViewState);

        public PlayerViewController(CameraControlService cameraControlService, GameControlsConfig controlsConfig = null)
        {
            _cameraControlService = cameraControlService;
            _stateMachine = new StateMachine();
            _roomViewState = new RoomViewState(cameraControlService);
            _videoEditingViewState = new VideoEditingViewState(cameraControlService);
            _clientDialogueViewState = new ClientDialogueViewState(cameraControlService);
            _clientCutsceneViewState = new ClientCutsceneViewState(cameraControlService, onCompleted: SwitchToRoomView);
            _controlsConfig = controlsConfig;
        }

        public void Initialize()
        {
            _stateMachine.ChangeState(_roomViewState);

            var dialogueService = ServiceLocator.Get<DialogueService>();
            if (dialogueService != null)
            {
                dialogueService.OnDialogueCompleted += HandleDialogueCompleted;
            }
        }

        public void SwitchToRoomView()
        {
            _stateMachine.ChangeState(_roomViewState);
        }

        public void SwitchToVideoEditingView()
        {
            _stateMachine.ChangeState(_videoEditingViewState);
        }

        public void SwitchToClientDialogueView(GamePlay.Data.ClientDataConfig clientData = null)
        {
            _clientDialogueViewState.SetClientData(clientData);
            _stateMachine.ChangeState(_clientDialogueViewState);
        }

        public void SwitchToClientCutsceneView()
        {
            _stateMachine.ChangeState(_clientCutsceneViewState);
        }

        public void SwitchToCassetteInsertingView(System.Action onCompleted = null, float duration = 2.0f)
        {
            _cassetteInsertingViewState = new CassetteInsertingViewState(_cameraControlService, onCompleted, duration);
            _stateMachine.ChangeState(_cassetteInsertingViewState);
        }

        public void SwitchToCassetteEjectingView(System.Action onCompleted = null, float duration = 2.0f)
        {
            _cassetteEjectingViewState = new CassetteEjectingViewState(_cameraControlService, onCompleted, duration);
            _stateMachine.ChangeState(_cassetteEjectingViewState);
        }

        public void Update()
        {
            _stateMachine.Update();

            KeyCode toggleKey = _controlsConfig != null ? _controlsConfig.ToggleCameraKey : KeyCode.Q;

            // Переключение камер по клавише разрешено только вне диалога, катсцены и анимаций кассеты
            if (Input.GetKeyDown(toggleKey))
            {
                if (IsControlsLocked)
                {
                    Debug.Log($"<color=lightblue>[PlayerViewController]</color> Переключение камер по {toggleKey} заблокировано во время анимации, диалога или катсцены.");
                    return;
                }

                ToggleView();
            }
        }

        private void HandleDialogueCompleted()
        {
            if (_stateMachine.CurrentState == _clientDialogueViewState)
            {
                Debug.Log("<color=lightblue>[PlayerViewController]</color> Диалог завершен. Переход к катсцене/анимации.");
                SwitchToClientCutsceneView();
            }
        }

        private void ToggleView()
        {
            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            if (gameStateManager != null && gameStateManager.CurrentState is MontageGameState)
            {
                gameStateManager.SwitchState<RoomGameState>();
            }
            else if (gameStateManager != null)
            {
                gameStateManager.SwitchState<MontageGameState>();
            }
            else
            {
                if (_stateMachine.CurrentState == _videoEditingViewState)
                {
                    SwitchToRoomView();
                }
                else
                {
                    SwitchToVideoEditingView();
                }
            }
        }
    }
}
