using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PlayerVoiceVolume
{
    /// <summary>Hooks into the game's player list. Each patch is applied on its own so one game change cannot break the rest.</summary>
    internal static class Patches
    {
        public static void Apply(Harmony harmony)
        {
            Postfix(harmony, "Start", nameof(PlayerListStarted));
            Postfix(harmony, nameof(PlayerPanelController.ButtonServer), nameof(ServerTabSelected));
            Postfix(harmony, nameof(PlayerPanelController.ButtonBanned), nameof(BannedTabSelected));
            Postfix(harmony, nameof(PlayerPanelController.ButtonClosePanel), nameof(PlayerListClosed));
            Postfix(harmony, nameof(PlayerPanelController.UpdateServerPanelOnChange), nameof(PlayersChanged));
        }

        static void Postfix(Harmony harmony, string method, string patch)
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(PlayerPanelController), method);
                if (target == null)
                {
                    Plugin.Log.LogWarning($"PlayerPanelController.{method} not found; the Voice tab may not work.");
                    return;
                }
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(Patches), patch));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not patch PlayerPanelController.{method}: {e}");
            }
        }

        static void PlayerListStarted(PlayerPanelController __instance)
        {
            Run(() =>
            {
                GameAccess.PlayerPanel = __instance;
                VoiceTab.EnsureInjected(__instance);
            });
        }

        // The game also calls ButtonServer when the list is opened with Tab.
        static void ServerTabSelected(PlayerPanelController __instance)
        {
            Run(() => GameTabSelected(__instance, GameRefs.Get(GameRefs.ServerButton, __instance)));
        }

        static void BannedTabSelected(PlayerPanelController __instance)
        {
            Run(() => GameTabSelected(__instance, GameRefs.Get(GameRefs.BannedButton, __instance)));
        }

        static void GameTabSelected(PlayerPanelController players, RectTransform button)
        {
            VoiceTab.EnsureInjected(players);
            VoiceTab tab = VoiceTab.Current;
            if (tab != null && tab.Panel == players)
                tab.OnGameTabSelected(button);
        }

        static void PlayerListClosed()
        {
            Run(() => Plugin.Instance.Store.Save());
        }

        static void PlayersChanged()
        {
            Run(() =>
            {
                VoiceTab tab = VoiceTab.Current;
                if (tab != null)
                    tab.MarkPlayersChanged();
            });
        }

        static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Player list hook failed: " + e);
            }
        }
    }
}
