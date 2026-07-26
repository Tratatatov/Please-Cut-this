using System;
using System.Collections.Generic;
using Core.Data;
using GamePlay.Data;
using TMPro;
using UnityEngine;

namespace Core.Services
{
    public class DialogueService : IInitializable, IUpdatable, IDisposableService
    {
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _messageText;
        private readonly GameObject _dialogueWindow;
        private readonly Canvas _dialogueCanvas;
        private readonly TypewriterService _typewriterService;
        private readonly float _fallbackPhraseDelay;

        private List<DialoguePhrase> _currentPhrases;
        private string _currentSpeakerName;
        private float _currentDefaultDelay;
        private int _currentPhraseIndex;
        private float _phraseTimer;

        private bool _isSpeaking;
        private bool _isPaused;

        public event Action OnDialogueStarted;
        public event Action<string, string> OnPhraseStarted;
        public event Action OnDialogueCompleted;

        public bool IsSpeaking => _isSpeaking;
        public bool IsPaused => _isPaused;
        public string CurrentSpeakerName => _currentSpeakerName;
        public string CurrentMessageText => (_currentPhrases != null && _currentPhraseIndex >= 0 && _currentPhraseIndex < _currentPhrases.Count) 
            ? _currentPhrases[_currentPhraseIndex].Text 
            : string.Empty;

        public DialogueService(TMP_Text nameText, TMP_Text messageText, GameObject dialogueWindow = null, float fallbackPhraseDelay = 2.0f, TypewriterService typewriterService = null)
        {
            _nameText = nameText;
            _messageText = messageText;
            _dialogueWindow = dialogueWindow;
            _fallbackPhraseDelay = fallbackPhraseDelay;
            _typewriterService = typewriterService;
        }

        public DialogueService(TMP_Text nameText, TMP_Text messageText, Canvas dialogueCanvas, float fallbackPhraseDelay = 2.0f, TypewriterService typewriterService = null)
            : this(nameText, messageText, dialogueCanvas != null ? dialogueCanvas.gameObject : null, fallbackPhraseDelay, typewriterService)
        {
            _dialogueCanvas = dialogueCanvas;
        }

        public void Initialize()
        {
            _isSpeaking = false;
            _isPaused = false;
            _currentPhrases = new List<DialoguePhrase>();
            _currentPhraseIndex = -1;
            _phraseTimer = 0f;

            ClearUI();
            SetWindowActive(false);
        }

        public void PlayDialogue(ClientDataConfig clientData)
        {
            if (clientData == null)
            {
                Debug.LogWarning("<color=white>[DialogueService]</color> Cannot play dialogue: clientData is null.");
                return;
            }

            string speakerName = !string.IsNullOrEmpty(clientData.ClientName) ? clientData.ClientName : "Client";
            float defaultDelay = clientData.PhraseDelay > 0f ? clientData.PhraseDelay : _fallbackPhraseDelay;
            List<DialoguePhrase> phrases = clientData.GetPhrases();

            PlayDialogue(speakerName, phrases, defaultDelay);
        }

        public void PlayDialogue(string speakerName, List<DialoguePhrase> phrases, float defaultDelay = 2.0f)
        {
            if (phrases == null || phrases.Count == 0)
            {
                Debug.LogWarning("<color=white>[DialogueService]</color> Cannot play dialogue: phrases list is empty.");
                return;
            }

            _currentSpeakerName = speakerName;
            _currentPhrases = new List<DialoguePhrase>(phrases);
            _currentDefaultDelay = defaultDelay > 0f ? defaultDelay : _fallbackPhraseDelay;
            _currentPhraseIndex = 0;
            _phraseTimer = 0f;
            _isSpeaking = true;
            _isPaused = false;

            SetWindowActive(true);
            OnDialogueStarted?.Invoke();
            DisplayCurrentPhrase();
        }

