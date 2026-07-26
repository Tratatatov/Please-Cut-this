using Core.Services;
using Core.StateMachines;
using GamePlay.Data;
using UnityEngine;

namespace GamePlay.States.PlayerView
{
    public class ClientDialogueViewState : IState
    {
        private readonly CameraControlService _cameraController;
        private readonly GameControlsConfig _controlsConfig;
        private ClientDataConfig _clientData;

        public ClientDialogueViewState(CameraControlService cameraControlService, GameControlsConfig controlsConfig = null)
        {
            _cameraController = cameraControlService;
            _controlsConfig = controlsConfig;
        }

        public void SetClientData(ClientDataConfig clientData)
        {
            _clientData = clientData;
        }

        public void Enter()
        {
            _cameraController?.SwitchToClientCamera(lockCamera: true);

            var dialogueService = ServiceLocator.Get<DialogueService>();
            if (dialogueService != null && _clientData != null)
            {
                dialogueService.PlayDialogue(_clientData);
            }
        }

        public void Exit()
        {
            _cameraController?.UnlockCamera();

            var dialogueService = ServiceLocator.Get<DialogueService>();
            dialogueService?.StopDialogue();
        }

        public void Update()
        {
            KeyCode interactKey = _controlsConfig != null ? _controlsConfig.InteractCassetteKey : KeyCode.E;
            if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0))
            {
                var dialogueService = ServiceLocator.Get<DialogueService>();
                dialogueService?.TryAdvanceManual();
            }
        }
    }
}

