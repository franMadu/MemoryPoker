using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>Evalúa manos clásicas de cinco cartas sin depender de la escena ni alterar las cartas.</summary>
    public static class PokerHandEvaluator
    {
        /// <summary>
        /// Valora cinco cartas distintas, con valores de 1 (as) a 13 (rey).
        /// Los palos no desempatan y la evaluación no modifica las cartas ni la lista de entrada.
        /// </summary>
        public static PokerHandResult Evaluate(IReadOnlyList<Card.Card> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (cards.Count != 5)
                throw new ArgumentException("A poker hand must contain exactly five cards.", nameof(cards));

            var counts = new int[15];
            var seen = new bool[4, 15];
            bool flush = true;
            int firstSuit = -1;
            int highest = 0;
            int distinct = 0;

            for (int i = 0; i < cards.Count; i++)
            {
                Card.Card card = cards[i];
                if (card == null)
                    throw new ArgumentException("A poker hand cannot contain null cards.", nameof(cards));

                int suit = (int)card.Suit;
                if (suit < 0 || suit > 3 || card.Value < 1 || card.Value > 13)
                    throw new ArgumentException("Invalid card suit or value.", nameof(cards));

                int value = card.Value == 1 ? 14 : card.Value;
                // El as compara como 14; la escalera A-2-3-4-5 se trata aparte más abajo.
                if (seen[suit, value])
                    throw new ArgumentException("A poker hand cannot contain duplicate cards.", nameof(cards));
                seen[suit, value] = true;
                if (counts[value]++ == 0) distinct++;
                highest = Math.Max(highest, value);
                if (i == 0) firstSuit = suit;
                else if (suit != firstSuit) flush = false;
            }

            int straightHigh = 0;
            if (distinct == 5)
            {
                if (counts[14] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1)
                    straightHigh = 5;
                else if (highest >= 6 && counts[highest - 1] == 1 && counts[highest - 2] == 1 &&
                         counts[highest - 3] == 1 && counts[highest - 4] == 1)
                    straightHigh = highest;
            }

            // Recorre de mayor a menor para dejar parejas y cartas sueltas en orden de desempate.
            var pairs = new List<int>(2);
            var singles = new List<int>(5);
            int three = 0;
            int four = 0;
            for (int value = 14; value >= 2; value--)
            {
                switch (counts[value])
                {
                    case 4: four = value; break;
                    case 3: three = value; break;
                    case 2: pairs.Add(value); break;
                    case 1: singles.Add(value); break;
                }
            }

            // Se comprueban primero las categorías más fuertes, incluidas las combinaciones de grupos.
            if (flush && straightHigh != 0)
                return new PokerHandResult(PokerHandRank.StraightFlush, highest, straightHigh);
            if (four != 0)
                return new PokerHandResult(PokerHandRank.FourOfAKind, highest, four, singles[0]);
            if (three != 0 && pairs.Count == 1)
                return new PokerHandResult(PokerHandRank.FullHouse, highest, three, pairs[0]);
            if (flush)
                return new PokerHandResult(PokerHandRank.Flush, highest, singles.ToArray());
            if (straightHigh != 0)
                return new PokerHandResult(PokerHandRank.Straight, highest, straightHigh);
            if (three != 0)
                return new PokerHandResult(PokerHandRank.ThreeOfAKind, highest, three, singles[0], singles[1]);
            if (pairs.Count == 2)
                return new PokerHandResult(PokerHandRank.TwoPair, highest, pairs[0], pairs[1], singles[0]);
            if (pairs.Count == 1)
                return new PokerHandResult(PokerHandRank.OnePair, highest, pairs[0], singles[0], singles[1], singles[2]);

            return new PokerHandResult(PokerHandRank.HighCard, highest, singles.ToArray());
        }
    }
}
