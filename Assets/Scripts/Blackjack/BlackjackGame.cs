using System;

namespace AIAdventure.Blackjack
{
    public enum BlackjackPhase { Idle, PlayerTurn, DealerTurn, Finished }
    public enum BlackjackResult { None, PlayerWins, DealerWins, Push }

    /// <summary>
    /// Blackjack kuralları. Unity'den tamamen bağımsızdır (saf C#):
    /// ekranı, tuşları veya kapıyı bilmez. Bu sayede ayrı test edilebilir.
    /// Krupiye 17 ve üstünde durur, altında kart çeker.
    /// </summary>
    public class BlackjackGame
    {
        public const int DealerStandsOn = 17;

        private readonly Random random;
        private Deck deck;

        public Hand PlayerHand { get; } = new Hand();
        public Hand DealerHand { get; } = new Hand();
        public BlackjackPhase Phase { get; private set; } = BlackjackPhase.Idle;
        public BlackjackResult Result { get; private set; } = BlackjackResult.None;

        /// <summary>Oyuncu oynarken krupiyenin ikinci kartı kapalıdır.</summary>
        public bool DealerHoleCardHidden => Phase == BlackjackPhase.PlayerTurn;

        public event Action<BlackjackResult> RoundFinished;

        public BlackjackGame(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void StartRound()
        {
            deck = new Deck(random);
            PlayerHand.Clear();
            DealerHand.Clear();
            Result = BlackjackResult.None;

            PlayerHand.Add(deck.Draw());
            DealerHand.Add(deck.Draw());
            PlayerHand.Add(deck.Draw());
            DealerHand.Add(deck.Draw());

            Phase = BlackjackPhase.PlayerTurn;

            if (PlayerHand.IsBlackjack || DealerHand.IsBlackjack)
                Finish(Compare());
        }

        public void Hit()
        {
            if (Phase != BlackjackPhase.PlayerTurn) return;

            PlayerHand.Add(deck.Draw());

            if (PlayerHand.IsBust)
                Finish(BlackjackResult.DealerWins);
            else if (PlayerHand.Value == 21)
                Stand();
        }

        public void Stand()
        {
            if (Phase != BlackjackPhase.PlayerTurn) return;
            Phase = BlackjackPhase.DealerTurn;
        }

        /// <summary>
        /// Krupiyenin bir hamlesi. Kart çektiyse ve oyun sürüyorsa true döner.
        /// Ekran tarafı bunu aralıklarla çağırarak kartların tek tek açılmasını gösterir.
        /// </summary>
        public bool DealerStep()
        {
            if (Phase != BlackjackPhase.DealerTurn) return false;

            if (DealerHand.Value < DealerStandsOn)
            {
                DealerHand.Add(deck.Draw());
                if (DealerHand.IsBust)
                {
                    Finish(BlackjackResult.PlayerWins);
                    return false;
                }
                return true;
            }

            Finish(Compare());
            return false;
        }

        private BlackjackResult Compare()
        {
            bool playerBlackjack = PlayerHand.IsBlackjack;
            bool dealerBlackjack = DealerHand.IsBlackjack;

            if (playerBlackjack && dealerBlackjack) return BlackjackResult.Push;
            if (playerBlackjack) return BlackjackResult.PlayerWins;
            if (dealerBlackjack) return BlackjackResult.DealerWins;

            int player = PlayerHand.Value;
            int dealer = DealerHand.Value;
            if (player > dealer) return BlackjackResult.PlayerWins;
            if (dealer > player) return BlackjackResult.DealerWins;
            return BlackjackResult.Push;
        }

        private void Finish(BlackjackResult result)
        {
            Result = result;
            Phase = BlackjackPhase.Finished;
            RoundFinished?.Invoke(result);
        }
    }
}
