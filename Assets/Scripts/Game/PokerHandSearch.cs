using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>Conserva una pareja de posiciones, sus cinco cartas y su valoración.</summary>
    public sealed class PokerHandCandidate
    {
        public int FirstBoardIndex { get; }
        public int SecondBoardIndex { get; }
        public IReadOnlyList<Card.Card> Cards { get; }
        public PokerHandResult Result { get; }

        internal PokerHandCandidate(int firstBoardIndex, int secondBoardIndex, Card.Card[] cards, PokerHandResult result)
        {
            FirstBoardIndex = firstBoardIndex;
            SecondBoardIndex = secondBoardIndex;
            Cards = Array.AsReadOnly((Card.Card[])cards.Clone());
            Result = result;
        }
    }

    /// <summary>Clasifica las parejas del tablero usando siempre las tres cartas comunitarias.</summary>
    public static class PokerHandSearch
    {
        // Cada mano usa las mismas tres comunitarias y dos posiciones distintas del tablero.
        // Las jugadas empatadas mantienen el orden del tablero para que el resultado sea estable.
        public static IReadOnlyList<PokerHandCandidate> FindBestHands(
            IReadOnlyList<Card.Card> board, IReadOnlyList<Card.Card> community, int limit = 3)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (community == null) throw new ArgumentNullException(nameof(community));
            if (board.Count < 2) throw new ArgumentException("At least two board cards are required.", nameof(board));
            if (community.Count != 3) throw new ArgumentException("Exactly three community cards are required.", nameof(community));
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));

            var best = new List<PokerHandCandidate>();
            for (int first = 0; first < board.Count - 1; first++)
            for (int second = first + 1; second < board.Count; second++)
            {
                var cards = new[] { board[first], board[second], community[0], community[1], community[2] };
                PokerHandResult result = PokerHandEvaluator.Evaluate(cards);
                int insertAt = 0;
                while (insertAt < best.Count && result.CompareTo(best[insertAt].Result) <= 0) insertAt++;
                if (insertAt >= limit) continue;

                best.Insert(insertAt, new PokerHandCandidate(first, second, cards, result));
                if (best.Count > limit) best.RemoveAt(best.Count - 1);
            }

            return best.AsReadOnly();
        }

        // Una mano equivalente cuenta aunque cambien sus palos o posiciones respecto al ejemplo.
        // Deben coincidir también todas las cartas de desempate, no solo la categoría de la jugada.
        public static int FindMatchingPlace(IReadOnlyList<PokerHandCandidate> bestHands, PokerHandResult playerHand)
        {
            if (bestHands == null) throw new ArgumentNullException(nameof(bestHands));
            if (playerHand == null) return 0;
            for (int i = 0; i < bestHands.Count; i++)
            {
                if (playerHand.CompareTo(bestHands[i].Result) == 0) return i + 1;
            }

            return 0;
        }
    }
}
