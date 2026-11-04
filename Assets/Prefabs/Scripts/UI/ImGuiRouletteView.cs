using System;
using System.Collections.Generic;
using AIAdventure.Roulette;
using UnityEngine;

namespace AIAdventure.UI
{
    /// <summary>
    /// Rulet masasının ekranı.
    /// - Oturunca kuralları anlatan bir kart çıkar ([Enter] ile kapanır).
    /// - Sol üst: Baron'un soğukkanlılık göstergesi (50'nin altında titrer),
    ///   tabancanın silindiri, kurşun sayısı ve sıra.
    /// - Sol alt: Baron'un portresi ve son sözü; sağ alt: senin portren ve son sözün.
    ///   Yazılar daktilo gibi harf harf çıkar.
    /// - Üst orta: anlatıcı satırları. Alt orta: yazı alanı.
    /// Enter: yazmaya başla / gönder. Esc: yazmayı bırak.
    /// </summary>
    public class ImGuiRouletteView : MonoBehaviour, IRouletteView
    {
        private const float ReferenceHeight = 1080f;
        private const string InputControl = "BaronInput";
        private const int MaxNarration = 3;

        [Header("Portreler")]
        [SerializeField] private string baronName = "Baron Mortis";
        [SerializeField] private Texture2D baronPortrait;
        [SerializeField] private string playerName = "Sen";
        [SerializeField] private Texture2D playerPortrait;

        [Header("Görünüm")]
        [SerializeField] private float lettersPerSecond = 45f;
        [SerializeField] private Color frameColor = new Color(0.75f, 0.6f, 0.35f, 1f);
        [SerializeField] private Color boxColor = new Color(0.05f, 0.04f, 0.03f, 0.88f);
        [SerializeField] private bool showRulesOnSit = true;

        private class Speech
        {
            public string Text;
            public float StartTime;
        }

        private struct Narration
        {
            public string Text;
            public Color Color;
            public float Time;
        }

        private readonly List<Narration> narration = new List<Narration>();
        private Speech baronSpeech;
        private Speech playerSpeech;
        private RouletteGame game;
        private string input = "";
        private string hint;
        private string prompt;
        private bool thinking;
        private bool rulesVisible;
        private bool endingVisible;
        private bool playerWon;
        private string endingText;
        private float endingStartTime;
        private float flashUntil;
        private bool focusRequested;

        // Gösterge animasyonları
        private float shownNerve;
        private int lastNerve;
        private string deltaText;
        private Color deltaColor;
        private float deltaStartTime = -10f;
        private float shownCylinderAngle;

        private Texture2D white, circle;
        private GUIStyle textStyle, nameStyle, smallStyle, titleStyle, narrationStyle, inputStyle,
            boxStyle, bigStyle, rulesStyle, endingTitleStyle, endingTextStyle, centerStyle;

        public event Action<string> MessageSubmitted;
        public bool IsTyping { get; private set; }
        public bool IsBlocking => game != null && rulesVisible;

        // ---------------------------------------------------------------- IRouletteView

        public void Open(RouletteGame rouletteGame)
        {
            game = rouletteGame;
            narration.Clear();
            baronSpeech = playerSpeech = null;
            input = "";
            thinking = false;
            endingVisible = false;
            rulesVisible = showRulesOnSit;
            shownNerve = lastNerve = RouletteGame.StartingNerve;
            deltaStartTime = -10f;
            shownCylinderAngle = 0f;
        }

        public void Close()
        {
            game = null;
            IsTyping = false;
            endingVisible = false;
            rulesVisible = false;
        }

        public void AddChatLine(string speaker, string text)
        {
            var speech = new Speech { Text = text ?? "", StartTime = Time.time };
            if (speaker == "Sen") playerSpeech = speech;
            else baronSpeech = speech;
        }

        public void AddNarration(string text) => AddNarrationLine(text, new Color(0.85f, 0.82f, 0.75f));
        public void AddSystemLine(string text) => AddNarrationLine(text, new Color(1f, 0.5f, 0.4f));
        public void SetThinking(bool value) => thinking = value;
        public void SetHint(string text) => hint = text;
        public void SetPrompt(string text) => prompt = text;
        public void FlashShot() => flashUntil = Time.time + 0.15f;

