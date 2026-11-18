using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>Separa los efectos de las cartas y botones de la música ambiental continua.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        // Atenuación base de los efectos; el volumen general lo controla GamePreferences.
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.65f;

        [Header("Background music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicLevel = 0.5f;

        [Header("Cards and interface")]
        [SerializeField] private AudioClip cardFlip;
        [SerializeField] private AudioClip sealSelect;
        [SerializeField] private AudioClip sealDeselect;
        [SerializeField] private AudioClip newRound;

        [Header("Countdown")]
        [SerializeField] private AudioClip countdownTick;
        [SerializeField] private AudioClip countdownWarning;

        [Header("Results")]
        [SerializeField] private AudioClip roundEnd;
        [SerializeField] private AudioClip topOne;
        [SerializeField] private AudioClip topTwo;
        [SerializeField] private AudioClip topThree;

        private void Awake()
        {
            EnsureSource();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnDisable()
        {
            StopAllSounds();
            if (musicSource != null) musicSource.Stop();
        }

        private void OnEnable()
        {
            // Inicia la música solo durante la ejecución, también al reactivar el componente.
            if (Application.isPlaying) StartMusic();
        }

        private void StartMusic()
        {
            if (musicSource == null || backgroundMusic == null || musicSource.isPlaying) return;
            musicSource.clip = backgroundMusic;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;
            musicSource.Play();
        }

        private void Update()
        {
            // Suaviza el inicio y los cambios de volumen sin depender del tiempo de la ronda.
            // El volumen general se aplica una sola vez, mediante AudioListener.
            if (musicSource == null) return;
            float target = musicLevel * GamePreferences.Current.MusicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * 0.15f);
        }

        private void EnsureSource()
        {
            if (source == null) source = GetComponent<AudioSource>();
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null || !isActiveAndEnabled) return;
            EnsureSource();
            source.PlayOneShot(clip, masterVolume * GamePreferences.Current.EffectsVolume * volume);
        }

        public void StopAllSounds()
        {
            // Cancela los efectos y premios pendientes; la música continúa entre rondas y menús.
            StopAllCoroutines();
            EnsureSource();
            if (source != null) source.Stop();
        }

        public void PlayCardFlip() => Play(cardFlip, 0.8f);
        public void PlaySealSelect() => Play(sealSelect, 0.85f);
        public void PlaySealDeselect() => Play(sealDeselect, 0.7f);
        public void PlayNewRound() => Play(newRound, 0.8f);
        public void PlayRoundEnd() => Play(roundEnd, 0.8f);

        public void PlayCountdown(int secondsRemaining)
        {
            if (secondsRemaining <= 0) return;
            Play(secondsRemaining <= 5 ? countdownWarning : countdownTick,
                secondsRemaining <= 5 ? 0.55f : 0.2f);
        }

        public void PlayTopResult(int place)
        {
            AudioClip clip = place switch { 1 => topOne, 2 => topTwo, 3 => topThree, _ => null };
            if (clip != null && isActiveAndEnabled) StartCoroutine(PlayResultAfterDelay(clip));
        }

        private IEnumerator PlayResultAfterDelay(AudioClip clip)
        {
            yield return new WaitForSeconds(0.3f);
            Play(clip);
        }
    }
}
