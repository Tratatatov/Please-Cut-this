using Core.StateMachines;
using UnityEngine;

namespace GamePlay.States.GameLoop
{
    public class WaitingForClientState : IState
    {
        public void Enter()
        {
            Debug.Log("[GameLoop] Waiting for next client...");
        }

        public void Exit()
        {
        }

        public void Update()
        {
        }
    }
}
