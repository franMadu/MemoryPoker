using UnityEngine;

namespace Game
{
    /// <summary>Copia editable de los ajustes; permite previsualizar y cancelar sin perder lo guardado.</summary>
    public sealed class GameSettingsData
    {
        public GameLanguage Language;
        public float MasterVolume = 1f;
        public float EffectsVolume = 1f;
        public float MusicVolume = 0.6f;
        public bool Fullscreen;
        public int Width;
        public int Height;
        public int Quality;
        public bool VSync;
        public int FrameLimit = 60;

        public GameSettingsData Copy() => (GameSettingsData)MemberwiseClone();
    }

    /// <summary>Guarda ajustes en PlayerPrefs y aplica idioma, audio y opciones gráficas.</summary>
    public static class GamePreferences
    {
        private const string Prefix = "MemoryPoker.Settings.";
        public static GameSettingsData Current { get; private set; } = new GameSettingsData();

        public static GameSettingsData Defaults() => new GameSettingsData
        {
            Language = Application.systemLanguage == SystemLanguage.French ? GameLanguage.French
                : Application.systemLanguage == SystemLanguage.English ? GameLanguage.English : GameLanguage.Spanish,
            Width = Mathf.Max(640, Screen.width),
            Height = Mathf.Max(360, Screen.height),
            Fullscreen = Screen.fullScreen,
            Quality = Mathf.Clamp(1, 0, Mathf.Max(0, QualitySettings.names.Length - 1))
        };

        public static void Load()
        {
            GameSettingsData data = Defaults();
            data.Language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Language", (int)data.Language), 0, 2);
            data.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Master", 1f));
            data.EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Effects", 1f));
            data.MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Music", 0.6f));
            data.Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", data.Fullscreen ? 1 : 0) != 0;
            data.Width = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Width", data.Width), 640, 16384);
            data.Height = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Height", data.Height), 360, 16384);
            data.Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", data.Quality), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            data.VSync = PlayerPrefs.GetInt(Prefix + "VSync", 0) != 0;
            int fps = PlayerPrefs.GetInt(Prefix + "FPS", 60);
            data.FrameLimit = fps == 0 || fps == 30 || fps == 60 || fps == 120 ? fps : 60;
            Preview(data);
            ApplyGraphics();
        }

        // Audio e idioma se previsualizan al instante; los cambios gráficos esperan a Aplicar.
        public static void Preview(GameSettingsData data)
        {
            Current = data.Copy();
            AudioListener.volume = Current.MasterVolume;
            GameLocalization.SetLanguage(Current.Language);
        }

        public static void Save(GameSettingsData data)
        {
            Preview(data);
            PlayerPrefs.SetInt(Prefix + "Language", (int)data.Language);
            PlayerPrefs.SetFloat(Prefix + "Master", data.MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "Effects", data.EffectsVolume);
            PlayerPrefs.SetFloat(Prefix + "Music", data.MusicVolume);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", data.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Width", data.Width);
            PlayerPrefs.SetInt(Prefix + "Height", data.Height);
            PlayerPrefs.SetInt(Prefix + "Quality", data.Quality);
            PlayerPrefs.SetInt(Prefix + "VSync", data.VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "FPS", data.FrameLimit);
            PlayerPrefs.Save();
            ApplyGraphics();
        }

        private static void ApplyGraphics()
        {
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(Current.Quality, true);
            QualitySettings.vSyncCount = Current.VSync ? 1 : 0;
            Application.targetFrameRate = Current.FrameLimit == 0 ? -1 : Current.FrameLimit;
            // Los ajustes de ventana solo afectan al juego compilado, nunca a la ventana del editor.
            if (!Application.isEditor)
                Screen.SetResolution(Current.Width, Current.Height,
                    Current.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
