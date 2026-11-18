using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game
{
    public enum GameLanguage { Spanish, English, French }

    /// <summary>Catálogo de textos en español, inglés y francés; avisa a las etiquetas al cambiar el idioma.</summary>
    public static class GameLocalization
    {
        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>
        {
            { "play", new[] { "Jugar", "Play", "Jouer" } },
            { "settings", new[] { "Configuración", "Settings", "Paramètres" } },
            { "language", new[] { "Idioma", "Language", "Langue" } },
            { "musicVolume", new[] { "Música de fondo", "Background music", "Musique de fond" } },
            { "masterVolume", new[] { "Volumen general", "Master volume", "Volume général" } },
            { "effectsVolume", new[] { "Efectos de sonido", "Sound effects", "Effets sonores" } },
            { "screenMode", new[] { "Modo de pantalla", "Display mode", "Mode d'affichage" } },
            { "fullscreen", new[] { "Pantalla completa", "Fullscreen", "Plein écran" } },
            { "windowed", new[] { "Ventana", "Windowed", "Fenêtre" } },
            { "resolution", new[] { "Resolución", "Resolution", "Résolution" } },
            { "quality", new[] { "Calidad gráfica", "Graphics quality", "Qualité graphique" } },
            { "quality.Mobile", new[] { "Baja", "Low", "Basse" } },
            { "quality.PC", new[] { "Alta", "High", "Haute" } },
            { "vsync", new[] { "Sincronización V", "VSync", "Synchronisation V" } },
            { "frameLimit", new[] { "Límite de FPS", "Frame rate limit", "Limite d'images/s" } },
            { "unlimited", new[] { "Sin límite", "Unlimited", "Illimité" } },
            { "on", new[] { "Activado", "On", "Activé" } },
            { "off", new[] { "Desactivado", "Off", "Désactivé" } },
            { "defaults", new[] { "Restablecer ajustes", "Restore defaults", "Rétablir les paramètres" } },
            { "apply", new[] { "Aplicar", "Apply", "Appliquer" } },
            { "back", new[] { "Volver", "Back", "Retour" } },
            { "menu", new[] { "Menú principal", "Main menu", "Menu principal" } },
            { "memorize", new[] { "MEMORIZAR", "MEMORIZE", "MÉMORISER" } },
            { "select", new[] { "SELECCIONAR", "SELECT", "CHOISIR" } },
            { "results", new[] { "RESULTADOS", "RESULTS", "RÉSULTATS" } },
            { "newRound", new[] { "Nueva ronda", "New round", "Nouvelle manche" } },
            { "ready", new[] { "PREPÁRATE", "GET READY", "PRÉPAREZ-VOUS" } },
            { "dealing", new[] { "Repartiendo cartas en...", "Dealing cards in...", "Distribution dans..." } },
            { "topHand", new[] { "TOP {0} - {1} ({2})", "TOP {0} - {1} ({2})", "TOP {0} - {1} ({2})" } },
            { "unavailable", new[] { "TOP {0} - No disponible", "TOP {0} - Unavailable", "TOP {0} - Indisponible" } },
            { "yourHand", new[] { "TU MANO - {0}", "YOUR HAND - {0}", "VOTRE MAIN - {0}" } },
            { "incomplete", new[] { "Incompleta", "Incomplete", "Incomplète" } },
            { "place", new[] { " - Puesto {0}/{1}", " - Place {0}/{1}", " - Rang {0}/{1}" } },
            { "hand.royal", new[] { "Escalera real", "Royal flush", "Quinte flush royale" } },
            { "hand.straightFlush", new[] { "Escalera de color", "Straight flush", "Quinte flush" } },
            { "hand.four", new[] { "Póker", "Four of a kind", "Carré" } },
            { "hand.full", new[] { "Full", "Full house", "Full" } },
            { "hand.flush", new[] { "Color", "Flush", "Couleur" } },
            { "hand.straight", new[] { "Escalera", "Straight", "Quinte" } },
            { "hand.three", new[] { "Trío", "Three of a kind", "Brelan" } },
            { "hand.twoPair", new[] { "Doble pareja", "Two pair", "Deux paires" } },
            { "hand.pair", new[] { "Pareja", "One pair", "Paire" } },
            { "hand.high", new[] { "Carta alta", "High card", "Carte haute" } }
        };

        public static GameLanguage Language { get; private set; } = GameLanguage.Spanish;
        public static event Action Changed;
        public static IReadOnlyCollection<string> Keys => Translations.Keys;

        public static void SetLanguage(GameLanguage language)
        {
            if (!Enum.IsDefined(typeof(GameLanguage), language)) language = GameLanguage.Spanish;
            if (Language == language) return;
            Language = language;
            Changed?.Invoke();
        }

        public static string Text(string key, params object[] arguments)
        {
            string text = Translations.TryGetValue(key, out var values) ? values[(int)Language] : key;
            return arguments.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, arguments);
        }

        public static string LanguageName(GameLanguage language) => language switch
        {
            GameLanguage.English => "English",
            GameLanguage.French => "Français",
            _ => "Español"
        };
    }
}
