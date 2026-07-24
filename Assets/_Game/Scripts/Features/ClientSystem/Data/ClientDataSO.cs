using System.Collections.Generic;
using Core.Data;
using UnityEngine;

namespace GamePlay.Data
{
    public enum ClientModelType
    {
        Normal,
        Doctor,
        Police,
        Monster
    }

    [CreateAssetMenu(fileName = "ClientData", menuName = "Gameplay/Client Data")]
    public class ClientDataSO : ScriptableObject
    {
        [Header("Client Identity")]
        public string ClientName;
        public ClientModelType ModelType;

        [Header("Dialog Configuration")]
        [Tooltip("Задержка между фразами по умолчанию для данного персонажа (в секундах).")]
        public float PhraseDelay = 2.0f;

        [Tooltip("Список фраз диалога с индивидуальными задержками.")]
        public List<DialoguePhrase> DialoguePhrases = new List<DialoguePhrase>();

        [Header("Dialogs (Legacy / Backup)")]
        [TextArea] public List<string> ArrivalPhrases = new List<string>();
        [TextArea] public string SuccessPhrase;

        [Header("Task & Level")]
        public CutLevelData LevelData;

        public List<DialoguePhrase> GetPhrases()
        {
            if (DialoguePhrases != null && DialoguePhrases.Count > 0)
            {
                return DialoguePhrases;
            }

            List<DialoguePhrase> phrases = new List<DialoguePhrase>();
            if (ArrivalPhrases != null)
            {
                foreach (string phrase in ArrivalPhrases)
                {
                    phrases.Add(new DialoguePhrase(phrase));
                }
            }
            return phrases;
        }
    }
}

