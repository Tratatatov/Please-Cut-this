using UnityEngine;

namespace Core.Services
{
    public enum TVScreenState
    {
        Off,
        Forward,
        Reverse
    }

    public class TVRendererService : IInitializable
    {
        private Renderer _renderer;
        private Renderer _reverseRenderer;
        private Renderer _offRenderer;

        private Material _defaultMaterial;
        private Material _reverseDefaultMaterial;
        private Material _offDefaultMaterial;

        private Texture _currentScreenTexture;
        private Texture _currentReverseScreenTexture;
        private string _currentTexturePropertyName = "_MainTex";
        private const int TargetMaterialIndex = 0;

        private bool _lastIsReversed = false;
        private bool _isCassetteInserted = false;
        private TVScreenState _currentState = TVScreenState.Off;

        public bool IsCassetteInserted
        {
            get => _isCassetteInserted;
            set
            {
                _isCassetteInserted = value;
                if (!_isCassetteInserted)
                {
                    SwitchToOffState();
                }
            }
        }

        public TVScreenState CurrentState => _currentState;
        public Renderer TargetRenderer => _renderer;
        public Renderer ReverseTargetRenderer => _reverseRenderer;
        public Renderer OffTargetRenderer => _offRenderer;

        public TVRendererService(Renderer renderer = null, Renderer reverseRenderer = null, Renderer offRenderer = null)
        {
            _renderer = renderer;
            _reverseRenderer = reverseRenderer;
            _offRenderer = offRenderer;
        }

        public void BindRenderer(Renderer renderer, Renderer reverseRenderer = null, Renderer offRenderer = null)
        {
            _renderer = renderer;
            _reverseRenderer = reverseRenderer;
            _offRenderer = offRenderer;
        }

        public void Initialize()
        {
            if (_renderer != null && _renderer.sharedMaterials != null && _renderer.sharedMaterials.Length > TargetMaterialIndex)
            {
                _defaultMaterial = _renderer.sharedMaterials[TargetMaterialIndex];
            }
            if (_reverseRenderer != null && _reverseRenderer.sharedMaterials != null && _reverseRenderer.sharedMaterials.Length > TargetMaterialIndex)
            {
                _reverseDefaultMaterial = _reverseRenderer.sharedMaterials[TargetMaterialIndex];
            }
            if (_offRenderer != null && _offRenderer.sharedMaterials != null && _offRenderer.sharedMaterials.Length > TargetMaterialIndex)
            {
                _offDefaultMaterial = _offRenderer.sharedMaterials[TargetMaterialIndex];
            }

            _isCassetteInserted = false;
            SwitchToOffState();
        }

        /// <summary>
        /// Переключает экран ТВ в состояние "Выключен" (активен _offRenderer).
        /// </summary>
        public void SwitchToOffState()
        {
            _currentState = TVScreenState.Off;
            SetRendererVisibility(_renderer, false);
            SetRendererVisibility(_reverseRenderer, false);
            SetRendererVisibility(_offRenderer, true);
        }

        /// <summary>
        /// Переключает экран ТВ в состояние прямого воспроизведения (активен _renderer).
        /// </summary>
        public void SwitchToForwardState()
        {
            if (!_isCassetteInserted)
            {
                SwitchToOffState();
                return;
            }

            _currentState = TVScreenState.Forward;
            SetRendererVisibility(_offRenderer, false);
            SetRendererVisibility(_reverseRenderer, false);
            SetRendererVisibility(_renderer, true);
        }

        /// <summary>
        /// Переключает экран ТВ в состояние реверсивной отмотки (активен _reverseRenderer).
        /// </summary>
        public void SwitchToReverseState()
        {
            if (!_isCassetteInserted)
            {
                SwitchToOffState();
                return;
            }

            _currentState = TVScreenState.Reverse;
            SetRendererVisibility(_offRenderer, false);
            SetRendererVisibility(_renderer, false);

            if (_reverseRenderer != null && _reverseRenderer != _renderer)
            {
                SetRendererVisibility(_reverseRenderer, true);
            }
            else
            {
                SetRendererVisibility(_renderer, true);
            }
        }

