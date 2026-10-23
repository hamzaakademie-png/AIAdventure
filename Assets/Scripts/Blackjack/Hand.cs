using System.Collections.Generic;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Bir oyuncunun elindeki kartlar ve elin değeri.
    /// Aslar önce 11 sayılır, el 21'i geçerse teker teker 1'e düşürülür.
    /// </summary>
    public class Hand
    {
        private readonly List<Card> cards = new List<Card>();

        public IReadOnlyList<Card> Cards => cards;

        public void Add(Card card) => cards.Add(card);
        public void Clear() => cards.Clear();

        public int Value
        {
            get
            {
                int total = 0;
                int aces = 0;
                foreach (Card card in cards)
                {
                    total += card.BaseValue;
                    if (card.IsAce) aces++;
                }
                while (total > 21 && aces > 0)
                {
                    total -= 10;
                    aces--;
                }
                return total;
            }
        }

        public bool IsBust => Value > 21;
        public bool IsBlackjack => cards.Count == 2 && Value == 21;
    }
}
