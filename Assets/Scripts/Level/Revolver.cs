using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AIAdventure.Level
{
    /// <summary>
    /// Altıpatlar: silindiri dönebilir, ateşlenince namlu ucunda parlama olur.
    /// Kurulum: boş bir nesneye ekle, ⋮ menüsünden "Silahı Oluştur".
    /// Namlu nesnenin +Z yönüne bakar.
    /// </summary>
    public class Revolver : MonoBehaviour
    {
        [Header("Parçalar (otomatik oluşur)")]
        [SerializeField] private Transform cylinder;
        [SerializeField] private Light muzzleFlash;

        [Header("Silindir")]
        [SerializeField] private float spinTime = 1.4f;
        [SerializeField] private float spinSpeed = 1440f;

        [Header("Oluşturma")]
        [SerializeField] private Material metalMaterial;
        [SerializeField] private Material gripMaterial;
        [SerializeField] private Material brassMaterial;

        /// <summary>Silindir dönmeye başladığında (ses çalmak için).</summary>
        public event Action SpinStarted;

        public void Spin() => StartCoroutine(SpinRoutine());

        public IEnumerator SpinRoutine()
        {
            SpinStarted?.Invoke();
            if (cylinder == null) yield break;

            for (float t = 0f; t < spinTime; t += Time.deltaTime)
            {
                float speed = Mathf.Lerp(spinSpeed, 0f, t / spinTime);
                cylinder.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
                yield return null;
            }
        }

        public void Fire() => StartCoroutine(FlashRoutine());

        private IEnumerator FlashRoutine()
        {
            if (muzzleFlash == null) yield break;
            muzzleFlash.enabled = true;
            yield return new WaitForSeconds(0.06f);
            muzzleFlash.enabled = false;
        }

        // ---------------------------------------------------------------- Oluşturma

        [ContextMenu("Silahı Oluştur")]
        private void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            Part(PrimitiveType.Cube, "Govde", transform, new Vector3(0f, 0f, 0f), Vector3.zero,
                new Vector3(0.05f, 0.08f, 0.16f), metalMaterial);
            Part(PrimitiveType.Cylinder, "Namlu", transform, new Vector3(0f, 0.022f, 0.19f), new Vector3(90f, 0f, 0f),
                new Vector3(0.024f, 0.11f, 0.024f), metalMaterial);
            Part(PrimitiveType.Cube, "Kabza", transform, new Vector3(0f, -0.075f, -0.09f), new Vector3(-20f, 0f, 0f),
                new Vector3(0.042f, 0.12f, 0.055f), gripMaterial);
            Part(PrimitiveType.Cube, "Horoz", transform, new Vector3(0f, 0.045f, -0.075f), new Vector3(30f, 0f, 0f),
                new Vector3(0.015f, 0.03f, 0.02f), metalMaterial);
            Part(PrimitiveType.Cube, "Tetik", transform, new Vector3(0f, -0.05f, -0.02f), new Vector3(15f, 0f, 0f),
                new Vector3(0.008f, 0.03f, 0.008f), metalMaterial);

            // Dönen silindir ve 6 hazne
            GameObject drum = Part(PrimitiveType.Cylinder, "Silindir", transform, new Vector3(0f, 0.008f, 0.02f),
                new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.035f, 0.07f), metalMaterial);
            cylinder = drum.transform;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 local = new Vector3(Mathf.Cos(angle) * 0.25f, 1.01f, Mathf.Sin(angle) * 0.25f);
                Part(PrimitiveType.Cylinder, $"Hazne_{i + 1}", cylinder, local, Vector3.zero,
                    new Vector3(0.22f, 0.02f, 0.22f), gripMaterial);
            }

            GameObject flash = new GameObject("Namlu_Parlamasi");
            flash.transform.SetParent(transform, false);
            flash.transform.localPosition = new Vector3(0f, 0.022f, 0.32f);
            muzzleFlash = flash.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.color = new Color(1f, 0.75f, 0.4f);
            muzzleFlash.range = 4f;
            muzzleFlash.intensity = 8f;
            muzzleFlash.enabled = false;

            BuildBullet();

#if UNITY_EDITOR
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
            Debug.Log("[Revolver] Silah ve mermi oluşturuldu.");
        }

        private void BuildBullet()
        {
            Transform parent = transform.parent;
            Transform old = parent != null ? parent.Find("Mermi") : null;
            if (old != null) DestroyImmediate(old.gameObject);

            Transform bullet = new GameObject("Mermi").transform;
            bullet.SetParent(parent, false);
            bullet.position = transform.position + new Vector3(-0.35f, -0.005f, -0.15f);

            Part(PrimitiveType.Cylinder, "Kovan", bullet, Vector3.zero, Vector3.zero,
                new Vector3(0.012f, 0.012f, 0.012f), brassMaterial);
            Part(PrimitiveType.Sphere, "Uc", bullet, new Vector3(0f, 0.013f, 0f), Vector3.zero,
                new Vector3(0.011f, 0.016f, 0.011f), metalMaterial);
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 euler, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            DestroyImmediate(part.GetComponent<Collider>());
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
    }
}