        /// <summary>
        /// Переключает воспроизведение между прямым и реверсивным.
        /// </summary>
        public void SwitchPlaybackState(bool isReversed)
        {
            _lastIsReversed = isReversed;

            if (!_isCassetteInserted)
            {
                SwitchToOffState();
                return;
            }

            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            bool isMontageState = gameStateManager != null && gameStateManager.CurrentState is MontageGameState;
            bool isEndCinematicState = gameStateManager != null && gameStateManager.CurrentState is EndCinematicGameState;

            if (!isMontageState && !isEndCinematicState)
            {
                SwitchToOffState();
                return;
            }

            if (isReversed)
            {
                SwitchToReverseState();
            }
            else
            {
                SwitchToForwardState();
            }
        }

        /// <summary>
        /// Переключает видимость между основным, реверс и выключенным рендерером.
        /// </summary>
        public void SwitchRenderer(bool isReversed)
        {
            SwitchPlaybackState(isReversed);
        }

        /// <summary>
        /// Обновляет видимость экранов в зависимости от текущего состояния игры (MontageGameState vs Off).
        /// </summary>
        public void UpdateScreenState()
        {
            if (!_isCassetteInserted)
            {
                SwitchToOffState();
                return;
            }

            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            bool isMontageState = gameStateManager != null && gameStateManager.CurrentState is MontageGameState;
            bool isEndCinematicState = gameStateManager != null && gameStateManager.CurrentState is EndCinematicGameState;

            if (!isMontageState && !isEndCinematicState)
            {
                SwitchToOffState();
            }
            else
            {
                if (_currentState == TVScreenState.Reverse || _lastIsReversed)
                {
                    SwitchToReverseState();
                }
                else
                {
                    SwitchToForwardState();
                }
            }
        }

        private void SetRendererVisibility(Renderer rnd, bool visible)
        {
            if (rnd == null) return;

            bool isUniqueGameObject = true;
            if (_renderer != null && rnd != _renderer && rnd.gameObject == _renderer.gameObject) isUniqueGameObject = false;
            if (_reverseRenderer != null && rnd != _reverseRenderer && rnd.gameObject == _reverseRenderer.gameObject) isUniqueGameObject = false;
            if (_offRenderer != null && rnd != _offRenderer && rnd.gameObject == _offRenderer.gameObject) isUniqueGameObject = false;

            if (isUniqueGameObject)
            {
                rnd.gameObject.SetActive(visible);
            }
            else
            {
                rnd.enabled = visible;
            }
        }

        /// <summary>
        /// Переключает/устанавливает материал экрана телевизора (по индексу 0).
        /// </summary>
        /// <param name="material">Основной новый материал экрана.</param>
        /// <param name="reverseMaterial">Отдельный новый материал экрана для отмотки назад (не копия основного).</param>
        public void SetScreenMaterial(Material material, Material reverseMaterial = null)
        {
            SetMaterialOnRenderer(_renderer, material, TargetMaterialIndex);
            if (reverseMaterial != null)
            {
                SetMaterialOnRenderer(_reverseRenderer, reverseMaterial, TargetMaterialIndex);
            }
            else if (_reverseRenderer != null && _reverseRenderer != _renderer && _reverseDefaultMaterial != null)
            {
                SetMaterialOnRenderer(_reverseRenderer, _reverseDefaultMaterial, TargetMaterialIndex);
            }
        }

        /// <summary>
        /// Переключает/устанавливает материал на основном Renderer по указанному индексу (по умолчанию 0).
        /// </summary>
        public void SetMaterial(Material material, int index = TargetMaterialIndex)
        {
            SetMaterialOnRenderer(_renderer, material, index);
        }

        private void SetMaterialOnRenderer(Renderer targetRenderer, Material material, int index)
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning("<color=yellow>[TVRendererService]</color> Target Renderer не назначен!");
                return;
            }

            Material[] materials = targetRenderer.sharedMaterials;
            if (materials == null || index < 0 || index >= materials.Length)
            {
                Debug.LogWarning($"<color=yellow>[TVRendererService]</color> Индекс материала {index} выходит за границы массива материалов.");
                return;
            }

