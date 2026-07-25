using Core.Services;
using GamePlay.Data;
using UnityEngine;

public class ClientDialogueGameState : IGameState
{
    private readonly ClientDataConfig _clientData;

    public ClientDialogueGameState(ClientDataConfig clientData = null)
    {
        _clientData = clientData;
    }

    public void Enter()
    {
        Debug.Log("<color=cyan>[GameState]</color> Enter Client Dialogue State");
        var dialogueService = ServiceLocator.Get<DialogueService>();
        if (dialogueService != null && _clientData != null)
        {
            dialogueService.PlayDialogue(_clientData);
        }
    }

    public void Update()
    {
    }

    public void Exit()
    {
        Debug.Log("<color=cyan>[GameState]</color> Exit Client Dialogue State");
        var dialogueService = ServiceLocator.Get<DialogueService>();
        dialogueService?.StopDialogue();
    }
}

