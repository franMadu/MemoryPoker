using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>Resultado inmutable: categoría, carta más alta y valores de desempate en orden.</summary>
    public sealed class PokerHandResult : IComparable<PokerHandResult>
    {
        public PokerHandRank Rank { get; }
        // El as vale 14 aquí, incluso en la escalera A-2-3-4-5.
        public int HighestCardValue { get; }
        // Compara primero el grupo que forma la jugada y después las cartas de desempate.
        // En A-2-3-4-5, la altura usada para comparar la escalera es 5.
        public IReadOnlyList<int> TieBreakValues { get; }
        public bool IsRoyalFlush => Rank == PokerHandRank.StraightFlush && TieBreakValues[0] == 14;
        public string DisplayName => GameLocalization.Text(IsRoyalFlush ? "hand.royal" : Rank switch
        {
            PokerHandRank.StraightFlush => "hand.straightFlush",
            PokerHandRank.FourOfAKind => "hand.four",
            PokerHandRank.FullHouse => "hand.full",
            PokerHandRank.Flush => "hand.flush",
            PokerHandRank.Straight => "hand.straight",
            PokerHandRank.ThreeOfAKind => "hand.three",
            PokerHandRank.TwoPair => "hand.twoPair",
            PokerHandRank.OnePair => "hand.pair",
            _ => "hand.high"
        });

        internal PokerHandResult(PokerHandRank rank, int highestCardValue, params int[] tieBreakValues)
        {
            Rank = rank;
            HighestCardValue = highestCardValue;
            TieBreakValues = Array.AsReadOnly((int[])tieBreakValues.Clone());
        }

        // Positivo: gana esta mano. Negativo: gana la otra. Cero: empate completo.
        public int CompareTo(PokerHandResult other)
        {
            if (ReferenceEquals(other, null)) return 1;

            int rankComparison = Rank.CompareTo(other.Rank);
            if (rankComparison != 0) return rankComparison;

            for (int i = 0; i < TieBreakValues.Count; i++)
            {
                int comparison = TieBreakValues[i].CompareTo(other.TieBreakValues[i]);
                if (comparison != 0) return comparison;
            }

            return 0;
        }
    }
}
