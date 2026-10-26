using System;
using System.Collections.Generic;
using AIAdventure.Roulette;
using UnityEngine;

namespace AIAdventure.UI
{
    /// <summary>
    /// Rulet masasının ekranı: sol üstte Baron'un soğukkanlılığı ve tabancanın
    /// durumu, altta sohbet geçmişi, "Baron düşünüyor..." göstergesi ve yazı alanı.
    /// Enter: yazmaya başla / gönder. Esc: yazmayı bırak.
    /// </summary>
    public class ImGuiRouletteView : MonoBehaviour, IRouletteView
    {
        private const float ReferenceHeight = 1080f;
        private const string InputControl = "BaronInput";
        private const int MaxLines = 7;

        private struct Line
        {
            public string Speaker;
            public string Text;
            public Color Color;
        }

        private readonly List<Line> lines = new List<Line>();
        private RouletteGame game;
        private string input = "";
        private string hint;
        private string prompt;
        private bool thinking;
        private bool endingVisible;
        private bool playerWon;
        private string endingText;
        private float endingStartTime;
        private float flashUntil;
        private bool focusRequested;

        private Texture2D white;
        private GUIStyle lineStyle;
        private GUIStyle smallStyle;
        private GUIStyle titleStyle;
        private GUIStyle inputStyle;
        private GUIStyle boxStyle;
        private GUIStyle endingTitleStyle;
        private GUIStyle endingTextStyle;

        public event Action<string> MessageSubmitted;
        public bool IsTyping { get; private set; }

        public void Open(RouletteGame rouletteGame)
        {
            game = rouletteGame;
            lines.Clear();
            input = "";
            thinking = false;
            endingVisible = false;
        }

        public void Close()
        {
            game = null;
            IsTyping = false;
            endingVisible = false;
        }

        public void AddChatLine(string speaker, string text) =>
            AddLine(speaker, text, speaker == "Sen" ? Color.white : new Color(0.95f, 0.75f, 0.35f));

        public void AddNarration(string text) => AddLine(null, text, new Color(0.7f, 0.7f, 0.7f));
        public void AddSystemLine(string text) => AddLine("!", text, new Color(1f, 0.45f, 0.3f));
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

        private void AddLine(string speaker, string text, Color color)
        {
            lines.Add(new Line { Speaker = speaker, Text = text, Color = color });
            while (lines.Count > MaxLines) lines.RemoveAt(0);
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
            DrawChat(width);

            if (endingVisible && !playerWon) DrawLoss(width);
        }

        private void DrawStatus()
        {
            Rect panel = new Rect(30f, 30f, 420f, 190f);
            Fill(panel, new Color(0f, 0f, 0f, 0.55f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, 380f, 30f), "Baron'un Soğukkanlılığı", titleStyle);
            Rect bar = new Rect(panel.x + 20f, panel.y + 50f, 380f, 22f);
            Fill(bar, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            float nerve = game.Nerve / (float)RouletteGame.StartingNerve;
            Fill(new Rect(bar.x, bar.y, bar.width * nerve, bar.height),
                Color.Lerp(new Color(0.8f, 0.15f, 0.1f), new Color(0.9f, 0.75f, 0.3f), nerve));
            GUI.Label(new Rect(bar.x, bar.y, bar.width, bar.height), $"{game.Nerve} / 100", smallStyle);

            // Tabanca: çekilmiş hazneler gri, kalanlar koyu. Merminin yeri gösterilmez.
            for (int i = 0; i < RouletteGame.ChamberCount; i++)
            {
                Rect chamber = new Rect(panel.x + 20f + i * 46f, panel.y + 90f, 36f, 36f);
                Fill(chamber, i < game.PullsSinceSpin ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.12f, 0.1f, 0.08f));
            }

            string turn = game.IsOver ? "Oyun bitti" : game.Turn == RouletteTurn.Player ? "Sıra: SENDE" : "Sıra: BARON'DA";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 136f, 380f, 24f), turn, titleStyle);
            if (!game.IsOver)
                GUI.Label(new Rect(panel.x + 20f, panel.y + 160f, 380f, 24f),
                    $"Sıradaki tetikte mermi ihtimali: %{Mathf.RoundToInt(game.NextShotChance * 100f)}", smallStyle);
        }

        private void DrawChat(float width)
        {
            float chatWidth = 1100f;
            float x = (width - chatWidth) / 2f;
            float lineHeight = 34f;
            float logHeight = MaxLines * lineHeight + 20f;
            float top = ReferenceHeight - logHeight - 150f;

            Fill(new Rect(x, top, chatWidth, logHeight + 130f), new Color(0f, 0f, 0f, 0.5f));

            for (int i = 0; i < lines.Count; i++)
            {
                Line line = lines[i];
                lineStyle.normal.textColor = line.Color;
                lineStyle.fontStyle = line.Speaker == null ? FontStyle.Italic : FontStyle.Normal;
                string text = line.Speaker == null ? line.Text : $"{line.Speaker}: {line.Text}";
                GUI.Label(new Rect(x + 20f, top + 10f + i * lineHeight, chatWidth - 40f, lineHeight), text, lineStyle);
            }

            float inputTop = top + logHeight + 10f;
            if (thinking)
            {
                string dots = new string('.', 1 + (int)(Time.time * 2f) % 3);
                smallStyle.normal.textColor = new Color(0.95f, 0.75f, 0.35f);
                GUI.Label(new Rect(x + 20f, inputTop, 400f, 30f), "Baron düşünüyor" + dots, smallStyle);
                smallStyle.normal.textColor = Color.white;
            }

            HandleInputKeys();

            GUI.enabled = !thinking && !game.IsOver;
            GUI.SetNextControlName(InputControl);
            input = GUI.TextField(new Rect(x + 20f, inputTop + 34f, chatWidth - 40f, 40f), input, 160, inputStyle);
            GUI.enabled = true;

            if (focusRequested)
            {
                GUI.FocusControl(InputControl);
                focusRequested = false;
            }
            IsTyping = GUI.GetNameOfFocusedControl() == InputControl;

            if (!string.IsNullOrEmpty(hint))
                GUI.Label(new Rect(x + 20f, inputTop + 80f, chatWidth - 40f, 30f), hint, smallStyle);
        }

        private void HandleInputKeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            bool focused = GUI.GetNameOfFocusedControl() == InputControl;

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
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

        private void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (lineStyle != null) return;

            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            lineStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, wordWrap = true, clipping = TextClipping.Clip };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, normal = { textColor = Color.white } };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.85f, 0.6f) }
            };
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 22, padding = new RectOffset(10, 10, 8, 8) };
            boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            endingTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.1f, 0.1f) }
            };
            endingTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26, wordWrap = true, alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (white != null) Destroy(white);
        }
    }
}
