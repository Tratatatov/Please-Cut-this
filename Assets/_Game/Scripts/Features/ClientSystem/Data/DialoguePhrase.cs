using System;
using UnityEngine;

namespace Core.Data
{
    [Serializable]
    public class DialoguePhrase
    {
        [TextArea(2, 4)]
        [SerializeField] private string text;
        
        [Tooltip("Задержка после этой фразы в секундах. Если меньше 0, используется задержка по умолчанию из конфига персонажа.")]
        [SerializeField] private float delayOverride = -1f;

        public string Text
        {
            get => text;
            set => text = value;
        }

        public float DelayOverride
        {
            get => delayOverride;
            set => delayOverride = value;
        }

        public DialoguePhrase()
        {
            text = string.Empty;
            delayOverride = -1f;
        }

        public DialoguePhrase(string phraseText, float customDelay = -1f)
        {
            text = phraseText;
            delayOverride = customDelay;
        }
    }
}
