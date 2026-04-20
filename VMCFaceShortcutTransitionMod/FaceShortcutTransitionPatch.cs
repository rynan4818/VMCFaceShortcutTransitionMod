using HarmonyLib;
using UnityMemoryMappedFile;
using VMC;

namespace VMCFaceShortcutTransitionMod
{
    [HarmonyPatch(typeof(ControlWPFWindow), nameof(ControlWPFWindow.DoKeyAction))]
    internal static class FaceShortcutTransitionPatch
    {
        private static bool Prefix(ControlWPFWindow __instance, KeyAction action)
        {
            var plugin = VMCFaceShortcutTransitionModPlugin.Instance;
            if (plugin == null)
            {
                return true;
            }

            return plugin.HandleDoKeyActionPrefix(__instance, action);
        }
    }
}
