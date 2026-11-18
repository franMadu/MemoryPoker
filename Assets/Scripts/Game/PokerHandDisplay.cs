using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>Agrupa las cartas para mostrar la jugada sin modificar la selección original.</summary>
    public static class PokerHandDisplay
    {
        // Devuelve una copia visual: primero los grupos, después las cartas de desempate de mayor a menor.
        // Las escaleras se muestran de mayor a menor; en 5-4-3-2-A el as queda al final.
        public static IReadOnlyList<Card.Card> OrderCards(IReadOnlyList<Card.Card> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            var ordered = new Card.Card[cards.Count];
            var counts = new int[15];
            for (int i = 0; i < cards.Count; i++)
            {
                Card.Card card = cards[i];
                ordered[i] = card;
                if (card == null) continue;
                if (card.Value < 1 || card.Value > 13)
                    throw new ArgumentException("Invalid card value.", nameof(cards));
                counts[HighValue(card)]++;
            }

            bool wheel = cards.Count == 5 && counts[14] == 1 && counts[5] == 1 &&
                counts[4] == 1 && counts[3] == 1 && counts[2] == 1;

            Array.Sort(ordered, (left, right) =>
            {
                if (left == null) return right == null ? 0 : 1;
                if (right == null) return -1;

                int leftValue = HighValue(left);
                int rightValue = HighValue(right);
                int groups = counts[rightValue].CompareTo(counts[leftValue]);
                if (groups != 0) return groups;

                if (wheel && leftValue == 14) leftValue = 1;
                if (wheel && rightValue == 14) rightValue = 1;
                int values = rightValue.CompareTo(leftValue);
                // El palo solo estabiliza el orden visual y nunca cambia la fuerza de la mano.
                return values != 0 ? values : left.Suit.CompareTo(right.Suit);
            });

            return Array.AsReadOnly(ordered);
        }

        private static int HighValue(Card.Card card) => card.Value == 1 ? 14 : card.Value;
    }
}
