using System;
using System.Collections.Generic;
using System.Reflection;
using Game;

public static class PokerHandEvaluatorTests
{
    private static int assertions;

    private static Card.Card Card(int value, int suit)
    {
        var card = new Card.Card();
        typeof(Card.Card).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, value);
        typeof(Card.Card).GetField("suit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, (SuitEnum)suit);
        return card;
    }

    // S/H/D/C; A=1, J=11, Q=12, K=13.
    private static Card.Card[] Cards(string hand)
    {
        string[] tokens = hand.Split(' ');
        var cards = new Card.Card[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
            cards[i] = Card(int.Parse(tokens[i].Substring(0, tokens[i].Length - 1)), "SHDC".IndexOf(tokens[i][tokens[i].Length - 1]));
        return cards;
    }

    private static PokerHandResult Hand(string hand) => PokerHandEvaluator.Evaluate(Cards(hand));

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void Wins(string winner, string loser)
    {
        var a = Hand(winner);
        var b = Hand(loser);
        Check(a.CompareTo(b) > 0 && b.CompareTo(a) < 0, "Incorrect comparison: " + winner + " / " + loser);
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { assertions++; return; }
        throw new Exception("Invalid input accepted.");
    }

    private static void CheckBestHands()
    {
        var board = Cards("10S 11S 9S 8S 7S 6S 5S 4S 3S 2S 1H 13H 12H 11H 10H 9H");
        var community = Cards("1S 13S 12S");
        var best = PokerHandSearch.FindBestHands(board, community);
        Check(best.Count == 3, "Expected three best combinations.");
        Check(best[0].Result.IsRoyalFlush && best[0].FirstBoardIndex == 0 && best[0].SecondBoardIndex == 1, "Royal flush should be best.");
        Check(best[1].FirstBoardIndex == 1 && best[1].SecondBoardIndex == 2, "Wrong second-best flush.");
        Check(best[2].FirstBoardIndex == 1 && best[2].SecondBoardIndex == 3, "Wrong third-best flush.");
        for (int i = 0; i < best.Count; i++)
        {
            Check(best[i].Cards.Count == 5 && best[i].FirstBoardIndex != best[i].SecondBoardIndex, "Invalid candidate cards.");
            for (int c = 0; c < community.Length; c++)
                Check(ReferenceEquals(best[i].Cards[c + 2], community[c]), "Community cards changed.");
            Check(PokerHandSearch.FindMatchingPlace(best, PokerHandEvaluator.Evaluate(best[i].Cards)) == i + 1, "Incorrect matching place.");
        }
        Check(PokerHandSearch.FindMatchingPlace(best, Hand("11S 7S 1S 13S 12S")) == 0, "Same category with weaker kickers should not count.");
        Check(PokerHandSearch.FindMatchingPlace(best, null) == 0, "Missing player hand should not match.");

        // Compare the bounded search with all 120 combinations sorted independently.
        var all = new List<PokerHandResult>();
        for (int first = 0; first < board.Length - 1; first++)
        for (int second = first + 1; second < board.Length; second++)
            all.Add(PokerHandEvaluator.Evaluate(new[] { board[first], board[second], community[0], community[1], community[2] }));
        all.Sort((a, b) => b.CompareTo(a));
        var everyCandidate = PokerHandSearch.FindBestHands(board, community, 200);
        Check(everyCandidate.Count == 120, "Expected exactly 120 board pairs.");
        var positions = new HashSet<string>();
        for (int i = 0; i < everyCandidate.Count; i++)
        {
            Check(everyCandidate[i].Result.CompareTo(all[i]) == 0, "Search ranking disagrees with full sorting.");
            Check(positions.Add(everyCandidate[i].FirstBoardIndex + ":" + everyCandidate[i].SecondBoardIndex), "Duplicate position pair.");
        }

        var tieBoard = Cards("1S 1H 1D 1C 13S 13H 13D 13C");
        var tieCommunity = Cards("8S 8H 8D");
        var tied = PokerHandSearch.FindBestHands(tieBoard, tieCommunity);
        Check(tied[0].FirstBoardIndex == 0 && tied[0].SecondBoardIndex == 1 && tied[1].SecondBoardIndex == 2 && tied[2].SecondBoardIndex == 3, "Ties must use deterministic board order.");
        Check(PokerHandSearch.FindMatchingPlace(tied, Hand("1H 1D 8S 8H 8D")) == 1, "An equivalent pair outside the logged top three should count as first place.");
        var completeRanking = PokerHandSearch.FindBestHands(tieBoard, tieCommunity, 28);
        Check(completeRanking.Count == 28, "Full ranking must include every pair.");
        Check(PokerHandSearch.FindMatchingPlace(completeRanking, Hand("1D 1C 8S 8H 8D")) == 1, "Tied aces must share first place.");
        Check(PokerHandSearch.FindMatchingPlace(completeRanking, Hand("13S 13H 8S 8H 8D")) == 7, "Six stronger ace pairs should put kings in seventh place.");
        Check(PokerHandSearch.FindMatchingPlace(completeRanking, Hand("1S 13H 8S 8H 8D")) == 13, "Twelve full houses should put mixed pairs in thirteenth place.");
        var smallRanking = PokerHandSearch.FindBestHands(Cards("1H 13H 12H 11H"), Cards("2S 5H 9D"), 6);
        Check(PokerHandSearch.FindMatchingPlace(smallRanking, Hand("13H 12H 2S 5H 9D")) == 4, "A hand outside the visible top three still needs its actual place.");
        Check(PokerHandSearch.FindMatchingPlace(smallRanking, Hand("12H 11H 2S 5H 9D")) == 6, "Last place must be reported correctly.");
        Check(PokerHandSearch.FindMatchingPlace(smallRanking, null) == 0, "Incomplete hands have no ranking.");
        Check(PokerHandSearch.FindBestHands(Cards("10S 11S"), community).Count == 1, "A two-card board has one possible hand.");
        Check(PokerHandSearch.FindBestHands(board, community, 1).Count == 1, "Limit ignored.");
        Array.Reverse(community);
        var reordered = PokerHandSearch.FindBestHands(board, community);
        for (int i = 0; i < best.Count; i++) Check(best[i].Result.CompareTo(reordered[i].Result) == 0, "Community order affects ranking.");
        Reject(() => PokerHandSearch.FindBestHands(null, community));
        Reject(() => PokerHandSearch.FindBestHands(board, null));
        Reject(() => PokerHandSearch.FindBestHands(new Card.Card[1], community));
        Reject(() => PokerHandSearch.FindBestHands(board, new Card.Card[2]));
        Reject(() => PokerHandSearch.FindBestHands(board, community, 0));
        Reject(() => PokerHandSearch.FindBestHands(Cards("10S 10S"), community));
        Reject(() => PokerHandSearch.FindBestHands(Cards("1S 11S"), community));
        Reject(() => PokerHandSearch.FindBestHands(new Card.Card[2], community));
    }

    private static void CheckCardDisplayOrder()
    {
        string[] inputs = {
            "1S 3H 13D 6C 11S", "1S 4H 13D 4C 9S", "1S 4H 9D 4C 9S",
            "1S 2H 13D 2C 2S", "5S 1H 3D 4C 2S", "11S 13H 10D 1C 12S",
            "6S 1S 3S 11S 9S", "13S 2H 13D 2C 2S", "1S 2H 2D 2C 2S", "3S 6S 2S 5S 4S"
        };
        int[][] expected = {
            new[] { 1, 13, 11, 6, 3 }, new[] { 4, 4, 1, 13, 9 }, new[] { 9, 9, 4, 4, 1 },
            new[] { 2, 2, 2, 1, 13 }, new[] { 5, 4, 3, 2, 1 }, new[] { 1, 13, 12, 11, 10 },
            new[] { 1, 11, 9, 6, 3 }, new[] { 2, 2, 2, 13, 13 }, new[] { 2, 2, 2, 2, 1 }, new[] { 6, 5, 4, 3, 2 }
        };
        for (int i = 0; i < inputs.Length; i++)
        {
            var cards = Cards(inputs[i]);
            var original = (Card.Card[])cards.Clone();
            var strength = PokerHandEvaluator.Evaluate(cards);
            var ordered = PokerHandDisplay.OrderCards(cards);
            for (int c = 0; c < cards.Length; c++)
            {
                Check(ordered[c].Value == expected[i][c], "Incorrect display order: " + inputs[i]);
                Check(ReferenceEquals(cards[c], original[c]), "Display sorting mutated input order.");
                Check(Array.IndexOf(original, ordered[c]) >= 0, "Display sorting introduced a different card.");
            }
            Check(strength.CompareTo(PokerHandEvaluator.Evaluate(ordered)) == 0, "Display sorting changed hand strength.");
            Array.Reverse(cards);
            var reversed = PokerHandDisplay.OrderCards(cards);
            for (int c = 0; c < cards.Length; c++) Check(ReferenceEquals(ordered[c], reversed[c]), "Visual order is not deterministic.");
        }
        var partial = new[] { null, Card(8, 1), Card(1, 0), null, Card(8, 2) };
        var partialOrder = PokerHandDisplay.OrderCards(partial);
        Check(partialOrder[0].Value == 8 && partialOrder[1].Value == 8 && partialOrder[2].Value == 1 && partialOrder[3] == null && partialOrder[4] == null, "Incomplete hands should group available cards before empty slots.");
        Check(PokerHandDisplay.OrderCards(new Card.Card[0]).Count == 0, "Empty hand display failed.");
        Reject(() => PokerHandDisplay.OrderCards(null));
    }

    public static void Main()
    {
        string[] categories = {
            "1S 11H 9D 6C 3S", "8S 8H 1D 6C 3S", "8S 8H 3D 3C 1S",
            "8S 8H 8D 6C 3S", "6S 5H 4D 3C 2S", "1S 11S 9S 6S 3S",
            "8S 8H 8D 3C 3S", "8S 8H 8D 8C 3S", "9S 8S 7S 6S 5S"
        };
        for (int i = 0; i < categories.Length; i++)
        {
            Check((int)Hand(categories[i]).Rank == i, "Wrong category " + i);
            if (i > 0) Wins(categories[i], categories[i - 1]);
        }

        Check(Hand("1S 13S 12S 11S 10S").IsRoyalFlush, "Royal flush missing.");
        Check(!Hand(categories[8]).IsRoyalFlush, "False royal flush.");
        Check(Hand("1S 2H 3D 4C 5S").TieBreakValues[0] == 5, "Wheel must be five-high.");
        Check(Hand("1S 2H 3D 4C 5S").HighestCardValue == 14, "Ace value not recorded.");
        Check(Hand("12S 13H 1D 2C 3S").Rank == PokerHandRank.HighCard, "Straight must not wrap.");
        Wins("6S 5H 4D 3C 2S", "1S 2H 3D 4C 5S");
        Wins("6S 5S 4S 3S 2S", "1H 2H 3H 4H 5H");
        Wins("1S 13H 12D 11C 10S", "13S 12H 11D 10C 9S");
        Wins("2S 2H 13D 12C 11S", "1S 13H 12D 10C 9S");
        Wins("9S 9H 2D 3C 4S", "8S 8H 1D 13C 12S");
        Wins("8S 8H 1D 13C 11S", "8D 8C 1S 13H 10D");
        Wins("8S 8H 1D 12C 2S", "8D 8C 1S 11H 10D");
        Wins("9S 9H 3D 3C 2S", "8S 8H 7D 7C 1S");
        Wins("9S 9H 4D 4C 2S", "9D 9C 3S 3H 1S");
        Wins("9S 9H 4D 4C 13S", "9D 9C 4S 4H 12S");
        Wins("8S 8H 8D 1C 13S", "8C 8H 8D 1S 12S");
        Wins("8S 8H 8D 4C 4S", "8C 8H 8D 3S 3H");
        Wins("9S 9H 9D 2C 2S", "8S 8H 8D 1C 1S");
        Wins("8S 8H 8D 8C 1S", "8S 8H 8D 8C 13S");
        Wins("1S 11S 9S 6S 4S", "1H 11H 9H 6H 3H");
        Wins("1S 11H 9D 6C 4S", "1H 11D 9C 6S 3H");
        Check(Hand("1S 13S 11S 8S 2S").CompareTo(Hand("1H 13H 11H 8H 2H")) == 0, "Suits must not break ties.");
        Check(Hand(categories[0]).CompareTo(null) > 0, "Null comparison.");

        var input = Cards(categories[2]);
        var original = (Card.Card[])input.Clone();
        var originalValues = Array.ConvertAll(input, card => card.Value);
        var originalSuits = Array.ConvertAll(input, card => card.Suit);
        var result = PokerHandEvaluator.Evaluate(input);
        Array.Reverse(input);
        Check(result.CompareTo(PokerHandEvaluator.Evaluate(input)) == 0, "Input order affects result.");
        Array.Reverse(input);
        for (int i = 0; i < input.Length; i++)
            Check(ReferenceEquals(input[i], original[i]) && input[i].Value == originalValues[i]
                && input[i].Suit == originalSuits[i], "Input mutated.");

        Reject(() => PokerHandEvaluator.Evaluate(null));
        Reject(() => PokerHandEvaluator.Evaluate(new Card.Card[4]));
        Reject(() => PokerHandEvaluator.Evaluate(new Card.Card[6]));
        Reject(() => PokerHandEvaluator.Evaluate(new Card.Card[5]));
        Reject(() => Hand("1S 1S 2H 3H 4H"));
        Reject(() => Hand("0S 2S 3H 4H 5H"));
        Reject(() => Hand("14S 2S 3H 4H 5H"));
        Reject(() => Hand("1X 2S 3H 4H 5H"));

        CheckBestHands();
        CheckCardDisplayOrder();

        // Exhaust every five-card combination and compare with the exact classic distribution.
        var deck = new Card.Card[52];
        for (int i = 0; i < 52; i++) deck[i] = Card(i % 13 + 1, i / 13);
        var counts = new int[9];
        var five = new Card.Card[5];
        int royals = 0;
        for (int a = 0; a < 48; a++)
        for (int b = a + 1; b < 49; b++)
        for (int c = b + 1; c < 50; c++)
        for (int d = c + 1; d < 51; d++)
        for (int e = d + 1; e < 52; e++)
        {
            five[0] = deck[a]; five[1] = deck[b]; five[2] = deck[c]; five[3] = deck[d]; five[4] = deck[e];
            var evaluation = PokerHandEvaluator.Evaluate(five);
            counts[(int)evaluation.Rank]++;
            if (evaluation.IsRoyalFlush) royals++;
        }
        int[] expected = { 1302540, 1098240, 123552, 54912, 10200, 5108, 3744, 624, 40 };
        for (int i = 0; i < counts.Length; i++) Check(counts[i] == expected[i], "Distribution mismatch: " + (PokerHandRank)i);
        Check(royals == 4, "Expected four royal flushes.");
        Console.WriteLine("PASS: " + assertions + " assertions; all 2,598,960 hands classified with the expected distribution.");
    }
}
