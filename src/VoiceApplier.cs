using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Services.Vivox;
using UnityEngine;

namespace PlayerVoiceVolume
{
    /// <summary>
    /// Keeps every Vivox participant at the volume saved for their player. Vivox forgets the adjustment when a
    /// participant leaves or the channel is rejoined, so the live participants are checked periodically and
    /// only the ones that differ are updated.
    /// </summary>
    internal sealed class VoiceApplier
    {
        const float Interval = 0.5f;
        const float SoonDelay = 0.1f;
        // A request is only re-sent if Vivox has not confirmed it after this long.
        const float RetryAfter = 3f;

        struct Request
        {
            public VivoxParticipant Participant;
            public int Value;
            public float Time;
        }

        readonly Dictionary<string, Request> _requests = new Dictionary<string, Request>(StringComparer.Ordinal);
        float _nextRun;
        string _lastError;

        /// <summary>Runs the next pass shortly, e.g. while a slider is being dragged.</summary>
        public void ApplySoon()
        {
            _nextRun = Mathf.Min(_nextRun, Time.unscaledTime + SoonDelay);
        }

        public void Tick()
        {
            float now = Time.unscaledTime;
            if (now < _nextRun)
                return;
            _nextRun = now + Interval;
            try
            {
                ApplyAll(now);
            }
            catch (Exception e)
            {
                Report(e);
            }
        }

        // Logs each distinct failure once instead of twice a second.
        void Report(Exception e)
        {
            string message = e.GetType().Name + ": " + e.Message;
            if (message == _lastError)
                return;
            _lastError = message;
            Plugin.Log.LogWarning("Could not apply player volumes: " + e);
        }

        void ApplyAll(float now)
        {
            IVivoxService vivox = VivoxService.Instance;
            if (vivox == null)
                return;
            ReadOnlyDictionary<string, ReadOnlyCollection<VivoxParticipant>> channels = vivox.ActiveChannels;
            if (channels == null || channels.Count == 0)
                return;
            PlayerPanelController players = GameAccess.PlayerPanel;
            if (players == null)
                return;

            foreach (KeyValuePair<string, ReadOnlyCollection<VivoxParticipant>> channel in channels)
            {
                if (channel.Value == null)
                    continue;
                foreach (VivoxParticipant participant in channel.Value)
                {
                    try
                    {
                        ApplyTo(participant, players, now);
                    }
                    catch (Exception e)
                    {
                        Report(e);
                    }
                }
            }

            if (_requests.Count > 64)
                _requests.Clear();
        }

        void ApplyTo(VivoxParticipant participant, PlayerPanelController players, float now)
        {
            if (participant == null || participant.IsSelf)
                return;

            // The player list keeps Vivox IDs and Steam IDs in parallel lists, like the game's own lookup.
            string vivoxId = participant.PlayerId;
            int index = players.VivoxIDs != null ? players.VivoxIDs.IndexOf(vivoxId) : -1;
            if (index < 0 || players.PlayerSteamIDs == null || index >= players.PlayerSteamIDs.Count)
                return;

            int target = VolumeMath.ToVivox(Plugin.Instance.VolumeFor(players.PlayerSteamIDs[index]));
            if (participant.LocalVolume == target)
            {
                _requests.Remove(vivoxId);
                return;
            }

            // LocalVolume only changes once Vivox confirms the request; do not flood it in the meantime.
            if (_requests.TryGetValue(vivoxId, out Request pending)
                && pending.Participant == participant
                && pending.Value == target
                && now - pending.Time < RetryAfter)
                return;

            participant.SetLocalVolume(target);
            _requests[vivoxId] = new Request { Participant = participant, Value = target, Time = now };
        }
    }
}
