using AIAdventure.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AIAdventure.Level
{
    /// <summary>
    /// Oyunun bitiş ekranı. Oyuncu bu alana girince ya da başka bir script
    /// Finish() çağırınca (örn. Leyla ile karşılaşma) ekran kararır ve son yazısı çıkar.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class EndingZone : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;

        [SerializeField] private string endingTitle = "TALİH TAPINAĞI'NDAN KAÇTIN";
        [SerializeField, TextArea(3, 8)] private string endingText =
            "Arkanızda masa boş duruyor. Kartlar hâlâ dağıtılmayı, tabanca hâlâ doldurulmayı bekliyor.\n" +
            "Tapınak hiçbir zaman sahipsiz kalmaz.\n\n" +
            "Şimdi ne olacak? Yeni Baron sen mi olacaksın, Mecnun? Ve kartları dağıtan... Leyla mı?\n\n" +
            "Belki de kaçmadın.";

        private bool finished;
        private float startTime;
        private Texture2D black;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null) Finish(player);
        }

        /// <summary>Bitiş ekranını açar.</summary>
        public void Finish(PlayerMovement player)
        {
            if (finished) return;
            finished = true;
            startTime = Time.time;
            if (player != null) player.SetInputEnabled(false);
            Debug.Log("[Oyun] Bitti: oyuncu çıkışa ulaştı.");
        }

        private void Update()
        {
            if (!finished || Keyboard.current == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Application.Quit();
        }

        private void OnGUI()
        {
            if (!finished) return;

            if (black == null)
            {
                black = new Texture2D(1, 1);
                black.SetPixel(0, 0, Color.black);
                black.Apply();
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 60, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
                };
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26, wordWrap = true, alignment = TextAnchor.UpperCenter,
                    normal = { textColor = Color.white }
                };
            }

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float alpha = Mathf.Clamp01((Time.time - startTime) / 2f);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), black);
            GUI.Label(new Rect(0f, 300f, width, 100f), endingTitle, titleStyle);
            GUI.Label(new Rect((width - 1000f) / 2f, 430f, 1000f, 300f),
                endingText + "\n\n[R] Yeniden oyna     [Esc] Çık", bodyStyle);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (black != null) Destroy(black);
        }
    }
}
