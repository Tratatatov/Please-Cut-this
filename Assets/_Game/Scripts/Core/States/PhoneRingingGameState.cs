using System;
using Core.Services;
using GamePlay.Controllers;
using GamePlay.Data;
using UnityEngine;

public class PhoneRingingGameState : IGameState
{
    private readonly PlayerViewController _playerViewController;
    private readonly GameControlsConfig _controlsConfig;
    private readonly Action _onPhonePickedUp;

    public PhoneRingingGameState(GameControlsConfig controlsConfig, PlayerViewController playerViewController, Action onPhonePickedUp)
    {
        _controlsConfig = controlsConfig;
        _playerViewController = playerViewController;
        _onPhonePickedUp = onPhonePickedUp;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter Phone Ringing State. Ring ring! Press E to pick up.");
        var playerVC = _playerViewController ?? ServiceLocator.Get<PlayerViewController>();
        playerVC?.SwitchToRoomView();
    }

    public void Update()
    {
        KeyCode interactKey = _controlsConfig != null ? _controlsConfig.InteractCassetteKey : KeyCode.E;
        if (Input.GetKeyDown(interactKey))
        {
            _onPhonePickedUp?.Invoke();
        }
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit Phone Ringing State. Phone picked up.");
    }
}
