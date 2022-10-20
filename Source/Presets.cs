using HandyUI_PersonalWorkCategories.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HandyUI_PersonalWorkCategories
{
    class PresetManager : IExposable
    {
        public Preset DEFAULT_PRESET;
        private List<Preset> userPresets = new List<Preset>();

        public List<Preset> presets
        {
            get
            {
                List<Preset> allPresets = new List<Preset>();
                allPresets.Add(DEFAULT_PRESET);
                allPresets.AddRange(userPresets);
                return allPresets;
            }
        }
        public PresetManager()
        {
        }

        public PresetManager(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
        {
            UpdateDefaultPreset(defaultWorkTypes, defaultWorkGivers);
        }

        public void UpdateDefaultPreset(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
        {
            DEFAULT_PRESET = new Preset();
            DEFAULT_PRESET.name = "personalWorkCategories_default".Translate();
            DEFAULT_PRESET.workTypes = defaultWorkTypes.ConvertAll(wtDef => new WorkType(wtDef.defName));

            foreach (WorkGiverDef wgDef in defaultWorkGivers)
            {
                WorkGiver workGiver = new WorkGiver(wgDef);
                WorkType workType = DEFAULT_PRESET.workTypes.Find(wt => wt.defName == wgDef.workType.defName);

                if (workType != null) workType.workGivers.Add(workGiver);
            }

            DEFAULT_PRESET.hash = ComputePresetHash(defaultWorkTypes, defaultWorkGivers);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look<Preset>(ref DEFAULT_PRESET, "DefaultPreset");
            Scribe_Collections.Look<Preset>(ref userPresets, "UserPresets", LookMode.Deep);
        }

        public string ComputePresetHash(List<WorkTypeDef> workTypes, List<WorkGiverDef> workGivers)
        {
            string stringToHash = workTypes.ToStringSafeEnumerable() + workGivers.ToStringSafeEnumerable();
            string hash = Sha256Util.ComputeSha256Hash(stringToHash);

            return hash;
        }

        internal void RenamePreset(Preset preset, string newName)
        {
            if (preset.name == newName) return;
            CommonUtils.CheckNameForUnique(ref newName, presets.ConvertAll<string>(p => p.name));
            preset.name = newName;
        }

        public Preset CopyPreset(Preset preset, string prefix = "", string postfix = "")
        {
            string newPresetName = prefix + preset + postfix;
            CommonUtils.CheckNameForUnique(ref newPresetName, presets.ConvertAll<string>(p => p.name));

            Preset newPreset = new Preset(preset)
            {
                name = newPresetName
            };

            userPresets.Add(newPreset);
            return newPreset;
        }

        /// <summary>Delete preset and return the index of deleted preset.</summary>
        public int DeletePreset(Preset preset)
        {
            if (preset == DEFAULT_PRESET) return -1;

            int presetIndex = userPresets.IndexOf(preset);
            if (presetIndex < 0) return -1;

            userPresets.RemoveAt(presetIndex);

            return presetIndex;
        }

        public bool SetWorkTypeContentToDefault(Preset selectedPreset, string workTypeDefName)
        {
            WorkType patternWT = DEFAULT_PRESET.workTypes.Find(wt => wt.defName == workTypeDefName);
            List<WorkGiver> pattern = patternWT.workGivers;

            return selectedPreset.SetWorkTypeContentToSample(workTypeDefName, pattern);
        }

        public WorkType GetDefaultTypeOfGiver(WorkGiver workGiver)
        {
            return DEFAULT_PRESET.FindWorkTypeByContainedGiverName(workGiver.defName);
        }

        internal bool DeleteWorkTypeInPreset(Preset preset, string workTypeDefName)
        {
            WorkType workType = preset.FindWorkTypeByDefName(workTypeDefName);

            if (workType == null || !workType.IsExtra()) return false;

            List<WorkGiver> workGivers = workType.workGivers;

            foreach (WorkGiver workGiver in workGivers)
            {
                WorkType defaultParent = GetDefaultTypeOfGiver(workGiver);
                WorkType targetParent = preset.FindWorkTypeByDefName(defaultParent.defName);
                targetParent.workGivers.Add(workGiver);
            }

            return true;
        }
    }

    public class Preset : IExposable, ICloneable
    {
        public string name;
        public string hash;
        public bool isAdvanced;
        public List<WorkType> workTypes = new List<WorkType>();

        public Preset() {}
        
        public Preset(Preset preset)
        {
            this.name = preset.name;
            this.hash = preset.hash;
            this.isAdvanced = preset.isAdvanced;

            foreach (WorkType workType in preset.workTypes)
            {
                WorkType workTypeCopy = new WorkType(workType);
                workTypeCopy.workGivers = workType.workGivers.ConvertAll(wg => new WorkGiver(wg));

                this.workTypes.Add(workTypeCopy);
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref name, "Name");
            Scribe_Values.Look<string>(ref hash, "Hash");
            Scribe_Values.Look<bool>(ref isAdvanced, "IsAdvanced");
            Scribe_Collections.Look<WorkType>(ref workTypes, "WorkTypes");
        }

        internal bool SetWorkTypeContentToSample(string workTypeDefName, List<WorkGiver> pattern)
        {
            WorkType target = workTypes.Find(wt => wt.defName == workTypeDefName);
            if (target == null) return false;

            foreach (WorkType wt in workTypes)
            {
                foreach (WorkGiver wg in wt.workGivers.ListFullCopy())
                {
                    if (pattern.Find(pwg => pwg.defName == wg.defName) != null)
                    {
                        wt.workGivers.Remove(wg);
                        target.workGivers.Add(wg);
                    }
                }
            }

            return true;
        }

        public WorkType FindWorkTypeByDefName(string defName)
        {
            return workTypes.Find(wt => wt.defName == defName);
        }

        public WorkType FindWorkTypeByContainedGiverName(string defName)
        {
            foreach (WorkType workType in workTypes)
            {
                if (workType.workGivers.Find(wg => wg.defName == defName) != null)
                {
                    return workType;
                }
            }

            return null;
        }

        public WorkType FindWorkTypeOfWorkGiver(WorkGiver workGiver)
        {
            foreach (WorkType workType in workTypes)
            {
                if (workType.workGivers.Contains(workGiver))
                {
                    return workType;
                }
            }

            return null;
        }

        public bool MoveWorkTypeToPosition(WorkType targetWorkType, WorkType positionWorkType)
        {
            if (!workTypes.Contains(targetWorkType) || !workTypes.Contains(positionWorkType)) return false;

            workTypes.Remove(targetWorkType);
            int index = workTypes.IndexOf(positionWorkType) ;
            workTypes.Insert(index, targetWorkType);

            return true;
        }

        public WorkType SplitWorkType(string workTypeDefName)
        {
            WorkType workType = FindWorkTypeByDefName(workTypeDefName);
            if (workType == null || workType.IsExtra()) return null;

            string extraWorkTypeDefName = workType.defName;
            CommonUtils.CheckNameForUnique(ref extraWorkTypeDefName, workTypes.ConvertAll(wt => wt.defName), false);

            string extraWorkTypeLabel = workType.GetLabel();
            CommonUtils.CheckNameForUnique(ref extraWorkTypeLabel, workTypes.ConvertAll(wt => wt.GetLabel()));

            WorkType extraWorkType = new WorkType(extraWorkTypeDefName, workType.defName, extraWorkTypeLabel);

            int newWorkTypeIndex = workTypes.IndexOf(workType) + 1;
            workTypes.Insert(newWorkTypeIndex, extraWorkType);

            return extraWorkType;
        }

        public WorkType CreateNewCustomWorkType()
        {
            string extraWorkTypeDefName = "custom";
            CommonUtils.CheckNameForUnique(ref extraWorkTypeDefName, workTypes.ConvertAll(wt => wt.defName), false);

            string extraWorkTypeLabel = "personalWorkCategories_defaultGroupLabel".Translate();
            CommonUtils.CheckNameForUnique(ref extraWorkTypeLabel, workTypes.ConvertAll(wt => wt.GetLabel()));

            WorkType extraWorkType = new WorkType(extraWorkTypeDefName, true);
            extraWorkType.extraData.labelShort = extraWorkTypeLabel;

            workTypes.Add(extraWorkType);

            return extraWorkType;
        }

        public void InsertWorkTypeByPriority(WorkType workType)
        {
            int insertedPriority = DefDatabase<WorkTypeDef>.GetNamed(workType.defName).naturalPriority;
            bool isInserted = false;

            for (int i = workTypes.Count - 1; i >= 0; i--)
            {
                if (workTypes[i].IsExtra()) continue;

                WorkTypeDef workTypeDef = DefDatabase<WorkTypeDef>.GetNamed(workTypes[i].defName);
                if (workTypeDef == null)
                {
                    continue;
                }

                if (workTypeDef.naturalPriority >= insertedPriority)
                {
                    workTypes.Insert(i + 1, workType);
                    isInserted = true;
                    break;
                }

                if (i == 0)
                {
                    workTypes.Insert(0, workType);
                    isInserted = true;
                    break;
                }
            }

            if (!isInserted)
            {
                workTypes.Add(workType);
            }
        }

        public object Clone()
        {
            return new Preset(this);
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
