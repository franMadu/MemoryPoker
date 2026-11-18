using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Anima las cuentas atrás de preparación y reparto usando el tiempo que indica la ronda.</summary>
    public class CentralCountdownView : MonoBehaviour
    {
        [SerializeField] private GameRoundController round;
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private Text numberLabel;
        [SerializeField] private Text phaseLabel;

        private Color numberColor;
        private Vector3 numberScale;
        private int displayedNumber = -1;
        private RoundPhase displayedPhase = RoundPhase.Inactive;

        private void Awake()
        {
            if (round == null || countdownPanel == null || numberLabel == null || phaseLabel == null)
            {
                Debug.LogError("Asigna la ronda y los elementos de la cuenta atras central.", this);
                enabled = false;
                return;
            }

            numberColor = numberLabel.color;
            numberScale = numberLabel.rectTransform.localScale;
            countdownPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (numberLabel != null)
            {
                numberLabel.color = numberColor;
                numberLabel.rectTransform.localScale = numberScale;
            }
            displayedNumber = -1;
            displayedPhase = RoundPhase.Inactive;
        }

        private void Update()
        {
            bool starting = round.Phase == RoundPhase.WaitingToStart;
            bool waiting = round.Phase == RoundPhase.WaitingForCommunityCards;
            float remaining = starting ? round.StartTimeRemaining : round.CommunityWaitTimeRemaining;
            if ((!starting && !waiting) || remaining <= 0f)
            {
                if (countdownPanel.activeSelf) countdownPanel.SetActive(false);
                displayedNumber = -1;
                return;
            }

            int number = Mathf.CeilToInt(remaining);
            if (number != displayedNumber || round.Phase != displayedPhase)
            {
                displayedNumber = number;
                displayedPhase = round.Phase;
                numberLabel.text = number.ToString();
                phaseLabel.text = GameLocalization.Text(starting ? "ready" : "dealing");
            }

            // Anima con el tiempo real de la ronda para mantener sincronizados el número y la fase.
            float progress = Mathf.Clamp01(1f - (remaining - (number - 1f)));
            float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.18f));
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.18f) / 0.14f));
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.07f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.72f) / 0.28f));

            numberLabel.rectTransform.localScale = numberScale * (Mathf.Lerp(0.65f, 1.15f, entrance) - 0.15f * settle);
            numberLabel.color = new Color(numberColor.r, numberColor.g, numberColor.b, numberColor.a * fadeIn * (1f - fadeOut));
            if (!countdownPanel.activeSelf) countdownPanel.SetActive(true);
        }
    }
}
