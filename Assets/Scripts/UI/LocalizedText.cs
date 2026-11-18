using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Actualiza una etiqueta estática por su clave sin reconstruir los objetos de la interfaz.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        private Text label;

        private void OnEnable()
        {
            label = GetComponent<Text>();
            GameLocalization.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => GameLocalization.Changed -= Refresh;
        private void Refresh() => label.text = GameLocalization.Text(key);
    }
}
