using UnityEngine;

namespace Core.Data
{
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "Core/Sound Config")]
    public class SoundConfig : ScriptableObject
    {
        [Header("Looping Sounds (Background)")]
        public AudioClip phoneCallingSound;
        public AudioClip stepSound;

        [Header("One-Shot Sounds (SFX)")]
        public AudioClip cassetteEjectSound;
        public AudioClip cassetteInsertSound;
        public AudioClip cassetteOnTableSound;
    }
}
