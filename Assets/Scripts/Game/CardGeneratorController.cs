using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Reparte 16 cartas del tablero y 3 comunitarias sin repetir cartas del mazo.</summary>
    public class CardGeneratorController : MonoBehaviour
    {
        [Header("Memory Cards")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Transform backCardContainer;
        [SerializeField] private List<Card.Card> cardList;

        [Header("Game Cards")]
        [SerializeField] private Transform memoryCardContainer;

        private readonly List<Card.Card> cardsInPlay = new(19);

        public void DealCards()
        {
            CleanCards();
            
            // Conserva el orden de la lista del Inspector.
            var deck = new List<Card.Card>(cardList);
            DealCards(deck, cardContainer, 16, backCardContainer);

            // Usa las cartas restantes de la misma baraja.
            DealCards(deck, memoryCardContainer, 3);
        }

        private void DealCards(List<Card.Card> deck, Transform container, int amount, Transform backs = null)
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
                cardsInPlay.Add(instance);

                // Ambas cuadrículas comparten el orden de hijos: cada reverso conserva su carta frontal.
                if (backs != null && i < backs.childCount &&
                    backs.GetChild(i).TryGetComponent<Card.BackCard>(out var back))
                {
                    back.SetCard(instance);
                }
            }
        }

        private void CleanCards()
        {
            // Primero desvincula los reversos; después retira las instancias de la ronda anterior.
            if (backCardContainer != null)
            {
                foreach (Card.BackCard back in backCardContainer.GetComponentsInChildren<Card.BackCard>(true))
                {
                    back.SetCard(null);
                }
            }

            foreach (Card.Card card in cardsInPlay)
            {
                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
            }

            cardsInPlay.Clear();
        }
    }
}
