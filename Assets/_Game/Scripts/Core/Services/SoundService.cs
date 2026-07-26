using Core.Data;
using UnityEngine;

namespace Core.Services
{
    public class SoundService : IInitializable
    {
        private readonly SoundConfig _config;
        private readonly AudioSource _bgmSource;
        private readonly AudioSource _sfxSource;

        public SoundService(SoundConfig config, AudioSource bgmSource, AudioSource sfxSource)
        {
            _config = config;
            _bgmSource = bgmSource;
            _sfxSource = sfxSource;
        }

        public void Initialize()
        {
            if (_bgmSource != null)
            {
                _bgmSource.loop = true;
                _bgmSource.Stop();
            }
        }

        public void SetPhoneCallingSound(bool isOn)
        {
            if (_config == null || _bgmSource == null || _config.phoneCallingSound == null) return;

            if (isOn)
            {
                _bgmSource.clip = _config.phoneCallingSound;
                if (!_bgmSource.isPlaying)
                {
                    _bgmSource.Play();
                }
            }
            else
            {
                if (_bgmSource.clip == _config.phoneCallingSound)
                {
                    _bgmSource.Stop();
                    _bgmSource.clip = null;
                }
            }
        }

        public void SetStepSound(bool isOn)
        {
            if (_config == null || _bgmSource == null || _config.stepSound == null) return;

            if (isOn)
            {
                _bgmSource.clip = _config.stepSound;
                if (!_bgmSource.isPlaying)
                {
                    _bgmSource.Play();
                }
            }
            else
            {
                if (_bgmSource.clip == _config.stepSound)
                {
                    _bgmSource.Stop();
                    _bgmSource.clip = null;
                }
            }
        }

        public void PlayCasseteEnjectSound()
        {
            PlaySFX(_config?.cassetteEjectSound);
        }

        public void PlayCasseteInsertSound()
        {
            PlaySFX(_config?.cassetteInsertSound);
        }

        public void PlayCasseteOnTapleSound()
        {
            PlaySFX(_config?.cassetteOnTableSound);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (_sfxSource != null && clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }
    }
}
