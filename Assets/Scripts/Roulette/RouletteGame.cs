using System;

namespace AIAdventure.Roulette
{
    public enum RouletteTurn { Player, Baron }
    public enum RouletteOutcome { None, PlayerShot, BaronShot, BaronSurrendered }

    /// <summary>
    /// Rus ruleti kuralları. Unity'den bağımsız saf C#.
    /// Merminin yeri rastgele ve gizlidir; dil modeli bunu asla bilmez.
    /// Baron'un soğukkanlılığı (0-100) sıfıra inerse Baron pes eder.
    /// </summary>
    public class RouletteGame
    {
        public const int ChamberCount = 6;
        public const int StartingNerve = 100;

        private readonly Random random;
        private int bulletChamber;
        private int currentChamber;

        public RouletteTurn Turn { get; private set; }
        public int Nerve { get; private set; }
        public int PullsSinceSpin { get; private set; }
        public int TotalPulls { get; private set; }
        public bool BaronSkipsNextTurn { get; private set; }
        public RouletteOutcome Outcome { get; private set; }
        public bool IsOver => Outcome != RouletteOutcome.None;

        /// <summary>Sıradaki tetikte merminin patlama ihtimali (0-1).</summary>
        public float NextShotChance => 1f / (ChamberCount - PullsSinceSpin);

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
            BaronSkipsNextTurn = false;
            SpinCylinder();
        }

        public void SpinCylinder()
        {
            bulletChamber = random.Next(ChamberCount);
            currentChamber = 0;
            PullsSinceSpin = 0;
        }

        /// <summary>Sırası gelen kişi tetiği çeker. Patladıysa true döner.</summary>
        public bool PullTrigger()
        {
            if (IsOver) return false;

            RouletteTurn shooter = Turn;
            bool fired = currentChamber == bulletChamber;
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

        /// <summary>Baron sırasını oyuncuya bırakır: oyuncu bir kez daha çeker.</summary>
        public void BaronPassesTurn()
        {
            if (!IsOver) BaronSkipsNextTurn = true;
        }

        public void ApplyNerveChange(int delta)
        {
            if (IsOver) return;
            Nerve = Math.Max(0, Math.Min(StartingNerve, Nerve + delta));
            if (Nerve == 0) Finish(RouletteOutcome.BaronSurrendered);
        }

        public void Surrender()
        {
            if (!IsOver) Finish(RouletteOutcome.BaronSurrendered);
        }

        private void AdvanceTurn()
        {
            if (Turn == RouletteTurn.Player && BaronSkipsNextTurn)
            {
                BaronSkipsNextTurn = false;
                return;
            }
            Turn = Turn == RouletteTurn.Player ? RouletteTurn.Baron : RouletteTurn.Player;
        }

        private void Finish(RouletteOutcome outcome)
        {
            Outcome = outcome;
            GameOver?.Invoke(outcome);
        }
    }
}
