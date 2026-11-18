using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // La fase determina qué contador se muestra y cuándo se permite seleccionar cartas.
    public enum RoundPhase
    {
        Inactive,
        WaitingToStart,
        Memorizing,
        WaitingForCommunityCards,
        Selecting,
        Completed,
        Flipping
    }

    /// <summary>Coordina el reparto, los tiempos, las selecciones y la clasificación final.</summary>
    public class GameRoundController : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private CardGeneratorController cardGenerator;
        [SerializeField] private GameObject boardFrontContainer;
        [SerializeField] private Transform boardBackContainer;
        [SerializeField] private GameObject communityFrontContainer;
        [SerializeField] private GameObject communityBackContainer;
        [SerializeField] private Player player;
        [SerializeField] private GameAudio gameAudio;

        [Header("Selection seals")]
        [SerializeField] private Sprite firstSelectionSeal;
        [SerializeField] private Sprite secondSelectionSeal;

        [Header("Round timing (seconds)")]
        [SerializeField, Min(0)] private float startDelay = 3f;
        [SerializeField, Min(0)] private float memorizationDuration = 20f;
        [SerializeField, Min(0)] private float communityRevealDelay = 5f;
        [SerializeField, Min(0)] private float selectionDuration = 60f;

        [Header("Card animation")]
        [SerializeField, Min(0)] private float flipDuration = 0.45f;

        private readonly List<Card.BackCard> boardBacks = new List<Card.BackCard>(16);
        // Cada posición conserva su sello: quitar el primero no renumera el segundo.
        private readonly Card.BackCard[] selectedCards = new Card.BackCard[2];
        private readonly CardFlipAnimation flipAnimation = new CardFlipAnimation();

        public RoundPhase Phase { get; private set; }
        public PokerHandResult Result { get; private set; }
        public IReadOnlyList<PokerHandCandidate> BestHands { get; private set; } = System.Array.Empty<PokerHandCandidate>();
        public IReadOnlyList<Card.Card> FinalPlayerCards { get; private set; } = System.Array.Empty<Card.Card>();
        // Vale 1, 2 o 3 si la mano equivale a una del top; cero si no coincide.
        public int AchievedBestHandPlace { get; private set; }
        public int PlayerCombinationPlace { get; private set; }
        public int TotalCombinations { get; private set; }
        public float MemorizationTimeRemaining { get; private set; }
        public float SelectionTimeRemaining { get; private set; }
        public float StartTimeRemaining { get; private set; }
        public float CommunityWaitTimeRemaining { get; private set; }
        public int SelectedCount => (selectedCards[0] != null ? 1 : 0) + (selectedCards[1] != null ? 1 : 0);
        public bool IsFirstSealPlaced => selectedCards[0] != null;
        public bool IsSecondSealPlaced => selectedCards[1] != null;
        public int FirstSelectedBoardIndex => selectedCards[0] != null ? boardBacks.IndexOf(selectedCards[0]) : -1;
        public int SecondSelectedBoardIndex => selectedCards[1] != null ? boardBacks.IndexOf(selectedCards[1]) : -1;

        private void Awake()
        {
            Application.runInBackground = true;
        }

        private void OnEnable()
        {
            StartRound();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (gameAudio != null) gameAudio.StopAllSounds();
            flipAnimation.Restore();
            Phase = RoundPhase.Inactive;
            Result = null;
            BestHands = System.Array.Empty<PokerHandCandidate>();
            FinalPlayerCards = System.Array.Empty<Card.Card>();
            AchievedBestHandPlace = 0;
            PlayerCombinationPlace = 0;
            TotalCombinations = 0;
            StartTimeRemaining = 0f;
            CommunityWaitTimeRemaining = 0f;
            ClearSelection();
            UnsubscribeBacks();
        }

        public void StartRound()
        {
            StopAllCoroutines();
            if (gameAudio != null) gameAudio.StopAllSounds();
            flipAnimation.Restore();
            Phase = RoundPhase.Inactive;
            Result = null;
            BestHands = System.Array.Empty<PokerHandCandidate>();
            FinalPlayerCards = System.Array.Empty<Card.Card>();
            AchievedBestHandPlace = 0;
            PlayerCombinationPlace = 0;
            TotalCombinations = 0;
            MemorizationTimeRemaining = memorizationDuration;
            SelectionTimeRemaining = selectionDuration;
            StartTimeRemaining = startDelay;
            CommunityWaitTimeRemaining = communityRevealDelay;
            ClearSelection();
            UnsubscribeBacks();
            if (!isActiveAndEnabled || !InitializeBacks()) return;

            boardBackContainer.gameObject.SetActive(true);
            boardFrontContainer.SetActive(false);
            communityBackContainer.SetActive(true);
            communityFrontContainer.SetActive(false);
            cardGenerator.DealCards();
            if (gameAudio != null) gameAudio.PlayNewRound();
            StartCoroutine(RunRound());
        }

        private bool InitializeBacks()
        {
            if (cardGenerator == null || boardFrontContainer == null || boardBackContainer == null ||
                communityFrontContainer == null || communityBackContainer == null || player == null ||
                firstSelectionSeal == null || secondSelectionSeal == null || boardBackContainer.childCount != 16)
            {
                Debug.LogError("Configure the round containers, player, two seals and sixteen card backs.", this);
                return false;
            }

            for (int i = 0; i < boardBackContainer.childCount; i++)
            {
                if (!boardBackContainer.GetChild(i).TryGetComponent<Card.BackCard>(out var back))
                {
                    Debug.LogError("Every board position must have a BackCard component.", this);
                    UnsubscribeBacks();
                    return false;
                }

                boardBacks.Add(back);
                back.Clicked += OnCardClicked;
            }

            return true;
        }

        private IEnumerator RunRound()
        {
            // Secuencia única: preparación, memorizar, cubrir, esperar, elegir y evaluar.
            // Las animaciones terminan antes de iniciar el siguiente contador.
            yield return CountDown(RoundPhase.WaitingToStart, startDelay);

            Phase = RoundPhase.Flipping;
            if (gameAudio != null) gameAudio.PlayCardFlip();
            yield return flipAnimation.Play(boardFrontContainer, boardBackContainer.gameObject, true, flipDuration);

            yield return CountDown(RoundPhase.Memorizing, memorizationDuration);

            Phase = RoundPhase.Flipping;
            if (gameAudio != null) gameAudio.PlayCardFlip();
            yield return flipAnimation.Play(boardFrontContainer, boardBackContainer.gameObject, false, flipDuration);

            yield return CountDown(RoundPhase.WaitingForCommunityCards, communityRevealDelay);

            Phase = RoundPhase.Flipping;
            if (gameAudio != null) gameAudio.PlayCardFlip();
            yield return flipAnimation.Play(communityFrontContainer, communityBackContainer, true, flipDuration);

            yield return CountDown(RoundPhase.Selecting, selectionDuration);

            FinishRound();
        }

        private IEnumerator CountDown(RoundPhase phase, float duration)
        {
            Phase = phase;
            // Calcula el tiempo restante desde una fecha de fin; no acumula esperas de un segundo.
            // runInBackground permite avanzar también cuando el juego pierde el foco.
            float endTime = Time.time + duration;
            int previousSecond = Mathf.CeilToInt(duration);
            float remaining;
            do
            {
                remaining = Mathf.Max(0f, endTime - Time.time);
                switch (phase)
                {
                    case RoundPhase.WaitingToStart: StartTimeRemaining = remaining; break;
                    case RoundPhase.Memorizing: MemorizationTimeRemaining = remaining; break;
                    case RoundPhase.WaitingForCommunityCards: CommunityWaitTimeRemaining = remaining; break;
                    case RoundPhase.Selecting: SelectionTimeRemaining = remaining; break;
                }

                int seconds = Mathf.CeilToInt(remaining);
                if (seconds > 0 && seconds != previousSecond && gameAudio != null)
                    gameAudio.PlayCountdown(seconds);
                previousSecond = seconds;

                if (remaining > 0f) yield return null;
            }
            while (remaining > 0f);
        }

        private void FinishRound()
        {
            // Bloquea la selección antes de capturar las cinco cartas definitivas.
            Phase = RoundPhase.Completed;
            if (gameAudio != null) gameAudio.PlayRoundEnd();
            UpdatePlayerHand();

            if (communityFrontContainer.transform.childCount != 3)
            {
                Debug.LogError("No se pueden buscar las mejores manos: faltan cartas del visor 1x3.", this);
                return;
            }

            var board = new List<Card.Card>(boardBacks.Count);
            foreach (Card.BackCard back in boardBacks) board.Add(back.Card);
            var community = new Card.Card[3];
            for (int i = 0; i < community.Length; i++)
                community[i] = communityFrontContainer.transform.GetChild(i).GetComponent<Card.Card>();

            FinalPlayerCards = System.Array.AsReadOnly(new[]
            {
                selectedCards[0] != null ? selectedCards[0].Card : null,
                selectedCards[1] != null ? selectedCards[1].Card : null,
                community[0], community[1], community[2]
            });

            int possiblePairs = board.Count * (board.Count - 1) / 2;
            // Con 16 posiciones hay 120 parejas. Se calculan todas para conocer el puesto real,
            // aunque la interfaz solo presente las tres primeras.
            IReadOnlyList<PokerHandCandidate> allHands = PokerHandSearch.FindBestHands(board, community, possiblePairs);
            TotalCombinations = allHands.Count;
            var topThree = new List<PokerHandCandidate>(3);
            for (int i = 0; i < System.Math.Min(3, allHands.Count); i++) topThree.Add(allHands[i]);
            BestHands = topThree.AsReadOnly();
            for (int i = 0; i < BestHands.Count; i++)
            {
                PokerHandCandidate hand = BestHands[i];
                char firstPosition = (char)('A' + hand.FirstBoardIndex);
                char secondPosition = (char)('A' + hand.SecondBoardIndex);
                Debug.Log($"Top {i + 1}: {hand.Result.DisplayName} | Posiciones: {firstPosition} + {secondPosition}" +
                    $" | Cartas: {FormatCards(hand.Cards)} | Desempate: {string.Join(", ", hand.Result.TieBreakValues)}", this);
            }

            if (SelectedCount != 2)
            {
                Debug.LogWarning("Tiempo agotado: no has seleccionado dos cartas. No has conseguido ninguna de las tres mejores manos.", this);
                return;
            }

            if (player.PlayerHand.Count != 5)
            {
                Debug.LogError("No se puede evaluar la mano: faltan cartas del visor 1x3.", this);
                return;
            }

            Result = PokerHandEvaluator.Evaluate(player.PlayerHand);
            // El puesto es 1 + el número de combinaciones estrictamente mejores; los empates comparten puesto.
            PlayerCombinationPlace = PokerHandSearch.FindMatchingPlace(allHands, Result);
            AchievedBestHandPlace = PokerHandSearch.FindMatchingPlace(BestHands, Result);
            if (gameAudio != null) gameAudio.PlayTopResult(AchievedBestHandPlace);
            Debug.Log($"Mano del jugador: {Result.DisplayName} | Cartas: {FormatCards(player.PlayerHand)}" +
                $" | Puesto: {PlayerCombinationPlace}/{TotalCombinations}" +
                $" | Carta mas alta: {Result.HighestCardValue} | Desempate: {string.Join(", ", Result.TieBreakValues)}", this);
            Debug.Log(AchievedBestHandPlace > 0
                ? $"Has conseguido la mano Top {AchievedBestHandPlace} (incluye combinaciones equivalentes en empate)."
                : "No has conseguido ninguna de las tres mejores manos.", this);
        }

        private static string FormatCards(IReadOnlyList<Card.Card> cards)
        {
            var labels = new string[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                Card.Card card = cards[i];
                string value = card.Value switch { 1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => card.Value.ToString() };
                string suit = card.Suit switch
                {
                    SuitEnum.Spades => "picas",
                    SuitEnum.Hearts => "corazones",
                    SuitEnum.Diamonds => "diamantes",
                    _ => "treboles"
                };
                labels[i] = $"{value} de {suit}";
            }

            return string.Join(", ", labels);
        }

        private void OnCardClicked(Card.BackCard back)
        {
            TryToggleSelection(back);
        }

        public bool TryToggleSelection(Card.BackCard back)
        {
            // La ronda valida las entradas; los reversos solo notifican que han recibido un clic.
            if (Phase != RoundPhase.Selecting || back == null || back.Card == null || !boardBacks.Contains(back))
                return false;

            // Permite quitar una elección aunque los dos huecos ya estén ocupados.
            for (int slot = 0; slot < selectedCards.Length; slot++)
            {
                if (selectedCards[slot] != back) continue;
                selectedCards[slot] = null;
                back.SetSelectionSeal(null);
                UpdatePlayerHand();
                if (gameAudio != null) gameAudio.PlaySealDeselect();
                return true;
            }

            for (int slot = 0; slot < selectedCards.Length; slot++)
            {
                if (selectedCards[slot] != null) continue;
                selectedCards[slot] = back;
                back.SetSelectionSeal(slot == 0 ? firstSelectionSeal : secondSelectionSeal);
                UpdatePlayerHand();
                if (gameAudio != null) gameAudio.PlaySealSelect();
                return true;
            }

            return false;
        }

        private void UpdatePlayerHand()
        {
            player.AddCardOne(selectedCards[0] != null ? selectedCards[0].Card : null);
            player.AddCardTwo(selectedCards[1] != null ? selectedCards[1].Card : null);
            if (SelectedCount == 2 && communityFrontContainer.transform.childCount == 3)
            {
                Transform community = communityFrontContainer.transform;
                player.CreatePlayerHand(community.GetChild(0).GetComponent<Card.Card>(),
                    community.GetChild(1).GetComponent<Card.Card>(), community.GetChild(2).GetComponent<Card.Card>());
            }
        }

        private void ClearSelection()
        {
            for (int slot = 0; slot < selectedCards.Length; slot++)
            {
                if (selectedCards[slot] != null) selectedCards[slot].SetSelectionSeal(null);
                selectedCards[slot] = null;
            }

            if (player != null) player.ClearHand();
        }

        private void UnsubscribeBacks()
        {
            // Evita suscripciones repetidas cuando se inicia otra ronda o se vuelve al menú.
            foreach (Card.BackCard back in boardBacks)
            {
                if (back != null) back.Clicked -= OnCardClicked;
            }

            boardBacks.Clear();
        }
    }
}