        public void ShowEnding(bool won, string text)
        {
            endingVisible = true;
            playerWon = won;
            endingText = text;
            endingStartTime = Time.time;
            IsTyping = false;
        }

        private void AddNarrationLine(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            narration.Add(new Narration { Text = text, Color = color, Time = Time.time });
            while (narration.Count > MaxNarration) narration.RemoveAt(0);
        }

        // ---------------------------------------------------------------- Animasyonlar

        private void Update()
        {
            if (game == null) return;

            // Soğukkanlılık değişince küçük bir "+10 / -22" yazısı gösterilir.
            if (game.Nerve != lastNerve)
            {
                int delta = game.Nerve - lastNerve;
                deltaText = delta > 0 ? $"+{delta}" : delta.ToString();
                deltaColor = delta < 0 ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.45f, 0.35f);
                deltaStartTime = Time.time;
                lastNerve = game.Nerve;
            }
            shownNerve = Mathf.MoveTowards(shownNerve, game.Nerve, 40f * Time.deltaTime);
            shownCylinderAngle = Mathf.LerpAngle(shownCylinderAngle, game.PullsSinceSpin * 60f, 8f * Time.deltaTime);
        }

        // ---------------------------------------------------------------- Çizim

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            if (game == null)
            {
                if (!string.IsNullOrEmpty(prompt))
                    GUI.Box(new Rect((width - 420f) / 2f, ReferenceHeight - 140f, 420f, 56f), prompt, boxStyle);
                return;
            }

