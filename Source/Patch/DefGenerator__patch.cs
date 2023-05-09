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
                bool IsChangesNeeded = PersonalWorkCategories.Settings.Initialize(
                    DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy(),
                    DefDatabase<WorkGiverDef>.AllDefsListForReading.ListFullCopy());

                if (IsChangesNeeded)
                {
                    ChangeWorkTypes();
                    ChangeWorkGivers();
                    ChangeMechEnabledWorkTypes();
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
            List<WorkTypeDef> defaultWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy();

            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<WorkType> customWorkTypes = mod.selectedPreset.workTypes;

            for (int i = 0; i < customWorkTypes.Count; i++)
            {
                WorkType customWorkType = customWorkTypes[i];

                WorkTypeDef workTypeDef;

                if (customWorkType.IsExtra())
                {
                    WorkType.ExtraData extraData = customWorkType.extraData;

                    workTypeDef = new WorkTypeDef();

                    workTypeDef.defName = customWorkType.defName;
                    workTypeDef.labelShort = extraData.labelShort;
                    workTypeDef.pawnLabel = string.IsNullOrEmpty(extraData.pawnLabel) ? "personalWorkCategories_defaultPawnLabel".Translate().RawText : extraData.pawnLabel;
                    workTypeDef.gerundLabel = string.IsNullOrEmpty(extraData.gerundLabel) ? "personalWorkCategories_defaultGerungLabel".Translate().RawText : extraData.gerundLabel;
                    workTypeDef.description = string.IsNullOrEmpty(extraData.description) ? "personalWorkCategories_defaultDescription".Translate().RawText : extraData.description;
                    workTypeDef.verb = string.IsNullOrEmpty(extraData.verb) ? "personalWorkCategories_defaultVerb".Translate().RawText : extraData.verb;
                    workTypeDef.relevantSkills = extraData.skills.ConvertAll(s => DefDatabase<SkillDef>.GetNamed(s));

                    if (customWorkType.IsRooted())
                    {
                        WorkTypeDef rootDef = defaultWorkTypes.Find(wt => wt.defName == customWorkType.extraData.root);
                        if (rootDef == null)
                        {
                            Log.Message("Can't find work type " + customWorkType.defName);
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

                    DefDatabase<WorkTypeDef>.Add(workTypeDef);
                }
                else
                {
                    workTypeDef = defaultWorkTypes.Find(wt => wt.defName == customWorkType.defName);
                    if (workTypeDef == null)
                    {
                        Log.Message("Can't find work type " + customWorkType.defName);
                        continue;
                    }
                }

                workTypeDef.naturalPriority = (customWorkTypes.Count - i) * 50;
                if (customWorkType.workGivers.Count <= 0) workTypeDef.visible = false;
            }

        }

        private static void ChangeWorkGivers()
        {
            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<WorkType> allWorkTypes = mod.selectedPreset.workTypes;
            foreach (WorkType workType in allWorkTypes)
            {
                int i = 0;

                foreach (WorkGiver workGiver in workType.workGivers)
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

        private static void ChangeMechEnabledWorkTypes()
        {
            List<WorkTypeDef> defaultWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.ListFullCopy();

            PersonalWorkCategoriesSettings mod = PersonalWorkCategories.Settings;

            List<WorkType> customWorkTypes = mod.selectedPreset.workTypes;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race == null) continue;

                //find race with mechanoids work types
                foreach (WorkTypeDef workTypeDef in def.race.mechEnabledWorkTypes.ListFullCopy())
                {
                    //go throught custom work types and add child works to this race
                    foreach (WorkType customWorkType in customWorkTypes)
                    {
                        if (customWorkType.IsRooted())
                        {
                            if (workTypeDef.defName == customWorkType.extraData.root)
                            {
                                def.race.mechEnabledWorkTypes.Add(defaultWorkTypes.Find(wt => wt.defName == customWorkType.defName));
                            }
                        }
                    }
                }
            };
        }
    }
}
