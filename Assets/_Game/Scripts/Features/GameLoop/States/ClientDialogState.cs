using Core.Services;
using Core.StateMachines;
using GamePlay.Controllers;
using UnityEngine;

namespace GamePlay.States.GameLoop
{
    public class ClientDialogState : IState
    {
        private Data.ClientDataConfig _clientData;

        public void SetClientData(Data.ClientDataConfig clientData)
        {
            _clientData = clientData;
        }

        public void Enter()
        {
            Debug.Log("<color=lightblue>[GameLoop]</color> Client arrived! Displaying dialog...");
            
            var playerViewController = ServiceLocator.Get<PlayerViewController>();
            if (playerViewController != null)
            {
                playerViewController.SwitchToClientDialogueView(_clientData);
            }
        }

        public void Exit()
        {
            Debug.Log("<color=lightblue>[GameLoop]</color> Dialog ended.");

            var playerViewController = ServiceLocator.Get<PlayerViewController>();
            if (playerViewController != null)
            {
                playerViewController.SwitchToRoomView();
            }
        }

        public void Update()
        {
        }
    }
}
