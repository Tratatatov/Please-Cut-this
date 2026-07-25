using Core.StateMachines;
using Core.Services;

namespace GamePlay.States.PlayerView
{
    public class RoomViewState : IState
    {
        private readonly CameraControlService _cameraController;

        public RoomViewState(CameraControlService cameraControlService)
        {
            _cameraController = cameraControlService;
        }

        public void Enter()
        {
            _cameraController.SwitchToCamera1();
        }

        public void Exit()
        {
        }

        public void Update()
        {
        }
    }
}
