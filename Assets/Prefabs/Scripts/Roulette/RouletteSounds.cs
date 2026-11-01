using UnityEngine;

namespace AIAdventure.Roulette
{
    /// <summary>
    /// Rulet masasının sesleri. Ses dosyaları Inspector'dan atanır;
    /// atanmamış bir ses sessizce atlanır, oyun bozulmaz.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RouletteSounds : MonoBehaviour
    {
        [SerializeField] private AudioClip click;
        [SerializeField] private AudioClip gunshot;
        [SerializeField] private AudioClip spin;
        [SerializeField] private AudioClip tableSlam;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        public void PlayClick() => Play(click);
        public void PlayGunshot() => Play(gunshot);
        public void PlaySpin() => Play(spin);
        public void PlayTableSlam() => Play(tableSlam);

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null) source.PlayOneShot(clip, volume);
        }
    }
}
