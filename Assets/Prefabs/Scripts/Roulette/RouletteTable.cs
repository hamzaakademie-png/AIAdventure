using System;
using System.Collections;
using AIAdventure.Core;
using AIAdventure.Level;
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

        [Header("Görsel ve ses")]
        [Tooltip("Üzerinde BaronBody olan Baron modeli.")]
        [SerializeField] private GameObject baronBodyObject;
        [SerializeField] private RouletteSounds sounds;
        [SerializeField] private Revolver revolver;
        [SerializeField] private float slamDelay = 0.6f;

        [Header("Kamera")]
        [SerializeField] private Vector3 seatOffset = new Vector3(0f, 1.5f, -1.7f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.5f, 1.0f);
        [SerializeField] private float cameraSpeed = 3f;

        private RouletteGame game;
        private IRouletteView view;
        private IOpenable door;
        private SeatCameraController seatCamera;
        private IBaronBody body;
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
            if (baronBodyObject != null) baronBodyObject.TryGetComponent(out body);
            if (revolver != null) revolver.SpinStarted += OnRevolverSpin;

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
            if (revolver != null) revolver.SpinStarted -= OnRevolverSpin;
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

        private void LateUpdate() => seatCamera?.Tick(cameraSpeed);

        // ---------------------------------------------------------------- Oturma

        private void SitDown()
        {
            seated = true;
            player.SetInputEnabled(false);
            view.SetPrompt(null);
            view.Open(game);
            seatCamera.Sit(TableTop + transform.rotation * seatOffset, TableTop + transform.rotation * lookOffset);

            game.Start();
            body?.SetNervous(false);
            baron.ResetConversation();
            baron.WarmUp();
            messagesLeft = messagesPerTurn;

            view.AddNarration("Baron Mortis eski tabancayı masanın ortasına bırakıyor. Altı hazne, tek kurşun.");
            StartCoroutine(BaronLoadsGun());
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

            // Beden dili: kızar, güler, gerilir ya da konuşur.
            body?.React(response.Intent);
            if (response.Intent == PlayerIntent.Insult && sounds != null)
                StartCoroutine(PlayDelayed(sounds.PlayTableSlam, slamDelay));

            // Model niyeti söyler, puanı oyun kuralları hesaplar.
            int delta = NerveRules.Delta(response.Intent, response.Impact);
            string effect = NerveRules.Describe(response.Intent, delta);
            if (effect != null) view.AddNarration(effect);

            game.ApplyNerveChange(delta);
            body?.SetNervous(game.HesitationChance > 0f);
            if (game.IsOver) return;

            NpcAction action = NerveRules.ValidateAction(response.Intent, response.Action);
            if (action != response.Action)
                Debug.Log($"[Baron] Tutarsız eylem reddedildi: {response.Action} ({response.Intent} niyetiyle uyuşmuyor)");

            switch (action)
            {
                case NpcAction.Spin:
                    game.SpinCylinder();
                    if (revolver != null) revolver.Spin();
                    else if (sounds != null) sounds.PlaySpin();
                    view.AddNarration("Baron silindiri çeviriyor. İhtimaller sıfırlandı.");
                    break;

                case NpcAction.PassTurn:
                    game.BaronPassesTurn();
                    view.AddNarration("Baron sırasını sana bıraktı: bir sonraki tetiği de sen çekeceksin.");
                    break;

                case NpcAction.AddBullet:
                    if (game.AddBullet())
                        view.AddNarration($"Baron cebinden bir kurşun daha çıkarıp silindire sürüyor. Artık {game.Bullets} kurşun var.");
                    break;

                case NpcAction.AcceptBaronFirst:
                    if (game.GiveTurnToBaron())
                    {
                        view.AddNarration("Anlaşma: Baron bu turda senden önce çekecek.");
                        messagesLeft = messagesPerTurn;
                        StartCoroutine(BaronTurn());
                    }
                    break;

                case NpcAction.AcceptDouble:
                    game.BaronAgreesToPullTwice();
                    view.AddNarration("Anlaşma: Baron sırası gelince art arda iki kez çekecek.");
                    break;

                case NpcAction.AcceptRemoveBullet:
                    view.AddNarration(game.RemoveBullet()
                        ? $"Anlaşma: Baron bir kurşunu çıkarıp masaya bırakıyor. Silindirde {game.Bullets} kurşun kaldı."
                        : "Silindirde çıkarılacak fazladan kurşun yok.");
                    break;

                case NpcAction.Surrender:
                    game.Surrender();
                    break;
            }
        }

        private string BuildContext()
        {
            string deals = game.BaronPullsTwiceNextTurn ? " Anlaşma gereği sıran gelince iki kez çekeceksin." : "";
            return $"Soğukkanlılığın: {game.Nerve}/100 (tetiği çekememe ihtimalin: %{Mathf.RoundToInt(game.HesitationChance * 100f)}). " +
                   $"Silindirde {game.Bullets} kurşun var. " +
                   $"Bu silindirde çekilen tetik: {game.PullsSinceSpin}/{RouletteGame.ChamberCount}. " +
                   $"Sıradaki tetikte patlama ihtimali: %{Mathf.RoundToInt(game.NextShotChance * 100f)}. " +
                   $"Sıra oyuncuda.{deals}";
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
            view.AddNarration(game.HesitationChance > 0f
                ? "Baron tabancayı alıyor. Elleri belirgin şekilde titriyor..."
                : "Baron tabancayı yavaşça şakağına götürüyor...");

            // Beden varsa silahı gerçekten alıp şakağına götürür; yoksa sadece bekler.
            if (body != null) yield return body.RaiseGunToTemple();
            else yield return new WaitForSeconds(baronPullDelay);

            // Titreyen el: soğukkanlılık düşükse Baron tetiği çekemeyebilir.
            if (game.RollBaronHesitation())
            {
                if (body != null) yield return body.LowerGun();
                baronActing = false;
                yield break;
            }

            bool shot = false;
            while (!game.IsOver && game.Turn == RouletteTurn.Baron)
            {
                shot = game.PullTrigger();
                if (shot || game.Turn != RouletteTurn.Baron) break;

                view.AddNarration("Anlaşma gereği Baron tetiği bir kez daha çekecek...");
                yield return new WaitForSeconds(baronPullDelay);
            }

            if (body != null)
            {
                if (shot) body.Die();
                else yield return body.LowerGun();
            }

            baronActing = false;
            UpdateHint();
        }

        private void OnTriggerPulled(RouletteTurn shooter, bool fired)
        {
            if (fired)
            {
                if (sounds != null) sounds.PlayGunshot();
                if (shooter == RouletteTurn.Player && revolver != null) revolver.Fire();
                view.FlashShot();
                return;
            }

            if (sounds != null) sounds.PlayClick();

            view.AddNarration(shooter == RouletteTurn.Player
                ? "Klik. Boş. Kalbin yerinden çıkacak gibi."
                : "Klik. Boş. Baron derin bir nefes veriyor.");
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

                case RouletteOutcome.BaronFlinched:
                    door?.Open();
                    view.AddNarration("Baron'un parmağı tetikte donup kalıyor. Çekemiyor. Tabanca elinden kayıp masaya düşüyor...");
                    view.AddNarration("Korkusu onu yendi. Arkadaki çıkış kapısı gıcırdayarak açıldı.");
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

        /// <summary>Oyun başında Baron mermiyi silaha koyar ve silindiri çevirir.</summary>
        private IEnumerator BaronLoadsGun()
        {
            baronActing = true;
            view.SetHint("Baron silahı hazırlıyor...");

            if (body != null)
            {
                view.AddNarration("Baron masadaki tek kurşunu alıp silindire yerleştiriyor...");
                yield return body.LoadGun();
            }

            baronActing = false;
            view.AddNarration("İlk tetik senin. Çekmeden önce onunla konuşabilir, sinirlerini bozabilir ya da pazarlık edebilirsin.");
            UpdateHint();
        }

        private void OnRevolverSpin()
        {
            if (sounds != null) sounds.PlaySpin();
        }

        private static IEnumerator PlayDelayed(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        private void UpdateHint()
        {
            if (game.IsOver) return;
            view.SetHint($"{PlayHint}     (Bu tur kalan konuşma: {messagesLeft})");
        }
    }
}
