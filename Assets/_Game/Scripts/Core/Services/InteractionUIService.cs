using UnityEngine;

namespace Core.Services
{
    public class InteractionUIService : IInitializable
    {
        private readonly GameObject _speakUI;
        private readonly GameObject _answerUI;
        private readonly GameObject _injectUI;
        private readonly GameObject _giveBackUI;

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

        public void HideAll()
        {
            SetActiveSafe(_speakUI, false);
            SetActiveSafe(_answerUI, false);
            SetActiveSafe(_injectUI, false);
            SetActiveSafe(_giveBackUI, false);
        }

        public void ShowSpeakUI()
        {
            HideAll();
            SetActiveSafe(_speakUI, true);
        }

        public void ShowAnswerUI()
        {
            HideAll();
            SetActiveSafe(_answerUI, true);
        }

        public void ShowInJectUI()
        {
            HideAll();
            SetActiveSafe(_injectUI, true);
        }

        public void ShowGiveBackUI()
        {
            HideAll();
            SetActiveSafe(_giveBackUI, true);
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
