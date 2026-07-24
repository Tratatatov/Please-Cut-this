using Core.Services;
using GamePlay.Data;
using UnityEngine;

public class ClientDialogueGameState : IGameState
{
    private readonly ClientDataSO _clientData;

    public ClientDialogueGameState(ClientDataSO clientData = null)
    {
        _clientData = clientData;
    }

    public void Enter()
    {
        Debug.Log("[GameState] Enter Client Dialogue State");
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
        Debug.Log("[GameState] Exit Client Dialogue State");
        var dialogueService = ServiceLocator.Get<DialogueService>();
        dialogueService?.StopDialogue();
    }
}

