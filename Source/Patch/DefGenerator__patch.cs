using HandyUI_PersonalWorkCategories.Utils;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;
using static HandyUI_PersonalWorkCategories.PersonalWorkCategoriesSettings;

namespace HandyUI_PersonalWorkCategories.Patch
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    [HarmonyPriority(999)]
    class DefGenerator__GenerateImpliedDefs_PostResolve
    {
        static void Prefix()
        {            
            try
            {
                bool IsChangesNeeded = PersonalWorkCategories.Settings.InitModSettings(
                    DefDatabase<WorkTypeDef>.AllDefsListForReading,
                    DefDatabase<WorkGiverDef>.AllDefsListForReading);

                if (IsChangesNeeded)
                {
                    ChangeWorkTypes();
                    ChangeWorkGivers();
                }
            }
            catch(Exception e)
            {
                Log.Error("personalWorkCategories_loadingError".Translate());
                Log.Error("personalWorkCategories_loadingErrorMessage".Translate() + " " + e);
            }
        }

        private static void ChangeWorkTypes()
        {
            List<WorkTypeDef> inGameWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy();
            DefDatabase<WorkTypeDef>.Clear();
            List<WorkTypeDef> moddedWorkTypes = new List<WorkTypeDef>();

            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<WorkType> allWorkTypes = mod.selectedPreset.workTypes;
            WorkTypeDef workTypeDef;
            int i = 0;
            foreach (WorkType workType in allWorkTypes)
            {
                if (workType.IsExtra())
                {
                    WorkType.ExtraData extraData = workType.extraData;

                    workTypeDef = new WorkTypeDef();

                    workTypeDef.defName = workType.defName;
                    workTypeDef.labelShort = extraData.labelShort;
                    workTypeDef.pawnLabel = string.IsNullOrEmpty(extraData.pawnLabel) ? "personalWorkCategories_defaultPawnLabel".Translate().RawText : extraData.pawnLabel;
                    workTypeDef.gerundLabel = string.IsNullOrEmpty(extraData.gerundLabel) ? "personalWorkCategories_defaultGerungLabel".Translate().RawText : extraData.gerundLabel;
                    workTypeDef.description = string.IsNullOrEmpty(extraData.description) ? "personalWorkCategories_defaultDescription".Translate().RawText : extraData.description;
                    workTypeDef.verb = string.IsNullOrEmpty(extraData.verb) ? "personalWorkCategories_defaultVerb".Translate().RawText : extraData.verb;
                    workTypeDef.relevantSkills = extraData.skills.ConvertAll(s => DefDatabase<SkillDef>.GetNamed(s));

                    if (workType.IsRooted())
                    {
                        WorkTypeDef rootDef = inGameWorkTypes.Find(wt => wt.defName == workType.extraData.root);
                        if (rootDef == null)
                        {
                            Log.Message("Can't find work type " + workType.defName);
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
                        Log.Message("Can't find work type " + workType.defName);
                        continue;
                    }
                }

                workTypeDef.naturalPriority = (allWorkTypes.Count - i) * 50;
                if (workType.workGivers.Count <= 0) workTypeDef.visible = false;

                moddedWorkTypes.Add(workTypeDef);

                i++;
            }

            DefDatabase<WorkTypeDef>.Add(moddedWorkTypes);
        }

        private static void ChangeWorkGivers()
        {
            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<WorkType> allWorkTypes = mod.selectedPreset.workTypes;
            foreach (WorkType workType in allWorkTypes)
            {
                int i = 0;

                List<WorkGiver> workGivers = workType.workGivers.ListFullCopy();
                workGivers.Sort((a, b) => {
                    WorkGiverDef aDef = DefDatabase<WorkGiverDef>.GetNamed(a.defName);
                    WorkGiverDef bDef = DefDatabase<WorkGiverDef>.GetNamed(b.defName);
                    return aDef.priorityInType >= bDef.priorityInType ? -1 : 1;
                });

                foreach (WorkGiver workGiver in workGivers)
                {
                    WorkGiverDef workGiverDef = DefDatabase<WorkGiverDef>.GetNamed(workGiver.defName);
                    if (workGiverDef == null)
                    {
                        workType.workGivers.Remove(workGiver);
                        Log.Message("Can't find work giver " + workGiver.defName);
                        continue;
                    }

                    workGiverDef.workType = DefDatabase<WorkTypeDef>.GetNamed(workType.defName);
                    workGiverDef.priorityInType = (workType.workGivers.Count - i) * 10;
                    i++;
                }
            }
        }
    }
}
