using UnityEngine;

namespace Core.Data
{
    [CreateAssetMenu(fileName = "TypewriterConfig", menuName = "Core/Typewriter Config")]
    public class TypewriterConfig : ScriptableObject
    {
        [Header("Typewriter Settings")]
        [Tooltip("Интервал между появлением символов в секундах.")]
        [SerializeField] private float _characterDelay = 0.05f;

        [Tooltip("Множитель задержки после знаков препинания (., !, ?, ,, ;, :).")]
        [SerializeField] private float _punctuationDelayMultiplier = 2.0f;

        [Tooltip("Включить дополнительную задержку на знаках препинания.")]
        [SerializeField] private bool _enablePunctuationDelay = true;

        public float CharacterDelay
        {
            get => _characterDelay;
            set => _characterDelay = value;
        }

        public float PunctuationDelayMultiplier
        {
            get => _punctuationDelayMultiplier;
            set => _punctuationDelayMultiplier = value;
        }

        public bool EnablePunctuationDelay
        {
            get => _enablePunctuationDelay;
            set => _enablePunctuationDelay = value;
        }
    }
}
