using AIAdventure.Blackjack;
using UnityEngine;

namespace AIAdventure.UI
{
    /// <summary>
    /// Blackjack masasını yeşil çuha üzerinde kartlarla çizer.
    /// Oyun durumunu her karede BlackjackGame'den okur.
    /// </summary>
    public class ImGuiBlackjackView : MonoBehaviour, IBlackjackView
    {
        private const float ReferenceHeight = 1080f;
        private const float CardWidth = 110f;
        private const float CardHeight = 160f;

        private BlackjackGame game;
        private string message;
        private string prompt;

        private Texture2D dimTexture;
        private Texture2D feltTexture;
        private Texture2D faceTexture;
        private Texture2D backTexture;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle messageStyle;
        private GUIStyle cardStyle;
        private GUIStyle promptStyle;

        public void Open(BlackjackGame blackjackGame) => game = blackjackGame;
        public void Close() => game = null;
        public void SetMessage(string text) => message = text;
        public void SetPrompt(string text) => prompt = text;

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            if (game != null)
                DrawTable(width);
            else if (!string.IsNullOrEmpty(prompt))
                GUI.Box(new Rect((width - 380f) / 2f, ReferenceHeight - 140f, 380f, 56f), prompt, promptStyle);
        }

        private void DrawTable(float width)
        {
            GUI.DrawTexture(new Rect(0f, 0f, width, ReferenceHeight), dimTexture);

            Rect felt = new Rect((width - 1100f) / 2f, (ReferenceHeight - 760f) / 2f, 1100f, 760f);
            GUI.DrawTexture(felt, feltTexture);
            GUI.Label(new Rect(felt.x, felt.y + 20f, felt.width, 50f), "Kâtip'in Masası", titleStyle);

            string dealerValue = game.DealerHoleCardHidden && game.DealerHand.Cards.Count > 0
                ? $"{game.DealerHand.Cards[0].BaseValue} + ?"
                : game.DealerHand.Value.ToString();
            GUI.Label(new Rect(felt.x + 60f, felt.y + 90f, 600f, 40f), $"Kâtip  —  {dealerValue}", labelStyle);
            DrawHand(game.DealerHand, felt.x + 60f, felt.y + 140f, game.DealerHoleCardHidden);

            GUI.Label(new Rect(felt.x + 60f, felt.y + 340f, 600f, 40f), $"Sen  —  {game.PlayerHand.Value}", labelStyle);
            DrawHand(game.PlayerHand, felt.x + 60f, felt.y + 390f, false);

            GUI.Label(new Rect(felt.x + 40f, felt.y + 590f, felt.width - 80f, 140f), message, messageStyle);
        }

        private void DrawHand(Hand hand, float x, float y, bool hideSecond)
        {
            for (int i = 0; i < hand.Cards.Count; i++)
            {
                Rect rect = new Rect(x + i * (CardWidth + 18f), y, CardWidth, CardHeight);

                if (hideSecond && i == 1)
                {
                    GUI.DrawTexture(rect, backTexture);
                    continue;
                }

                Card card = hand.Cards[i];
                GUI.DrawTexture(rect, faceTexture);
                cardStyle.normal.textColor = card.IsRed ? new Color(0.75f, 0.1f, 0.1f) : Color.black;
                GUI.Label(rect, $"{card.RankText}\n{card.SuitSymbol}", cardStyle);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            dimTexture = MakeTexture(new Color(0f, 0f, 0f, 0.75f));
            feltTexture = MakeTexture(new Color(0.06f, 0.30f, 0.16f));
            faceTexture = MakeTexture(new Color(0.97f, 0.95f, 0.90f));
            backTexture = MakeTexture(new Color(0.45f, 0.08f, 0.10f));

            Color gold = new Color(0.95f, 0.82f, 0.45f);
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = gold }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };
            messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, wordWrap = true, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = gold }
            };
            cardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
            };
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in new[] { dimTexture, feltTexture, faceTexture, backTexture })
                if (texture != null) Destroy(texture);
        }
    }
}
