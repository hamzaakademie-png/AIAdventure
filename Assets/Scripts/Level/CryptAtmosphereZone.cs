using System.Collections;
using AIAdventure.Player;
using UnityEngine;

namespace AIAdventure.Level
{
    /// <summary>
    /// Oyuncu mahzene girince ortamı karartır ve kamerayı alçaltır,
    /// çıkınca eski haline döndürür.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CryptAtmosphereZone : MonoBehaviour
    {
        [SerializeField] private float insideAmbient = 0.15f;
        [SerializeField] private float insideSun = 0.1f;
        [SerializeField] private Vector3 insideCameraOffset = new Vector3(0f, 1.6f, -4f);
        [SerializeField] private float insideLookHeight = 0.3f;
        [SerializeField] private float transitionTime = 1.5f;
        [SerializeField] private Light sunLight;
        [SerializeField] private CameraFollow cameraFollow;

        private float outsideAmbient;
        private float outsideReflection;
        private float outsideSun = 1f;
        private Vector3 outsideCameraOffset;
        private float outsideLookHeight;
        private Coroutine fade;

        public void Configure(Light sun, CameraFollow follow)
        {
            sunLight = sun;
            cameraFollow = follow;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            outsideAmbient = RenderSettings.ambientIntensity;
            outsideReflection = RenderSettings.reflectionIntensity;
            if (sunLight != null) outsideSun = sunLight.intensity;
            if (cameraFollow != null)
            {
                outsideCameraOffset = cameraFollow.Offset;
                outsideLookHeight = cameraFollow.LookHeight;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other)) Apply(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) Apply(false);
        }

        private static bool IsPlayer(Collider other) => other.GetComponentInParent<PlayerMovement>() != null;

        private void Apply(bool inside)
        {
            Debug.Log(inside ? "[Mahzen] İçeri girildi." : "[Mahzen] Dışarı çıkıldı.");

            if (cameraFollow != null)
            {
                cameraFollow.SetView(inside ? insideCameraOffset : outsideCameraOffset,
                                     inside ? insideLookHeight : outsideLookHeight);
            }

            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(inside
                ? Fade(insideAmbient, insideAmbient, insideSun)
                : Fade(outsideAmbient, outsideReflection, outsideSun));
        }

        private IEnumerator Fade(float ambient, float reflection, float sun)
        {
            float startAmbient = RenderSettings.ambientIntensity;
            float startReflection = RenderSettings.reflectionIntensity;
            float startSun = sunLight != null ? sunLight.intensity : 0f;
            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionTime;
                RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, ambient, t);
                RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, reflection, t);
                if (sunLight != null) sunLight.intensity = Mathf.Lerp(startSun, sun, t);
                yield return null;
            }
        }
    }
}
