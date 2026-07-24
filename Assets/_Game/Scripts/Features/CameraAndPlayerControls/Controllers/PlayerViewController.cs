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
        private readonly GameControlsConfigSO _controlsConfig;

        public StateMachine StateMachine => _stateMachine;
        public VideoEditingViewState VideoEditingViewState => _videoEditingViewState;

        public PlayerViewController(CameraControlService cameraControlService, GameControlsConfigSO controlsConfig = null)
        {
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

        public void SwitchToClientDialogueView(GamePlay.Data.ClientDataSO clientData = null)
        {
            _clientDialogueViewState.SetClientData(clientData);
            _stateMachine.ChangeState(_clientDialogueViewState);
        }

        public void SwitchToClientCutsceneView()
        {
            _stateMachine.ChangeState(_clientCutsceneViewState);
        }

        public void Update()
        {
            _stateMachine.Update();

            KeyCode toggleKey = _controlsConfig != null ? _controlsConfig.ToggleCameraKey : KeyCode.Q;

            // Переключение камер по клавише разрешено только вне диалога и вне катсцены
            if (Input.GetKeyDown(toggleKey))
            {
                if (_stateMachine.CurrentState == _clientDialogueViewState || _stateMachine.CurrentState == _clientCutsceneViewState)
                {
                    Debug.Log($"[PlayerViewController] Переключение камер по {toggleKey} заблокировано во время диалога и катсцены.");
                    return;
                }

                ToggleView();
            }
        }

        private void HandleDialogueCompleted()
        {
            if (_stateMachine.CurrentState == _clientDialogueViewState)
            {
                Debug.Log("[PlayerViewController] Диалог завершен. Переход к катсцене/анимации.");
                SwitchToClientCutsceneView();
            }
        }

        private void ToggleView()
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
