using Core.StateMachines;
using UnityEngine;

namespace GamePlay.States.GameLoop
{
    public class WorkingOnTapeState : IState
    {
        public void Enter()
        {
            Debug.Log("[GameLoop] Started working on tape.");
        }

        public void Exit()
        {
            Debug.Log("[GameLoop] Finished working on tape.");
        }

        public void Update()
        {
        }
    }
}
