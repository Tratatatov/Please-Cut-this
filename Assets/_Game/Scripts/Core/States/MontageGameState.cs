using UnityEngine;

public class MontageGameState : IGameState
{
    public void Enter()
    {
        Debug.Log("[GameState] Enter Montage State (Interact with video)");
        // TODO: Enable montage UI, activate timeline, etc.
    }

    public void Update()
    {
        // TODO: Handle input/updates specific to montage
    }

    public void Exit()
    {
        Debug.Log("[GameState] Exit Montage State");
        // TODO: Disable montage UI, hide timeline, etc.
    }
}
