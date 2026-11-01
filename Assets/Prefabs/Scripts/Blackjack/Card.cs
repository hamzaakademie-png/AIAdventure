using System;

namespace AIAdventure.Blackjack
{
    public enum Suit { Spades, Hearts, Diamonds, Clubs }

    /// <summary>
    /// Tek bir oyun kartı. Rank: 1 = As, 2-10, 11 = J, 12 = Q, 13 = K.
    /// </summary>
    public readonly struct Card
    {
        public Suit Suit { get; }
        public int Rank { get; }

        public Card(Suit suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public bool IsAce => Rank == 1;
        public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;

        /// <summary>As 11 sayılır; gerekirse Hand onu 1'e düşürür.</summary>
        public int BaseValue => IsAce ? 11 : Math.Min(Rank, 10);

        public string RankText => Rank switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => Rank.ToString()
        };

        public string SuitSymbol => Suit switch
        {
            Suit.Spades => "♠",
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            _ => "♣"
        };

        public override string ToString() => RankText + SuitSymbol;
    }
}
