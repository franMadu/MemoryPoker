using System;
using Game;
using UnityEngine;

public static class SettingsTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    { assertions++; if (!condition) throw new Exception(message); }

    public static void Main()
    {
        foreach (GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
        {
            GameLocalization.SetLanguage(language);
            foreach (string key in GameLocalization.Keys)
            {
                string translation = GameLocalization.Text(key, 1, "Full", "A + B");
                Check(!string.IsNullOrWhiteSpace(translation) && translation != key, "Missing translation: " + language + "/" + key);
            }
            Check(!string.IsNullOrWhiteSpace(GameLocalization.LanguageName(language)), "Missing language name.");
        }
        GameLocalization.SetLanguage(GameLanguage.Spanish);
        Check(GameLocalization.Text("place", 42, 120).Contains("42/120"), "Rank formatting failed.");
        Check(GameLocalization.Text("yourHand", "Full").Contains("Full"), "Hand formatting failed.");
        int languageEvents = 0;
        Action listener = () => languageEvents++;
        GameLocalization.Changed += listener;
        GameLocalization.SetLanguage(GameLanguage.English);
        GameLocalization.SetLanguage(GameLanguage.English);
        Check(languageEvents == 1, "Language event fired without a language change.");
        GameLocalization.Changed -= listener;
        GameLocalization.SetLanguage((GameLanguage)99);
        Check(GameLocalization.Language == GameLanguage.Spanish, "Invalid language fallback failed.");

        PlayerPrefs.Values.Clear();
        GamePreferences.Load();
        Check(GamePreferences.Current.MasterVolume == 1f && GamePreferences.Current.EffectsVolume == 1f, "Incorrect default volume.");
        Check(GamePreferences.Current.MusicVolume == 0.6f, "Missing music preference should default to a quiet level.");
        Check(Application.targetFrameRate == 60 && QualitySettings.Level == 1, "Default graphics not applied.");
        Check(Screen.SetResolutionCalls == 0, "Loading settings resized the editor.");
        var saved = GamePreferences.Current.Copy();
        var draft = saved.Copy();
        draft.Language = GameLanguage.English;
        draft.MasterVolume = 0.3f; draft.EffectsVolume = 0.6f;
        draft.MusicVolume = 0.2f;
        draft.Fullscreen = true; draft.Width = 1280; draft.Height = 720;
        draft.Quality = 0; draft.VSync = true; draft.FrameLimit = 120;
        GamePreferences.Preview(draft);
        Check(AudioListener.volume == 0.3f && GameLocalization.Language == GameLanguage.English, "Audio/language preview failed.");
        Check(GamePreferences.Current.MusicVolume == 0.2f, "Music volume preview failed.");
        Check(PlayerPrefs.Saves == 0 && Screen.SetResolutionCalls == 0, "Preview saved or changed display mode.");
        Check(saved.MasterVolume == 1f, "Draft mutated saved preferences.");
        GamePreferences.Preview(saved);
        Check(AudioListener.volume == 1f && GameLocalization.Language == saved.Language, "Cancel failed to restore preview.");
        Check(GamePreferences.Current.MusicVolume == 0.6f, "Cancel failed to restore music volume.");
        GamePreferences.Save(draft);
        Check(PlayerPrefs.Saves == 1, "Apply did not save preferences.");
        Check(Screen.SetResolutionCalls == 0, "Apply resized the editor.");
        Check(QualitySettings.Level == 0 && QualitySettings.vSyncCount == 1 && Application.targetFrameRate == 120, "Graphics settings not applied.");
        draft.MasterVolume = 0.9f;
        Check(GamePreferences.Current.MasterVolume == 0.3f, "Settings retain mutable draft reference.");
        GamePreferences.Preview(saved);
        GamePreferences.Load();
        Check(GamePreferences.Current.MasterVolume == 0.3f && GamePreferences.Current.EffectsVolume == 0.6f, "Volume did not survive reload.");
        Check(GamePreferences.Current.MusicVolume == 0.2f, "Music volume did not survive reload.");
        Check(GamePreferences.Current.Language == GameLanguage.English && GamePreferences.Current.Fullscreen && GamePreferences.Current.Width == 1280 && GamePreferences.Current.Height == 720, "Language/display did not survive reload.");

        PlayerPrefs.SetInt("MemoryPoker.Settings.Language", 99);
        PlayerPrefs.SetFloat("MemoryPoker.Settings.Master", 2f);
        PlayerPrefs.SetFloat("MemoryPoker.Settings.Effects", -1f);
        PlayerPrefs.SetFloat("MemoryPoker.Settings.Music", -4f);
        PlayerPrefs.SetInt("MemoryPoker.Settings.Width", -1);
        PlayerPrefs.SetInt("MemoryPoker.Settings.Height", 99999);
        PlayerPrefs.SetInt("MemoryPoker.Settings.Quality", 99);
        PlayerPrefs.SetInt("MemoryPoker.Settings.FPS", 17);
        GamePreferences.Load();
        Check((int)GamePreferences.Current.Language <= 2 && GamePreferences.Current.MasterVolume == 1f && GamePreferences.Current.EffectsVolume == 0f, "Corrupt language/volume not sanitized.");
        Check(GamePreferences.Current.MusicVolume == 0f, "Negative music volume not sanitized.");
        PlayerPrefs.SetFloat("MemoryPoker.Settings.Music", 8f);
        GamePreferences.Load();
        Check(GamePreferences.Current.MusicVolume == 1f, "Excessive music volume not sanitized.");
        Check(GamePreferences.Current.Width == 640 && GamePreferences.Current.Height == 16384 && GamePreferences.Current.Quality == 1 && GamePreferences.Current.FrameLimit == 60, "Corrupt graphics settings not sanitized.");
        Application.isEditor = false;
        draft.Width = 1280; draft.Height = 720; draft.MasterVolume = 0.3f;
        GamePreferences.Save(draft);
        Check(Screen.SetResolutionCalls == 1 && Screen.Mode == FullScreenMode.FullScreenWindow && Screen.width == 1280 && Screen.height == 720, "Standalone display settings not applied.");
        draft.Fullscreen = false; draft.FrameLimit = 0; draft.VSync = false;
        GamePreferences.Save(draft);
        Check(Screen.Mode == FullScreenMode.Windowed && Application.targetFrameRate == -1 && QualitySettings.vSyncCount == 0, "Windowed/unlimited settings failed.");
        foreach (SystemLanguage language in Enum.GetValues(typeof(SystemLanguage)))
        {
            Application.systemLanguage = language;
            GameLanguage expected = language == SystemLanguage.English ? GameLanguage.English : language == SystemLanguage.French ? GameLanguage.French : GameLanguage.Spanish;
            Check(GamePreferences.Defaults().Language == expected, "Default system language selection failed.");
        }
        Console.WriteLine("PASS: " + assertions + " settings/localization checks; no Unity editor or real display/audio APIs used.");
    }
}
