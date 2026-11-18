using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Refleja los tiempos y el estado de cada sello; no modifica la lógica de selección.</summary>
    [RequireComponent(typeof(Canvas))]
    public class RoundHudView : MonoBehaviour
    {
        [SerializeField] private GameRoundController round;

        private static readonly Color Gold = new Color(1f, 0.83f, 0.43f);
        private static readonly Color Muted = new Color(0.65f, 0.7f, 0.72f);
        [SerializeField] private Text memorizationCounter;
        [SerializeField] private Text selectionCounter;
        [Header("Available selection seals")]
        [SerializeField] private Image firstSeal;
        [SerializeField] private Image secondSeal;
        [SerializeField] private Material placedSealMaterial;
        private int lastMemorizationSeconds = -1;
        private int lastSelectionSeconds = -1;

        private void Awake()
        {
            if (round == null || memorizationCounter == null || selectionCounter == null)
            {
                Debug.LogError("Asigna la ronda y los dos contadores en el HUD.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (round == null) return;

            UpdateCounter(memorizationCounter, round.MemorizationTimeRemaining, ref lastMemorizationSeconds);
            UpdateCounter(selectionCounter, round.SelectionTimeRemaining, ref lastSelectionSeconds);
            memorizationCounter.color = round.Phase == RoundPhase.Memorizing ? Gold : Muted;
            selectionCounter.color = round.Phase == RoundPhase.Selecting ? Gold : Muted;
            UpdateSeal(firstSeal, round.IsFirstSealPlaced);
            UpdateSeal(secondSeal, round.IsSecondSealPlaced);
        }

        private void UpdateSeal(Image seal, bool placed)
        {
            // El material convierte el sello a gris de verdad, conservando su textura y transparencia.
            // Al liberarlo se recupera el material normal y, con él, sus colores originales.
            if (seal == null) return;
            Material material = placed ? placedSealMaterial : null;
            if (seal.material != (material != null ? material : seal.defaultMaterial))
                seal.material = material;
        }

        private static void UpdateCounter(Text counter, float remaining, ref int previousSeconds)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            if (seconds == previousSeconds) return;
            previousSeconds = seconds;
            counter.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

    }
}
