using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : IInitializable, IUpdatable
{
    private IGameState _currentState;
    private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();

    public void RegisterState(IGameState state)
    {
        var type = state.GetType();
        if (!_states.ContainsKey(type))
        {
            _states.Add(type, state);
        }
    }

    public void SwitchState<T>() where T : IGameState
    {
        var type = typeof(T);
        if (_states.TryGetValue(type, out IGameState nextState))
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
