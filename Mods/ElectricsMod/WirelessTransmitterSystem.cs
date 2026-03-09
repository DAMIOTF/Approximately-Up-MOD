using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ApproximatelyUpMod
{
    internal static class WirelessTransmitterSystem
    {
        internal const int DefaultMaxChannels = 99;
        internal const int MinChannels = 1;
        internal const int MaxChannelsLimit = 9999;

        private static int _desiredMaxChannels = DefaultMaxChannels;
        private static double _nextRescanAt;
        private const double RescanInterval = 0.4;

        internal static int DesiredMaxChannels => _desiredMaxChannels;

        internal static void SetMaxChannels(int value)
        {
            _desiredMaxChannels = Math.Max(MinChannels, Math.Min(MaxChannelsLimit, value));
            _nextRescanAt = 0;
        }

        internal static void Reset()
        {
            SetMaxChannels(DefaultMaxChannels);
        }

        internal static void Tick()
        {
            if (Time.realtimeSinceStartupAsDouble < _nextRescanAt)
            {
                return;
            }

            _nextRescanAt = Time.realtimeSinceStartupAsDouble + RescanInterval;

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            Apply(World.DefaultGameObjectInjectionWorld.EntityManager);
        }

        private static void Apply(EntityManager em)
        {
            int desired = _desiredMaxChannels;

            EntityQuery query = em.CreateEntityQuery(typeof(SCTypeWirelessTransmitter));
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            query.Dispose();

            int changed = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                {
                    continue;
                }

                SCTypeWirelessTransmitter tx = em.GetComponentData<SCTypeWirelessTransmitter>(entity);
                Entity lever = tx._actionableLeverEntity;
                if (lever == Entity.Null || !em.Exists(lever))
                {
                    continue;
                }

                ActionableData ad = em.GetComponentData<ActionableData>(lever);
                if (ad._roundingIntervals == desired)
                {
                    continue;
                }

                // Preserve current channel position within the new range
                int currentChannel = ad.GetValueInterval().x;
                ad._roundingIntervals = desired;
                int clampedChannel = Math.Max(0, Math.Min(desired - 1, currentChannel));
                ad._value.x = (clampedChannel + 0.5f) / desired;
                em.SetComponentData(lever, ad);
                changed++;
            }

            entities.Dispose();

            if (changed > 0)
            {
                ModLog.Info($"Wireless transmitter max channels -> {desired} ({changed} units updated).");
            }
        }
    }
}
