using HarmonyLib;

namespace GearSets.Integrations
{
    internal static class Adapters
    {
        public static void Init(Harmony harmony)
        {
            ExtraSlotsAdapter.Init();
        }
    }
}
