using Core.StateMachines;
using GamePlay.Data;
using GamePlay.States.GameLoop;
using GamePlay.View;
using UnityEngine;
using System.Collections.Generic;

namespace GamePlay.Controllers
{
    public class GameLoopController : IInitializable, IUpdatable
    {
        private readonly StateMachine _stateMachine;
        
        private readonly WaitingForClientState _waitingState;
        private readonly ClientDialogState _dialogState;
        private readonly WorkingOnTapeState _workingState;
        private readonly ReturningTapeState _returningState;

        private DayScheduleSO _currentSchedule;
        private Queue<ClientDataSO> _clientsQueue;
        private ClientDataSO _currentClient;
        private readonly ClientView _clientView;

        public GameLoopController(DayScheduleSO initialSchedule = null, ClientView clientView = null)
        {
            _stateMachine = new StateMachine();
            
            _waitingState = new WaitingForClientState();
            _dialogState = new ClientDialogState();
            _workingState = new WorkingOnTapeState();
            _returningState = new ReturningTapeState();
            
            _currentSchedule = initialSchedule;
            _clientView = clientView;
        }

        public void Initialize()
        {
            if (_currentSchedule != null)
            {
                LoadSchedule(_currentSchedule);
            }
            else
            {
                _stateMachine.ChangeState(_waitingState);
            }
        }

        public void LoadSchedule(DayScheduleSO schedule)
        {
            _currentSchedule = schedule;
            _clientsQueue = new Queue<ClientDataSO>(schedule.Clients);
            _stateMachine.ChangeState(_waitingState);
        }

        public void Update()
        {
            _stateMachine.Update();

            // Mock logic to switch states using Spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AdvanceStateMock();
            }
        }

        private void AdvanceStateMock()
        {
            var currentState = _stateMachine.CurrentState;
            if (currentState == _waitingState)
            {
                if (_clientsQueue != null && _clientsQueue.Count > 0)
                {
                    _currentClient = _clientsQueue.Dequeue();
                    Debug.Log($"[GameLoop] Client {_currentClient.ClientName} is arriving.");
                    _clientView?.SetClientData(_currentClient);
                    _stateMachine.ChangeState(_dialogState);
                }
                else
                {
                    Debug.Log("[GameLoop] No more clients for today.");
                    _clientView?.SetClientData(null);
                }
            }
            else if (currentState == _dialogState)
            {
                _stateMachine.ChangeState(_workingState);
            }
            else if (currentState == _workingState)
            {
                _stateMachine.ChangeState(_returningState);
            }
            else if (currentState == _returningState)
            {
                _clientView?.SetClientData(null);
                _stateMachine.ChangeState(_waitingState);
            }
        }
    }
}
