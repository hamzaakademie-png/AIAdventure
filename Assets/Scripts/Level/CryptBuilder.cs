using AIAdventure.Player;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AIAdventure.Level
{
    /// <summary>
    /// Sadece editörde kullanılan yardımcı: tapınağın arkasına merdiven, meşaleli
    /// yosunlu iniş yolu ve avluyu tek tıkla inşa eder, notları hikâyeyle doldurur.
    /// Kullanım: bileşenin ⋮ menüsünden "Mahzeni İnşa Et".
    ///
    /// Harita (Z ekseni boyunca):
    ///   z 20-26  : merdiven (y 0 → -3)
    ///   z 26-42  : eğimli koridor (y -3 → -6)
    ///   z 42-62  : avlu (y -6), arkasında kilitli avlu kapısı
    /// </summary>
    public class CryptBuilder : MonoBehaviour
    {
        [Header("Materyaller")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material paperMaterial;
        [SerializeField] private Material flameMaterial;

        [Header("Atmosfer")]
        [SerializeField] private Light sunLight;

        private const string RootName = "Crypt";

        private const float StairStartZ = 20f;
        private const int StairCount = 12;
        private const float StairRise = 0.25f;
        private const float StairRun = 0.5f;
        private const float CorridorStartZ = 26f;
        private const float CorridorEndZ = 42f;
        private const float CorridorStartY = -3f;
        private const float CourtyardY = -6f;
        private const float CourtyardEndZ = 62f;

        [ContextMenu("Mahzeni İnşa Et")]
        private void Build()
        {
            Clear();

            Transform root = new GameObject(RootName).transform;
            root.SetParent(transform, false);

            BuildEntrance(Group(root, "1_Giris_Merdiven"));
            BuildCorridor(Group(root, "2_Koridor"));
            BuildCourtyard(Group(root, "3_Avlu"));
            BuildAtmosphere(root);

            MarkDirty();
            Debug.Log("[CryptBuilder] Mahzen inşa edildi. Level'da Clear + Bake yapmayı unutma.");
        }

        [ContextMenu("Mahzeni Sil")]
        private void Clear()
        {
            Transform existing = transform.Find(RootName);
            if (existing != null) DestroyImmediate(existing.gameObject);
            MarkDirty();
        }

        // ---------------------------------------------------------------- Bölümler

        private void BuildEntrance(Transform p)
        {
            // Tapınağın arka duvarı: ortada merdiven ağzı kalır.
            Box("ArkaDuvar_Sol", p, new Vector3(-11.5f, 1.5f, 20.5f), new Vector3(17f, 3f, 1f), wallMaterial);
            Box("ArkaDuvar_Sag", p, new Vector3(11.5f, 1.5f, 20.5f), new Vector3(17f, 3f, 1f), wallMaterial);
            Box("Giris_Kemeri", p, new Vector3(0f, 2.75f, 20.5f), new Vector3(6f, 0.5f, 1f), wallMaterial);

            for (int i = 0; i < StairCount; i++)
            {
                float top = -StairRise * (i + 1);
                const float bottom = -3.5f;
                float z = StairStartZ + StairRun * i + StairRun / 2f;
                Box($"Basamak_{i + 1:00}", p, new Vector3(0f, (top + bottom) / 2f, z),
                    new Vector3(4f, top - bottom, StairRun), wallMaterial);
            }

            Box("Merdiven_Duvar_Sol", p, new Vector3(-2.5f, -0.5f, 23f), new Vector3(1f, 6f, 6f), wallMaterial);
            Box("Merdiven_Duvar_Sag", p, new Vector3(2.5f, -0.5f, 23f), new Vector3(1f, 6f, 6f), wallMaterial);
            Box("Merdiven_Tavan", p, new Vector3(0f, 1.75f, 23.75f), new Vector3(6f, 0.5f, 4.5f), wallMaterial);
            Box("Koridor_Kemeri", p, new Vector3(0f, 1.25f, 26f), new Vector3(6f, 1.5f, 0.5f), wallMaterial);

            Torch(p, "Mesale_Merdiven_Sol", new Vector3(-1.85f, 0.4f, 23f));
            Torch(p, "Mesale_Merdiven_Sag", new Vector3(1.85f, 0.4f, 23f));
        }

        private void BuildCorridor(Transform p)
        {
            float run = CorridorEndZ - CorridorStartZ;
            float drop = CorridorStartY - CourtyardY;
            float length = Mathf.Sqrt(run * run + drop * drop);
            Quaternion slope = Quaternion.Euler(Mathf.Atan2(drop, run) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 up = slope * Vector3.up;
            Vector3 surfaceCenter = new Vector3(0f, (CorridorStartY + CourtyardY) / 2f, (CorridorStartZ + CorridorEndZ) / 2f);

            Box("Koridor_Zemin_Yosunlu", p, surfaceCenter - up * 0.25f, new Vector3(4f, 0.5f, length), floorMaterial, slope);
            Box("Koridor_Tavan", p, surfaceCenter + Vector3.up * 3f + up * 0.25f, new Vector3(6f, 0.5f, length), wallMaterial, slope);
            Box("Koridor_Duvar_Sol", p, new Vector3(-2.5f, -3.5f, 34f), new Vector3(1f, 7f, run), wallMaterial);
            Box("Koridor_Duvar_Sag", p, new Vector3(2.5f, -3.5f, 34f), new Vector3(1f, 7f, run), wallMaterial);

            float[] torchZ = { 28f, 32f, 36f, 40f };
            for (int i = 0; i < torchZ.Length; i++)
            {
                float x = i % 2 == 0 ? -1.85f : 1.85f;
                Torch(p, $"Mesale_Koridor_{i + 1}", new Vector3(x, FloorY(torchZ[i]) + 1.8f, torchZ[i]));
            }

            NotePedestal(p, "Not_1_KaziGunlugu", new Vector3(1.4f, FloorY(30f), 30f), NoteTitles[0], NoteBodies[0]);
        }

        private void BuildCourtyard(Transform p)
        {
            const float y = CourtyardY;

            Box("Avlu_Zemin", p, new Vector3(0f, y - 0.25f, 52f), new Vector3(20f, 0.5f, 20f), floorMaterial);
            Box("Avlu_Tavan", p, new Vector3(0f, 0.25f, 52f), new Vector3(22f, 0.5f, 22f), wallMaterial);

            Box("Avlu_OnDuvar_Sol", p, new Vector3(-6.5f, -3f, 41.5f), new Vector3(9f, 6f, 1f), wallMaterial);
            Box("Avlu_OnDuvar_Sag", p, new Vector3(6.5f, -3f, 41.5f), new Vector3(9f, 6f, 1f), wallMaterial);
            Box("Avlu_GirisUstu", p, new Vector3(0f, -1.45f, 41.5f), new Vector3(4f, 2.9f, 1f), wallMaterial);
            Box("Avlu_YanDuvar_Sol", p, new Vector3(-10.5f, -3f, 52f), new Vector3(1f, 6f, 22f), wallMaterial);
            Box("Avlu_YanDuvar_Sag", p, new Vector3(10.5f, -3f, 52f), new Vector3(1f, 6f, 22f), wallMaterial);
            Box("Avlu_ArkaDuvar_Sol", p, new Vector3(-6.25f, -3f, 62.5f), new Vector3(9.5f, 6f, 1f), wallMaterial);
            Box("Avlu_ArkaDuvar_Sag", p, new Vector3(6.25f, -3f, 62.5f), new Vector3(9.5f, 6f, 1f), wallMaterial);
            Box("Avlu_KapiUstu", p, new Vector3(0f, -1.5f, 62.5f), new Vector3(3f, 3f, 1f), wallMaterial);

            // Kilitli kapı: blackjack kazanılınca açılacak. Parmaklıkla aynı Gate scripti.
            GameObject door = Box("Avlu_Kapisi", p, new Vector3(0f, y + 1.5f, 62.5f), new Vector3(3f, 3f, 0.5f), wallMaterial);
            door.AddComponent<Gate>();
            door.AddComponent<NavMeshObstacle>().carving = true;
            door.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            foreach (float px in new[] { -5f, 5f })
            foreach (float pz in new[] { 47f, 53f })
                Box($"Sutun_{(px < 0 ? "Sol" : "Sag")}_{pz}", p, new Vector3(px, y + 3f, pz), new Vector3(1f, 6f, 1f), wallMaterial);

            Box("Kart_Masasi", p, new Vector3(0f, y + 0.5f, 56f), new Vector3(3f, 1f, 1.4f), wallMaterial);
            GameObject dealer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dealer.name = "Katip_Krupiye (gecici)";
            dealer.transform.SetParent(p, false);
            dealer.transform.localPosition = new Vector3(0f, y + 1f, 57.6f);
            dealer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (float tz in new[] { 45f, 52f, 59f })
            {
                Torch(p, $"Mesale_Avlu_Sol_{tz}", new Vector3(-9.85f, y + 1.8f, tz));
                Torch(p, $"Mesale_Avlu_Sag_{tz}", new Vector3(9.85f, y + 1.8f, tz));
            }
            Torch(p, "Mesale_Kapi_Sol", new Vector3(-2.2f, y + 2f, 61.85f));
            Torch(p, "Mesale_Kapi_Sag", new Vector3(2.2f, y + 2f, 61.85f));

            NotePedestal(p, "Not_2_DuvarYaziti", new Vector3(-8f, y, 45f), NoteTitles[1], NoteBodies[1]);
            NotePedestal(p, "Not_3_KaziGunlugu", new Vector3(8f, y, 49f), NoteTitles[2], NoteBodies[2]);
            NotePedestal(p, "Not_4_ImzasizNot", new Vector3(-8f, y, 57f), NoteTitles[3], NoteBodies[3]);
            NotePedestal(p, "Not_5_SonSayfa", new Vector3(4.5f, y, 61f), NoteTitles[4], NoteBodies[4]);
        }

        private void BuildAtmosphere(Transform root)
        {
            GameObject zone = new GameObject("Mahzen_Atmosfer_Alani");
            zone.transform.SetParent(root, false);
            zone.transform.localPosition = new Vector3(0f, -3f, 42f);

            BoxCollider area = zone.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.size = new Vector3(24f, 12f, 42f);

            Light sun = sunLight != null ? sunLight : RenderSettings.sun;
            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            zone.AddComponent<CryptAtmosphereZone>().Configure(sun, follow);
        }

        // ---------------------------------------------------------------- Yardımcılar

        private static float FloorY(float z)
        {
            if (z <= StairStartZ) return 0f;
            if (z <= CorridorStartZ) return CorridorStartY * (z - StairStartZ) / (CorridorStartZ - StairStartZ);
            if (z <= CorridorEndZ)
                return Mathf.Lerp(CorridorStartY, CourtyardY, (z - CorridorStartZ) / (CorridorEndZ - CorridorStartZ));
            return CourtyardY;
        }

        private static Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            => Box(name, parent, position, scale, material, Quaternion.identity);

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localRotation = rotation;
            box.transform.localScale = scale;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private void Torch(Transform parent, string name, Vector3 position)
        {
            Transform torch = new GameObject(name).transform;
            torch.SetParent(parent, false);
            torch.localPosition = position;

            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = "Sap";
            stick.transform.SetParent(torch, false);
            stick.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            DestroyImmediate(stick.GetComponent<Collider>());
            if (wallMaterial != null) stick.GetComponent<Renderer>().sharedMaterial = wallMaterial;

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Alev";
            flame.transform.SetParent(torch, false);
            flame.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            flame.transform.localScale = Vector3.one * 0.2f;
            DestroyImmediate(flame.GetComponent<Collider>());
            if (flameMaterial != null) flame.GetComponent<Renderer>().sharedMaterial = flameMaterial;

            GameObject lightObject = new GameObject("Isik");
            lightObject.transform.SetParent(torch, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 7f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            lightObject.AddComponent<TorchFlicker>();
        }

        private void NotePedestal(Transform parent, string name, Vector3 floorPosition, string title, string body)
        {
            Box(name + "_Kaide", parent, floorPosition + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f), wallMaterial);
            GameObject paper = Box(name, parent, floorPosition + Vector3.up * 1.01f,
                new Vector3(0.35f, 0.02f, 0.45f), paperMaterial, Quaternion.Euler(0f, 15f, 0f));
            paper.AddComponent<Note>().Setup(title, body);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        // ---------------------------------------------------------------- Hikâye

        private static readonly string[] NoteTitles =
        {
            "Kazı Günlüğü — 3. Gün",
            "Duvar Yazıtı (kopya)",
            "Kazı Günlüğü — 5. Gün",
            "İmzasız Not",
            "Kazı Günlüğü — Son Sayfa",
        };

        private static readonly string[] NoteBodies =
        {
            "Haritadaki işaret doğruymuş. Tapınağın altında, merdivenlerin bittiği yerde eski bir mahzen var. " +
            "Köylüler buraya \"Talih Tapınağı\" diyor ve kimse yaklaşmıyor.\n\n" +
            "Garip olan şu: duvarlardaki meşaleler biz gelmeden önce de yanıyordu. Kerem bunun birinin şakası " +
            "olduğunu söylüyor. Ben o kadar emin değilim.\n\n— Dr. Leyla Aras",

            "Talihin üç kapısı vardır.\n\n" +
            "Birincisi küçüklere açılır; iri olan geçemez.\n" +
            "İkincisi kartlara açılır; yirmi biri aşan yanar.\n" +
            "Üçüncüsü altı hazneye açılır; orada ya cesaret kazanır ya kurşun.\n\n" +
            "Üç kapıyı da açan, Baron'un masasından özgür kalkar.",

            "Avluda bir masa ve masanın başında biri var. Köylülerin \"Kâtip\" dediği şey bu olmalı. " +
            "Konuşmuyor, yalnızca kart dağıtıyor.\n\n" +
            "Kerem oturdu, üç el üst üste kaybetti. Kalktığında gözleri bomboştu. Şimdi o da masanın yanında " +
            "duruyor, kıpırdamadan.\n\nBu yer kazananı değil, kaybedeni seviyor.\n\n— L.",

            "Ben de bir zamanlar senin gibi geldim. Baron'a kaybettim ve sonsuza dek kart dağıtmaya mahkûm oldum.\n\n" +
            "Sana bir sır vereyim: kasa her zaman kazanmaz. On yedi ve üstünde dururum, altında kart çekerim. " +
            "Kural bu, bozamam.\n\nSabırlı ol. Yirmi bire yaklaş, ama asla geçme.",

            "Kâtip'i yendim. Kapı açıldı ve onu gördüm: Baron Mortis. Masada eski bir tabanca vardı. " +
            "Altı hazne, tek kurşun. \"Sıra sende,\" dedi gülümseyerek.\n\n" +
            "Ama bir şey fark ettim: o da korkuyor. Her konuştuğumda parmakları masada tıkırdıyordu. " +
            "Baron'u şans değil, sözler yener. Onu konuşturan, sinirlerini bozan kazanır.\n\n" +
            "Ben cesaretimi kaybettim. Bunu okuyan sen, kaybetme.\n\n— L.",
        };
    }
}
