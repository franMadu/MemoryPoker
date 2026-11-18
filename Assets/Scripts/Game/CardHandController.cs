using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class CardHandController : MonoBehaviour
    {
        [SerializeField] public List<Card.Card> gameCards = new List<Card.Card>();
        [SerializeField] public List<Card.Card> playerCards = new List<Card.Card>();
        
        
    }
}
