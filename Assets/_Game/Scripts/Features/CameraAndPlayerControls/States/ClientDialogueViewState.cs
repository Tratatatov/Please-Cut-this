using Core.Services;
using Core.StateMachines;
using GamePlay.Data;
using UnityEngine;

namespace GamePlay.States.PlayerView
{
    public class ClientDialogueViewState : IState
    {
        private readonly CameraControlService _cameraController;
        private ClientDataConfig _clientData;

        public ClientDialogueViewState(CameraControlService cameraControlService)
        {
            _cameraController = cameraControlService;
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
        }
    }
}

