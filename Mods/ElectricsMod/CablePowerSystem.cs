using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ApproximatelyUpMod
{
    internal static class CablePowerSystem
    {
        internal sealed class CableTypeEntry
        {
            internal readonly string Label;
            internal readonly SpaceshipCableType Type;
            internal float OriginalMaxPower;
            internal float OverrideMaxPower; // 0 = use original
            internal bool OriginalDiscovered;

            internal CableTypeEntry(string label, SpaceshipCableType type)
            {
                Label = label;
                Type = type;
            }
        }

        private static readonly CableTypeEntry[] _entries =
        {
            new CableTypeEntry("Power Cable",      SpaceshipCableType.Power),
            new CableTypeEntry("Power Nano Cable", SpaceshipCableType.PowerNano),
            new CableTypeEntry("Plasma Cable",     SpaceshipCableType.Plasma),
        };

        private static double _nextRescanAt;
        private const double RescanInterval = 0.4;
        private static int _discoveryRevision;

        internal static CableTypeEntry[] Entries => _entries;
        internal static int DiscoveryRevision => _discoveryRevision;

        internal static void ForceRescan()
        {
            _nextRescanAt = 0;
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
            EntityQuery query = em.CreateEntityQuery(typeof(SCTypeCableCell));
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            query.Dispose();

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                {
                    continue;
                }

                SCTypeCableCell cell = em.GetComponentData<SCTypeCableCell>(entity);
                if (cell._maxPower <= 0f)
                {
                    continue;
                }

                CableTypeEntry entry = GetEntry(cell._type);
                if (entry == null)
                {
                    continue;
                }

                // Record original value from first encountered entity of this type
                if (!entry.OriginalDiscovered)
                {
                    entry.OriginalMaxPower = cell._maxPower;
                    entry.OriginalDiscovered = true;
                    _discoveryRevision++;
                    ModLog.Info($"Cable original max power discovered: {entry.Label} = {entry.OriginalMaxPower} P/s");
                }

                float desired = entry.OverrideMaxPower > 0f ? entry.OverrideMaxPower : entry.OriginalMaxPower;
                if (Math.Abs(cell._maxPower - desired) < 0.01f)
                {
                    continue;
                }

                cell._maxPower = desired;
                em.SetComponentData(entity, cell);
            }

            entities.Dispose();
        }

        private static CableTypeEntry GetEntry(SpaceshipCableType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i];
                }
            }

            return null;
        }
    }
}
