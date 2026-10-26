using System;
using System.Collections;
using AIAdventure.Core;
using AIAdventure.LLM;
using AIAdventure.NPC;
using AIAdventure.Player;
using AIAdventure.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AIAdventure.Roulette
{
    /// <summary>
    /// Baron'un masası. Üç bağımsız parçayı birbirine bağlar:
    /// RouletteGame (kurallar), BaronNpc (dil modeli) ve IRouletteView (ekran).
    /// LLM'in JSON kararları (nerve_change, action) burada oyun durumuna uygulanır.
    /// Baron pes ederse ya da kurşun ona denk gelirse çıkış kapısı açılır.
    /// </summary>
    public class RouletteTable : MonoBehaviour
    {
        private const string PlayHint = "[Enter] Baron'a yaz     [Space] Tetiği çek";

        [SerializeField] private PlayerMovement player;
        [SerializeField] private BaronNpc baron;
        [SerializeField] private GameObject exitDoor;
        [SerializeField] private float interactRange = 2.6f;
        [SerializeField] private float baronPullDelay = 2.2f;
        [SerializeField] private int messagesPerTurn = 2;

        [Header("Kamera")]
        [SerializeField] private Vector3 seatOffset = new Vector3(0f, 1.5f, -1.7f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.5f, 1.0f);
        [SerializeField] private float cameraSpeed = 3f;

        private RouletteGame game;
        private IRouletteView view;
        private IOpenable door;
        private SeatCameraController seatCamera;
        private Collider tableCollider;
        private bool seated;
        private bool baronActing;
        private int messagesLeft;

        public void Configure(BaronNpc baronNpc, GameObject door)
        {
            baron = baronNpc;
            exitDoor = door;
        }

        private void Awake()
        {
            game = new RouletteGame();
            game.TriggerPulled += OnTriggerPulled;
            game.GameOver += OnGameOver;

            view = GetComponent<IRouletteView>();
            tableCollider = GetComponent<Collider>();
            if (exitDoor != null) door = exitDoor.GetComponent<IOpenable>();

            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            seatCamera = new SeatCameraController(follow);

            if (view == null) Debug.LogError("[RouletteTable] Aynı nesneye ImGuiRouletteView ekle.");
            if (player == null) Debug.LogError("[RouletteTable] Player alanına oyuncuyu sürükle.");
            if (baron == null) Debug.LogError("[RouletteTable] Baron alanına Baron'u sürükle.");
            if (door == null) Debug.LogError("[RouletteTable] Exit Door alanına çıkış kapısını sürükle.");
        }

        private void OnEnable()
        {
            if (view != null) view.MessageSubmitted += OnMessageSubmitted;
        }

        private void OnDisable()
        {
            if (view != null) view.MessageSubmitted -= OnMessageSubmitted;
        }

        private void OnDestroy()
        {
            game.TriggerPulled -= OnTriggerPulled;
            game.GameOver -= OnGameOver;
        }

        private Vector3 TableTop =>
            tableCollider != null
                ? new Vector3(transform.position.x, tableCollider.bounds.max.y, transform.position.z)
                : transform.position;

        // ---------------------------------------------------------------- Her kare

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || view == null || player == null) return;

            if (!seated)
            {
                Vector3 offset = player.transform.position - transform.position;
                offset.y = 0f;
                bool near = !game.IsOver && offset.magnitude <= interactRange;
                view.SetPrompt(near ? "[F] Baron'un masasına otur" : null);
                if (near && keyboard.fKey.wasPressedThisFrame) SitDown();
                return;
            }

            if (view.IsTyping) return;

            if (game.IsOver)
            {
                if (game.Outcome == RouletteOutcome.PlayerShot && keyboard.rKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                else if (game.Outcome != RouletteOutcome.PlayerShot && keyboard.escapeKey.wasPressedThisFrame)
                    StandUp();
                return;
            }

            bool canAct = game.Turn == RouletteTurn.Player && !baronActing && !baron.IsBusy;
            if (canAct && keyboard.spaceKey.wasPressedThisFrame) PlayerPull();
        }

        private void LateUpdate() => seatCamera.Tick(cameraSpeed);

        // ---------------------------------------------------------------- Oturma

        private void SitDown()
        {
            seated = true;
            player.SetInputEnabled(false);
            view.SetPrompt(null);
            view.Open(game);
            seatCamera.Sit(TableTop + transform.rotation * seatOffset, TableTop + transform.rotation * lookOffset);

            game.Start();
            baron.ResetConversation();
            baron.WarmUp();
            messagesLeft = messagesPerTurn;

            view.AddNarration("Baron Mortis eski tabancayı masanın ortasına bırakıyor. Altı hazne, tek kurşun.");
            view.AddNarration("İlk tetik senin. Çekmeden önce onunla konuşabilirsin.");
            UpdateHint();
        }

        private void StandUp()
        {
            seated = false;
            view.Close();
            seatCamera.Stand();
            player.SetInputEnabled(true);
        }

        // ---------------------------------------------------------------- Konuşma (LLM)

        private async void OnMessageSubmitted(string text)
        {
            try
            {
                if (!seated || game.IsOver || baron.IsBusy) return;

                if (game.Turn != RouletteTurn.Player || baronActing)
                {
                    view.SetHint("Baron'un sırası. Bekle.");
                    return;
                }
                if (messagesLeft <= 0)
                {
                    view.SetHint("Bu tur daha fazla konuşamazsın. [Space] Tetiği çek");
                    return;
                }

                messagesLeft--;
                view.AddChatLine("Sen", text);
                view.SetThinking(true);

                NpcTurnResult result = await baron.TalkAsync(text, BuildContext());

                view.SetThinking(false);
                if (!seated || game.IsOver) return;

                if (result.Success) ApplyBaronResponse(result.Response);
                else view.AddSystemLine(result.UserMessage);

                UpdateHint();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                view.SetThinking(false);
                view.AddSystemLine("Beklenmeyen bir hata oluştu, oyun devam ediyor.");
            }
        }

        /// <summary>LLM'in JSON kararlarını oyun durumuna uygular.</summary>
        private void ApplyBaronResponse(NpcResponse response)
        {
            view.AddChatLine("Baron", response.Reply);
            game.ApplyNerveChange(response.NerveChange);
            if (game.IsOver) return;

            switch (response.Action)
            {
                case NpcAction.Spin:
                    game.SpinCylinder();
                    view.AddNarration("Baron silindiri öfkeyle çeviriyor. İhtimaller sıfırlandı.");
                    break;
                case NpcAction.PassTurn:
                    game.BaronPassesTurn();
                    view.AddNarration("Baron sırasını sana bıraktı: bir sonraki tetiği de sen çekeceksin.");
                    break;
                case NpcAction.Surrender:
                    game.Surrender();
                    break;
            }
        }

        private string BuildContext()
        {
            return $"Baron'un soğukkanlılığı: {game.Nerve}/100. " +
                   $"Bu silindirde çekilen tetik: {game.PullsSinceSpin}/{RouletteGame.ChamberCount}. " +
                   $"Sıradaki tetikte mermi ihtimali: %{Mathf.RoundToInt(game.NextShotChance * 100f)}. " +
                   $"Sıra oyuncuda.";
        }

        // ---------------------------------------------------------------- Tetik

        private void PlayerPull()
        {
            messagesLeft = messagesPerTurn;
            bool fired = game.PullTrigger();
            if (fired) return;

            if (game.Turn == RouletteTurn.Baron) StartCoroutine(BaronTurn());
            else
            {
                view.AddNarration("Sıra yine sende.");
                UpdateHint();
            }
        }

        private IEnumerator BaronTurn()
        {
            baronActing = true;
            view.SetHint("Baron'un sırası...");
            view.AddNarration("Baron tabancayı yavaşça şakağına götürüyor...");

            yield return new WaitForSeconds(baronPullDelay);

            bool fired = game.PullTrigger();
            baronActing = false;
            if (!fired) UpdateHint();
        }

        private void OnTriggerPulled(RouletteTurn shooter, bool fired)
        {
            if (fired)
            {
                view.FlashShot();
                return;
            }

            view.AddNarration(shooter == RouletteTurn.Player
                ? "Klik. Boş. Kalbin yerinden çıkacak gibi."
                : "Klik. Boş. Baron derin bir nefes veriyor. Sıra sende.");
        }

        private void OnGameOver(RouletteOutcome outcome)
        {
            switch (outcome)
            {
                case RouletteOutcome.PlayerShot:
                    view.ShowEnding(false, "Kurşun sendeydi. Baron Mortis gülümseyerek tabancayı geri alıyor...");
                    break;

                case RouletteOutcome.BaronShot:
                    door?.Open();
                    view.AddNarration("Patlama mahzende yankılanıyor. Baron Mortis'in silueti duman gibi dağılıyor...");
                    view.AddNarration("Arkadaki çıkış kapısı gıcırdayarak açıldı.");
                    view.ShowEnding(true, null);
                    view.SetHint("[Esc] Masadan kalk");
                    break;

                case RouletteOutcome.BaronSurrendered:
                    door?.Open();
                    view.AddNarration("Baron'un elleri titriyor. Tabancayı masaya bırakıp başını eğiyor: pes etti.");
                    view.AddNarration("Arkadaki çıkış kapısı gıcırdayarak açıldı.");
                    view.ShowEnding(true, null);
                    view.SetHint("[Esc] Masadan kalk");
                    break;
            }
        }

        private void UpdateHint()
        {
            if (game.IsOver) return;
            view.SetHint($"{PlayHint}     (Bu tur kalan konuşma: {messagesLeft})");
        }
    }
}
