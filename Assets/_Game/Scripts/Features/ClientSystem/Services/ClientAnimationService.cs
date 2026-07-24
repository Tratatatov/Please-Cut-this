using UnityEngine;

namespace Core.Services
{
    public enum ClientAnimationState
    {
        Idle,
        Walk,
        Talk,
        Success,
        Failure,
        Wave,
        Leave
    }

    public class ClientAnimationService
    {
        private Animator _animator;

        public Animator CurrentAnimator => _animator;

        public void BindAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void ClearAnimator()
        {
            _animator = null;
        }

        public void PlayIdle(float crossFadeDuration = 0.1f)
        {
            SetBool("Idle", true);
            SetBool("Walk", false);
            PlayState(ClientAnimationState.Idle, crossFadeDuration);
        }

        public void PlayWalk(float crossFadeDuration = 0.1f)
        {
            SetBool("Walk", true);
            SetBool("Idle", false);
            PlayState(ClientAnimationState.Walk, crossFadeDuration);
        }

        public void PlayWalking(float crossFadeDuration = 0.1f)
        {
            PlayWalk(crossFadeDuration);
        }

        public void PlayTakeAnimation()
        {
            SetBool("Idle", true);
            SetBool("Walk", false);
            SetTrigger("Take");
        }

        public void PlayState(ClientAnimationState state, float crossFadeDuration = 0.1f)
        {
            PlayState(state.ToString(), crossFadeDuration);
        }

        public void PlayState(string stateName, float crossFadeDuration = 0.1f)
        {
            if (_animator == null)
            {
                Debug.LogWarning("[ClientAnimationService] Animator не назначен!");
                return;
            }

            if (crossFadeDuration > 0f)
            {
                _animator.CrossFade(stateName, crossFadeDuration);
            }
            else
            {
                _animator.Play(stateName);
            }
        }

        public void PlayState(int stateHash, float crossFadeDuration = 0.1f)
        {
            if (_animator == null)
            {
                Debug.LogWarning("[ClientAnimationService] Animator не назначен!");
                return;
            }

            if (crossFadeDuration > 0f)
            {
                _animator.CrossFade(stateHash, crossFadeDuration);
            }
            else
            {
                _animator.Play(stateHash);
            }
        }

        public void SetTrigger(string triggerName)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(triggerName);
            }
        }

        public void SetBool(string boolName, bool value)
        {
            if (_animator != null)
            {
                _animator.SetBool(boolName, value);
            }
        }

        public void SetFloat(string floatName, float value)
        {
            if (_animator != null)
            {
                _animator.SetFloat(floatName, value);
            }
        }
    }
}
