using HarmonyLib;

namespace TiltFive.Patches
{
    [HarmonyPatch(typeof(SmoothCameraController), nameof(SmoothCameraController.ActivateSmoothCameraIfNeeded))]
    static class InitOnMainAvailable
    {
        static bool initialized = false;

        static void Postfix(MainSettingsModelSO ____mainSettingsModel)
        {
            if (initialized) return;

            initialized = true;
            Plugin.Log.Notice("Game is ready, Initializing...");
            Plugin.SetupEverything();
        }
    }
}
