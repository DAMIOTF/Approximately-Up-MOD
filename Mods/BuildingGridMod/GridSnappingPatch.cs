using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Entities;
using Unity.Mathematics;

namespace ApproximatelyUpMod
{
    [HarmonyPatch(typeof(GarageGrabber), nameof(GarageGrabber.OnUpdate))]
    internal static class GridSnappingPatch
    {
        private static bool _loggedExecution;

        private static bool Prepare(MethodBase original)
        {
            ModLog.Info("[BuildingGridMod] Applying patch: GridSnappingPatch -> " + original);
            return true;
        }

        private static void Prefix()
        {
            if (_loggedExecution)
            {
                return;
            }

            _loggedExecution = true;
            ModLog.Info("[BuildingGridMod] Patch executed: GarageGrabber.OnUpdate (GridSnappingPatch)");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo snappingField = AccessTools.Field(typeof(SpaceshipComponentPrefabData), nameof(SpaceshipComponentPrefabData._snapping));
            MethodInfo adjustMethod = AccessTools.Method(typeof(BuildingModConfig), nameof(BuildingModConfig.GetEffectiveSnapping));

            int replacements = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, snappingField))
                {
                    replacements++;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, adjustMethod);
                    continue;
                }

                yield return instruction;
            }

            if (replacements == 0)
            {
                ModLog.Warn("GridSnappingPatch: no _snapping field usage replaced in GarageGrabber.OnUpdate.");
            }
            else
            {
                ModLog.Info("GridSnappingPatch: injected custom grid snapping hook x" + replacements + ".");
            }
        }
    }

    [HarmonyPatch]
    internal static class GridSnapMathPatch
    {
        private static bool _loggedExecution;

        private static MethodBase TargetMethod()
        {
            MethodBase method = AccessTools.Method(
                typeof(GarageGrabber),
                "SetGarageTransform",
                new[]
                {
                    typeof(EntityManager),
                    typeof(GarageTransform),
                    typeof(Entity),
                    typeof(GarageGrabberSingleton),
                    typeof(GarageTransform)
                });

            ModLog.Info("[BuildingGridMod] Applying patch: GridSnapMathPatch -> " + (method == null ? "NOT FOUND" : method.ToString()));
            return method;
        }

        // Intercept final placement transform and reapply snap using configured grid step.
        private static void Prefix(EntityManager em, Entity entity, ref GarageTransform garageTransform)
        {
            if (!_loggedExecution)
            {
                _loggedExecution = true;
                ModLog.Info("[BuildingGridMod] Patch executed: GarageGrabber.SetGarageTransform (GridSnapMathPatch)");
            }

            // Cable cells use dedicated 0.125 m snapping — never resnap them.
            if (em.HasComponent<SCTypeCableCell>(entity))
            {
                return;
            }

            if (!em.HasComponent<SCPrefab>(entity))
            {
                return;
            }

            Core core = Core.Get();
            if (core == null || core._componentsMap == null)
            {
                return;
            }

            SCPrefab prefab = em.GetComponentData<SCPrefab>(entity);
            EPC_SpaceshipComponent component;
            if (!core._componentsMap.TryGetValue(prefab, out component) || component == null)
            {
                return;
            }

            // Re-snap using the ECS-overridden grid (idempotent safety net).
            // The game's own Burst code already snapped to this grid via BuildingRuntimeOverrides.
            // For any 90°-multiple rotation, abs(rotate(q, (g,g,g))) == (g,g,g), so the step is
            // always uniform and consistent regardless of part orientation.
            quaternion rotation = garageTransform.Rotation();
            float3 bounds = component._bounds;
            float grid = BuildingModConfig.GridSize;
            float3 step = math.abs(math.rotate(rotation, new float3(grid, grid, grid)));
            step = math.max(step, new float3(0.0001f, 0.0001f, 0.0001f));

            // Use the original part bounds (not ECS-modified ones) for the phase anchor so the
            // formula stays idempotent whether the game path or this fallback ran first.
            float3 offset = math.abs(math.rotate(rotation, 0.5f * bounds));
            float3 position = garageTransform.Position();
            float3 snapped = math.round((position - offset) / step) * step + offset;

            garageTransform = new GarageTransform(snapped, rotation);
        }
    }
}
