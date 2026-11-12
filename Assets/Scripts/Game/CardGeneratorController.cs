using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class CardGeneratorController : MonoBehaviour
    {
        [SerializeField] public GameObject cardContainer;   
        [SerializeField] public List<GameObject> cardList;

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
            
            for(int i = 0; i < 16; i++)
            {
                GameObject card = cardList[i];
                Instantiate(card,  cardContainer.transform);
            }
        }

        private void CleanCards()
        {
            Transform container = cardContainer.transform;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