            if (Time.time < flashUntil)
            {
                GUI.color = new Color(1f, 0.95f, 0.85f, 0.85f);
                GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), white);
                GUI.color = Color.white;
            }

            DrawStatus();
            DrawNarration(width);
            DrawSpeechBoxes(width);
            DrawInput(width);

            if (rulesVisible) DrawRules(width);
            if (endingVisible && !playerWon) DrawLoss(width);
        }

        // ---------- Sol üst: durum paneli

        private void DrawStatus()
        {
            Rect panel = new Rect(30f, 30f, 440f, 300f);
            Framed(panel, new Color(0.04f, 0.03f, 0.02f, 0.82f));
            float x = panel.x + 22f;
            float w = panel.width - 44f;

            float hesitation = game.HesitationChance;
            Vector2 shake = hesitation > 0f
                ? new Vector2(Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f)
                  * (4f + 10f * hesitation)
                : Vector2.zero;

            GUI.Label(new Rect(x, panel.y + 14f, w, 30f), "BARON'UN SOĞUKKANLILIĞI", titleStyle);

            Rect bar = new Rect(x + shake.x, panel.y + 52f + shake.y, w, 30f);
            Fill(new Rect(bar.x - 2f, bar.y - 2f, bar.width + 4f, bar.height + 4f), frameColor * 0.6f);
            Fill(bar, new Color(0.12f, 0.1f, 0.09f));

            float t = shownNerve / RouletteGame.MaxNerve;
            Color calm = new Color(0.55f, 0.65f, 0.75f);   // çelik mavi: sakin
            Color shaky = new Color(0.85f, 0.15f, 0.1f);   // kırmızı: titriyor
            Color fill = t >= 0.5f ? calm : Color.Lerp(shaky, calm, t / 0.5f);
            if (hesitation > 0f) fill = Color.Lerp(fill, Color.white, 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.time * 10f)));
            Fill(new Rect(bar.x, bar.y, bar.width * t, bar.height), fill);

            float threshold = RouletteGame.HesitationThreshold / (float)RouletteGame.MaxNerve;
            Fill(new Rect(bar.x + bar.width * threshold - 1f, bar.y - 6f, 2f, bar.height + 12f), Color.white);
            GUI.Label(new Rect(bar.x, bar.y, bar.width, bar.height), Mathf.RoundToInt(shownNerve).ToString(), centerStyle);

            // Değişim yazısı (−22 / +10) yukarı süzülerek kaybolur
            float age = Time.time - deltaStartTime;
            if (age < 1.6f)
            {
                GUI.color = new Color(deltaColor.r, deltaColor.g, deltaColor.b, 1f - age / 1.6f);
                GUI.Label(new Rect(bar.xMax - 90f, bar.y - 30f - age * 25f, 90f, 30f), deltaText, bigStyle);
                GUI.color = Color.white;
            }

            int hesitationPercent = Mathf.RoundToInt(hesitation * 100f);
            smallStyle.normal.textColor = hesitation > 0f ? new Color(1f, 0.5f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(x, panel.y + 92f, w, 26f),
                hesitation > 0f ? $"ELLERİ TİTRİYOR! Tetiği çekememe ihtimali: %{hesitationPercent}"
                                : "Elleri sağlam. 50'nin altına düşür, titremeye başlasın.", smallStyle);
            smallStyle.normal.textColor = Color.white;

            DrawCylinder(new Vector2(x + 70f, panel.y + 200f), 52f);

            // Kurşunlar
            float infoX = x + 160f;
            GUI.Label(new Rect(infoX, panel.y + 140f, 220f, 26f), "Kurşun", smallStyle);
            for (int i = 0; i < RouletteGame.MaxBullets; i++)
            {
                Rect b = new Rect(infoX + 80f + i * 24f, panel.y + 143f, 14f, 22f);
                Fill(b, i < game.Bullets ? new Color(0.85f, 0.65f, 0.3f) : new Color(0.25f, 0.22f, 0.2f));
            }

            if (!game.IsOver)
                GUI.Label(new Rect(infoX, panel.y + 172f, 260f, 26f),
                    $"Patlama ihtimali: %{Mathf.RoundToInt(game.NextShotChance * 100f)}", smallStyle);

            // Sıra rozeti
            bool playerTurn = game.Turn == RouletteTurn.Player;
            string turn = game.IsOver ? "OYUN BİTTİ" : playerTurn ? "SIRA SENDE" : "SIRA BARON'DA";
            Color turnColor = game.IsOver ? new Color(0.4f, 0.4f, 0.4f)
                : playerTurn ? new Color(0.2f, 0.5f, 0.25f) : new Color(0.6f, 0.15f, 0.12f);
            Rect pill = new Rect(infoX, panel.y + 208f, 230f, 36f);
            Fill(pill, turnColor);
            GUI.Label(pill, turn, centerStyle);

            if (game.BaronPullsTwiceNextTurn && !game.IsOver)
                GUI.Label(new Rect(infoX, panel.y + 252f, 260f, 26f), "Anlaşma: Baron iki kez çekecek", smallStyle);
        }

        /// <summary>Altı hazneli silindir. Sıradaki hazne en üstte durur.</summary>
        private void DrawCylinder(Vector2 center, float radius)
        {
            Circle(center, radius + 16f, new Color(0.18f, 0.17f, 0.16f));
            Circle(center, radius + 12f, new Color(0.3f, 0.28f, 0.26f));
            Circle(center, 10f, new Color(0.15f, 0.14f, 0.13f));

            for (int i = 0; i < RouletteGame.ChamberCount; i++)
            {
                float angle = (-90f + i * 60f - shownCylinderAngle) * Mathf.Deg2Rad;
                Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.62f;
                bool pulled = i < game.PullsSinceSpin;
                bool next = i == game.PullsSinceSpin && !game.IsOver;

                if (next) Circle(p, 17f, frameColor);
                Circle(p, 14f, pulled ? new Color(0.45f, 0.43f, 0.4f) : new Color(0.07f, 0.06f, 0.05f));
                if (pulled) Circle(p, 8f, new Color(0.3f, 0.28f, 0.26f));
            }
        }

        // ---------- Üst orta: anlatıcı

        private void DrawNarration(float width)
        {
            float w = Mathf.Min(900f, width - 1000f);
            if (w < 500f) w = Mathf.Min(900f, width - 560f);
            float x = Mathf.Max(500f, (width - w) / 2f);
            float y = 34f;

            for (int i = 0; i < narration.Count; i++)
            {
                Narration n = narration[i];
                float age = Time.time - n.Time;
                bool newest = i == narration.Count - 1;
                float alpha = newest ? Mathf.Clamp01(age * 3f) : 0.55f - 0.15f * (narration.Count - 1 - i);

                narrationStyle.normal.textColor = new Color(n.Color.r, n.Color.g, n.Color.b, alpha);
                narrationStyle.fontSize = newest ? 23 : 19;
                float h = narrationStyle.CalcHeight(new GUIContent(n.Text), w);
                GUI.Label(new Rect(x, y, w, h), n.Text, narrationStyle);
                y += h + 6f;
            }
        }

        // ---------- Alt: konuşma kutuları

        private void DrawSpeechBoxes(float width)
        {
            float boxWidth = Mathf.Min(760f, (width - 90f) / 2f);
            float boxHeight = 170f;
            float y = ReferenceHeight - 340f;

            DrawSpeech(new Rect(30f, y, boxWidth, boxHeight), baronName, baronPortrait, baronSpeech,
                portraitLeft: true, showThinking: thinking);
            DrawSpeech(new Rect(width - 30f - boxWidth, y, boxWidth, boxHeight), playerName, playerPortrait,
                playerSpeech, portraitLeft: false, showThinking: false);
        }

        private void DrawSpeech(Rect box, string speaker, Texture2D portrait, Speech speech, bool portraitLeft, bool showThinking)
        {
            if (speech == null && !showThinking) return;

            // Yeni söz gelince kutu kenardan kısa bir kayma ile girer.
            float age = speech != null ? Time.time - speech.StartTime : 1f;
            float slide = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / 0.25f), 3f);
            float offset = (1f - slide) * 120f * (portraitLeft ? -1f : 1f);
            box.x += offset;

            GUI.color = new Color(1f, 1f, 1f, slide);
            Framed(box, boxColor);

            float pad = 12f;
            float size = box.height - pad * 2f;
            Rect textRect = new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f);

            if (portrait != null)
            {
                Rect frame = portraitLeft
                    ? new Rect(box.x + pad, box.y + pad, size, size)
                    : new Rect(box.xMax - pad - size, box.y + pad, size, size);
                Framed(frame, Color.black);
                GUI.DrawTexture(Shrink(frame, 3f), portrait, ScaleMode.ScaleAndCrop);

                textRect.width -= size + pad;
                if (portraitLeft) textRect.x += size + pad;
            }

            nameStyle.alignment = portraitLeft ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            GUI.Label(new Rect(textRect.x, textRect.y, textRect.width, 28f), speaker, nameStyle);

            Rect body = new Rect(textRect.x, textRect.y + 32f, textRect.width, textRect.height - 32f);
            if (showThinking)
            {
                string dots = new string('.', 1 + (int)(Time.time * 2.5f) % 3);
                GUI.Label(body, "Düşünüyor" + dots, textStyle);
            }
            else if (speech != null)
            {
                int letters = Mathf.Min(speech.Text.Length, Mathf.FloorToInt(Mathf.Max(0f, age - 0.15f) * lettersPerSecond));
                GUI.Label(body, speech.Text.Substring(0, letters), textStyle);
            }
            GUI.color = Color.white;
        }

        // ---------- Alt orta: yazı alanı

        private void DrawInput(float width)
        {
            float w = Mathf.Min(1000f, width - 60f);
            float x = (width - w) / 2f;
            float y = ReferenceHeight - 140f;

            HandleInputKeys();

            GUI.enabled = !thinking && !game.IsOver && !rulesVisible;
            GUI.SetNextControlName(InputControl);
            input = GUI.TextField(new Rect(x, y, w, 44f), input, 160, inputStyle);
            GUI.enabled = true;

            if (string.IsNullOrEmpty(input) && GUI.GetNameOfFocusedControl() != InputControl)
            {
                smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
                GUI.Label(new Rect(x + 12f, y + 9f, w, 30f), "Baron'a bir şey söylemek için Enter'a bas...", smallStyle);
                smallStyle.normal.textColor = Color.white;
            }

            if (focusRequested)
            {
                GUI.FocusControl(InputControl);
                focusRequested = false;
            }
            IsTyping = GUI.GetNameOfFocusedControl() == InputControl;

            if (!string.IsNullOrEmpty(hint))
                GUI.Label(new Rect(x, y + 52f, w, 30f), hint, smallStyle);
        }

        private void HandleInputKeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            bool enter = e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter;

            if (rulesVisible)
            {
                if (enter) { rulesVisible = false; e.Use(); }
                return;
            }

            bool focused = GUI.GetNameOfFocusedControl() == InputControl;

            if (enter)
            {
                if (!focused)
                {
                    focusRequested = !thinking && !game.IsOver;
                }
                else
                {
                    string message = input.Trim();
                    input = "";
                    GUI.FocusControl(null);
                    if (message.Length > 0) MessageSubmitted?.Invoke(message);
                }
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape && focused)
            {
                GUI.FocusControl(null);
                e.Use();
            }
        }

        // ---------- Kurallar kartı

        private void DrawRules(float width)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), white);
            GUI.color = Color.white;

            Rect card = new Rect((width - 900f) / 2f, 170f, 900f, 640f);
            Framed(card, new Color(0.06f, 0.05f, 0.04f, 0.97f));

            GUI.Label(new Rect(card.x, card.y + 24f, card.width, 50f), "BARON'UN MASASI", endingTitleStyle);

            const string rules =
                "Altı hazne, tek kurşun. Sırayla tetiği çekeceksiniz.\n" +
                "Ama Baron'u yenmenin tek yolu şans değil: <b>sinirlerini bozmak</b>.\n\n" +
                "<color=#8CF08C><b>Soğukkanlılığı DÜŞER</b></color>  eğer...\n" +
                "    blöf yaparsan, tehdit edersen, kurşunun yerini bildiğine onu inandırırsan.\n\n" +
                "<color=#FF8070><b>Soğukkanlılığı ARTAR</b></color>  eğer...\n" +
                "    hakaret edersen (öfke onu güçlendirir), yalvarırsan ya da saçmalarsan.\n\n" +
                "Soğukkanlılık <b>50'nin altına</b> inince Baron'un eli titremeye başlar.\n" +
                "Kendi sırasında tetiği çekemezse <b>kaybeder</b>. <b>0</b>'a inerse pes eder.\n\n" +
                "Pazarlık da edebilirsin: <i>\"Önce sen çek\"</i>, <i>\"İki kez çek\"</i>, <i>\"Bir kurşunu çıkar\"</i>.\n" +
                "Her turda sınırlı sayıda konuşma hakkın var.";

            GUI.Label(new Rect(card.x + 50f, card.y + 100f, card.width - 100f, 440f), rules, rulesStyle);

            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
            GUI.color = new Color(1f, 1f, 1f, pulse);
            GUI.Label(new Rect(card.x, card.yMax - 70f, card.width, 40f),
                "[Enter] Başla        [Enter] Yaz    [Space] Tetiği çek", titleStyle.CenteredCopy());
            GUI.color = Color.white;
        }

        // ---------- Kaybetme ekranı

        private void DrawLoss(float width)
        {
            float alpha = Mathf.Clamp01((Time.time - endingStartTime) / 1.5f);
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), white);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(0f, 360f, width, 100f), "OYUN BİTTİ", endingTitleStyle);
            GUI.Label(new Rect((width - 1000f) / 2f, 480f, 1000f, 160f), endingText + "\n\n[R] Tekrar dene", endingTextStyle);
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- Yardımcılar

        private void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * old.a);
            GUI.DrawTexture(rect, white);
            GUI.color = old;
        }

        private void Framed(Rect rect, Color fill)
        {
            Fill(rect, frameColor);
            Fill(Shrink(rect, 2f), fill);
        }

        private void Circle(Vector2 center, float radius, Color color)
        {
            Color old = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * old.a);
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), circle);
            GUI.color = old;
        }

        private static Rect Shrink(Rect r, float by) =>
            new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);

        private void EnsureStyles()
        {
            if (textStyle != null) return;

            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            const int size = 64;
            circle = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(c - d)));
            }
            circle.Apply();

            Color cream = new Color(0.95f, 0.92f, 0.85f);
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = cream }
            };
            nameStyle = new GUIStyle(textStyle)
            {
                fontSize = 21, fontStyle = FontStyle.Bold, wordWrap = false, normal = { textColor = frameColor }
            };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, normal = { textColor = Color.white } };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.85f, 0.6f) }
            };
            centerStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
            };
            bigStyle = new GUIStyle(titleStyle) { fontSize = 28, alignment = TextAnchor.MiddleRight };
            narrationStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23, wordWrap = true, fontStyle = FontStyle.Italic, alignment = TextAnchor.UpperCenter
            };
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 22, padding = new RectOffset(12, 12, 9, 9) };
            boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            rulesStyle = new GUIStyle(textStyle) { fontSize = 23, richText = true };
            endingTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.15f, 0.1f) }
            };
            endingTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26, wordWrap = true, alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (white != null) Destroy(white);
            if (circle != null) Destroy(circle);
        }
    }

    internal static class GuiStyleExtensions
    {
        public static GUIStyle CenteredCopy(this GUIStyle style) =>
            new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
    }
}
