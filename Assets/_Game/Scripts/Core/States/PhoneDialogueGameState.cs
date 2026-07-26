using Core.Services;
using GamePlay.Controllers;
using GamePlay.Data;
using UnityEngine;

public class PhoneDialogueGameState : IGameState
{
    private readonly PlayerViewController _playerViewController;
    private readonly GameControlsConfig _controlsConfig;
    private DialogueService _dialogueService;

    public PhoneDialogueGameState(GameControlsConfig controlsConfig, PlayerViewController playerViewController = null)
    {
        _controlsConfig = controlsConfig;
        _playerViewController = playerViewController;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter Phone Dialogue State");
        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToRoomView();
        _dialogueService = ServiceLocator.Get<DialogueService>();
    }

    public void Update()
    {
        KeyCode interactKey = _controlsConfig != null ? _controlsConfig.InteractCassetteKey : KeyCode.E;
        if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0))
        {
            _dialogueService?.TryAdvanceManual();
        }
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit Phone Dialogue State");
    }
}
