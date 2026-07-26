using UnityEngine;
using GamePlay.Controllers;
using GamePlay.States.PlayerView;

namespace Core.Services
{
    public class InteractionUIService : IInitializable, IUpdatable
    {
        private readonly GameObject _speakUI;
        private readonly GameObject _answerUI;
        private readonly GameObject _injectUI;
        private readonly GameObject _giveBackUI;

        private GameObject _currentActiveUI;

        public InteractionUIService(GameObject speakUI, GameObject answerUI, GameObject injectUI, GameObject giveBackUI)
        {
            _speakUI = speakUI;
            _answerUI = answerUI;
            _injectUI = injectUI;
            _giveBackUI = giveBackUI;
        }

        public void Initialize()
        {
            HideAll();
        }

        public void Update()
        {
            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            var gameLoopController = ServiceLocator.Get<GameLoopController>();
            var dialogueService = ServiceLocator.Get<DialogueService>();

            if (gameStateManager == null || gameLoopController == null)
            {
                return;
            }

            GameObject uiToShow = null;

            if (gameStateManager.CurrentState is PhoneDialogueGameState)
            {
                if (dialogueService != null && dialogueService.IsSpeaking)
                {
                    uiToShow = _answerUI;
                }
            }
            else if (gameStateManager.CurrentState is RoomGameState || gameStateManager.CurrentState is ClientDialogueGameState)
            {
                if (dialogueService != null && dialogueService.IsSpeaking)
                {
                    uiToShow = _speakUI;
                }
                else if (gameLoopController.CurrentCassetteState == CassetteState.ClientGaveCassette)
                {
                    uiToShow = _injectUI;
                }
                else if (gameLoopController.CurrentCassetteState == CassetteState.TapeReadyToReturn)
                {
                    uiToShow = _giveBackUI;
                }
            }
            else if (gameStateManager.CurrentState is MontageGameState)
            {
                // In Montage state, maybe no interaction UI?
            }

            ShowUI(uiToShow);
        }

        private void ShowUI(GameObject uiToShow)
        {
            if (_currentActiveUI != uiToShow)
            {
                HideAll();
                _currentActiveUI = uiToShow;
                SetActiveSafe(_currentActiveUI, true);
            }
        }

        public void HideAll()
        {
            SetActiveSafe(_speakUI, false);
            SetActiveSafe(_answerUI, false);
            SetActiveSafe(_injectUI, false);
            SetActiveSafe(_giveBackUI, false);
            _currentActiveUI = null;
        }

        private void SetActiveSafe(GameObject obj, bool state)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}
