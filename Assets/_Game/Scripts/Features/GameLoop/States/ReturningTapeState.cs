using Core.StateMachines;
using UnityEngine;

namespace GamePlay.States.GameLoop
{
    public class ReturningTapeState : IState
    {
        public void Enter()
        {
            Debug.Log("[GameLoop] Returning tape to client. Mission accomplished.");
        }

        public void Exit()
        {
        }

        public void Update()
        {
        }
    }
}
