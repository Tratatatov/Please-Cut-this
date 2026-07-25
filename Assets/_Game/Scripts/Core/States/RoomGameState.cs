using Core.Services;
using GamePlay.Controllers;
using UnityEngine;

public class RoomGameState : IGameState
{
    private readonly PlayerViewController _playerViewController;

    public RoomGameState(PlayerViewController playerViewController = null)
    {
        _playerViewController = playerViewController;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter Room State");
        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToRoomView();
    }

    public void Update()
    {
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit Room State");
    }
}
