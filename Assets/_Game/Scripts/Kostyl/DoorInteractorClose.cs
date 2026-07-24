using UnityEngine;

public class DoorInteractorClose : MonoBehaviour
{
    [SerializeField] private Animator _doorAnimator;

    private void OnTriggerEnter(Collider other)
    {
        _doorAnimator.SetTrigger("Close");
    }

}
