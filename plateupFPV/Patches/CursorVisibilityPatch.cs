using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitchenFirstPersonView.Patches
{
    [HarmonyPatch(typeof(Controllers.MouseUI))]
    [HarmonyPatch("UpdateMouseVisibility")]
    class CursorVisibilityPatch
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
