// Standalone logic tests only. This directory is outside Assets and never loads in Unity.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public enum SystemLanguage { Spanish, English, French }
    public enum FullScreenMode { FullScreenWindow, Windowed }
    public static class Application
    {
        public static SystemLanguage systemLanguage = SystemLanguage.Spanish;
        public static bool isEditor = true;
        public static int targetFrameRate;
    }
    public static class Screen
    {
        public static int width = 1920, height = 1080;
        public static bool fullScreen;
        public static int SetResolutionCalls;
        public static FullScreenMode Mode;
        public static void SetResolution(int w, int h, FullScreenMode mode)
        { width = w; height = h; Mode = mode; SetResolutionCalls++; }
    }
    public static class QualitySettings
    {
        public static string[] names = { "Mobile", "PC" };
        public static int Level, vSyncCount;
        public static void SetQualityLevel(int level, bool expensive) { Level = level; }
    }
    public static class AudioListener { public static float volume; }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public static int Saves;
        public static int GetInt(string key, int fallback) => Values.TryGetValue(key, out var value) ? (int)value : fallback;
        public static float GetFloat(string key, float fallback) => Values.TryGetValue(key, out var value) ? (float)value : fallback;
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void SetFloat(string key, float value) => Values[key] = value;
        public static void Save() { Saves++; }
    }
}
