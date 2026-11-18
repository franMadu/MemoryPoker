using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Presenta el top 3 y la mano elegida, con cartas agrupadas y puesto entre las 120 parejas.</summary>
    public class RoundResultsView : MonoBehaviour
    {
        [Serializable]
        private sealed class HandRow
        {
            public Text title;
            public Image background;
            public Image[] cards;
        }

        [SerializeField] private GameRoundController round;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private HandRow[] bestHands;
        [SerializeField] private HandRow playerHand;
        [SerializeField] private Button newRoundButton;
        [SerializeField] private Sprite emptyCardSprite;

        private static readonly Color NormalBackground = new Color(0.035f, 0.1f, 0.16f, 1f);
        private static readonly Color MatchedBackground = new Color(0.2f, 0.17f, 0.075f, 1f);
        private static readonly Color Gold = new Color(1f, 0.83f, 0.43f);
        private bool displayed;

        private void Awake()
        {
            if (round == null || resultsPanel == null || newRoundButton == null ||
                bestHands == null || bestHands.Length != 3 || !IsConfigured(playerHand))
            {
                Debug.LogError("Configura el panel de resultados, las cuatro manos y el boton Nueva ronda.", this);
                enabled = false;
                return;
            }

            foreach (HandRow row in bestHands)
            {
                if (IsConfigured(row)) continue;
                Debug.LogError("Cada mano de resultados necesita un titulo, fondo y cinco imagenes de cartas.", this);
                enabled = false;
                return;
            }

            resultsPanel.SetActive(false);
        }

        private static bool IsConfigured(HandRow row)
        {
            if (row == null || row.title == null || row.background == null || row.cards == null || row.cards.Length != 5)
                return false;
            foreach (Image card in row.cards) if (card == null) return false;
            return true;
        }

        private void OnEnable()
        {
            newRoundButton.onClick.AddListener(StartNewRound);
        }

        private void OnDisable()
        {
            if (newRoundButton != null) newRoundButton.onClick.RemoveListener(StartNewRound);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            displayed = false;
        }

        private void Update()
        {
            if (round.Phase != RoundPhase.Completed)
            {
                if (resultsPanel.activeSelf) resultsPanel.SetActive(false);
                displayed = false;
                return;
            }

            if (displayed) return;
            ShowResults();
            displayed = true;
        }

        private void ShowResults()
        {
            for (int i = 0; i < bestHands.Length; i++)
            {
                if (i >= round.BestHands.Count)
                {
                    DrawHand(bestHands[i], GameLocalization.Text("unavailable", i + 1), Array.Empty<Card.Card>(), false);
                    continue;
                }

                PokerHandCandidate hand = round.BestHands[i];
                bool matched = round.Result != null && round.Result.CompareTo(hand.Result) == 0;
                string positions = $"{(char)('A' + hand.FirstBoardIndex)} + {(char)('A' + hand.SecondBoardIndex)}";
                DrawHand(bestHands[i], GameLocalization.Text("topHand", i + 1, hand.Result.DisplayName, positions), hand.Cards, matched);
            }

            string playerTitle = GameLocalization.Text("yourHand", round.Result != null ? round.Result.DisplayName : GameLocalization.Text("incomplete"));
            playerTitle += $" ({BoardPosition(round.FirstSelectedBoardIndex)} + {BoardPosition(round.SecondSelectedBoardIndex)})";
            if (round.Result != null)
                playerTitle += GameLocalization.Text("place", round.PlayerCombinationPlace, round.TotalCombinations);
            DrawHand(playerHand, playerTitle, round.FinalPlayerCards, round.AchievedBestHandPlace > 0);
            newRoundButton.interactable = round.isActiveAndEnabled;
            resultsPanel.transform.SetAsLastSibling();
            resultsPanel.SetActive(true);
        }

        private static string BoardPosition(int index) => index >= 0 ? ((char)('A' + index)).ToString() : "-";

        private void DrawHand(HandRow row, string title, IReadOnlyList<Card.Card> cards, bool matched)
        {
            // Solo ordena una copia para dibujarla: el orden de los sellos sigue disponible para el título.
            row.title.text = title;
            row.title.color = matched ? Gold : Color.white;
            row.background.color = matched ? MatchedBackground : NormalBackground;
            IReadOnlyList<Card.Card> orderedCards = PokerHandDisplay.OrderCards(cards);
            for (int i = 0; i < row.cards.Length; i++)
            {
                Card.Card card = i < orderedCards.Count ? orderedCards[i] : null;
                row.cards[i].sprite = card != null ? card.CardImage : emptyCardSprite;
                row.cards[i].color = card != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                row.cards[i].preserveAspect = true;
            }
        }

        private void StartNewRound()
        {
            displayed = false;
            resultsPanel.SetActive(false);
            round.StartRound();
        }
    }
}
