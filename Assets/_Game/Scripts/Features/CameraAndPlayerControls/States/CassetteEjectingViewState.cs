using Core.Services;
using Core.StateMachines;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.States.PlayerView
{
    public class CassetteEjectingViewState : IState
    {
        private readonly CameraControlService _cameraController;
        private readonly System.Action _onCompleted;
        private readonly float _duration;
        private float _timer;

        public CassetteEjectingViewState(CameraControlService cameraControlService, System.Action onCompleted = null, float duration = 2.0f)
        {
            _cameraController = cameraControlService;
            _onCompleted = onCompleted;
            _duration = duration;
        }

        public void Enter()
        {
            _timer = 0f;
            _cameraController?.SwitchToCassetteCamera(lockCamera: true);

            var controlsView = ServiceLocator.Get<VideoPlayerControlsUIView>();
            controlsView?.SetCanvasActive(false);

            var tvService = ServiceLocator.Get<TVRendererService>();
            tvService?.UpdateScreenState();

            Debug.Log($"<color=orange>[CassetteEjectingViewState]</color> Запуск анимации извлечения кассеты (длительность: {_duration} сек). Управление заблокировано.");
        }

        public void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                Debug.Log("<color=orange>[CassetteEjectingViewState]</color> Анимация извлечения кассеты завершена.");
                _onCompleted?.Invoke();
            }
        }

        public void Exit()
        {
            _cameraController?.UnlockCamera();
        }
    }
}