        public void PlayDialogue(string speakerName, List<string> phrases, float defaultDelay = 2.0f)
        {
            if (phrases == null || phrases.Count == 0)
            {
                Debug.LogWarning("<color=white>[DialogueService]</color> Cannot play dialogue: phrases list is empty.");
                return;
            }

            List<DialoguePhrase> dialoguePhrases = new List<DialoguePhrase>();
            foreach (string phrase in phrases)
            {
                dialoguePhrases.Add(new DialoguePhrase(phrase));
            }

            PlayDialogue(speakerName, dialoguePhrases, defaultDelay);
        }

        public void PlayPhrase(string speakerName, string message)
        {
            List<DialoguePhrase> singlePhrase = new List<DialoguePhrase> { new DialoguePhrase(message) };
            PlayDialogue(speakerName, singlePhrase, _fallbackPhraseDelay);
        }

        public void PauseDialogue()
        {
            _isPaused = true;
        }

        public void ResumeDialogue()
        {
            _isPaused = false;
        }

        public void StopDialogue()
        {
            _isSpeaking = false;
            _isPaused = false;
            _currentPhrases?.Clear();
            _currentPhraseIndex = -1;
            _phraseTimer = 0f;

            ClearUI();
            SetWindowActive(false);
        }

        public void TryAdvanceManual()
        {
            if (!_isSpeaking || _currentPhrases == null || _currentPhrases.Count == 0) return;

            if (_typewriterService != null && _messageText != null && _typewriterService.IsTyping(_messageText))
            {
                _typewriterService.Skip(_messageText);
            }
            else
            {
                AdvanceToNextPhrase();
            }
        }

        public void Update()
        {
            // Auto-advance is disabled per user request. 
            // All dialogue advancements must be done manually via TryAdvanceManual().
        }

        private float GetCurrentPhraseDelay()
        {
            if (_currentPhraseIndex >= 0 && _currentPhraseIndex < _currentPhrases.Count)
            {
                DialoguePhrase phrase = _currentPhrases[_currentPhraseIndex];
                if (phrase != null && phrase.DelayOverride >= 0f)
                {
                    return phrase.DelayOverride;
                }
            }

            return _currentDefaultDelay;
        }

        private void DisplayCurrentPhrase()
        {
            if (_currentPhraseIndex < 0 || _currentPhraseIndex >= _currentPhrases.Count)
            {
                return;
            }

            DialoguePhrase phrase = _currentPhrases[_currentPhraseIndex];
            string message = phrase != null ? phrase.Text : string.Empty;

            if (_nameText != null)
            {
                _nameText.text = _currentSpeakerName;
            }

            if (_messageText != null)
            {
                if (_typewriterService != null)
                {
                    _typewriterService.TypeText(_messageText, message);
                }
                else
                {
                    _messageText.text = message;
                }
            }

            SetWindowActive(true);
            _phraseTimer = 0f;
            OnPhraseStarted?.Invoke(_currentSpeakerName, message);
        }

        private void AdvanceToNextPhrase()
        {
            _currentPhraseIndex++;

            if (_currentPhraseIndex < _currentPhrases.Count)
            {
                DisplayCurrentPhrase();
            }
            else
            {
                _isSpeaking = false;
                _currentPhraseIndex = -1;
                ClearUI();
                SetWindowActive(false);
                OnDialogueCompleted?.Invoke();
            }
        }

        private void ClearUI()
        {
            if (_nameText != null)
            {
                _nameText.text = string.Empty;
            }

            if (_messageText != null)
            {
                if (_typewriterService != null)
                {
                    _typewriterService.Stop(_messageText);
                }
                _messageText.text = string.Empty;
            }
        }

        private void SetWindowActive(bool isActive)
        {
            if (_dialogueWindow != null)
            {
                _dialogueWindow.SetActive(isActive);
            }
            else if (_dialogueCanvas != null)
            {
                _dialogueCanvas.gameObject.SetActive(isActive);
            }
        }

        public void Dispose()
        {
            StopDialogue();
            OnDialogueStarted = null;
            OnPhraseStarted = null;
            OnDialogueCompleted = null;
        }
    }
}
