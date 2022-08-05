using HarmonyLib;
using RimWorld;
using Verse;

namespace HandyUI_PersonalWorkCategories.Patch;

[HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
internal class MainMenuDrawer__MainMenuOnGUI
{
    private static void Postfix()
    {
        if (PersonalWorkCategories.Settings.isSavedDataVersionDeprecated)
        {
            Find.WindowStack.Add(new WarningMessage(
                "personalWorkCategories_warningHeader".Translate(),
                "personalWorkCategories_newVersionWarningMessage".Translate(),
                () => PersonalWorkCategories.Settings.isSavedDataVersionDeprecated = false));
        }

        if (PersonalWorkCategories.Settings.isWorksListWasChanged)
        {
            Find.WindowStack.Add(new WarningMessage(
                "personalWorkCategories_warningHeader".Translate(),
                "personalWorkCategories_warningBody".Translate(),
                () => PersonalWorkCategories.Settings.isWorksListWasChanged = false));
        }
    }
}