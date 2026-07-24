using UnityEngine;
using UnityEngine.Events;

public class DoorInteractorOpen : MonoBehaviour
{
    [SerializeField] private Animator _doorAnimator;

    private void OnTriggerEnter(Collider other)
    {
        _doorAnimator.SetTrigger("Open");
    }

}
