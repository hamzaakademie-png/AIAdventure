using System;
using System.Collections.Generic;

namespace AIAdventure.Blackjack
{
    /// <summary>
    /// 52 kartlık deste. Fisher-Yates algoritmasıyla karıştırılır.
    /// </summary>
    public class Deck
    {
        private readonly List<Card> cards = new List<Card>();
        private readonly Random random;

        public int Count => cards.Count;

        public Deck(Random random)
        {
            this.random = random;
            Reset();
        }

        public void Reset()
        {
            cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                for (int rank = 1; rank <= 13; rank++)
                    cards.Add(new Card(suit, rank));
            Shuffle();
        }

        public Card Draw()
        {
            if (cards.Count == 0) Reset();
            Card card = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            return card;
        }

        private void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
