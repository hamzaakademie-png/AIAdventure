using System.Collections;
using AIAdventure.Core;
using AIAdventure.Player;
using AIAdventure.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Kâtip'in masası: oyuncu yaklaşıp F'ye basınca oturur, tuşlarla oynar.
    /// Kurallar BlackjackGame'de, çizim IBlackjackView'da; bu sınıf sadece
    /// ikisini birbirine bağlar. Oyuncu kazanınca ödül kapısını IOpenable ile açar.
    /// </summary>
    public class BlackjackTable : MonoBehaviour
    {
        private const string PlayHint = "[H] Kart çek     [S] Dur";
        private const string AfterRoundHint = "\n[Enter] Tekrar oyna     [Esc] Masadan kalk";

        [SerializeField] private PlayerMovement player;
        [SerializeField] private GameObject rewardDoor;
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private float dealerDelay = 0.8f;

        private BlackjackGame game;
        private IBlackjackView view;
        private IOpenable door;
        private bool seated;
        private bool doorOpened;
        private Coroutine dealerRoutine;

        private void Awake()
        {
            game = new BlackjackGame();
            game.RoundFinished += OnRoundFinished;

            view = GetComponent<IBlackjackView>();
            if (view == null)
                Debug.LogError("[BlackjackTable] Aynı nesneye ImGuiBlackjackView bileşenini ekle.");

            if (rewardDoor != null)
                door = rewardDoor.GetComponent<IOpenable>();
            if (door == null)
                Debug.LogError("[BlackjackTable] Reward Door alanına avlu kapısını sürükle.");
            if (player == null)
                Debug.LogError("[BlackjackTable] Player alanına oyuncuyu sürükle.");
        }

        private void OnDestroy()
        {
            if (game != null) game.RoundFinished -= OnRoundFinished;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || view == null || player == null) return;

            if (!seated)
            {
                bool near = !doorOpened &&
                            Vector3.Distance(player.transform.position, transform.position) <= interactRange;
                view.SetPrompt(near ? "[F] Kâtip'in masasına otur" : null);
                if (near && keyboard.fKey.wasPressedThisFrame) SitDown();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame && game.Phase != BlackjackPhase.DealerTurn)
            {
                StandUp();
                return;
            }

            switch (game.Phase)
            {
                case BlackjackPhase.PlayerTurn:
                    if (keyboard.hKey.wasPressedThisFrame) game.Hit();
                    else if (keyboard.sKey.wasPressedThisFrame) game.Stand();

                    if (game.Phase == BlackjackPhase.DealerTurn && dealerRoutine == null)
                    {
                        view.SetMessage("Kâtip kapalı kartını çeviriyor...");
                        dealerRoutine = StartCoroutine(PlayDealer());
                    }
                    break;

                case BlackjackPhase.Finished:
                    if (!doorOpened && (keyboard.enterKey.wasPressedThisFrame ||
                                        keyboard.numpadEnterKey.wasPressedThisFrame))
                        NewRound();
                    break;
            }
        }

        private void SitDown()
        {
            seated = true;
            view.SetPrompt(null);
            player.SetInputEnabled(false);
            view.Open(game);
            NewRound();
        }

        private void StandUp()
        {
            if (dealerRoutine != null) StopCoroutine(dealerRoutine);
            dealerRoutine = null;
            seated = false;
            view.Close();
            player.SetInputEnabled(true);
        }

        private void NewRound()
        {
            view.SetMessage("Kâtip sessizce kartları dağıttı.\n" + PlayHint);
            game.StartRound();
            Debug.Log($"[Blackjack] Yeni el. Oyuncu: {game.PlayerHand.Value}");
        }

        private IEnumerator PlayDealer()
        {
            yield return new WaitForSeconds(dealerDelay);
            while (game.DealerStep())
                yield return new WaitForSeconds(dealerDelay);
            dealerRoutine = null;
        }

        private void OnRoundFinished(BlackjackResult result)
        {
            Debug.Log($"[Blackjack] Sonuç: {result} (Oyuncu {game.PlayerHand.Value} - Kâtip {game.DealerHand.Value})");

            switch (result)
            {
                case BlackjackResult.PlayerWins:
                    doorOpened = true;
                    door?.Open();
                    view.SetMessage(game.DealerHand.IsBust
                        ? "Kâtip 21'i geçti! Başını eğiyor... Arkadaki kapı gıcırdayarak açıldı.\n[Esc] Masadan kalk"
                        : "Kazandın! Kâtip başını eğiyor... Arkadaki kapı gıcırdayarak açıldı.\n[Esc] Masadan kalk");
                    break;

                case BlackjackResult.DealerWins:
                    view.SetMessage((game.PlayerHand.IsBust
                        ? "21'i geçtin. Kâtip kartlarını sessizce topluyor."
                        : "Kâtip kazandı. Masada soğuk bir sessizlik var.") + AfterRoundHint);
                    break;

                default:
                    view.SetMessage("Berabere. Kâtip yeniden karıştırıyor." + AfterRoundHint);
                    break;
            }
        }
    }
}
