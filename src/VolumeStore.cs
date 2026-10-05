using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlayerVoiceVolume
{
    internal static class VolumeMath
    {
        /// <summary>Vivox local volume adjustment that silences a participant.</summary>
        public const int VivoxMute = -50;
        public const int VivoxMax = 50;

        /// <summary>
        /// Slider percent to the Vivox local volume adjustment (-50..50, about 1 dB per step, 0 = unchanged).
        /// Every doubling of the percentage adds 10 dB, which is roughly twice the perceived loudness,
        /// so 50% sounds about half as loud and 200% about twice as loud. 0% silences the player.
        /// </summary>
        public static int ToVivox(int percent)
        {
            if (percent <= 0)
                return VivoxMute;
            double decibels = 10.0 * Math.Log(percent / 100.0, 2.0);
            return Mathf.Clamp((int)Math.Round(decibels), VivoxMute + 1, VivoxMax);
        }

        /// <summary>Same format as the game's own volume sliders ("%100").</summary>
        public static string Format(int percent) => "%" + percent;
    }

    [Serializable]
    internal sealed class SavedVolume
    {
        public string steamId;
        public string name;
        public int volume;
    }

    [Serializable]
    internal sealed class SavedVolumes
    {
        public int version = 1;
        public List<SavedVolume> players = new List<SavedVolume>();
    }

    /// <summary>
    /// Per-player volumes keyed by Steam ID, kept in a JSON file next to the plugin's config.
    /// Writes are delayed a little so dragging a slider does not hit the disk on every step.
    /// </summary>
    internal sealed class VolumeStore
    {
        const float SaveDelay = 2f;
        const int MaxPercent = 400;

        readonly string _path;
        readonly Dictionary<string, SavedVolume> _byId = new Dictionary<string, SavedVolume>(StringComparer.Ordinal);
        bool _dirty;
        float _saveAt;

        public VolumeStore(string path)
        {
            _path = path;
        }

        public int Count => _byId.Count;

        public bool TryGet(string steamId, out int percent)
        {
            if (!string.IsNullOrEmpty(steamId) && _byId.TryGetValue(steamId, out SavedVolume entry))
            {
                percent = entry.volume;
                return true;
            }
            percent = 0;
            return false;
        }

        public void Set(string steamId, string name, int percent)
        {
            if (string.IsNullOrEmpty(steamId))
                return;
            percent = Mathf.Clamp(percent, 0, MaxPercent);
            if (!_byId.TryGetValue(steamId, out SavedVolume entry))
            {
                entry = new SavedVolume { steamId = steamId, volume = percent };
                _byId[steamId] = entry;
                MarkDirty();
            }
            else if (entry.volume != percent)
            {
                entry.volume = percent;
                MarkDirty();
            }
            if (!string.IsNullOrEmpty(name) && entry.name != name)
            {
                entry.name = name;
                MarkDirty();
            }
        }

        public void Remove(string steamId)
        {
            if (!string.IsNullOrEmpty(steamId) && _byId.Remove(steamId))
                MarkDirty();
        }

        /// <summary>Keeps the stored nickname current so the file stays readable. Only touches saved players.</summary>
        public void RememberName(string steamId, string name)
        {
            if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(name))
                return;
            if (_byId.TryGetValue(steamId, out SavedVolume entry) && entry.name != name)
            {
                entry.name = name;
                MarkDirty();
            }
        }

        void MarkDirty()
        {
            _dirty = true;
            _saveAt = Time.unscaledTime + SaveDelay;
        }

        public void SaveIfDue()
        {
            if (_dirty && Time.unscaledTime >= _saveAt)
                Save();
        }

        public void Load()
        {
            _byId.Clear();
            if (!File.Exists(_path))
                return;
            try
            {
                SavedVolumes file = JsonUtility.FromJson<SavedVolumes>(File.ReadAllText(_path, Encoding.UTF8));
                if (file == null || file.players == null)
                    return;
                foreach (SavedVolume entry in file.players)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.steamId))
                        continue;
                    entry.volume = Mathf.Clamp(entry.volume, 0, MaxPercent);
                    _byId[entry.steamId] = entry;
                }
            }
            catch (Exception e)
            {
                // Keep the unreadable file for the user instead of overwriting it silently on the next save.
                string backup = _path + ".bad";
                Plugin.Log.LogError($"Could not read {_path}: {e.Message}. A copy was kept as {backup}.");
                try
                {
                    File.Copy(_path, backup, true);
                }
                catch (Exception copyError)
                {
                    Plugin.Log.LogWarning("Could not back up the file: " + copyError.Message);
                }
            }
        }

        public void Save()
        {
            if (!_dirty)
                return;
            _dirty = false;
            try
            {
                var file = new SavedVolumes
                {
                    players = _byId.Values
                        .OrderBy(e => e.name ?? e.steamId, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
                string temp = _path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(file, true), new UTF8Encoding(false));
                File.Copy(temp, _path, true);
                File.Delete(temp);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not save player volumes to {_path}: {e.Message}");
            }
        }
    }
}
