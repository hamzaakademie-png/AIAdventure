using System.Collections;
using AIAdventure.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace AIAdventure.Level
{
    /// <summary>
    /// Oyuncu mahzene girince ortam ışığını sabit bir renge çevirir, sis çöktürür
    /// ve kamerayı alçaltır; çıkınca her şeyi eski haline döndürür.
    /// Mahzenin aydınlığı "Inside Ambient Color" ile ayarlanır: renk ne kadar
    /// açıksa içerisi o kadar aydınlık olur.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CryptAtmosphereZone : MonoBehaviour
    {
        [Header("Işık")]
        [SerializeField] private Color insideAmbientColor = new Color(0.32f, 0.27f, 0.22f);
        [SerializeField] private float insideSun = 0.05f;
        [Tooltip("Mahzenin genel aydınlığı. Karanlıksa artır (ör. 0.8), fazla aydınlıksa azalt.")]
        [SerializeField] private float insideFillIntensity = 0.5f;
        [SerializeField] private Color fillColor = new Color(1f, 0.85f, 0.7f);

        [Header("Sis")]
        [SerializeField] private float insideFogDensity = 0.012f;
        [SerializeField] private Color fogColor = new Color(0.10f, 0.09f, 0.08f);

        [Header("Kamera")]
        [SerializeField] private Vector3 insideCameraOffset = new Vector3(0f, 1.6f, -4f);
        [SerializeField] private float insideLookHeight = 0.3f;

        [Header("Geçiş")]
        [SerializeField] private float transitionTime = 1.5f;
        [SerializeField] private Light sunLight;
        [SerializeField] private CameraFollow cameraFollow;

        private AmbientMode outsideMode;
        private Color outsideAmbientColor;
        private float outsideReflection;
        private float outsideSun = 1f;
        private Vector3 outsideCameraOffset;
        private float outsideLookHeight;
        private Coroutine fade;
        private Light fillLight;

        public void Configure(Light sun, CameraFollow follow)
        {
            sunLight = sun;
            cameraFollow = follow;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            outsideMode = RenderSettings.ambientMode;
            outsideAmbientColor = RenderSettings.ambientLight;
            outsideReflection = RenderSettings.reflectionIntensity;
            if (sunLight != null) outsideSun = sunLight.intensity;
            if (cameraFollow != null)
            {
                outsideCameraOffset = cameraFollow.Offset;
                outsideLookHeight = cameraFollow.LookHeight;
            }

            CreateFillLight();
        }

        /// <summary>
        /// Gölgesiz, yukarıdan vuran yumuşak bir ışık. Tavanı delip mahzenin
        /// her yerini eşit aydınlatır; sadece oyuncu içerideyken yanar.
        /// </summary>
        private void CreateFillLight()
        {
            GameObject lightObject = new GameObject("Mahzen_Dolgu_Isigi");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(65f, 30f, 0f);

            fillLight = lightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = fillColor;
            fillLight.shadows = LightShadows.None;
            fillLight.intensity = 0f;
            fillLight.enabled = false;
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
            fade = StartCoroutine(inside ? FadeIn() : FadeOut());
        }

        private IEnumerator FadeIn()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;

            Color startAmbient = RenderSettings.ambientLight;
            float startReflection = RenderSettings.reflectionIntensity;
            float startSun = sunLight != null ? sunLight.intensity : 0f;
            float startFill = fillLight.intensity;
            fillLight.color = fillColor;
            fillLight.enabled = true;

            for (float elapsed = 0f; elapsed < transitionTime; elapsed += Time.deltaTime)
            {
                float t = elapsed / transitionTime;
                fillLight.intensity = Mathf.Lerp(startFill, insideFillIntensity, t);
                RenderSettings.ambientLight = Color.Lerp(startAmbient, insideAmbientColor, t);
                RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, 0.2f, t);
                RenderSettings.fogDensity = Mathf.Lerp(0f, insideFogDensity, t);
                if (sunLight != null) sunLight.intensity = Mathf.Lerp(startSun, insideSun, t);
                yield return null;
            }

            fillLight.intensity = insideFillIntensity;
            RenderSettings.ambientLight = insideAmbientColor;
            RenderSettings.fogDensity = insideFogDensity;
            if (sunLight != null) sunLight.intensity = insideSun;
        }

        private IEnumerator FadeOut()
        {
            float startFog = RenderSettings.fogDensity;
            float startReflection = RenderSettings.reflectionIntensity;
            float startSun = sunLight != null ? sunLight.intensity : 0f;
            float startFill = fillLight.intensity;

            for (float elapsed = 0f; elapsed < transitionTime; elapsed += Time.deltaTime)
            {
                float t = elapsed / transitionTime;
                fillLight.intensity = Mathf.Lerp(startFill, 0f, t);
                RenderSettings.fogDensity = Mathf.Lerp(startFog, 0f, t);
                RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, outsideReflection, t);
                if (sunLight != null) sunLight.intensity = Mathf.Lerp(startSun, outsideSun, t);
                yield return null;
            }

            fillLight.intensity = 0f;
            fillLight.enabled = false;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = outsideMode;
            RenderSettings.ambientLight = outsideAmbientColor;
            RenderSettings.reflectionIntensity = outsideReflection;
            if (sunLight != null) sunLight.intensity = outsideSun;
        }
    }
}
