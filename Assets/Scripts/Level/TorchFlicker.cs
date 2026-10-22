using UnityEngine;

namespace AIAdventure.Level
{
    /// <summary>
    /// Meşale ışığını rastgele titreştirir.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.6f;
        [SerializeField] private float speed = 3f;

        private Light torchLight;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            torchLight = GetComponent<Light>();
            baseIntensity = torchLight.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed);
            torchLight.intensity = baseIntensity + (noise - 0.5f) * 2f * amplitude;
        }
    }
}
