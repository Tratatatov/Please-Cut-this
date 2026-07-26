using Core.Services;
using GamePlay.Controllers;
using UnityEngine;

public class EndCinematicGameState : IGameState
{
    private readonly PlayerViewController _playerViewController;

    public EndCinematicGameState(PlayerViewController playerViewController = null)
    {
        _playerViewController = playerViewController;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter End Cinematic State (No UI)");

        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToVideoEditingView();
    }

    public void Update()
    {
        // No input allowed during the cinematic
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit End Cinematic State");
        var tvService = ServiceLocator.Get<TVRendererService>();
        tvService?.SwitchToOffState();
    }
}
