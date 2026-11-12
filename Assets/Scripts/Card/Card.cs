using UnityEngine;

namespace Card
{
    public class Card : MonoBehaviour
    {
        [SerializeField] private SuitEnum suit;
        [SerializeField] private int value;
        [SerializeField] private Sprite cardImage;
    }
}
