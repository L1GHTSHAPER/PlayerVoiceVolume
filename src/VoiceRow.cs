using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlayerVoiceVolume
{
    /// <summary>
    /// One player in the Voice tab: the game's mic button, the name, a volume slider, the value and a reset button.
    /// Built from the game's own player row prefab, so it looks like the rows of the Server tab.
    /// </summary>
    internal sealed class VoiceRow : MonoBehaviour
    {
        // Horizontal layout inside the 1084 px wide row, measured from its left edge.
        const float GapAfterName = 18f;
        const float SliderX = 474f;
        const float SliderWidth = 360f;
        const float SliderHeight = 30f;
        const float ValueX = 850f;
        const float ValueWidth = 140f;
        const float ResetButtonCenterFromRight = 42.5f;
        const float ResetIconSize = 40f;

        VoiceTab _tab;
        TMP_Text _name;
        Slider _slider;
        CanvasGroup _sliderGroup;
        TMP_Text _value;
        RectTransform _voiceButton;
        Image _voiceBackground;
        Image _voiceIcon;
        Sprite _micOpen;
        Sprite _micMuted;
        RectTransform _resetButton;

        string _rawName;
        bool _binding;
        VoiceStatus? _shownStatus;

        internal string SteamId { get; private set; }
        internal string DisplayName { get; private set; }
        internal VoiceStatus Status { get; private set; } = VoiceStatus.Inactive;

        internal static VoiceRow Create(VoiceTab tab, GameObject prefab, Transform parent, Slider sliderTemplate, Sprite panelSprite)
        {
            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = "Item_PlayerVoiceVolume";
            VoiceRow row = go.AddComponent<VoiceRow>();
            row._tab = tab;
            try
            {
                row.Build(go.GetComponent<PlayerItemController>(), sliderTemplate, panelSprite);
                return row;
            }
            catch
            {
                Object.Destroy(go);
                throw;
            }
        }

        void Build(PlayerItemController item, Slider sliderTemplate, Sprite panelSprite)
        {
            if (item == null)
                throw new InvalidOperationException("the player row prefab has no PlayerItemController");

            TextMeshProUGUI name = GameRefs.Get(GameRefs.ItemName, item);
            Button voiceButton = GameRefs.Get(GameRefs.ItemVoiceMute, item);
            if (name == null || voiceButton == null)
                throw new InvalidOperationException("the player row layout has changed");

            _name = name;
            _voiceButton = (RectTransform)voiceButton.transform;
            _voiceBackground = GameRefs.Get(GameRefs.ItemVoiceBg, item);
            _voiceIcon = GameRefs.Get(GameRefs.ItemVoiceIcon, item);
            _micOpen = GameRefs.Get(GameRefs.ItemMicOpen, item);
            _micMuted = GameRefs.Get(GameRefs.ItemMicMuted, item);
            Button resetTemplate = GameRefs.Get(GameRefs.ItemMute, item);
            Transform buttonGroup = resetTemplate != null ? resetTemplate.transform.parent : null;

            // Strip what belongs to the Server tab: the other buttons, the pomodoro sync widgets, the game's
            // row logic and the hover handlers that look for it.
            var remove = new List<GameObject>();
            foreach (Button button in new[]
                     {
                         GameRefs.Get(GameRefs.ItemReport, item), GameRefs.Get(GameRefs.ItemBan, item),
                         GameRefs.Get(GameRefs.ItemSteam, item), GameRefs.Get(GameRefs.ItemIgnore, item)
                     })
            {
                if (button != null)
                    remove.Add(button.gameObject);
            }
            remove.Add(GameRefs.Get(GameRefs.ItemSync, item));
            remove.Add(GameRefs.Get(GameRefs.ItemSyncedWithMe, item));
            foreach (GameObject obj in remove)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            foreach (PlayerPanelButtonController hover in GetComponentsInChildren<PlayerPanelButtonController>(true))
                Object.DestroyImmediate(hover);
            Object.DestroyImmediate(item);

            // Mic button: same look and states as in the Server tab; a click toggles the game's voice mute.
            UiUtil.SetOnClick(voiceButton, OnVoiceButton);
            UiUtil.AddHover(voiceButton.gameObject, ShowVoiceInfo, HideInfo);
            UiUtil.DisableNavigation(voiceButton);

            // Name: shortened to make room for the slider.
            RectTransform nameRect = _name.rectTransform;
            float nameWidth = SliderX - GapAfterName - nameRect.anchoredPosition.x;
            if (nameWidth > 50f && nameWidth < nameRect.sizeDelta.x)
                nameRect.sizeDelta = new Vector2(nameWidth, nameRect.sizeDelta.y);
            _name.overflowMode = TextOverflowModes.Ellipsis;

            // Slider: a copy of the game's Voice Volume slider from the settings screen when available.
            _slider = sliderTemplate != null ? Object.Instantiate(sliderTemplate, transform, false) : BuildSlider(transform, panelSprite);
            _slider.name = "Slider_Volume";
            _slider.onValueChanged = new Slider.SliderEvent();
            UiUtil.DisableNavigation(_slider);
            _slider.direction = Slider.Direction.LeftToRight;
            _slider.minValue = 0f;
            _slider.wholeNumbers = true;
            UiUtil.Place((RectTransform)_slider.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(SliderX, 0f), new Vector2(SliderWidth, SliderHeight));
            _sliderGroup = _slider.gameObject.AddComponent<CanvasGroup>();
            _slider.onValueChanged.AddListener(OnSliderChanged);

            // Value: same font and colour as the name.
            _value = Object.Instantiate(_name.gameObject, transform, false).GetComponent<TMP_Text>();
            _value.name = "Text_Volume";
            _value.alignment = TextAlignmentOptions.Center;
            _value.overflowMode = TextOverflowModes.Overflow;
            UiUtil.Place(_value.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(ValueX, nameRect.anchoredPosition.y), new Vector2(ValueWidth, nameRect.sizeDelta.y));

            // Reset: the game's orange chat-mute button with our icon, moved out of its button group.
            Button reset = resetTemplate != null ? resetTemplate : BuildButton(transform, panelSprite);
            reset.name = "Button_ResetVolume";
            _resetButton = (RectTransform)reset.transform;
            Vector2 resetSize = _resetButton.rect.size.x > 1f ? _resetButton.rect.size : new Vector2(65f, 65f);
            _resetButton.SetParent(transform, false);
            UiUtil.Place(_resetButton, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-ResetButtonCenterFromRight, 0f), resetSize);
            Image icon = FindIcon(reset);
            if (icon != null)
            {
                icon.sprite = Icons.Reset;
                icon.preserveAspect = true;
                icon.rectTransform.anchoredPosition = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(ResetIconSize, ResetIconSize);
            }
            UiUtil.SetOnClick(reset, OnResetButton);
            UiUtil.AddHover(reset.gameObject, ShowResetInfo, HideInfo);
            UiUtil.DisableNavigation(reset);

            if (buttonGroup != null && buttonGroup != transform && buttonGroup.childCount == 0)
                Object.DestroyImmediate(buttonGroup.gameObject);
        }

        /// <summary>Shows a player. The slider is only moved for a different player or when forced.</summary>
        internal void Bind(string steamId, string rawName, bool force)
        {
            bool otherPlayer = steamId != SteamId;
            SteamId = steamId;
            if (otherPlayer || rawName != _rawName)
            {
                _rawName = rawName;
                DisplayName = GameAccess.FilterName(rawName) ?? string.Empty;
                _name.text = DisplayName;
                Plugin.Instance.Store.RememberName(steamId, DisplayName);
            }
            if (otherPlayer || force)
            {
                ShowVolume(Plugin.Instance.VolumeFor(steamId));
                _shownStatus = null;
            }
        }

        void ShowVolume(int percent)
        {
            _binding = true;
            try
            {
                _slider.maxValue = Plugin.Instance.MaxVolume.Value;
                _slider.SetValueWithoutNotify(percent);
            }
            finally
            {
                _binding = false;
            }
            _value.text = VolumeMath.Format(percent);
        }

        void OnSliderChanged(float value)
        {
            if (_binding || string.IsNullOrEmpty(SteamId))
                return;
            int percent = Mathf.RoundToInt(value);
            Plugin.Instance.SetVolume(SteamId, DisplayName, percent);
            _value.text = VolumeMath.Format(percent);
        }

        void OnResetButton()
        {
            if (string.IsNullOrEmpty(SteamId))
                return;
            Plugin.Instance.ResetVolume(SteamId);
            ShowVolume(Plugin.Instance.VolumeFor(SteamId));
            GameAccess.PlayUIClick();
        }

        /// <summary>
        /// Toggles the game's own voice mute for this player (the same as the mic button in the Server tab:
        /// it is saved by the game and listed under Banned). The game runs it on its hidden Server row.
        /// </summary>
        void OnVoiceButton()
        {
            PlayerPanelController players = _tab.Panel;
            PlayerItemController gameRow = FindGameRow(players);
            if (gameRow == null)
            {
                players.UpdateServerPanel();
                gameRow = FindGameRow(players);
            }
            if (gameRow == null)
            {
                Plugin.Log.LogWarning($"No player list entry for {DisplayName}; cannot toggle the voice mute.");
                return;
            }
            gameRow.ButtonVoiceMutePlayer();
            RefreshStatus();
            ShowVoiceInfo();
        }

        PlayerItemController FindGameRow(PlayerPanelController players)
        {
            List<PlayerItemController> rows = GameRefs.Get(GameRefs.Items, players);
            if (rows == null)
                return null;
            foreach (PlayerItemController row in rows)
            {
                if (row != null && row.gameObject.activeSelf && GameRefs.Get(GameRefs.ItemSteamId, row) == SteamId)
                    return row;
            }
            return null;
        }

        /// <summary>Mirrors PlayerItemController.SetVoiceSpeaking.</summary>
        internal void RefreshStatus()
        {
            PlayerPanelController players = _tab.Panel;
            if (players == null || string.IsNullOrEmpty(SteamId))
                return;
            Status = GameAccess.VoiceStatusOf(players, SteamId);
            if (_shownStatus == Status)
                return;
            _shownStatus = Status;

            bool muted = Status == VoiceStatus.Banned;
            if (_voiceBackground != null)
                _voiceBackground.color = muted ? players.RedColor : players.IconDeactiveColor;
            if (_voiceIcon != null)
            {
                switch (Status)
                {
                    case VoiceStatus.Speaking:
                        _voiceIcon.color = players.IconActiveColor;
                        break;
                    case VoiceStatus.Banned:
                        _voiceIcon.color = players.IconColor;
                        break;
                    default:
                        _voiceIcon.color = players.MicIconInactiveColor;
                        break;
                }
                Sprite sprite = Status == VoiceStatus.Banned || Status == VoiceStatus.Inactive ? _micMuted : _micOpen;
                if (sprite != null)
                    _voiceIcon.sprite = sprite;
            }
            // The slider has no audible effect while the game mutes the player; show that, but keep it usable.
            _sliderGroup.alpha = muted ? 0.45f : 1f;
        }

        void ShowVoiceInfo()
        {
            PlayerPanelController players = _tab.Panel;
            if (players != null)
                players.OpenButtonInfoPopup(PlayerPanelButtonType.VoiceMute, _voiceButton, Status, PomoSyncStatus.Inactive);
        }

        void ShowResetInfo()
        {
            int fallback = Mathf.Clamp(Plugin.Instance.DefaultVolume.Value, 0, Plugin.Instance.MaxVolume.Value);
            _tab.ShowInfo(_resetButton, Strings.ResetTitle, string.Format(Strings.ResetInfo, VolumeMath.Format(fallback)));
        }

        void HideInfo()
        {
            PlayerPanelController players = _tab != null ? _tab.Panel : null;
            if (players != null)
                players.CloseButtonInfoPopup();
        }

        static Image FindIcon(Button button)
        {
            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject != button.gameObject)
                    return image;
            }
            return null;
        }

        /// <summary>Fallback when the settings slider is not available: same colours as the game's sliders.</summary>
        static Slider BuildSlider(Transform parent, Sprite sprite)
        {
            RectTransform root = UiUtil.NewRect("Slider", parent);
            Image background = UiUtil.NewImage("Background", root, sprite, new Color(0.773f, 0.714f, 0.682f), 10f);
            UiUtil.Stretch(background.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);

            RectTransform fillArea = UiUtil.NewRect("Fill Area", root);
            UiUtil.Stretch(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(-20f, 0f), new Vector2(-5f, 0f));
            Image fill = UiUtil.NewImage("Fill", fillArea, sprite, new Color(0.416f, 0.302f, 0.322f), 10f);
            UiUtil.Stretch(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(10f, 0f), Vector2.zero);

            RectTransform handleArea = UiUtil.NewRect("Handle Slide Area", root);
            UiUtil.Stretch(handleArea, Vector2.zero, Vector2.one, new Vector2(-20f, 0f), Vector2.zero);
            Image handle = UiUtil.NewImage("Handle", handleArea, sprite, new Color(0.961f, 0.929f, 0.882f), 6f);
            UiUtil.Stretch(handle.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(22f, 12f), Vector2.zero);
            // Fully qualified: the game has its own global Outline component.
            handle.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.416f, 0.302f, 0.322f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        /// <summary>Fallback when the row prefab has no button to reuse.</summary>
        static Button BuildButton(Transform parent, Sprite sprite)
        {
            Image background = UiUtil.NewImage("Button", parent, sprite, new Color(0.906f, 0.663f, 0.4f), 7.2f);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            Image icon = UiUtil.NewImage("Image_Icon", background.transform, null, new Color(0.961f, 0.929f, 0.882f), 1f);
            icon.raycastTarget = false;
            background.rectTransform.sizeDelta = new Vector2(65f, 65f);
            return button;
        }
    }
}
