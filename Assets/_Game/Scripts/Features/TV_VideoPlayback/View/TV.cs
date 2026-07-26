/*  */using Core.Services;
using UnityEngine;

namespace GamePlay.View
{
    public class TV : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Renderer _reverseRenderer;
        [SerializeField] private Renderer _offRenderer;

        private TVRendererService _tvRendererService;

        public TVRendererService TVRendererService => _tvRendererService;

        public void Initialize()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            _tvRendererService = new TVRendererService(_renderer, _reverseRenderer, _offRenderer);
            _tvRendererService.Initialize();
        }

        public void Initialize(TVRendererService tvRendererService)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            _tvRendererService = tvRendererService ?? new TVRendererService(_renderer, _reverseRenderer, _offRenderer);
            _tvRendererService.BindRenderer(_renderer, _reverseRenderer, _offRenderer);
            _tvRendererService.Initialize();
        }

        public void SetScreenMaterial(Material material, Material reverseMaterial = null)
        {
            _tvRendererService?.SetScreenMaterial(material, reverseMaterial);
        }

        public void ResetToDefaultMaterial()
        {
            _tvRendererService?.ResetToDefaultMaterial();
        }

        public void TurnOff()
        {
            _tvRendererService?.SwitchToOffState();
        }

        public void SetForwardState()
        {
            _tvRendererService?.SwitchToForwardState();
        }

        public void SetReverseState()
        {
            _tvRendererService?.SwitchToReverseState();
        }
    }
}
