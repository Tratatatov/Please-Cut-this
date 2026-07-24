using System;
using System.Collections.Generic;
using Core.Services;
using GamePlay.Data;
using UnityEngine;

namespace GamePlay.View
{
    public class ClientView : MonoBehaviour
    {
        [Serializable]
        public struct ModelEntry
        {
            public GameObject modelObject;
            public Animator modelAnimator;
        }

        [Serializable]
        public struct ModelMapping
        {
            public ClientModelType modelType;
            public List<ModelEntry> models;
        }

        [SerializeField] private List<ModelMapping> _modelMappings = new List<ModelMapping>();

        private readonly ClientAnimationService _animationService = new ClientAnimationService();
        private readonly Dictionary<ClientModelType, List<ModelEntry>> _availablePools = new Dictionary<ClientModelType, List<ModelEntry>>();

        public ClientAnimationService AnimationService => _animationService;

        public void Initialize()
        {
            DisableAllModels();
            _animationService.ClearAnimator();
            ResetAllPools();
        }

        public void ResetAllPools()
        {
            _availablePools.Clear();
            foreach (var mapping in _modelMappings)
            {
                RefillPoolForType(mapping.modelType);
            }
        }

        private void RefillPoolForType(ClientModelType modelType)
        {
            foreach (var mapping in _modelMappings)
            {
                if (mapping.modelType == modelType)
                {
                    List<ModelEntry> poolCopy = new List<ModelEntry>();
                    if (mapping.models != null)
                    {
                        poolCopy.AddRange(mapping.models);
                    }
                    _availablePools[modelType] = poolCopy;
                    return;
                }
            }
        }

        public void SetClientData(ClientDataSO clientData)
        {
            if (clientData == null)
            {
                DisableAllModels();
                _animationService.ClearAnimator();
                return;
            }

            ApplyModel(clientData.ModelType);
        }

        public void ApplyModel(ClientModelType modelType)
        {
            DisableAllModels();

            if (!_availablePools.TryGetValue(modelType, out List<ModelEntry> pool) || pool == null || pool.Count == 0)
            {
                // Если список свободных моделей пуст (все модели типа уже приходили), заполняем его заново
                RefillPoolForType(modelType);
                pool = _availablePools.GetValueOrDefault(modelType);
            }

            if (pool != null && pool.Count > 0)
            {
                // Выбираем случайную модель из неиспользованных в текущем пуле
                int selectedIndex = UnityEngine.Random.Range(0, pool.Count);
                ModelEntry selectedEntry = pool[selectedIndex];

                // Исключаем ее из пула доступных моделей
                pool.RemoveAt(selectedIndex);

                if (selectedEntry.modelObject != null)
                {
                    selectedEntry.modelObject.SetActive(true);

                    Animator targetAnimator = selectedEntry.modelAnimator != null 
                        ? selectedEntry.modelAnimator 
                        : selectedEntry.modelObject.GetComponentInChildren<Animator>();

                    if (targetAnimator != null)
                    {
                        _animationService.BindAnimator(targetAnimator);
                    }
                    else
                    {
                        _animationService.ClearAnimator();
                        Debug.LogWarning($"[ClientView] Animator не найден на модели {selectedEntry.modelObject.name}.");
                    }
                    return;
                }
            }

            _animationService.ClearAnimator();
            Debug.LogWarning($"[ClientView] Модели для типа {modelType} не найдены или не назначены.");
        }

        public void PlayIdle(float crossFadeDuration = 0.1f)
        {
            _animationService.PlayIdle(crossFadeDuration);
        }

        public void PlayWalk(float crossFadeDuration = 0.1f)
        {
            _animationService.PlayWalk(crossFadeDuration);
        }

        public void PlayWalking(float crossFadeDuration = 0.1f)
        {
            _animationService.PlayWalking(crossFadeDuration);
        }

        public void PlayTakeAnimation()
        {
            _animationService.PlayTakeAnimation();
        }

        public void PlayAnimation(ClientAnimationState state, float crossFadeDuration = 0.1f)
        {
            _animationService.PlayState(state, crossFadeDuration);
        }

        public void PlayAnimation(string stateName, float crossFadeDuration = 0.1f)
        {
            _animationService.PlayState(stateName, crossFadeDuration);
        }

        public void SetAnimationTrigger(string triggerName)
        {
            _animationService.SetTrigger(triggerName);
        }

        public void SetAnimationBool(string boolName, bool value)
        {
            _animationService.SetBool(boolName, value);
        }

        public void SetAnimationFloat(string floatName, float value)
        {
            _animationService.SetFloat(floatName, value);
        }

        private void DisableAllModels()
        {
            foreach (var mapping in _modelMappings)
            {
                if (mapping.models != null)
                {
                    foreach (var entry in mapping.models)
                    {
                        if (entry.modelObject != null)
                        {
                            entry.modelObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }
}
