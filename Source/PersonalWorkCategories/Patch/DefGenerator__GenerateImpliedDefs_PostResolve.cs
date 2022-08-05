using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HandyUI_PersonalWorkCategories.Patch;

[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
[HarmonyPriority(999)]
internal class DefGenerator__GenerateImpliedDefs_PostResolve
{
    private static void Prefix()
    {
        try
        {
            var IsChangesNeeded = PersonalWorkCategories.Settings.InitModSettings(
                DefDatabase<WorkTypeDef>.AllDefsListForReading,
                DefDatabase<WorkGiverDef>.AllDefsListForReading);

            if (!IsChangesNeeded)
            {
                return;
            }

            ChangeWorkTypes();
            ChangeWorkGivers();
        }
        catch (Exception e)
        {
            Log.Error("personalWorkCategories_loadingError".Translate());
            Log.Error("personalWorkCategories_loadingErrorMessage".Translate() + " " + e);
        }
    }

    private static void ChangeWorkTypes()
    {
        var inGameWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy();
        DefDatabase<WorkTypeDef>.Clear();
        var moddedWorkTypes = new List<WorkTypeDef>();

        var mod = PersonalWorkCategories.Settings;

        var allWorkTypes = mod.selectedPreset.workTypes;
        var i = 0;
        foreach (var workType in allWorkTypes)
        {
            WorkTypeDef workTypeDef;
            if (workType.IsExtra())
            {
                var extraData = workType.extraData;

                workTypeDef = new WorkTypeDef
                {
                    defName = workType.defName,
                    labelShort = extraData.labelShort,
                    pawnLabel = string.IsNullOrEmpty(extraData.pawnLabel)
                        ? "personalWorkCategories_defaultPawnLabel".Translate().RawText
                        : extraData.pawnLabel,
                    gerundLabel = string.IsNullOrEmpty(extraData.gerundLabel)
                        ? "personalWorkCategories_defaultGerungLabel".Translate().RawText
                        : extraData.gerundLabel,
                    description = string.IsNullOrEmpty(extraData.description)
                        ? "personalWorkCategories_defaultDescription".Translate().RawText
                        : extraData.description,
                    verb = string.IsNullOrEmpty(extraData.verb)
                        ? "personalWorkCategories_defaultVerb".Translate().RawText
                        : extraData.verb,
                    relevantSkills = extraData.skills.ConvertAll(s => DefDatabase<SkillDef>.GetNamed(s))
                };

                if (workType.IsRooted())
                {
                    var rootDef = inGameWorkTypes.Find(wt => wt.defName == workType.extraData.root);
                    if (rootDef == null)
                    {
                        Log.Message($"Can't find work type {workType.defName}");
                        continue;
                    }

                    workTypeDef.alwaysStartActive = rootDef.alwaysStartActive;
                    workTypeDef.requireCapableColonist = rootDef.requireCapableColonist;
                    workTypeDef.workTags = rootDef.workTags;
                    workTypeDef.relevantSkills = rootDef.relevantSkills;
                    workTypeDef.alwaysStartActive = rootDef.alwaysStartActive;
                    workTypeDef.disabledForSlaves = rootDef.disabledForSlaves;
                    workTypeDef.requireCapableColonist = rootDef.requireCapableColonist;
                }
            }
            else
            {
                workTypeDef = inGameWorkTypes.Find(wt => wt.defName == workType.defName);
                if (workTypeDef == null)
                {
                    Log.Message($"Can't find work type {workType.defName}");
                    continue;
                }
            }

            workTypeDef.naturalPriority = (allWorkTypes.Count - i) * 50;
            if (workType.workGivers.Count <= 0)
            {
                workTypeDef.visible = false;
            }

            moddedWorkTypes.Add(workTypeDef);

            i++;
        }

        DefDatabase<WorkTypeDef>.Add(moddedWorkTypes);
    }

    private static void ChangeWorkGivers()
    {
        var mod = PersonalWorkCategories.Settings;

        var allWorkTypes = mod.selectedPreset.workTypes;
        foreach (var workType in allWorkTypes)
        {
            var i = 0;
            foreach (var workGiver in workType.workGivers.ListFullCopy())
            {
                var workGiverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail(workGiver.defName);
                if (workGiverDef == null)
                {
                    if (mod.selectedPreset.isBuildingWorksSplitted &&
                        workGiver.defName == Const.PLACE_QUALITY_FRAME_DEF_NAME)
                    {
                        var placeFrameDef = DefDatabase<WorkGiverDef>.GetNamed(Const.PLACE_FRAME_DEF_NAME);
                        var placeQualityFrameDef = Gen.MemberwiseClone(placeFrameDef);
                        //TODO: Create separate workgiver for Quality frames and block quality frames from the starndard workgiver
                    }
                    else
                    {
                        workType.workGivers.Remove(workGiver);
                        Log.Message($"Can't find work giver {workGiver.defName}");
                    }

                    continue;
                }

                workGiverDef.workType = DefDatabase<WorkTypeDef>.GetNamed(workType.defName);
                workGiverDef.priorityInType = (workType.workGivers.Count - i) * 10;

                i++;
            }
        }
    }
}