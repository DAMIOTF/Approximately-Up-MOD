using HarmonyLib;

namespace ApproximatelyUpMod
{
    [HarmonyPatch(typeof(Core.Singleton), nameof(Core.Singleton.GetAvailableComponents))]
    public static class CoreSingletonGetAvailableComponentsPatch
    {
        private static void Postfix(ref int __result)
        {
            if (ItemListController.InfiniteAllMaterials && __result < 999)
            {
                __result = 999;
            }
        }
    }
}
