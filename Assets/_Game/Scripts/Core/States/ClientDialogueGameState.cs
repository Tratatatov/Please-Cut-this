using UnityEngine;

public class ClientDialogueGameState : IGameState
{
    public void Enter()
    {
        Debug.Log("[GameState] Enter Client Dialogue State");
        // TODO: Show dialogue UI, characters, etc.
    }

    public void Update()
    {
        // TODO: Handle input/updates specific to dialogue
    }

    public void Exit()
    {
        Debug.Log("[GameState] Exit Client Dialogue State");
        // TODO: Hide dialogue UI
    }
}
