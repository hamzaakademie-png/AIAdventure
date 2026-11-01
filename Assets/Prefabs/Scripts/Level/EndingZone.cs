using AIAdventure.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AIAdventure.Level
{
    /// <summary>
    /// Oyuncu çıkışa ulaşınca oyunun bitiş ekranını gösterir.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class EndingZone : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;

        [SerializeField] private string title = "TALİH TAPINAĞI'NDAN KAÇTIN";
        [SerializeField, TextArea(3, 8)] private string body =
            "Gün ışığı yüzüne vurduğunda arkanda mahzenin kapısı kendiliğinden kapanıyor.\n" +
            "Leyla'nın günlüğü hâlâ cebinde. Kâtip'in masası boş, Baron'un sesi artık duyulmuyor.\n\n" +
            "Talih bu sefer seninleydi.";

        private bool finished;
        private float startTime;
        private PlayerMovement player;
        private Texture2D black;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (finished) return;
            player = other.GetComponentInParent<PlayerMovement>();
            if (player == null) return;

            finished = true;
            startTime = Time.time;
            player.SetInputEnabled(false);
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
            GUI.Label(new Rect(0f, 300f, width, 100f), title, titleStyle);
            GUI.Label(new Rect((width - 1000f) / 2f, 430f, 1000f, 300f),
                body + "\n\n[R] Yeniden oyna     [Esc] Çık", bodyStyle);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (black != null) Destroy(black);
        }
    }
}
