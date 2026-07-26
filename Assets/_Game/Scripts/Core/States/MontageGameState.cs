using Core.Services;
using GamePlay.Controllers;
using GamePlay.View;
using UnityEngine;

public class MontageGameState : IGameState
{
    private readonly VideoPlayerControlsUIView _controlsView;
    private readonly PlayerViewController _playerViewController;

    public MontageGameState(VideoPlayerControlsUIView controlsView = null, PlayerViewController playerViewController = null)
    {
        _controlsView = controlsView;
        _playerViewController = playerViewController;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter Montage State (Interact with video)");

        var tvService = ServiceLocator.Get<TVRendererService>();
        if (tvService != null && !tvService.IsCassetteInserted)
        {
            Debug.LogWarning("<color=yellow>[MontageGameState]</color> Вход в режим монтажа заблокирован: кассета не вставлена в плеер!");
            var gameStateManager = ServiceLocator.Get<GameStateManager>();
            gameStateManager?.SwitchState<RoomGameState>();
            return;
        }

        if (_controlsView != null)
        {
            _controlsView.SetCanvasActive(true);
        }

        tvService?.SwitchToForwardState();

        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToVideoEditingView();
    }

    public void Update()
    {
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit Montage State");
        if (_controlsView != null)
        {
            _controlsView.SetCanvasActive(false);
        }

        var tvService = ServiceLocator.Get<TVRendererService>();
        tvService?.SwitchToOffState();
    }
}
