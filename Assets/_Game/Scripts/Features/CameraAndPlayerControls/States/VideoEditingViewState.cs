using Core.StateMachines;
using Core.Services;

namespace GamePlay.States.PlayerView
{
    public class VideoEditingViewState : IState
    {
        private readonly CameraControlService _cameraController;

        public VideoEditingViewState(CameraControlService cameraControlService)
        {
            _cameraController = cameraControlService;
        }

        public void Enter()
        {
            _cameraController.SwitchToCamera2();
        }

        public void Exit()
        {
        }

        public void Update()
        {
        }
    }
}
