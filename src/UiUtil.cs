using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlayerVoiceVolume
{
    internal static class UiUtil
    {
        /// <summary>
        /// Removes the game's LocalizeStringEvent components from a copied object; otherwise they would put the
        /// original label ("Banned", ...) back whenever the language is applied.
        /// </summary>
        public static void RemoveLocalization(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "LocalizeStringEvent")
                    Object.DestroyImmediate(behaviour);
            }
        }

        /// <summary>Drops the click handlers a copied button brought along and installs ours.</summary>
        public static void SetOnClick(Button button, Action action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("UI action failed: " + e);
                }
            });
        }

        /// <summary>
        /// Keeps a control from becoming the selected UI object when clicked, so keys handled by the UI
        /// navigation (Space, Enter, arrows, WASD) cannot press it again or move it afterwards.
        /// </summary>
        public static void DisableNavigation(Selectable selectable)
        {
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void AddHover(GameObject target, Action enter, Action exit)
        {
            HoverRelay relay = target.GetComponent<HoverRelay>();
            if (relay == null)
                relay = target.AddComponent<HoverRelay>();
            relay.Enter = enter;
            relay.Exit = exit;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, Color color, float pixelsPerUnit)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = pixelsPerUnit;
            image.color = color;
            return image;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
        }
    }

    /// <summary>Forwards pointer enter/exit to callbacks (used for the game's info popup).</summary>
    internal sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal Action Enter;
        internal Action Exit;

        bool _hovered;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Invoke(Enter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Invoke(Exit);
        }

        // Closing the list under the cursor sends no exit event; hide the popup ourselves.
        void OnDisable()
        {
            if (!_hovered)
                return;
            _hovered = false;
            Invoke(Exit);
        }

        static void Invoke(Action action)
        {
            if (action == null)
                return;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Hover action failed: " + e.Message);
            }
        }
    }
}
