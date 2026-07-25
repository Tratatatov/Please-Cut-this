using System;
using System.Collections.Generic;
using Core.StateMachines;
using UnityEngine;

public class GameStateManager : IInitializable, IUpdatable
{
    private IState _currentState;
    private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

    public IState CurrentState => _currentState;

    public void RegisterState(IState state)
    {
        var type = state.GetType();
        if (!_states.ContainsKey(type))
        {
            _states.Add(type, state);
        }
    }

    public void SwitchState<T>() where T : IState
    {
        var type = typeof(T);
        if (_states.TryGetValue(type, out IState nextState))
        {
            if (_currentState != null)
            {
                _currentState.Exit();
            }

            _currentState = nextState;
            _currentState.Enter();
        }
        else
        {
            Debug.LogError($"GameStateManager: State {type.Name} is not registered!");
        }
    }

    public void Initialize()
    {
        // Manager initialization if needed
    }

    public void Update()
    {
        _currentState?.Update();
    }
}
