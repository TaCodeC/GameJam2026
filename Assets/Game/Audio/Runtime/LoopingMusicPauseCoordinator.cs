using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Audio
{
    internal static class LoopingMusicPauseCoordinator
    {
        private sealed class PauseRecord
        {
            public PauseRecord(AudioSource source)
            {
                Source = source;
            }

            public AudioSource Source { get; }
            public int HoldCount { get; set; }
        }

        private static readonly Dictionary<AudioSource, PauseRecord> s_recordsBySource = new();
        private static readonly Dictionary<object, List<AudioSource>> s_sourcesByOwner = new();

        public static void PauseFor(object owner, AudioSource exemptSource)
        {
            if (owner == null || s_sourcesByOwner.ContainsKey(owner))
                return;

            CleanupDeadSources();

            List<AudioSource> ownedSources = new();
            foreach (PauseRecord record in s_recordsBySource.Values)
            {
                AudioSource source = record.Source;
                if (source == null || source == exemptSource)
                    continue;

                record.HoldCount++;
                ownedSources.Add(source);
            }

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null
                    || source == exemptSource
                    || !source.loop
                    || !source.isPlaying
                    || s_recordsBySource.ContainsKey(source))
                {
                    continue;
                }

                PauseRecord record = new(source)
                {
                    HoldCount = 1
                };

                source.Pause();
                s_recordsBySource.Add(source, record);
                ownedSources.Add(source);
            }

            s_sourcesByOwner.Add(owner, ownedSources);
        }

        public static void ReleaseFor(object owner)
        {
            if (owner == null || !s_sourcesByOwner.Remove(owner, out List<AudioSource> ownedSources))
                return;

            for (int i = 0; i < ownedSources.Count; i++)
            {
                AudioSource source = ownedSources[i];
                if (source == null || !s_recordsBySource.TryGetValue(source, out PauseRecord record))
                    continue;

                record.HoldCount--;
                if (record.HoldCount > 0)
                    continue;

                s_recordsBySource.Remove(source);
                source.UnPause();
            }

            CleanupDeadSources();
        }

        private static void CleanupDeadSources()
        {
            List<AudioSource> deadSources = null;
            foreach (KeyValuePair<AudioSource, PauseRecord> pair in s_recordsBySource)
            {
                if (pair.Key != null && pair.Value.Source != null)
                    continue;

                deadSources ??= new List<AudioSource>();
                deadSources.Add(pair.Key);
            }

            if (deadSources != null)
            {
                for (int i = 0; i < deadSources.Count; i++)
                    s_recordsBySource.Remove(deadSources[i]);
            }

            List<object> emptyOwners = null;
            foreach (KeyValuePair<object, List<AudioSource>> pair in s_sourcesByOwner)
            {
                pair.Value.RemoveAll(static source => source == null);
                if (pair.Value.Count == 0)
                {
                    emptyOwners ??= new List<object>();
                    emptyOwners.Add(pair.Key);
                }
            }

            if (emptyOwners == null)
                return;

            for (int i = 0; i < emptyOwners.Count; i++)
                s_sourcesByOwner.Remove(emptyOwners[i]);
        }
    }
}