            materials[index] = material;

            if (index == TargetMaterialIndex && materials[index] != null)
            {
                Texture texToRestore = (targetRenderer == _reverseRenderer && _currentReverseScreenTexture != null)
                    ? _currentReverseScreenTexture
                    : _currentScreenTexture;

                if (texToRestore != null)
                {
                    materials[index].SetTexture(_currentTexturePropertyName, texToRestore);
                }
            }

            targetRenderer.sharedMaterials = materials;
        }

        /// <summary>
        /// Устанавливает текстуру на материале экрана и переключает активный рендерер.
        /// </summary>
        public void SetScreenTexture(Texture texture, string propertyName = "_MainTex", bool isReversed = false)
        {
            if (isReversed)
            {
                _currentReverseScreenTexture = texture;
            }
            else
            {
                _currentScreenTexture = texture;
            }
            _currentTexturePropertyName = propertyName;

            Renderer target = (isReversed && _reverseRenderer != null) ? _reverseRenderer : _renderer;
            SetTextureOnRenderer(target, texture, propertyName, TargetMaterialIndex);
            SwitchRenderer(isReversed);
        }

        /// <summary>
        /// Устанавливает текстуру на конкретный рендер без изменения активности экранов.
        /// </summary>
        public void SetTextureForRenderer(Texture texture, string propertyName = "_MainTex", bool isReversed = false)
        {
            if (isReversed)
            {
                _currentReverseScreenTexture = texture;
            }
            else
            {
                _currentScreenTexture = texture;
            }
            _currentTexturePropertyName = propertyName;

            Renderer target = (isReversed && _reverseRenderer != null) ? _reverseRenderer : _renderer;
            SetTextureOnRenderer(target, texture, propertyName, TargetMaterialIndex);
        }

        /// <summary>
        /// Устанавливает текстуру на материале по указанному индексу основного рендерера.
        /// </summary>
        public void SetTexture(Texture texture, string propertyName = "_MainTex", int index = TargetMaterialIndex)
        {
            SetTextureOnRenderer(_renderer, texture, propertyName, index);
        }

        private void SetTextureOnRenderer(Renderer targetRenderer, Texture texture, string propertyName, int index)
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning("<color=yellow>[TVRendererService]</color> Renderer не назначен!");
                return;
            }

            Material[] materials = targetRenderer.sharedMaterials;
            if (materials == null || index < 0 || index >= materials.Length)
            {
                Debug.LogWarning($"<color=yellow>[TVRendererService]</color> Индекс материала {index} выходит за границы массива материалов.");
                return;
            }

            if (materials[index] != null)
            {
                materials[index].SetTexture(propertyName, texture);
            }
        }

        /// <summary>
        /// Сбрасывает материал экрана на значение по умолчанию.
        /// </summary>
        public void ResetToDefaultMaterial()
        {
            if (_defaultMaterial != null && _renderer != null)
            {
                SetMaterialOnRenderer(_renderer, _defaultMaterial, TargetMaterialIndex);
            }
            if (_reverseDefaultMaterial != null && _reverseRenderer != null)
            {
                SetMaterialOnRenderer(_reverseRenderer, _reverseDefaultMaterial, TargetMaterialIndex);
            }
            if (_offDefaultMaterial != null && _offRenderer != null)
            {
                SetMaterialOnRenderer(_offRenderer, _offDefaultMaterial, TargetMaterialIndex);
            }
        }

        /// <summary>
        /// Возвращает текущий материал экрана (по индексу 0).
        /// </summary>
        public Material GetScreenMaterial(bool isReversed = false)
        {
            return GetMaterial(TargetMaterialIndex, isReversed);
        }

        /// <summary>
        /// Возвращает материал по указанному индексу.
        /// </summary>
        public Material GetMaterial(int index = TargetMaterialIndex, bool isReversed = false)
        {
            Renderer target = (isReversed && _reverseRenderer != null) ? _reverseRenderer : _renderer;
            if (target == null)
            {
                return null;
            }

            Material[] materials = target.sharedMaterials;
            if (index >= 0 && index < materials.Length)
            {
                return materials[index];
            }

            return null;
        }
    }
}
