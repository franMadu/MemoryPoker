using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Navega entre menús y partida y mantiene una copia provisional de los ajustes.</summary>
    [DefaultExecutionOrder(-200)]
    public class MainMenuController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private GameRoundController round;
        [SerializeField] private GameAudio gameAudio;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject gameHud;
        [SerializeField] private GameObject navigation;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resultsMenuButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button defaultsButton;

        [Header("Settings controls")]
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageValue;
        [SerializeField] private Slider masterVolume;
        [SerializeField] private Text masterValue;
        [SerializeField] private Slider effectsVolume;
        [SerializeField] private Text effectsValue;
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Text musicValue;
        [SerializeField] private Button screenButton;
        [SerializeField] private Text screenValue;
        [SerializeField] private Button resolutionButton;
        [SerializeField] private Text resolutionValue;
        [SerializeField] private Button qualityButton;
        [SerializeField] private Text qualityValue;
        [SerializeField] private Button vSyncButton;
        [SerializeField] private Text vSyncValue;
        [SerializeField] private Button frameLimitButton;
        [SerializeField] private Text frameLimitValue;

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private GameSettingsData saved;
        private GameSettingsData draft;
        private static readonly int[] FrameLimits = { 30, 60, 120, 0 };

        private void Awake()
        {
            GamePreferences.Load();
            round.enabled = false;
            saved = GamePreferences.Current.Copy();
            BuildResolutions();
            ShowMainMenu();
        }

        private void OnEnable()
        {
            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(OpenSettings);
            menuButton.onClick.AddListener(ReturnToMainMenu);
            resultsMenuButton.onClick.AddListener(ReturnToMainMenu);
            backButton.onClick.AddListener(CancelSettings);
            applyButton.onClick.AddListener(ApplySettings);
            defaultsButton.onClick.AddListener(RestoreDefaults);
            languageButton.onClick.AddListener(ChangeLanguage);
            screenButton.onClick.AddListener(ChangeScreen);
            resolutionButton.onClick.AddListener(ChangeResolution);
            qualityButton.onClick.AddListener(ChangeQuality);
            vSyncButton.onClick.AddListener(ChangeVSync);
            frameLimitButton.onClick.AddListener(ChangeFrameLimit);
            masterVolume.onValueChanged.AddListener(ChangeMasterVolume);
            effectsVolume.onValueChanged.AddListener(ChangeEffectsVolume);
            musicVolume.onValueChanged.AddListener(ChangeMusicVolume);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(Play);
            settingsButton.onClick.RemoveListener(OpenSettings);
            menuButton.onClick.RemoveListener(ReturnToMainMenu);
            resultsMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            backButton.onClick.RemoveListener(CancelSettings);
            applyButton.onClick.RemoveListener(ApplySettings);
            defaultsButton.onClick.RemoveListener(RestoreDefaults);
            languageButton.onClick.RemoveListener(ChangeLanguage);
            screenButton.onClick.RemoveListener(ChangeScreen);
            resolutionButton.onClick.RemoveListener(ChangeResolution);
            qualityButton.onClick.RemoveListener(ChangeQuality);
            vSyncButton.onClick.RemoveListener(ChangeVSync);
            frameLimitButton.onClick.RemoveListener(ChangeFrameLimit);
            masterVolume.onValueChanged.RemoveListener(ChangeMasterVolume);
            effectsVolume.onValueChanged.RemoveListener(ChangeEffectsVolume);
            musicVolume.onValueChanged.RemoveListener(ChangeMusicVolume);
        }

        private void Update()
        {
            bool showNavigation = !mainPanel.activeSelf && !settingsPanel.activeSelf && round.Phase != RoundPhase.Completed;
            if (navigation.activeSelf != showNavigation) navigation.SetActive(showNavigation);
        }

        private void Click() { if (gameAudio != null) gameAudio.PlayNewRound(); }

        private void Play()
        {
            // Habilitar la ronda inicia su secuencia; una ronda ya habilitada se reinicia explícitamente.
            mainPanel.SetActive(false);
            settingsPanel.SetActive(false);
            gameHud.SetActive(true);
            navigation.SetActive(true);
            if (round.enabled) round.StartRound();
            else round.enabled = true;
        }

        private void ShowMainMenu()
        {
            round.enabled = false;
            gameHud.SetActive(false);
            navigation.SetActive(false);
            settingsPanel.SetActive(false);
            mainPanel.transform.SetAsLastSibling();
            mainPanel.SetActive(true);
        }

        private void ReturnToMainMenu()
        {
            ShowMainMenu();
            // Detener la ronda cancela los efectos: reproduce el clic después de volver al menú.
            Click();
        }

        private void OpenSettings()
        {
            // Trabaja sobre una copia: Volver puede restaurar el audio y el idioma anteriores.
            draft = saved.Copy();
            RefreshSettings();
            mainPanel.SetActive(false);
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
            Click();
        }

        private void CancelSettings()
        {
            GamePreferences.Preview(saved);
            ShowMainMenu();
            Click();
        }

        private void ApplySettings()
        {
            GamePreferences.Save(draft);
            saved = GamePreferences.Current.Copy();
            ShowMainMenu();
            Click();
        }

        private void RestoreDefaults()
        {
            draft = GamePreferences.Defaults();
            RefreshSettings();
            Click();
        }

        private void RefreshSettings()
        {
            // Actualiza los controles sin disparar sus eventos, evitando cambios y sonidos recursivos.
            GamePreferences.Preview(draft);
            masterVolume.SetValueWithoutNotify(draft.MasterVolume);
            effectsVolume.SetValueWithoutNotify(draft.EffectsVolume);
            musicVolume.SetValueWithoutNotify(draft.MusicVolume);
            masterValue.text = $"{Mathf.RoundToInt(draft.MasterVolume * 100f)} %";
            effectsValue.text = $"{Mathf.RoundToInt(draft.EffectsVolume * 100f)} %";
            musicValue.text = $"{Mathf.RoundToInt(draft.MusicVolume * 100f)} %";
            languageValue.text = GameLocalization.LanguageName(draft.Language) + " >";
            screenValue.text = GameLocalization.Text(draft.Fullscreen ? "fullscreen" : "windowed") + " >";
            resolutionValue.text = $"{draft.Width} x {draft.Height} >";
            string[] names = QualitySettings.names;
            string qualityName = names.Length > 0 ? names[Mathf.Clamp(draft.Quality, 0, names.Length - 1)] : "PC";
            string qualityKey = "quality." + qualityName;
            string qualityLabel = GameLocalization.Text(qualityKey);
            qualityValue.text = (qualityLabel == qualityKey ? qualityName : qualityLabel) + " >";
            vSyncValue.text = GameLocalization.Text(draft.VSync ? "on" : "off") + " >";
            frameLimitValue.text = (draft.FrameLimit == 0 ? GameLocalization.Text("unlimited") : draft.FrameLimit + " FPS") + " >";
            frameLimitButton.interactable = !draft.VSync;
        }

        private void ChangeLanguage() { draft.Language = (GameLanguage)(((int)draft.Language + 1) % 3); RefreshSettings(); Click(); }
        private void ChangeScreen() { draft.Fullscreen = !draft.Fullscreen; RefreshSettings(); Click(); }
        private void ChangeQuality() { draft.Quality = (draft.Quality + 1) % Mathf.Max(1, QualitySettings.names.Length); RefreshSettings(); Click(); }
        private void ChangeVSync() { draft.VSync = !draft.VSync; RefreshSettings(); Click(); }
        private void ChangeFrameLimit()
        {
            int index = System.Array.IndexOf(FrameLimits, draft.FrameLimit);
            draft.FrameLimit = FrameLimits[(index + 1) % FrameLimits.Length];
            RefreshSettings(); Click();
        }
        private void ChangeResolution()
        {
            int index = resolutions.IndexOf(new Vector2Int(draft.Width, draft.Height));
            Vector2Int size = resolutions[(index + 1) % resolutions.Count];
            draft.Width = size.x; draft.Height = size.y;
            RefreshSettings(); Click();
        }
        private void ChangeMasterVolume(float value) { draft.MasterVolume = value; RefreshSettings(); }
        private void ChangeEffectsVolume(float value) { draft.EffectsVolume = value; RefreshSettings(); }
        private void ChangeMusicVolume(float value) { draft.MusicVolume = value; RefreshSettings(); }

        private void BuildResolutions()
        {
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 640 && size.y >= 360 && !resolutions.Contains(size)) resolutions.Add(size);
            }
            var current = new Vector2Int(saved.Width, saved.Height);
            if (!resolutions.Contains(current)) resolutions.Add(current);
            resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }
    }
}
