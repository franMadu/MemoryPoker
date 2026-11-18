using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class CardGeneratorController : MonoBehaviour
    {
        [Header("Memory Cards")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private List<Card.Card> cardList;

        [Header("Game Cards")]
        [SerializeField] private Transform memoryCardContainer;

        private readonly List<Card.Card> cardsInPlay = new(19);

        private void Update()
        {
            if (Keyboard.current != null && 
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                DealCards();
            }
        }
        
        private void DealCards()
        {
            CleanCards();
            
            // Conserva el orden de la lista del Inspector.
            var deck = new List<Card.Card>(cardList);
            DealCards(deck, cardContainer, 16);

            // Usa las cartas restantes de la misma baraja.
            DealCards(deck, memoryCardContainer, 3);
        }

        private void DealCards(List<Card.Card> deck, Transform container, int amount)
        {
            int count = Mathf.Min(amount, deck.Count);

            for (int i = 0; i < count; i++)
            {
                // Elige entre las cartas que todavía no se han repartido.
                int randomIndex = UnityEngine.Random.Range(0, deck.Count);
                Card.Card selected = deck[randomIndex];
                // Retira la elegida para que no vuelva a salir en ningún contenedor.
                int lastIndex = deck.Count - 1;
                deck[randomIndex] = deck[lastIndex];
                deck.RemoveAt(lastIndex);

                Card.Card instance = Instantiate(selected, container);
                instance.InPlay = true;
                cardsInPlay.Add(instance);
            }
        }

        private void CleanCards()
        {
            foreach (Card.Card card in cardsInPlay)
            {
                card.InPlay = false;
                Destroy(card.gameObject);
            }

            cardsInPlay.Clear();
        }
    }
}
