using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
    
namespace Card
{
    /// <summary>Relaciona un reverso con su carta y comunica los clics a la ronda.</summary>
    public class BackCard : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Card _card;
        private Image selectionSeal;

        public Card Card => _card;
        public event Action<BackCard> Clicked;

        public void SetCard(Card card)
        {
            // El reverso se reutiliza en cada reparto; el sello anterior deja de ser válido.
            _card = card;
            SetSelectionSeal(null);
        }

        public void SetSelectionSeal(Sprite sprite)
        {
            if (selectionSeal == null && sprite != null)
            {
                // Se crea una sola imagen por reverso y se reutiliza al seleccionar o quitar el sello.
                var sealObject = new GameObject("SelectionSeal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sealObject.layer = gameObject.layer;
                sealObject.transform.SetParent(transform, false);
                selectionSeal = sealObject.GetComponent<Image>();
                selectionSeal.raycastTarget = false;
                selectionSeal.preserveAspect = true;

                RectTransform rect = selectionSeal.rectTransform;
                rect.anchorMin = new Vector2(0.16f, 0.27f);
                rect.anchorMax = new Vector2(0.84f, 0.73f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            if (selectionSeal != null)
            {
                selectionSeal.sprite = sprite;
                selectionSeal.gameObject.SetActive(sprite != null);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                 return;
            }
            
            Clicked?.Invoke(this);
        }
    }
}
