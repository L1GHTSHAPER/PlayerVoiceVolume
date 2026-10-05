using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlayerVoiceVolume
{
    /// <summary>
    /// The "Voice" tab of the player list (Tab key), next to the game's Server and Banned tabs.
    /// The tab button is a copy of the Banned button and the list a copy of the Server list, so both inherit the
    /// game's look. This component lives on the list and updates the rows while the tab is open.
    /// </summary>
    internal sealed class VoiceTab : MonoBehaviour
    {
        // The game's tab heights and animation time (PlayerPanelController.ButtonServer/ButtonBanned).
        const float SelectedHeight = 107.2f;
        const float UnselectedHeight = 97.2f;
        const float TweenTime = 0.2f;
        const float ListSyncInterval = 0.5f;

        static readonly Color FallbackSelected = new Color(0.961f, 0.929f, 0.882f);
        static readonly Color FallbackServer = new Color(0.906f, 0.663f, 0.4f);
        static readonly Color FallbackBanned = new Color(0.906f, 0.502f, 0.427f);
        static readonly Color FallbackGreen = new Color(0.702f, 0.82f, 0.498f);

        static PlayerPanelController _failedFor;

        internal static VoiceTab Current { get; private set; }

        PlayerPanelController _players;
        RectTransform _tabButton;
        TMP_Text _tabLabel;
        Transform _background;
        ScrollRect _scroll;
        TMP_Text _header;
        TMP_Text _empty;
        GameObject _rowPrefab;
        Slider _sliderTemplate;
        Sprite _panelSprite;
        readonly List<VoiceRow> _rows = new List<VoiceRow>();
        bool _selected;
        bool _rowsFailed;
        float _nextSync;
        string _lastError;

        internal PlayerPanelController Panel => _players;

        /// <summary>Adds the tab to this player list once. Safe to call repeatedly.</summary>
        internal static void EnsureInjected(PlayerPanelController players)
        {
            if (players == null || players == _failedFor)
                return;
            if (Current != null && Current._players == players)
                return;
            try
            {
                Inject(players);
            }
            catch (Exception e)
            {
                _failedFor = players;
                Plugin.Log.LogError("Could not add the Voice tab to the player list (volumes still apply): " + e);
            }
        }

        static void Inject(PlayerPanelController players)
        {
            GameObject serverList = GameRefs.Get(GameRefs.ServerList, players);
            RectTransform serverButton = GameRefs.Get(GameRefs.ServerButton, players);
            RectTransform bannedButton = GameRefs.Get(GameRefs.BannedButton, players);
            GameObject rowPrefab = GameRefs.Get(GameRefs.ItemPrefab, players);
            if (serverList == null || serverButton == null || bannedButton == null || rowPrefab == null)
                throw new InvalidOperationException("the player list layout has changed");
            // The tabs sit on the panel background: the selected one in front of it, the others behind.
            Transform background = serverList.transform.parent;
            if (background == null || background.parent != bannedButton.parent)
                throw new InvalidOperationException("the player list layout has changed");

            Strings.Refresh();

            // Tab button: one step to the right of Banned, as far as Banned is from Server.
            GameObject tab = Object.Instantiate(bannedButton.gameObject, bannedButton.parent, false);
            tab.name = "Button_Voice";
            UiUtil.RemoveLocalization(tab);
            var tabRect = (RectTransform)tab.transform;
            tabRect.anchoredPosition = bannedButton.anchoredPosition + (bannedButton.anchoredPosition - serverButton.anchoredPosition);
            tabRect.SetSiblingIndex(background.GetSiblingIndex());

            // List: the Server list without its player rows.
            GameObject list = Object.Instantiate(serverList, background, false);
            list.name = "Scrollview_Voice";
            list.SetActive(false);
            UiUtil.RemoveLocalization(list);
            ScrollRect scroll = list.GetComponent<ScrollRect>();
            if (scroll == null || scroll.content == null)
            {
                Object.Destroy(tab);
                Object.Destroy(list);
                throw new InvalidOperationException("the Server list has no scroll view");
            }
            RectTransform content = scroll.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                if (child.GetComponent<PlayerItemController>() != null)
                    Object.DestroyImmediate(child);
            }

            VoiceTab view = list.AddComponent<VoiceTab>();
            view._players = players;
            view._tabButton = tabRect;
            view._tabLabel = tab.GetComponentInChildren<TMP_Text>(true);
            view._background = background;
            view._scroll = scroll;
            view._rowPrefab = rowPrefab;
            view._sliderTemplate = GameRefs.Get(GameRefs.SettingsVoiceSlider, MonoSingleton<SettingsController>.I);
            Image backgroundImage = background.GetComponent<Image>();
            view._panelSprite = backgroundImage != null ? backgroundImage.sprite : null;

            // The first text of the Server list is the lobby name; ours shows the title instead.
            foreach (Transform child in content)
            {
                view._header = child.GetComponent<TMP_Text>();
                if (view._header != null)
                    break;
            }
            if (view._header != null)
            {
                view._empty = Object.Instantiate(view._header.gameObject, content, false).GetComponent<TMP_Text>();
                view._empty.name = "Text_NoPlayers";
                view._empty.fontSize = view._header.fontSize * 0.85f;
                view._empty.color = players.IconActiveColor;
                view._empty.gameObject.SetActive(false);
            }

            Button tabButton = tab.GetComponent<Button>();
            UiUtil.SetOnClick(tabButton, view.OnTabClicked);
            UiUtil.DisableNavigation(tabButton);
            view.RefreshTexts();
            view.ShowTabAsSelected(false, false);

            Current = view;
            _failedFor = null;
            Plugin.Log.LogInfo("Added the Voice tab to the player list.");
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
            if (_tabButton != null)
                Destroy(_tabButton.gameObject);
            if (Plugin.Instance != null)
                Plugin.Instance.Store.Save();
        }

        void OnTabClicked()
        {
            if (_selected)
                return;
            GameAccess.PlayUIChange();
            Select();
        }

        void Select()
        {
            _selected = true;
            // Lets the game's own tabs switch back: ButtonServer/ButtonBanned do nothing for the current type.
            if (GameRefs.PanelType != null)
                GameRefs.PanelType(_players) = PlayerPanelType.None;

            GameObject serverList = GameRefs.Get(GameRefs.ServerList, _players);
            GameObject bannedList = GameRefs.Get(GameRefs.BannedList, _players);
            if (serverList != null)
                serverList.SetActive(false);
            if (bannedList != null)
                bannedList.SetActive(false);

            RectTransform serverButton = GameRefs.Get(GameRefs.ServerButton, _players);
            RectTransform bannedButton = GameRefs.Get(GameRefs.BannedButton, _players);
            Animate(serverButton, ColorOf(GameRefs.ServerColor, FallbackServer), UnselectedHeight, true);
            Animate(bannedButton, ColorOf(GameRefs.BannedColor, FallbackBanned), UnselectedHeight, true);
            ShowTabAsSelected(true, true);
            PutInFront(_tabButton, serverButton, bannedButton);

            gameObject.SetActive(true);
            RefreshTexts();
            SyncRows(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Called after the game switched to its Server or Banned tab (also when the list is opened).</summary>
        internal void OnGameTabSelected(RectTransform selectedButton)
        {
            bool wasSelected = _selected;
            _selected = false;
            gameObject.SetActive(false);
            ShowTabAsSelected(false, wasSelected);

            RectTransform serverButton = GameRefs.Get(GameRefs.ServerButton, _players);
            RectTransform bannedButton = GameRefs.Get(GameRefs.BannedButton, _players);
            RectTransform otherButton = selectedButton == serverButton ? bannedButton : serverButton;
            PutInFront(selectedButton, otherButton, _tabButton);

            if (!wasSelected)
                RefreshTexts();
        }

        void ShowTabAsSelected(bool selected, bool animate)
        {
            Color color = selected ? ColorOf(GameRefs.SelectedColor, FallbackSelected) : ColorOf(GameRefs.GreenColor, FallbackGreen);
            Animate(_tabButton, color, selected ? SelectedHeight : UnselectedHeight, animate);
        }

        Color ColorOf(HarmonyLib.AccessTools.FieldRef<PlayerPanelController, Color> field, Color fallback)
        {
            if (field == null || _players == null)
                return fallback;
            Color color = field(_players);
            return color.a > 0f ? color : fallback;
        }

        static void Animate(RectTransform button, Color color, float height, bool animate)
        {
            if (button == null)
                return;
            Image image = button.GetComponent<Image>();
            var size = new Vector2(button.sizeDelta.x, height);
            button.DOKill();
            if (image != null)
                image.DOKill();
            if (!animate)
            {
                if (image != null)
                    image.color = color;
                button.sizeDelta = size;
                return;
            }
            if (image != null)
                image.DOColor(color, TweenTime);
            button.DOSizeDelta(size, TweenTime);
        }

        /// <summary>
        /// Draws the selected tab over the panel background and the others under it. The game does this with
        /// fixed sibling indices that do not account for a third tab, so the order is fixed up afterwards.
        /// </summary>
        void PutInFront(RectTransform selected, params RectTransform[] others)
        {
            if (_background == null)
                return;
            foreach (RectTransform other in others)
            {
                if (other != null && other.parent == _background.parent && other.GetSiblingIndex() > _background.GetSiblingIndex())
                    other.SetSiblingIndex(_background.GetSiblingIndex());
            }
            if (selected != null && selected.parent == _background.parent && selected.GetSiblingIndex() < _background.GetSiblingIndex())
                selected.SetSiblingIndex(_background.GetSiblingIndex());
        }

        void RefreshTexts()
        {
            Strings.Refresh();
            if (_tabLabel != null)
                _tabLabel.text = Strings.Tab;
            if (_header != null)
                _header.text = $"{Strings.Header}  <size=70%><color=#{ColorUtility.ToHtmlStringRGB(_players.IconActiveColor)}>· {Strings.Hint}</color></size>";
            if (_empty != null)
                _empty.text = Strings.NoPlayers;
        }

        /// <summary>Moves every slider to the saved value again (after a config change).</summary>
        internal void RefreshVolumes()
        {
            if (_selected)
                SyncRows(true);
        }

        internal void MarkPlayersChanged()
        {
            _nextSync = 0f;
        }

        void Update()
        {
            try
            {
                if (Time.unscaledTime >= _nextSync)
                    SyncRows(false);
                for (int i = 0; i < _rows.Count; i++)
                {
                    VoiceRow row = _rows[i];
                    if (row != null && row.gameObject.activeSelf)
                        row.RefreshStatus();
                }
            }
            catch (Exception e)
            {
                // Log each distinct error once instead of every frame.
                string message = e.GetType().Name + ": " + e.Message;
                if (message != _lastError)
                {
                    _lastError = message;
                    Plugin.Log.LogError("Voice tab update failed: " + e);
                }
            }
        }

        /// <summary>One row per other player, in the order of the Server list.</summary>
        void SyncRows(bool force)
        {
            _nextSync = Time.unscaledTime + ListSyncInterval;
            PlayerPanelController players = _players;
            if (players == null || players.PlayerSteamIDs == null)
                return;

            int shown = 0;
            for (int i = 0; i < players.PlayerSteamIDs.Count; i++)
            {
                string steamId = players.PlayerSteamIDs[i];
                if (string.IsNullOrEmpty(steamId) || GameAccess.IsLocalPlayer(players, i))
                    continue;
                VoiceRow row = shown < _rows.Count ? _rows[shown] : AddRow();
                if (row == null)
                    break;
                if (!row.gameObject.activeSelf)
                    row.gameObject.SetActive(true);
                row.Bind(steamId, GameAccess.RawPlayerName(players, i), force);
                shown++;
            }
            for (int i = shown; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].gameObject.activeSelf)
                    _rows[i].gameObject.SetActive(false);
            }
            if (_empty != null && _empty.gameObject.activeSelf != (shown == 0))
                _empty.gameObject.SetActive(shown == 0);
        }

        VoiceRow AddRow()
        {
            if (_rowsFailed)
                return null;
            try
            {
                VoiceRow row = VoiceRow.Create(this, _rowPrefab, _scroll.content, _sliderTemplate, _panelSprite);
                _rows.Add(row);
                return row;
            }
            catch (Exception e)
            {
                _rowsFailed = true;
                Plugin.Log.LogError("Could not create a player row in the Voice tab: " + e);
                return null;
            }
        }

        /// <summary>Shows the game's info popup next to one of our controls.</summary>
        internal void ShowInfo(RectTransform anchor, string title, string text)
        {
            RectTransform popup = GameRefs.Get(GameRefs.InfoPanel, _players);
            CanvasGroup group = GameRefs.Get(GameRefs.InfoGroup, _players);
            if (popup == null || group == null || anchor == null)
                return;
            Image headerBackground = GameRefs.Get(GameRefs.InfoHeaderBg, _players);
            TextMeshProUGUI header = GameRefs.Get(GameRefs.InfoHeader, _players);
            TextMeshProUGUI info = GameRefs.Get(GameRefs.InfoText, _players);
            if (headerBackground != null)
                headerBackground.color = ColorOf(GameRefs.OrangeColor, FallbackServer);
            if (header != null)
                header.text = title;
            if (info != null)
                info.text = text;
            popup.position = anchor.position;
            group.DOFade(1f, 0.15f);
        }
    }
}
