using Core.Services;
using Core.StateMachines;
using UnityEngine;

namespace GamePlay.States.PlayerView
{
    public class CassetteInsertingViewState : IState
    {
        private readonly CameraControlService _cameraController;
        private readonly System.Action _onCompleted;
        private readonly float _duration;
        private float _timer;
        private bool _hasTriggeredCompleted;

        public CassetteInsertingViewState(CameraControlService cameraControlService, System.Action onCompleted = null, float duration = 2.0f)
        {
            _cameraController = cameraControlService;
            _onCompleted = onCompleted;
            _duration = duration;
        }

        public void Enter()
        {
            _timer = 0f;
            _hasTriggeredCompleted = false;
            Debug.Log("<color=magenta>[DEBUG_STEP]</color> CassetteInsertingViewState.Enter: Switching camera to CassetteCamera."); //DELETE THIS AFTER DEBUG
            _cameraController?.SwitchToCassetteCamera(lockCamera: true);

            Debug.Log($"<color=orange>[CassetteInsertingViewState]</color> Запуск анимации вставки кассеты (длительность: {_duration} сек). Управление заблокировано.");
        }

        public void Update()
        {
            if (_hasTriggeredCompleted) return;

            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                _hasTriggeredCompleted = true;
                Debug.Log($"<color=magenta>[DEBUG_STEP]</color> CassetteInsertingViewState.Update: Timer finished ({_timer:F2}s >= {_duration}s). Invoking callback."); //DELETE THIS AFTER DEBUG
                _onCompleted?.Invoke();
            }
        }

        public void Exit()
        {
            Debug.Log("<color=magenta>[DEBUG_STEP]</color> CassetteInsertingViewState.Exit: Unlocking camera."); //DELETE THIS AFTER DEBUG
            _cameraController?.UnlockCamera();
        }
    }
}
