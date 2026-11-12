
using UnityEngine;
using UnityEngine.EventSystems;
    
namespace Card
{
    public class BackCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Sprite cardImage;
        private IPointerClickHandler _pointerClickHandlerImplementation;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                 return;
            }
            
            Debug.Log("Carta seleccionada: " + gameObject.name, this);
        }
    }
}
