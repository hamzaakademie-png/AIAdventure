using System.Collections;
using AIAdventure.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Level
{
    /// <summary>
    /// Şifreli sandık. Oyuncu yaklaşıp F'ye basınca rakamlı kilit açılır:
    /// A/D (ya da ←/→) hane seçer, W/S (ya da ↑/↓) rakamı çevirir, Enter dener, Esc vazgeçer.
    /// Doğru şifrede kapak açılır ve içindeki et otomatik olarak oyuncunun cebine eklenir.
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        private enum State { Locked, Solving, Opening, Open, Empty }

        [Header("Bağlantılar")]
        [SerializeField] private PlayerMovement player;
        [SerializeField] private PlayerCommander commander;

        [Header("Kilit")]
        [Tooltip("Şifre (rakamlar). Nottaki bilmecenin cevabı: 4-2-3.")]
        [SerializeField] private string code = "423";
        [SerializeField] private float interactRange = 1.8f;
        [Tooltip("Bu kadar yanlış denemeden sonra ipucu gösterilir.")]
        [SerializeField] private int hintAfterAttempts = 3;
        [SerializeField, TextArea(2, 4)] private string hint =
            "Bir insanın ömrünü düşün: bebekken nasıl ilerler, gençken nasıl yürür, yaşlanınca neye dayanır?";

        [Header("Kapak (isteğe bağlı)")]
        [Tooltip("Sandığın kapağı ayrı bir parçaysa buraya sürükle. Boşsa kapak animasyonu atlanır.")]
        [SerializeField] private Transform lid;
        [Tooltip("Kapağın açılınca döneceği açı (kendi ekseninde, derece).")]
        [SerializeField] private Vector3 lidOpenRotation = new Vector3(-110f, 0f, 0f);
        [SerializeField] private float openDuration = 1.2f;

        [Header("İçerik")]
        [Tooltip("Sandığın içindeki et modeli (açılınca görünür, alınınca kaybolur). Boş olabilir.")]
        [SerializeField] private GameObject meatObject;

        [Header("Ses (isteğe bağlı)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip wrongSound;
        [SerializeField] private AudioClip unlockSound;

        private CombinationLock combination;
        private State state = State.Locked;
        private string message;
        private float messageTimer;
        private float shakeTimer;
        private GUIStyle titleStyle, digitStyle, textStyle, promptStyle;

        private bool PlayerNear
        {
            get
            {
                if (player == null) return false;
                Vector3 offset = player.transform.position - transform.position;
                offset.y = 0f;
                return offset.magnitude <= interactRange;
            }
        }

        private void Awake()
        {
            combination = new CombinationLock(code);
            if (meatObject != null) meatObject.SetActive(false);

            // Sandık bir oda prefab'ının içindeyse sahnedeki oyuncuyu kendisi bulur.
            if (player == null) player = FindAnyObjectByType<PlayerMovement>();
            if (player == null) Debug.LogError("[TreasureChest] Sahnede oyuncu (PlayerMovement) bulunamadı.");
            ResolveLid();

            if (commander == null && player != null) commander = player.GetComponentInChildren<PlayerCommander>();
            if (commander == null) commander = FindAnyObjectByType<PlayerCommander>();
            if (commander == null) Debug.LogError("[TreasureChest] Sahnede PlayerCommander bulunamadı; et cebe eklenemez.");
        }

        /// <summary>
        /// Lid alanına yanlışlıkla FBX dosyasının içindeki parça (sahnedeki değil) sürüklenmiş
        /// olabilir. O zaman sandığın kendi içinde aynı isimli parçayı bulur.
        /// </summary>
        private void ResolveLid()
        {
            if (lid == null || lid.IsChildOf(transform)) return;

            string lidName = lid.name;
            lid = null;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name == lidName)
                {
                    lid = child;
                    break;
                }
            }
            if (lid == null)
                Debug.LogWarning($"[TreasureChest] Kapak '{lidName}' sandığın içinde bulunamadı; kapak animasyonu atlanacak.");
        }

        private void Update()
        {
            if (messageTimer > 0f) messageTimer -= Time.deltaTime;
            if (shakeTimer > 0f) shakeTimer -= Time.deltaTime;

            Keyboard k = Keyboard.current;
            if (k == null) return;

            switch (state)
            {
                case State.Locked:
                    if (PlayerNear && k.fKey.wasPressedThisFrame) BeginSolving();
                    break;

                case State.Solving:
                    HandleLockInput(k);
                    break;

                case State.Open:
                    if (PlayerNear && k.fKey.wasPressedThisFrame) TakeMeat();
                    break;
            }
        }

        // ---------------------------------------------------------------- Kilit

        private void BeginSolving()
        {
            state = State.Solving;
            player.SetInputEnabled(false);
        }

        private void StopSolving()
        {
            state = State.Locked;
            player.SetInputEnabled(true);
        }

        private void HandleLockInput(Keyboard k)
        {
            if (k.escapeKey.wasPressedThisFrame) { StopSolving(); return; }

            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) { combination.MoveSelection(-1); Play(clickSound); }
            if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) { combination.MoveSelection(1); Play(clickSound); }
            if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) { combination.Rotate(1); Play(clickSound); }
            if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) { combination.Rotate(-1); Play(clickSound); }

            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
            {
                if (combination.TryOpen())
                {
                    Play(unlockSound);
                    StartCoroutine(OpenRoutine());
                }
                else
                {
                    Play(wrongSound);
                    shakeTimer = 0.4f;
                    Show(combination.Attempts >= hintAfterAttempts ? hint : "Kilit açılmadı.");
                }
            }
        }

        private IEnumerator OpenRoutine()
        {
            state = State.Opening;
            player.SetInputEnabled(true);
            if (meatObject != null) meatObject.SetActive(true);

            if (lid != null)
            {
                Quaternion closed = lid.localRotation;
                Quaternion open = closed * Quaternion.Euler(lidOpenRotation);
                for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
                {
                    lid.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                lid.localRotation = open;
            }

            // Et otomatik olarak alınır; oyuncunun ayrıca bir tuşa basması gerekmez.
            yield return new WaitForSeconds(0.6f);
            TakeMeat();
        }

        private void TakeMeat()
        {
            state = State.Empty;
            if (meatObject != null) meatObject.SetActive(false);
            if (commander != null)
            {
                commander.AddMeat(1);
                Debug.Log($"[TreasureChest] Et '{commander.name}' nesnesindeki PlayerCommander'a eklendi. Cepteki et: {commander.MeatCount}");
            }
            Show("Sandıktan kurutulmuş bir et parçası aldın. Şimdi kolun yanına git ve E ile tilkiye ver.");
        }

        private void Show(string text)
        {
            message = text;
            messageTimer = 3.5f;
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        // ---------------------------------------------------------------- Ekran

        private void OnGUI()
        {
            CreateStyles();

            if (state == State.Solving) DrawLock();
            else if (PlayerNear && (state == State.Locked || state == State.Open))
                DrawPrompt(state == State.Locked ? "[F] Sandığı incele" : "[F] Eti al");

            if (messageTimer > 0f && !string.IsNullOrEmpty(message))
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(messageTimer));
                Rect r = new Rect(Screen.width * 0.2f, Screen.height - 200f, Screen.width * 0.6f, 50f);
                GUI.Box(r, GUIContent.none);
                GUI.Label(r, message, promptStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawPrompt(string text)
        {
            Rect r = new Rect((Screen.width - 360f) / 2f, Screen.height - 140f, 360f, 50f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(r, text, promptStyle);
        }

        private void DrawLock()
        {
            float w = 460f, h = 300f;
            float shake = shakeTimer > 0f ? Mathf.Sin(Time.time * 70f) * 10f * (shakeTimer / 0.4f) : 0f;
            Rect panel = new Rect((Screen.width - w) / 2f + shake, (Screen.height - h) / 2f, w, h);

            GUI.color = new Color(0.08f, 0.06f, 0.04f, 0.95f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x, panel.y + 15f, w, 36f), "PASLI ŞİFRE KİLİDİ", titleStyle);

            float size = 90f, gap = 20f;
            float startX = panel.x + (w - (combination.Length * size + (combination.Length - 1) * gap)) / 2f;
            for (int i = 0; i < combination.Length; i++)
            {
                Rect box = new Rect(startX + i * (size + gap), panel.y + 75f, size, 120f);
                bool selected = i == combination.Selected;
                GUI.color = selected ? new Color(0.85f, 0.65f, 0.3f) : new Color(0.35f, 0.3f, 0.25f);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = new Color(0.12f, 0.1f, 0.08f);
                GUI.DrawTexture(new Rect(box.x + 4f, box.y + 4f, box.width - 8f, box.height - 8f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(box, combination.DigitAt(i).ToString(), digitStyle);
                if (selected)
                {
                    GUI.Label(new Rect(box.x, box.y - 28f, box.width, 24f), "▲", textStyle);
                    GUI.Label(new Rect(box.x, box.yMax + 2f, box.width, 24f), "▼", textStyle);
                }
            }

            GUI.Label(new Rect(panel.x, panel.yMax - 60f, w, 50f),
                "A / D: hane seç     W / S: çevir\nEnter: dene     Esc: vazgeç", textStyle);
        }

        private void CreateStyles()
        {
            if (titleStyle != null) return;
            Color cream = new Color(0.95f, 0.9f, 0.8f);
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.65f, 0.3f) }
            };
            digitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = cream }
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = { textColor = cream }
            };
            promptStyle = new GUIStyle(textStyle) { fontSize = 20, wordWrap = true };
        }
    }
}
