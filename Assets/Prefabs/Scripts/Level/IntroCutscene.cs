using System;
using System.Collections;
using AIAdventure.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Level
{
    /// <summary>
    /// Oyun başında kısa bir ara sahne: kamera sırayla verilen noktalara gider,
    /// konuşan karakterin portresiyle birlikte yandan kayarak gelen bir diyalog
    /// kutusu gösterilir. Oyuncu bu sırada hareket edemez.
    /// Space: yazıyı tamamla / sonraki çekim, Esc: ara sahneyi geç.
    /// </summary>
    public class IntroCutscene : MonoBehaviour
    {
        [Serializable]
        public class Shot
        {
            [Tooltip("Kameranın duracağı nokta (boş obje). Boşsa kamera oyuncuda kalır.")]
            public Transform cameraPoint;
            [Tooltip("Kameranın bakacağı nesne. Boşsa noktanın baktığı yöne bakar.")]
            public Transform lookAt;
            [Tooltip("Konuşan kişinin adı (kutunun üstünde görünür).")]
            public string speakerName = "Sen";
            [Tooltip("Konuşan kişinin fotoğrafı. Boşsa portre karesi gösterilmez.")]
            public Texture2D portrait;
            [TextArea(2, 4)] public string text;
            public float duration = 4f;
        }

        [SerializeField] private PlayerMovement player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private float cameraSpeed = 1.5f;
        [SerializeField] private Shot[] shots;

        [Header("Diyalog kutusu")]
        [Tooltip("Saniyede yazılan harf sayısı.")]
        [SerializeField] private float lettersPerSecond = 40f;
        [Tooltip("Kutunun kenardan kayarak gelme süresi (sn).")]
        [SerializeField] private float slideDuration = 0.35f;
        [SerializeField] private Color boxColor = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        [SerializeField] private Color frameColor = new Color(0.75f, 0.6f, 0.35f, 1f);

        [Header("Bitiş ipucu")]
        [TextArea(2, 3)]
        [SerializeField] private string endHint = "Tilkinin yanına git, kolun yakınında E'ye bas.";
        [SerializeField] private float endHintDuration = 6f;

        private SeatCameraController seatCamera;
        private PlayerCommander commander;
        private Shot currentShot;
        private float shotElapsed;
        private float hintTimer;
        private GUIStyle textStyle, nameStyle, smallStyle, hintStyle;

        public bool IsPlaying { get; private set; }

        private void Start()
        {
            if (shots == null || shots.Length == 0) return;
            seatCamera = new SeatCameraController(cameraFollow);
            if (player != null) commander = player.GetComponent<PlayerCommander>();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            IsPlaying = true;
            SetPlayerControl(false);

            Shot previous = null;
            foreach (Shot shot in shots)
            {
                MoveCamera(shot);

                // Konuşan kişi değişmediyse kutu tekrar kaymasın.
                bool sameSpeaker = previous != null && previous.speakerName == shot.speakerName;
                currentShot = shot;
                shotElapsed = sameSpeaker ? slideDuration : 0f;
                float typingTime = shot.text.Length / Mathf.Max(1f, lettersPerSecond);
                float total = Mathf.Max(shot.duration, typingTime + 1f);
                yield return null; // aynı karede tuş iki kez sayılmasın

                while (shotElapsed < total)
                {
                    shotElapsed += Time.deltaTime;

                    Keyboard k = Keyboard.current;
                    if (k != null && k.escapeKey.wasPressedThisFrame) { Finish(); yield break; }
                    if (k != null && k.spaceKey.wasPressedThisFrame)
                    {
                        if (VisibleLetters() < shot.text.Length)
                            shotElapsed = slideDuration + typingTime; // yazıyı hemen tamamla
                        else
                            break;
                    }
                    yield return null;
                }
                previous = shot;
            }
            Finish();
        }

        private void MoveCamera(Shot shot)
        {
            if (shot.cameraPoint == null) { seatCamera.Stand(); return; }

            Vector3 look = shot.lookAt != null ? shot.lookAt.position
                : shot.cameraPoint.position + shot.cameraPoint.forward;
            seatCamera.Sit(shot.cameraPoint.position, look);
        }

        private int VisibleLetters()
        {
            if (currentShot == null) return 0;
            float typing = Mathf.Max(0f, shotElapsed - slideDuration);
            return Mathf.Min(currentShot.text.Length, Mathf.FloorToInt(typing * lettersPerSecond));
        }

        private void Finish()
        {
            currentShot = null;
            seatCamera.Stand();
            SetPlayerControl(true);
            IsPlaying = false;
            hintTimer = endHintDuration;
        }

        private void SetPlayerControl(bool enabled)
        {
            if (player != null) player.SetInputEnabled(enabled);
            if (commander != null) commander.enabled = enabled;
        }

        private void LateUpdate()
        {
            seatCamera?.Tick(cameraSpeed);
            if (hintTimer > 0f) hintTimer -= Time.deltaTime;
        }

        // ---------- Çizim ----------

        private void OnGUI()
        {
            CreateStyles();

            if (IsPlaying)
            {
                DrawCinemaBars();
                if (currentShot != null) DrawDialogBox(currentShot);
                GUI.Label(new Rect(0, Screen.height - 28f, Screen.width - 20f, 24f),
                    "Space: devam   Esc: geç", smallStyle);
            }
            else if (hintTimer > 0f && !string.IsNullOrEmpty(endHint))
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(hintTimer));
                Rect r = new Rect(Screen.width * 0.2f, 40f, Screen.width * 0.6f, 60f);
                GUI.Box(r, GUIContent.none);
                GUI.Label(r, endHint, hintStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawCinemaBars()
        {
            float bar = Screen.height * 0.08f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, bar), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - bar, Screen.width, bar), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawDialogBox(Shot shot)
        {
            float boxHeight = Mathf.Clamp(Screen.height * 0.2f, 120f, 190f);
            float boxWidth = Mathf.Min(Screen.width * 0.6f, 900f);
            float margin = 30f;
            float bottom = Screen.height * 0.08f + 15f;

            // Soldan kayarak gelir (yumuşak yavaşlama ile).
            float t = Mathf.Clamp01(shotElapsed / slideDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float x = Mathf.Lerp(-boxWidth - margin, margin, ease);

            Rect box = new Rect(x, Screen.height - bottom - boxHeight, boxWidth, boxHeight);
            DrawFramed(box, boxColor);

            float pad = 12f;
            float textLeft = box.x + pad;

            // Portre karesi
            if (shot.portrait != null)
            {
                float size = boxHeight - pad * 2f;
                Rect frame = new Rect(box.x + pad, box.y + pad, size, size);
                DrawFramed(frame, Color.black);
                GUI.DrawTexture(Shrink(frame, 3f), shot.portrait, ScaleMode.ScaleAndCrop);
                textLeft = frame.xMax + pad * 1.5f;
            }

            float textWidth = box.xMax - textLeft - pad;

            if (!string.IsNullOrEmpty(shot.speakerName))
                GUI.Label(new Rect(textLeft, box.y + pad, textWidth, 30f), shot.speakerName, nameStyle);

            string visible = shot.text.Substring(0, VisibleLetters());
            GUI.Label(new Rect(textLeft, box.y + pad + 34f, textWidth, boxHeight - pad * 2f - 34f),
                visible, textStyle);
        }

        private void DrawFramed(Rect rect, Color fill)
        {
            GUI.color = frameColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(Shrink(rect, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Rect Shrink(Rect r, float by) =>
            new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);

        private void CreateStyles()
        {
            if (textStyle != null) return;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, wordWrap = true, alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.95f, 0.92f, 0.85f) }
            };
            nameStyle = new GUIStyle(textStyle)
            {
                fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = false,
                normal = { textColor = frameColor }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight, fontSize = 14,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };
            hintStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
        }
    }
}
