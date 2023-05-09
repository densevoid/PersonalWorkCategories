using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HandyUI_PersonalWorkCategories.Patch
{
    [HarmonyPatch(typeof(Pawn_WorkSettings), "ExposeData")]
    class Pawn_WorkSettings__ExposeData
    {
        static void Postfix(Pawn_WorkSettings __instance, Pawn ___pawn)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit && ___pawn.IsColonyMech)
            {
                __instance.EnableAndInitialize();
            }
        }
    }
}
