using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Ajusta la escala del menú y distribuye los ajustes según la orientación de la pantalla.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class MenuScreenLayout : MonoBehaviour
    {
        [Serializable]
        private class Item
        {
            public RectTransform target;
            public Vector2 portraitPosition;
            public Vector2 portraitSize;
            public Vector2 landscapePosition;
            public Vector2 landscapeSize;
            public Text label;
            public int portraitFontSize;
            public int landscapeFontSize;
        }

        [SerializeField] private Vector2 portraitSize = new Vector2(400, 700);
        [SerializeField] private Vector2 landscapeSize = new Vector2(800, 560);
        [SerializeField, Range(0.5f, 1f)] private float screenCoverage = 0.88f;
        [SerializeField] private Item[] items = Array.Empty<Item>();
        private Vector2 previousSize = new Vector2(-1, -1);

        private void OnEnable() { Refresh(); }
        private void OnValidate() { previousSize = new Vector2(-1, -1); }
        private void Update()
        {
            // También funciona en el editor para previsualizar el menú; solo recalcula si cambia el tamaño.
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.size != previousSize) Refresh();
        }

        private void Refresh()
        {
            var content = (RectTransform)transform;
            var parent = content.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            previousSize = parent.rect.size;
            bool landscape = previousSize.x > previousSize.y * 1.2f;
            Vector2 design = landscape ? landscapeSize : portraitSize;
            content.sizeDelta = design;
            float scale = Mathf.Min(previousSize.x / design.x, previousSize.y / design.y) * screenCoverage;
            // Una escala uniforme conserva las proporciones de fuentes, botones y sellos.
            content.localScale = new Vector3(scale, scale, 1);
            foreach (var item in items)
            {
                if (item.target == null) continue;
                item.target.anchoredPosition = landscape ? item.landscapePosition : item.portraitPosition;
                item.target.sizeDelta = landscape ? item.landscapeSize : item.portraitSize;
                if (item.label != null)
                    item.label.fontSize = landscape ? item.landscapeFontSize : item.portraitFontSize;
            }
        }
    }
}
