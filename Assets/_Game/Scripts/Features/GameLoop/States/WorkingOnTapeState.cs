using Core.Services;
using Core.StateMachines;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.States.GameLoop
{
    public class WorkingOnTapeState : IState
    {
        private readonly VideoPlayerControlsUIView _controlsView;

        public WorkingOnTapeState(VideoPlayerControlsUIView controlsView = null)
        {
            _controlsView = controlsView;
        }

        public void Enter()
        {
            Debug.Log("<color=lightblue>[GameLoop]</color> Started working on tape.");
            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            if (gameStateManager != null)
            {
                gameStateManager.SwitchState<MontageGameState>();
            }
            else if (_controlsView != null)
            {
                _controlsView.SetCanvasActive(true);
            }
        }

        public void Exit()
        {
            Debug.Log("<color=lightblue>[GameLoop]</color> Finished working on tape.");
            if (_controlsView != null)
            {
                _controlsView.SetCanvasActive(false);
            }
        }

        public void Update()
        {
        }
    }
}
