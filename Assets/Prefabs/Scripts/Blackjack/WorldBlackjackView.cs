using System.Collections.Generic;
using AIAdventure.Player;
using AIAdventure.UI;
using UnityEngine;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Blackjack'i ayrı bir ekran açmadan, masanın üzerinde 3D kartlarla oynatır.
    /// Oturunca kamera masaya iner; kartlar desteden dağıtılır, Kâtip'in kapalı
    /// kartı sırası gelince çevrilir. Mesajlar altyazı gibi alt kısımda görünür.
    /// IBlackjackView'ı uyguladığı için BlackjackTable'da hiçbir değişiklik gerekmez.
    /// </summary>
    public class WorldBlackjackView : MonoBehaviour, IBlackjackView
    {
        private const float ReferenceHeight = 1080f;

        [Header("Kamera")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Vector3 seatOffset = new Vector3(0f, 1.6f, -0.9f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0f, 0.15f);
        [SerializeField] private float cameraSpeed = 4f;

        [Header("Masa düzeni")]
        [SerializeField] private float firstCardX = -0.55f;
        [SerializeField] private float cardSpacing = 0.32f;
        [SerializeField] private float dealerRowZ = 0.32f;
        [SerializeField] private float playerRowZ = -0.30f;
        [SerializeField] private Vector3 deckOffset = new Vector3(1.15f, 0f, 0.35f);
        [SerializeField] private float dealInterval = 0.18f;

        private readonly List<CardVisual> dealerCards = new List<CardVisual>();
        private readonly List<CardVisual> playerCards = new List<CardVisual>();

        private BlackjackGame game;
        private string message;
        private string prompt;
        private Font font;
        private Camera viewCamera;
        private Transform dressing;
        private Transform cardRoot;
        private TextMesh dealerLabel;
        private TextMesh playerLabel;
        private float nextDealTime;

        private GUIStyle subtitleStyle;
        private GUIStyle promptStyle;
        private Texture2D subtitleTexture;

        private Vector3 TableTop => transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        private Vector3 Point(Vector3 local) => TableTop + transform.rotation * local;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
            viewCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;

            BuildDressing();
        }

        // ---------------------------------------------------------------- IBlackjackView

        public void Open(BlackjackGame blackjackGame)
        {
            game = blackjackGame;
            nextDealTime = 0f;
            if (cameraFollow != null) cameraFollow.enabled = false;
            dealerLabel.gameObject.SetActive(true);
            playerLabel.gameObject.SetActive(true);
        }

        public void Close()
        {
            game = null;
            ClearCards();
            dealerLabel.gameObject.SetActive(false);
            playerLabel.gameObject.SetActive(false);
            if (cameraFollow != null) cameraFollow.enabled = true;
        }

        public void SetMessage(string text) => message = text;
        public void SetPrompt(string text) => prompt = text;

        // ---------------------------------------------------------------- Her kare

        private void LateUpdate()
        {
            if (game == null) return;

            SyncCards();
            UpdateLabels();
            MoveCamera();
        }

        private void SyncCards()
        {
            if (game.PlayerHand.Cards.Count < playerCards.Count || game.DealerHand.Cards.Count < dealerCards.Count)
                ClearCards();

            // Gerçek masadaki gibi sırayla dağıt: oyuncu, krupiye, oyuncu, krupiye...
            bool spawned = true;
            while (spawned)
            {
                spawned = false;
                if (playerCards.Count < game.PlayerHand.Cards.Count)
                {
                    Spawn(playerCards, game.PlayerHand, playerRowZ, false);
                    spawned = true;
                }
                if (dealerCards.Count < game.DealerHand.Cards.Count)
                {
                    Spawn(dealerCards, game.DealerHand, dealerRowZ, true);
                    spawned = true;
                }
            }

            if (dealerCards.Count > 1 && dealerCards[1].IsFaceDown && !game.DealerHoleCardHidden)
                dealerCards[1].Flip();
        }

        private void Spawn(List<CardVisual> visuals, Hand hand, float rowZ, bool isDealer)
        {
            int index = visuals.Count;
            CardVisual card = CardVisual.Create(hand.Cards[index], cardRoot, font);

            bool faceDown = isDealer && index == 1 && game.DealerHoleCardHidden;
            Vector3 target = Point(new Vector3(firstCardX + index * cardSpacing, 0.009f + index * 0.001f, rowZ));
            Vector3 from = Point(deckOffset) + Vector3.up * 0.07f;

            float delay = Mathf.Max(0f, nextDealTime - Time.time);
            nextDealTime = Time.time + delay + dealInterval;

            card.Deal(from, target, transform.rotation, delay, faceDown);
            visuals.Add(card);
        }

        private void UpdateLabels()
        {
            string dealerValue = game.DealerHoleCardHidden && game.DealerHand.Cards.Count > 0
                ? $"{game.DealerHand.Cards[0].BaseValue} + ?"
                : game.DealerHand.Value.ToString();
            dealerLabel.text = $"Kâtip: {dealerValue}";
            playerLabel.text = $"Sen: {game.PlayerHand.Value}";
        }

        private void MoveCamera()
        {
            if (viewCamera == null) return;

            Vector3 position = Point(seatOffset);
            Quaternion rotation = Quaternion.LookRotation(Point(lookOffset) - position);
            float t = cameraSpeed * Time.deltaTime;
            viewCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(viewCamera.transform.position, position, t),
                Quaternion.Slerp(viewCamera.transform.rotation, rotation, t));
        }

        private void ClearCards()
        {
            foreach (CardVisual card in dealerCards) if (card != null) Destroy(card.gameObject);
            foreach (CardVisual card in playerCards) if (card != null) Destroy(card.gameObject);
            dealerCards.Clear();
            playerCards.Clear();
            nextDealTime = 0f;
        }

        // ---------------------------------------------------------------- Masa süsleri

        private void BuildDressing()
        {
            // Masa ölçekli olduğu için süsleri masanın değil, onun ebeveyninin altına koyuyoruz.
            dressing = new GameObject("Masa_Susleri").transform;
            dressing.SetParent(transform.parent, false);
            dressing.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            cardRoot = new GameObject("Kartlar").transform;
            cardRoot.SetParent(dressing, false);

            GameObject felt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            felt.name = "Cuha";
            felt.transform.SetParent(dressing, false);
            felt.transform.SetPositionAndRotation(Point(new Vector3(0f, 0.003f, 0f)), transform.rotation);
            felt.transform.localScale = new Vector3(transform.lossyScale.x - 0.12f, 0.006f, transform.lossyScale.z - 0.12f);
            Destroy(felt.GetComponent<Collider>());
            felt.GetComponent<Renderer>().material.color = new Color(0.05f, 0.28f, 0.15f);

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deste";
            deck.transform.SetParent(dressing, false);
            deck.transform.SetPositionAndRotation(Point(deckOffset + new Vector3(0f, 0.035f, 0f)), transform.rotation);
            deck.transform.localScale = new Vector3(0.26f, 0.06f, 0.36f);
            Destroy(deck.GetComponent<Collider>());
            deck.GetComponent<Renderer>().material.color = new Color(0.45f, 0.08f, 0.10f);

            Color gold = new Color(0.95f, 0.82f, 0.45f);
            dealerLabel = CardVisual.CreateText(dressing, "", Point(new Vector3(-1.05f, 0.008f, dealerRowZ)), 0.006f, gold, font);
            playerLabel = CardVisual.CreateText(dressing, "", Point(new Vector3(-1.05f, 0.008f, playerRowZ)), 0.006f, gold, font);
            dealerLabel.gameObject.SetActive(false);
            playerLabel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- Altyazı

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            if (game != null && !string.IsNullOrEmpty(message))
            {
                Rect strip = new Rect((width - 1000f) / 2f, ReferenceHeight - 170f, 1000f, 120f);
                GUI.DrawTexture(strip, subtitleTexture);
                GUI.Label(strip, message, subtitleStyle);
            }
            else if (game == null && !string.IsNullOrEmpty(prompt))
            {
                GUI.Box(new Rect((width - 380f) / 2f, ReferenceHeight - 140f, 380f, 56f), prompt, promptStyle);
            }
        }

        private void EnsureStyles()
        {
            if (subtitleStyle != null) return;

            subtitleTexture = new Texture2D(1, 1);
            subtitleTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            subtitleTexture.Apply();

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, wordWrap = true, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.88f, 0.65f) }
            };
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (subtitleTexture != null) Destroy(subtitleTexture);
        }
    }
}
