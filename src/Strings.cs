using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace PlayerVoiceVolume
{
    /// <summary>UI texts in the game's current language (English for languages without a translation).</summary>
    internal static class Strings
    {
        static string _language = "en";

        public static string Tab => Pick("Voice", ru: "Голос", fr: "Voix", es: "Voz", pt: "Voz", ja: "ボイス", zh: "语音");

        public static string Hint => Pick("only for you", ru: "только для вас", fr: "pour vous seulement",
            es: "solo para ti", pt: "só para você", ja: "自分にだけ反映", zh: "仅对你生效");

        public static string NoPlayers => Pick("No other players here yet.", ru: "Других игроков пока нет.",
            fr: "Aucun autre joueur ici pour l'instant.", es: "Todavía no hay otros jugadores aquí.",
            pt: "Ainda não há outros jogadores aqui.", ja: "まだ他のプレイヤーはいません。", zh: "这里还没有其他玩家。");

        public static string ResetTitle => Pick("Reset Volume", ru: "Сбросить громкость", fr: "Réinitialiser le volume",
            es: "Restablecer volumen", pt: "Redefinir volume", ja: "音量をリセット", zh: "重置音量");

        public static string ResetInfo => Pick("Set this player's volume back to {0}.",
            ru: "Вернуть громкость этого игрока к {0}.", fr: "Remettre le volume de ce joueur à {0}.",
            es: "Devuelve el volumen de este jugador a {0}.", pt: "Volta o volume deste jogador para {0}.",
            ja: "このプレイヤーの音量を{0}に戻します。", zh: "将该玩家的音量恢复为{0}。");

        /// <summary>The game's own "Voice Volume" label, so the header matches the settings screen.</summary>
        public static string Header => GameString("VoiceVolume") ?? Pick("Voice Volume", ru: "Громкость голоса",
            fr: "Volume de la voix", es: "Volumen de voz", pt: "Volume da voz", ja: "ボイス音量", zh: "语音音量");

        /// <summary>Reads the language the game is set to. Call before building texts.</summary>
        public static void Refresh()
        {
            try
            {
                _language = CurrentLanguage() ?? "en";
            }
            catch (Exception)
            {
                _language = "en";
            }
        }

        static string CurrentLanguage()
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : null;
            if (string.IsNullOrEmpty(code))
                return null;
            int dash = code.IndexOfAny(new[] { '-', '_' });
            return (dash > 0 ? code.Substring(0, dash) : code).ToLowerInvariant();
        }

        static string GameString(string key)
        {
            try
            {
                string text = LocalizationSettings.StringDatabase.GetLocalizedString("General", key);
                // A missing entry comes back as a "No translation found" message rather than null.
                if (string.IsNullOrEmpty(text) || text.StartsWith("No translation", StringComparison.OrdinalIgnoreCase))
                    return null;
                return text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string Pick(string en, string ru, string fr, string es, string pt, string ja, string zh)
        {
            switch (_language)
            {
                case "ru": return ru;
                case "fr": return fr;
                case "es": return es;
                case "pt": return pt;
                case "ja": return ja;
                case "zh": return zh;
                default: return en;
            }
        }
    }
}
