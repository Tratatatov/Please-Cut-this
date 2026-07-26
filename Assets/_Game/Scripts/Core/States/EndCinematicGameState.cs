using Core.Services;
using GamePlay.Controllers;
using UnityEngine;
using GamePlay.View;
using GamePlay.Services;

public class EndCinematicGameState : IGameState
{
    private readonly PlayerViewController _playerViewController;
    private readonly EndDayStatsUIView _endDayStatsView;

    public EndCinematicGameState(PlayerViewController playerViewController = null, EndDayStatsUIView endDayStatsView = null)
    {
        _playerViewController = playerViewController;
        _endDayStatsView = endDayStatsView;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter End Cinematic State (No UI)");

        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToVideoEditingView();

        var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
        if (videoPlayerService != null)
        {
            videoPlayerService.OnVideoFinished += HandleVideoFinished;
        }
    }

    private void HandleVideoFinished()
    {
        Debug.Log("<color=cyan>[EndCinematicGameState]</color> Видео окончания дня завершено. Показ статистики.");
        
        var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
        if (videoPlayerService != null)
        {
            videoPlayerService.OnVideoFinished -= HandleVideoFinished;
        }

        var statsService = ServiceLocator.Get<GameStatsService>();
        float averageScore = statsService != null ? statsService.GetAverageScore() : 0f;

        var view = _endDayStatsView ?? ServiceLocator.Get<EndDayStatsUIView>();
        if (view != null)
        {
            view.ShowStats(averageScore);
        }
        else
        {
            Debug.LogWarning("<color=red>[EndCinematicGameState]</color> EndDayStatsUIView не найден!");
        }
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

        var videoPlayerService = ServiceLocator.Get<VideoPlayerService>();
        if (videoPlayerService != null)
        {
            videoPlayerService.OnVideoFinished -= HandleVideoFinished;
        }
    }
}
