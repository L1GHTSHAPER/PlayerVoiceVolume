using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PlayerVoiceVolume
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("OnTogether.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ontogether.playervoicevolume";
        public const string PluginName = "PlayerVoiceVolume";
        public const string PluginVersion = "1.0.1";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        internal ConfigEntry<int> MaxVolume;
        internal ConfigEntry<int> DefaultVolume;

        internal VolumeStore Store { get; private set; }

        VoiceApplier _applier;
        Harmony _harmony;

        void Awake()
        {
            Instance = this;
            Log = Logger;

            MaxVolume = Config.Bind("General", "MaxVolumePercent", 200,
                new ConfigDescription(
                    "Right end of the per-player volume slider, in percent. 100% leaves a player unchanged; " +
                    "every doubling is +10 dB, so 200% sounds about twice as loud. Values above 200% can distort loud voices.",
                    new AcceptableValueRange<int>(100, 400)));
            DefaultVolume = Config.Bind("General", "DefaultVolumePercent", 100,
                new ConfigDescription(
                    "Volume of players you have not adjusted, in percent. The reset button on a player returns them to this value.",
                    new AcceptableValueRange<int>(0, 400)));

            Store = new VolumeStore(Path.Combine(Paths.ConfigPath, PluginGuid + ".volumes.json"));
            Store.Load();
            _applier = new VoiceApplier();

            _harmony = new Harmony(PluginGuid);
            Patches.Apply(_harmony);

            Config.SettingChanged += OnSettingChanged;
            Log.LogInfo($"{PluginName} {PluginVersion} loaded; {Store.Count} saved player volume(s).");
        }

        void Update()
        {
            _applier.Tick();
            Store.SaveIfDue();
        }

        /// <summary>Volume in percent that applies to a player: the saved one, or the default.</summary>
        internal int VolumeFor(string steamId)
        {
            int percent = Store.TryGet(steamId, out int saved) ? saved : DefaultVolume.Value;
            return Mathf.Clamp(percent, 0, MaxVolume.Value);
        }

        internal bool HasSavedVolume(string steamId) => Store.TryGet(steamId, out _);

        internal void SetVolume(string steamId, string playerName, int percent)
        {
            Store.Set(steamId, playerName, percent);
            _applier.ApplySoon();
        }

        internal void ResetVolume(string steamId)
        {
            Store.Remove(steamId);
            _applier.ApplySoon();
        }

        void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            _applier.ApplySoon();
            VoiceTab tab = VoiceTab.Current;
            if (tab != null)
                tab.RefreshVolumes();
        }

        void OnApplicationQuit()
        {
            Store.Save();
        }

        void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            Store.Save();
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
    }
}
