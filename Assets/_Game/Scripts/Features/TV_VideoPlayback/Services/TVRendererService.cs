using UnityEngine;

namespace Core.Services
{
    public class TVRendererService : IInitializable
    {
        private Renderer _renderer;
        private Material _defaultMaterial;
        private const int TargetMaterialIndex = 0;

        public Renderer TargetRenderer => _renderer;

        public TVRendererService(Renderer renderer = null)
        {
            _renderer = renderer;
        }

        public void BindRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        public void Initialize()
        {
            if (_renderer != null && _renderer.sharedMaterials != null && _renderer.sharedMaterials.Length > TargetMaterialIndex)
            {
                _defaultMaterial = _renderer.sharedMaterials[TargetMaterialIndex];
            }
        }

        /// <summary>
        /// Переключает/устанавливает материал экрана телевизора (по индексу 0).
        /// </summary>
        /// <param name="material">Новый материал экрана.</param>
        public void SetScreenMaterial(Material material)
        {
            SetMaterial(material, TargetMaterialIndex);
        }

        /// <summary>
        /// Переключает/устанавливает материал на Renderer по указанному индексу (по умолчанию 0).
        /// </summary>
        /// <param name="material">Новый материал.</param>
        /// <param name="index">Индекс элемента в массиве материалов.</param>
        public void SetMaterial(Material material, int index = TargetMaterialIndex)
        {
            if (_renderer == null)
            {
                Debug.LogWarning("[TVRendererService] Renderer не назначен!");
                return;
            }

            Material[] materials = _renderer.materials;
            if (index < 0 || index >= materials.Length)
            {
                Debug.LogWarning($"[TVRendererService] Индекс материала {index} выходит за границы массива материалов (длина: {materials.Length}).");
                return;
            }

            materials[index] = material;
            _renderer.materials = materials;
        }

        /// <summary>
        /// Устанавливает текстуру на материале экрана (по индексу 0).
        /// </summary>
        /// <param name="texture">Текстура для установки.</param>
        /// <param name="propertyName">Имя свойства текстуры в шейдере (по умолчанию "_MainTex").</param>
        public void SetScreenTexture(Texture texture, string propertyName = "_MainTex")
        {
            SetTexture(texture, propertyName, TargetMaterialIndex);
        }

        /// <summary>
        /// Устанавливает текстуру на материале по указанному индексу.
        /// </summary>
        /// <param name="texture">Текстура для установки.</param>
        /// <param name="propertyName">Имя свойства текстуры в шейдере.</param>
        /// <param name="index">Индекс материала.</param>
        public void SetTexture(Texture texture, string propertyName = "_MainTex", int index = TargetMaterialIndex)
        {
            if (_renderer == null)
            {
                Debug.LogWarning("[TVRendererService] Renderer не назначен!");
                return;
            }

            Material[] materials = _renderer.materials;
            if (index < 0 || index >= materials.Length)
            {
                Debug.LogWarning($"[TVRendererService] Индекс материала {index} выходит за границы массива материалов (длина: {materials.Length}).");
                return;
            }

            if (materials[index] != null)
            {
                materials[index].SetTexture(propertyName, texture);
            }
        }

        /// <summary>
        /// Сбрасывает материал экрана (индекс 0) на значение по умолчанию.
        /// </summary>
        public void ResetToDefaultMaterial()
        {
            if (_defaultMaterial != null)
            {
                SetScreenMaterial(_defaultMaterial);
            }
        }

        /// <summary>
        /// Возвращает текущий материал экрана (по индексу 0).
        /// </summary>
        public Material GetScreenMaterial()
        {
            return GetMaterial(TargetMaterialIndex);
        }

        /// <summary>
        /// Возвращает материал по указанному индексу.
        /// </summary>
        public Material GetMaterial(int index = TargetMaterialIndex)
        {
            if (_renderer == null)
            {
                return null;
            }

            Material[] materials = _renderer.sharedMaterials;
            if (index >= 0 && index < materials.Length)
            {
                return materials[index];
            }

            return null;
        }
    }
}
