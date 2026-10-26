using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AIAdventure.Level
{
    /// <summary>
    /// Tavandan sarkan, hafifçe sallanan lamba. Altına gölgeli sarı bir spot ışık
    /// verir; sallandıkça gölgeler de oynar. Bu nesnenin konumu askı noktasıdır
    /// (tavan). Kurulum: bileşenin ⋮ menüsünden "Lambayı Oluştur".
    /// </summary>
    public class HangingLamp : MonoBehaviour
    {
        [Header("Görünüm")]
        [SerializeField] private float cordLength = 3.4f;
        [SerializeField] private Material shadeMaterial;
        [SerializeField] private Material bulbMaterial;

        [Header("Işık")]
        [SerializeField] private Color lightColor = new Color(1f, 0.75f, 0.45f);
        [SerializeField] private float intensity = 12f;
        [SerializeField] private float range = 7f;
        [SerializeField] private float spotAngle = 80f;

        [Header("Sallanma")]
        [SerializeField] private float swingAngle = 2.5f;
        [SerializeField] private float swingSpeed = 0.6f;

        private Quaternion baseRotation;

        private void Awake()
        {
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float x = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
            float z = Mathf.Sin(Time.time * swingSpeed * 0.7f + 1f) * swingAngle * 0.5f;
            transform.localRotation = baseRotation * Quaternion.Euler(x, 0f, z);
        }

        public void SetMaterials(Material shade, Material bulb)
        {
            shadeMaterial = shade;
            bulbMaterial = bulb;
        }

        public void SetIntensity(float value) => intensity = value;

        [ContextMenu("Lambayı Oluştur")]
        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            Part(PrimitiveType.Cylinder, "Kablo", new Vector3(0f, -cordLength / 2f, 0f),
                new Vector3(0.015f, cordLength / 2f, 0.015f), shadeMaterial);
            Part(PrimitiveType.Cylinder, "Abajur", new Vector3(0f, -cordLength, 0f),
                new Vector3(0.55f, 0.08f, 0.55f), shadeMaterial);
            Part(PrimitiveType.Sphere, "Ampul", new Vector3(0f, -cordLength - 0.1f, 0f),
                Vector3.one * 0.13f, bulbMaterial);

            GameObject lightObject = new GameObject("Spot_Isik");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, -cordLength - 0.12f, 0f);
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spot = lightObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = lightColor;
            spot.intensity = intensity;
            spot.range = range;
            spot.spotAngle = spotAngle;
            spot.innerSpotAngle = spotAngle * 0.5f;
            spot.shadows = LightShadows.Soft;

#if UNITY_EDITOR
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private void Part(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            DestroyImmediate(part.GetComponent<Collider>());
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
