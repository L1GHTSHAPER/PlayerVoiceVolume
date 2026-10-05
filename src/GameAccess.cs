using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerVoiceVolume
{
    /// <summary>
    /// Cached, exception-safe access to the game objects the mod reads.
    /// The game's singleton getters fall back to FindAnyObjectByType when the instance is missing
    /// (e.g. in the main menu), so lookups of missing instances are throttled.
    /// </summary>
    internal static class GameAccess
    {
        const float LookupInterval = 1f;

        static PlayerPanelController _players;
        static TextChannelManager _chat;
        static UIManager _ui;
        static float _nextPlayersLookup;
        static float _nextChatLookup;
        static float _nextUiLookup;

        /// <summary>The Tab player list; also the game's registry of who is in the lobby.</summary>
        public static PlayerPanelController PlayerPanel
        {
            get
            {
                if (_players == null && Time.unscaledTime >= _nextPlayersLookup)
                {
                    _nextPlayersLookup = Time.unscaledTime + LookupInterval;
                    _players = NetworkSingleton<PlayerPanelController>.I;
                }
                return _players;
            }
            set => _players = value;
        }

        public static TextChannelManager Chat
        {
            get
            {
                if (_chat == null && Time.unscaledTime >= _nextChatLookup)
                {
                    _nextChatLookup = Time.unscaledTime + LookupInterval;
                    _chat = NetworkSingleton<TextChannelManager>.I;
                }
                return _chat;
            }
        }

        public static UIManager UI
        {
            get
            {
                if (_ui == null && Time.unscaledTime >= _nextUiLookup)
                {
                    _nextUiLookup = Time.unscaledTime + LookupInterval;
                    _ui = MonoSingleton<UIManager>.I;
                }
                return _ui;
            }
        }

        /// <summary>True for your own entry in the player list.</summary>
        public static bool IsLocalPlayer(PlayerPanelController players, int index)
        {
            TextChannelManager chat = Chat;
            if (chat == null || chat.MainNetTransform == null)
                return false;
            return index < players.PlayerTransforms.Count && players.PlayerTransforms[index] == chat.MainNetTransform;
        }

        /// <summary>The name the game shows in the player list (without the host tag).</summary>
        public static string RawPlayerName(PlayerPanelController players, int index)
        {
            PlayerController controller = index < players.PlayerControllers.Count ? players.PlayerControllers[index] : null;
            TMP_Text text = controller != null ? controller.PlayerNameText : null;
            return text != null ? text.text : null;
        }

        /// <summary>Runs a name through the game's bad word filter, as the player list does.</summary>
        public static string FilterName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            try
            {
                BadWordFilterManager filter = MonoSingleton<BadWordFilterManager>.I;
                return filter != null ? filter.FilterString(name) : name;
            }
            catch (Exception)
            {
                return name;
            }
        }

        /// <summary>Same states and priority as the voice icon in the game's player list.</summary>
        public static VoiceStatus VoiceStatusOf(PlayerPanelController players, string steamId)
        {
            int index = players.PlayerSteamIDs.IndexOf(steamId);
            if (index < 0)
                return VoiceStatus.Inactive;

            DataManager data = MonoSingleton<DataManager>.I;
            List<string> muted = data != null && data.BanData != null ? data.BanData.VoiceMutedPlayers : null;
            if (muted != null && muted.Contains(steamId))
                return VoiceStatus.Banned;

            if (index >= players.IsVoiceChatActive.Count || !players.IsVoiceChatActive[index])
                return VoiceStatus.Inactive;

            PlayerController controller = index < players.PlayerControllers.Count ? players.PlayerControllers[index] : null;
            PlayerVoiceController voice = controller != null ? controller.PlayerVoice : null;
            return voice != null && voice.IsSpeaking ? VoiceStatus.Speaking : VoiceStatus.NotSpeaking;
        }

        public static void PlayUIChange()
        {
            try
            {
                UIManager ui = UI;
                if (ui != null)
                    ui.PlayUIChange();
            }
            catch (Exception)
            {
                // A missing sound is not worth an error.
            }
        }

        public static void PlayUIClick()
        {
            try
            {
                UIManager ui = UI;
                if (ui != null)
                    ui.PlayUIClick();
            }
            catch (Exception)
            {
                // A missing sound is not worth an error.
            }
        }
    }

    /// <summary>Accessors for private fields of the game's player list and settings screens.</summary>
    internal static class GameRefs
    {
        // PlayerPanelController: the Tab player list.
        public static readonly AccessTools.FieldRef<PlayerPanelController, GameObject> ServerList = Field<PlayerPanelController, GameObject>("_serverPanel");
        public static readonly AccessTools.FieldRef<PlayerPanelController, GameObject> BannedList = Field<PlayerPanelController, GameObject>("_bannedPanel");
        public static readonly AccessTools.FieldRef<PlayerPanelController, RectTransform> ServerButton = Field<PlayerPanelController, RectTransform>("_serverButtonTransform");
        public static readonly AccessTools.FieldRef<PlayerPanelController, RectTransform> BannedButton = Field<PlayerPanelController, RectTransform>("_bannedButtonTransform");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Color> SelectedColor = Field<PlayerPanelController, Color>("_selectedColor");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Color> ServerColor = Field<PlayerPanelController, Color>("_serverColor");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Color> BannedColor = Field<PlayerPanelController, Color>("_bannedColor");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Color> GreenColor = Field<PlayerPanelController, Color>("_greenColor");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Color> OrangeColor = Field<PlayerPanelController, Color>("_orangeColor");
        public static readonly AccessTools.FieldRef<PlayerPanelController, PlayerPanelType> PanelType = Field<PlayerPanelController, PlayerPanelType>("_playerPanelType");
        public static readonly AccessTools.FieldRef<PlayerPanelController, GameObject> ItemPrefab = Field<PlayerPanelController, GameObject>("_playerItemPrefab");
        public static readonly AccessTools.FieldRef<PlayerPanelController, List<PlayerItemController>> Items = Field<PlayerPanelController, List<PlayerItemController>>("_playerItemControllers");
        public static readonly AccessTools.FieldRef<PlayerPanelController, RectTransform> InfoPanel = Field<PlayerPanelController, RectTransform>("_buttonInfoPanelTransfrom");
        public static readonly AccessTools.FieldRef<PlayerPanelController, CanvasGroup> InfoGroup = Field<PlayerPanelController, CanvasGroup>("_buttonInfoPanelCanvasGroup");
        public static readonly AccessTools.FieldRef<PlayerPanelController, Image> InfoHeaderBg = Field<PlayerPanelController, Image>("_buttonInfoHeaderBgImage");
        public static readonly AccessTools.FieldRef<PlayerPanelController, TextMeshProUGUI> InfoHeader = Field<PlayerPanelController, TextMeshProUGUI>("_buttonInfoHeaderText");
        public static readonly AccessTools.FieldRef<PlayerPanelController, TextMeshProUGUI> InfoText = Field<PlayerPanelController, TextMeshProUGUI>("_buttonInfoText");

        // PlayerItemController: one row of the Server list (and the prefab our rows are made from).
        public static readonly AccessTools.FieldRef<PlayerItemController, string> ItemSteamId = Field<PlayerItemController, string>("_playerSteamId");
        public static readonly AccessTools.FieldRef<PlayerItemController, TextMeshProUGUI> ItemName = Field<PlayerItemController, TextMeshProUGUI>("_playerNameText");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemReport = Field<PlayerItemController, Button>("_reportButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemBan = Field<PlayerItemController, Button>("_banButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemSteam = Field<PlayerItemController, Button>("_steamButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemMute = Field<PlayerItemController, Button>("_muteButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemIgnore = Field<PlayerItemController, Button>("_ignoreButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, Button> ItemVoiceMute = Field<PlayerItemController, Button>("_voiceMuteButton");
        public static readonly AccessTools.FieldRef<PlayerItemController, GameObject> ItemSync = Field<PlayerItemController, GameObject>("_syncButtonObject");
        public static readonly AccessTools.FieldRef<PlayerItemController, GameObject> ItemSyncedWithMe = Field<PlayerItemController, GameObject>("_syncWithMeObject");
        public static readonly AccessTools.FieldRef<PlayerItemController, Image> ItemVoiceIcon = Field<PlayerItemController, Image>("_voiceMuteImage");
        public static readonly AccessTools.FieldRef<PlayerItemController, Image> ItemVoiceBg = Field<PlayerItemController, Image>("_voiceMuteBgImage");
        public static readonly AccessTools.FieldRef<PlayerItemController, Sprite> ItemMicOpen = Field<PlayerItemController, Sprite>("_voiceChatOpen");
        public static readonly AccessTools.FieldRef<PlayerItemController, Sprite> ItemMicMuted = Field<PlayerItemController, Sprite>("_voiceChatMute");

        // SettingsController: the Voice Volume slider is the template for ours.
        public static readonly AccessTools.FieldRef<SettingsController, Slider> SettingsVoiceSlider = Field<SettingsController, Slider>("_voiceVolumeSlider");

        public static TField Get<TObject, TField>(AccessTools.FieldRef<TObject, TField> field, TObject instance)
            where TObject : class
        {
            return field != null && instance != null ? field(instance) : default;
        }

        static AccessTools.FieldRef<TObject, TField> Field<TObject, TField>(string name)
        {
            try
            {
                return AccessTools.FieldRefAccess<TObject, TField>(name);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Game field {typeof(TObject).Name}.{name} not found ({e.Message}); some UI may be missing.");
                return null;
            }
        }
    }
}
