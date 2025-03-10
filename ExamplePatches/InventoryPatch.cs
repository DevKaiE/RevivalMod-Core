using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RevivalMod.Constants;
using EFT.InventoryLogic;

namespace RevivalMod.ExamplePatches
{
    internal class InventoryPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Inventory), nameof(Inventory.GetPlayerItems));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            return true; // Let the original method run
        }

        [PatchPostfix]
        static void Postfix(ref IEnumerable<Item> __result, EPlayerItems itemsMask)
        {
            // Only run our code when specifically looking for Equipment items
            // This way we don't log every time the method is called
            if (itemsMask == EPlayerItems.Equipment)
            {
                if (__result != null)
                {
                    // Log count of in-raid items
                    int itemCount = __result.Count();
                    UnityEngine.Debug.LogWarning($"[RevivalMod] Found {itemCount} equipment items in raid");

                    // Log individual items
                    foreach (var item in __result)
                    {
                        UnityEngine.Debug.LogWarning($"[RevivalMod] In-raid item: {item.TemplateId} - {item.Name?.Localized()}");
                    }
                }
            }
        }
    }
}
