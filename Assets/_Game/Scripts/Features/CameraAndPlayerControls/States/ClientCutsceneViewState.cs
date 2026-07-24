using Core.Services;
using Core.StateMachines;
using GamePlay.Controllers;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.States.PlayerView
{
    public class ClientCutsceneViewState : IState
    {
        private readonly CameraControlService _cameraController;
        private readonly float _duration;
        private readonly System.Action _onCompleted;
        private float _timer;

        public ClientCutsceneViewState(CameraControlService cameraControlService, System.Action onCompleted = null, float duration = 2.5f)
        {
            _cameraController = cameraControlService;
            _onCompleted = onCompleted;
            _duration = duration;
        }

        public void Enter()
        {
            _timer = 0f;
            _cameraController?.SwitchToClientCamera(lockCamera: true);

            Debug.Log($"[ClientCutsceneViewState] Запуск моковой сцены/анимации после диалога (длительность: {_duration} сек)...");

            // Запускаем анимацию передачи/взятия кассеты (Take trigger)
            var clientsController = ServiceLocator.Get<ClientsController>();
            if (clientsController != null && clientsController.ClientView != null)
            {
                clientsController.ClientView.PlayTakeAnimation();
            }
        }

        public void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                Debug.Log("[ClientCutsceneViewState] Катсцена завершена. Возврат к стандартному управлению камерой.");
                
                // Возвращаем анимацию в Idle перед завершением
                var clientsController = ServiceLocator.Get<ClientsController>();
                if (clientsController != null && clientsController.ClientView != null)
                {
                    clientsController.ClientView.PlayIdle();
                }

                _onCompleted?.Invoke();
            }
        }

        public void Exit()
        {
            _cameraController?.UnlockCamera();
        }
    }
}
