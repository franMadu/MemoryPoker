using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Jugador único de la escena; guarda las dos elecciones y su mano de cinco cartas.</summary>
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        private Card.Card cardOne;
        private Card.Card cardTwo;
        
        private List<Card.Card> playerHand = new List<Card.Card>();

        public IReadOnlyList<Card.Card> PlayerHand => playerHand;

        public void ClearHand()
        {
            cardOne = null;
            cardTwo = null;
            playerHand.Clear();
        }

        private void Awake()
        {
            // Impide que dos componentes distintos actúen como jugador a la vez.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddCardOne(Card.Card card)
        {
            // Cambiar una elección invalida la mano evaluable hasta reconstruirla con las comunitarias.
            cardOne = card;
            playerHand.Clear();
        }

        public void AddCardTwo(Card.Card card)
        {
            cardTwo = card;
            playerHand.Clear();
        }
        
        public void CreatePlayerHand(Card.Card rightCard, Card.Card leftCard, Card.Card centerCard)
        {
            // Una selección incompleta nunca debe conservar las cinco cartas de la elección anterior.
            playerHand.Clear();
            if (cardOne == null || cardTwo == null || rightCard == null || leftCard == null || centerCard == null)
            {
                return;
            }

            playerHand.Add(cardOne);
            playerHand.Add(cardTwo);
            playerHand.Add(rightCard);
            playerHand.Add(leftCard);
            playerHand.Add(centerCard);
        }
    }
}
