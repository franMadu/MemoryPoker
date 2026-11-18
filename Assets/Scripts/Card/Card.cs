using UnityEngine;

namespace Card
{
    /// <summary>Datos de una carta del mazo: palo, valor (as = 1) e imagen para los resultados.</summary>
    public class Card : MonoBehaviour
    {
        [SerializeField] private SuitEnum suit;
        [SerializeField] private int value;
        [SerializeField] private Sprite cardImage;

        public SuitEnum Suit => suit;
        public int Value => value;
        public Sprite CardImage => cardImage;
    }
}
