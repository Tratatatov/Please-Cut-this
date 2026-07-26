using System;
using System.Collections.Generic;
using Core.Data;
using TMPro;
using UnityEngine;

namespace Core.Services
{
    public class TypewriterService : IInitializable, IUpdatable, IDisposableService
    {
        private class TypingTask
        {
            public TMP_Text TargetText { get; private set; }
            public string FullText { get; private set; }
            public int CurrentCharacterIndex { get; set; }
            public int TotalCharacters { get; private set; }
            public float Timer { get; set; }
            public float BaseDelay { get; private set; }
            public Action OnComplete { get; private set; }

            public TypingTask(TMP_Text targetText, string fullText, int totalCharacters, float baseDelay, Action onComplete)
            {
                TargetText = targetText;
                FullText = fullText;
                CurrentCharacterIndex = 0;
                TotalCharacters = totalCharacters;
                Timer = 0f;
                BaseDelay = baseDelay;
                OnComplete = onComplete;
            }
        }

        private readonly TypewriterConfig _config;
        private readonly Dictionary<TMP_Text, TypingTask> _activeTasks = new Dictionary<TMP_Text, TypingTask>();
        private readonly List<TMP_Text> _completedTargetsCache = new List<TMP_Text>();

        private const float DefaultFallbackDelay = 0.05f;

        public event Action<TMP_Text, int> OnCharacterTyped;
        public event Action<TMP_Text> OnTextCompleted;

        public bool IsTypingAny => _activeTasks.Count > 0;

        public TypewriterService(TypewriterConfig config = null)
        {
            _config = config;
        }

        public void Initialize()
        {
            StopAll();
        }

        public void TypeText(TMP_Text target, string text, Action onComplete = null, float? customDelay = null)
        {
            if (target == null)
            {
                Debug.LogWarning("<color=white>[TypewriterService]</color> Target TMP_Text is null.");
                onComplete?.Invoke();
                return;
            }

            Stop(target);

            if (string.IsNullOrEmpty(text))
            {
                target.text = string.Empty;
                target.maxVisibleCharacters = 0;
                onComplete?.Invoke();
                return;
            }

            target.text = text;
            target.ForceMeshUpdate();

            int characterCount = target.textInfo.characterCount;
            if (characterCount <= 0)
            {
                target.maxVisibleCharacters = int.MaxValue;
                onComplete?.Invoke();
                return;
            }

            target.maxVisibleCharacters = 0;

            float delay = customDelay ?? (_config != null ? _config.CharacterDelay : DefaultFallbackDelay);
            if (delay <= 0f)
            {
                target.maxVisibleCharacters = characterCount;
                onComplete?.Invoke();
                OnTextCompleted?.Invoke(target);
                return;
            }

            TypingTask task = new TypingTask(target, text, characterCount, delay, onComplete);
            _activeTasks[target] = task;
        }

        public void Skip(TMP_Text target)
        {
            if (target == null || !_activeTasks.TryGetValue(target, out TypingTask task))
            {
                return;
            }

            _activeTasks.Remove(target);
            target.maxVisibleCharacters = task.TotalCharacters;
            task.OnComplete?.Invoke();
            OnTextCompleted?.Invoke(target);
        }

        public void SkipAll()
        {
            _completedTargetsCache.Clear();
            _completedTargetsCache.AddRange(_activeTasks.Keys);

            foreach (TMP_Text target in _completedTargetsCache)
            {
                Skip(target);
            }

            _completedTargetsCache.Clear();
        }

        public void Stop(TMP_Text target)
        {
            if (target != null && _activeTasks.ContainsKey(target))
            {
                _activeTasks.Remove(target);
            }
        }

        public void StopAll()
        {
            _activeTasks.Clear();
            _completedTargetsCache.Clear();
        }

        public bool IsTyping(TMP_Text target)
        {
            return target != null && _activeTasks.ContainsKey(target);
        }

        public void Update()
        {
            if (_activeTasks.Count == 0)
            {
                return;
            }

            _completedTargetsCache.Clear();

            foreach (KeyValuePair<TMP_Text, TypingTask> pair in _activeTasks)
            {
                TMP_Text target = pair.Key;
                TypingTask task = pair.Value;

                if (target == null)
                {
                    _completedTargetsCache.Add(target);
                    continue;
                }

                task.Timer += Time.deltaTime;

                float currentDelay = GetCharacterDelay(task);

                if (task.Timer >= currentDelay)
                {
                    task.Timer = 0f;
                    task.CurrentCharacterIndex++;
                    target.maxVisibleCharacters = task.CurrentCharacterIndex;

                    OnCharacterTyped?.Invoke(target, task.CurrentCharacterIndex);

                    if (task.CurrentCharacterIndex >= task.TotalCharacters)
                    {
                        target.maxVisibleCharacters = task.TotalCharacters;
                        _completedTargetsCache.Add(target);
                    }
                }
            }

            for (int i = 0; i < _completedTargetsCache.Count; i++)
            {
                TMP_Text target = _completedTargetsCache[i];
                if (target != null && _activeTasks.TryGetValue(target, out TypingTask task))
                {
                    _activeTasks.Remove(target);
                    task.OnComplete?.Invoke();
                    OnTextCompleted?.Invoke(target);
                }
                else
                {
                    _activeTasks.Remove(target);
                }
            }

            _completedTargetsCache.Clear();
        }

        private float GetCharacterDelay(TypingTask task)
        {
            float delay = task.BaseDelay;

            if (_config != null && _config.EnablePunctuationDelay && task.CurrentCharacterIndex > 0)
            {
                TMP_CharacterInfo[] charInfos = task.TargetText.textInfo.characterInfo;
                int prevCharIdx = task.CurrentCharacterIndex - 1;
                if (prevCharIdx >= 0 && prevCharIdx < charInfos.Length)
                {
                    char c = charInfos[prevCharIdx].character;
                    if (IsPunctuation(c))
                    {
                        delay *= _config.PunctuationDelayMultiplier;
                    }
                }
            }

            return delay;
        }

        private bool IsPunctuation(char c)
        {
            return c == '.' || c == '!' || c == '?' || c == ',' || c == ';' || c == ':';
        }

        public void Dispose()
        {
            StopAll();
            OnCharacterTyped = null;
            OnTextCompleted = null;
        }
    }
}
