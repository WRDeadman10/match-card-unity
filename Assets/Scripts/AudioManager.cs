using UnityEngine;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class AudioManager
    {
        private readonly AudioSource audioSource;
        private readonly AudioClip flipClip;
        private readonly AudioClip matchClip;
        private readonly AudioClip mismatchClip;
        private readonly AudioClip gameOverClip;

        public AudioManager(GameObject host)
        {
            audioSource = host.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = host.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            flipClip = Resources.Load<AudioClip>("Audio/card-flip");
            matchClip = Resources.Load<AudioClip>("Audio/match");
            mismatchClip = Resources.Load<AudioClip>("Audio/mismatch");
            gameOverClip = Resources.Load<AudioClip>("Audio/game-over");
        }

        public void PlayFlip()
        {
            PlayOneShot(flipClip);
        }

        public void PlayMatch()
        {
            PlayOneShot(matchClip);
        }

        public void PlayMismatch()
        {
            PlayOneShot(mismatchClip);
        }

        public void PlayGameOver()
        {
            PlayOneShot(gameOverClip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
