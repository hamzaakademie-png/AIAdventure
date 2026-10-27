using System;

namespace AIAdventure.Roulette
{
    public enum RouletteTurn { Player, Baron }
    public enum RouletteOutcome { None, PlayerShot, BaronShot, BaronSurrendered, BaronFlinched }

    /// <summary>
    /// Rus ruleti kuralları. Unity'den bağımsız saf C#.
    /// - Kurşunların yeri rastgele ve gizlidir; dil modeli bunu asla bilmez.
    /// - Baron'un soğukkanlılığı 50'nin altına düşünce kendi sırasında tetiği
    ///   çekememe ihtimali doğar (titreyen el). Çekemezse kaybeder.
    /// - Pazarlıklar (önce sen çek, iki kez çek, kurşun çıkar) kuralları değiştirir.
    /// </summary>
    public class RouletteGame
    {
        public const int ChamberCount = 6;
        public const int StartingNerve = 80;
        public const int MaxNerve = 100;
        public const int HesitationThreshold = 50;
        public const int MaxBullets = 3;

        private readonly Random random;
        private readonly bool[] chambers = new bool[ChamberCount];
        private int currentChamber;
        private int baronExtraPulls;

        public RouletteTurn Turn { get; private set; }
        public int Nerve { get; private set; }
        public int Bullets { get; private set; }
        public int PullsSinceSpin { get; private set; }
        public int TotalPulls { get; private set; }
        public bool BaronSkipsNextTurn { get; private set; }
        public bool BaronPullsTwiceNextTurn { get; private set; }
        public RouletteOutcome Outcome { get; private set; }
        public bool IsOver => Outcome != RouletteOutcome.None;

        /// <summary>Sıradaki tetikte patlama ihtimali (0-1).</summary>
        public float NextShotChance => Math.Min(1f, Bullets / (float)(ChamberCount - PullsSinceSpin));

        /// <summary>Baron'un kendi sırasında tetiği çekememe ihtimali (0-0.9).</summary>
        public float HesitationChance => Math.Max(0, Math.Min(90, (HesitationThreshold - Nerve) * 2)) / 100f;

        /// <summary>(tetiği çeken, patladı mı)</summary>
        public event Action<RouletteTurn, bool> TriggerPulled;
        public event Action<RouletteOutcome> GameOver;

        public RouletteGame(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void Start()
        {
            Nerve = StartingNerve;
            Outcome = RouletteOutcome.None;
            Turn = RouletteTurn.Player;
            TotalPulls = 0;
            Bullets = 1;
            BaronSkipsNextTurn = false;
            BaronPullsTwiceNextTurn = false;
            baronExtraPulls = 0;
            SpinCylinder();
        }

        /// <summary>Silindiri çevirir: kurşunlar yeniden rastgele haznelere dağılır.</summary>
        public void SpinCylinder()
        {
            Array.Clear(chambers, 0, ChamberCount);
            for (int placed = 0; placed < Bullets;)
            {
                int index = random.Next(ChamberCount);
                if (chambers[index]) continue;
                chambers[index] = true;
                placed++;
            }
            currentChamber = 0;
            PullsSinceSpin = 0;
        }

        /// <summary>Henüz çekilmemiş boş bir hazneye kurşun ekler.</summary>
        public bool AddBullet()
        {
            if (IsOver || Bullets >= MaxBullets) return false;
            int index = RandomRemainingChamber(loaded: false);
            if (index < 0) return false;
            chambers[index] = true;
            Bullets++;
            return true;
        }

        /// <summary>Fazladan bir kurşunu çıkarır. Son kurşun çıkarılamaz.</summary>
        public bool RemoveBullet()
        {
            if (IsOver || Bullets <= 1) return false;
            int index = RandomRemainingChamber(loaded: true);
            if (index < 0) return false;
            chambers[index] = false;
            Bullets--;
            return true;
        }

        /// <summary>Sırası gelen kişi tetiği çeker. Patladıysa true döner.</summary>
        public bool PullTrigger()
        {
            if (IsOver) return false;

            RouletteTurn shooter = Turn;
            bool fired = chambers[currentChamber];
            currentChamber = (currentChamber + 1) % ChamberCount;
            PullsSinceSpin++;
            TotalPulls++;

            TriggerPulled?.Invoke(shooter, fired);

            if (fired)
            {
                Finish(shooter == RouletteTurn.Player ? RouletteOutcome.PlayerShot : RouletteOutcome.BaronShot);
                return true;
            }

            AdvanceTurn();
            return false;
        }

        /// <summary>
        /// Baron sırası gelince çağrılır. Titreyen eli tetiği çekmesine engel
        /// olursa Baron kaybeder ve true döner.
        /// </summary>
        public bool RollBaronHesitation()
        {
            if (IsOver || Turn != RouletteTurn.Baron) return false;
            if (random.NextDouble() >= HesitationChance) return false;

            Finish(RouletteOutcome.BaronFlinched);
            return true;
        }

        // ---------------------------------------------------------------- LLM kararları ve pazarlıklar

        public void ApplyNerveChange(int delta)
        {
            if (IsOver) return;
            Nerve = Math.Max(0, Math.Min(MaxNerve, Nerve + delta));
            if (Nerve == 0) Finish(RouletteOutcome.BaronSurrendered);
        }

        /// <summary>Kızgın Baron sırasını oyuncuya bırakır: oyuncu bir kez daha çeker.</summary>
        public void BaronPassesTurn()
        {
            if (!IsOver) BaronSkipsNextTurn = true;
        }

        /// <summary>Pazarlık: "Önce sen çek". Oyuncunun sırası Baron'a geçer.</summary>
        public bool GiveTurnToBaron()
        {
            if (IsOver || Turn != RouletteTurn.Player) return false;
            Turn = RouletteTurn.Baron;
            BaronSkipsNextTurn = false;
            return true;
        }

        /// <summary>Pazarlık: "İki kez çek". Baron sırası gelince art arda iki kez çeker.</summary>
        public void BaronAgreesToPullTwice()
        {
            if (!IsOver) BaronPullsTwiceNextTurn = true;
        }

        public void Surrender()
        {
            if (!IsOver) Finish(RouletteOutcome.BaronSurrendered);
        }

        // ---------------------------------------------------------------- İç işleyiş

        private void AdvanceTurn()
        {
            if (Turn == RouletteTurn.Baron)
            {
                if (baronExtraPulls > 0)
                {
                    baronExtraPulls--;
                    return;
                }
                Turn = RouletteTurn.Player;
                return;
            }

            if (BaronSkipsNextTurn)
            {
                BaronSkipsNextTurn = false;
                return;
            }

            Turn = RouletteTurn.Baron;
            if (BaronPullsTwiceNextTurn)
            {
                BaronPullsTwiceNextTurn = false;
                baronExtraPulls = 1;
            }
        }

        private int RandomRemainingChamber(bool loaded)
        {
            int remaining = ChamberCount - PullsSinceSpin;
            int start = random.Next(remaining);
            for (int i = 0; i < remaining; i++)
            {
                int index = (currentChamber + (start + i) % remaining) % ChamberCount;
                if (chambers[index] == loaded) return index;
            }
            return -1;
        }

        private void Finish(RouletteOutcome outcome)
        {
            Outcome = outcome;
            GameOver?.Invoke(outcome);
        }
    }
}
