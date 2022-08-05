using System.Collections.Generic;
using HandyUI_PersonalWorkCategories.Utils;
using RimWorld;
using Verse;

namespace HandyUI_PersonalWorkCategories;

internal class PresetManager : IExposable
{
    public Preset DEFAULT_PRESET;
    private List<Preset> userPresets = new List<Preset>();

    public PresetManager()
    {
    }

    public PresetManager(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
    {
        DEFAULT_PRESET = new Preset
        {
            name = "personalWorkCategories_default".Translate(),
            workTypes = defaultWorkTypes.ConvertAll(wtDef => new WorkType(wtDef.defName))
        };

        foreach (var wgDef in defaultWorkGivers)
        {
            var workGiver = new WorkGiver(wgDef);
            var workType = DEFAULT_PRESET.workTypes.Find(wt => wt.defName == wgDef.workType.defName);

            if (workType != null)
            {
                workType.workGivers.Add(workGiver);
            }
        }

        DEFAULT_PRESET.hash = ComputePresetHash(defaultWorkTypes, defaultWorkGivers);
    }

    public List<Preset> presets
    {
        get
        {
            var allPresets = new List<Preset> { DEFAULT_PRESET };
            allPresets.AddRange(userPresets);
            return allPresets;
        }
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref DEFAULT_PRESET, "DefaultPreset");
        Scribe_Collections.Look(ref userPresets, "UserPresets", LookMode.Deep);
    }

    public string ComputePresetHash(List<WorkTypeDef> workTypes, List<WorkGiverDef> workGivers)
    {
        var stringToHash = workTypes.ToStringSafeEnumerable() + workGivers.ToStringSafeEnumerable();
        var hash = Sha256Util.ComputeSha256Hash(stringToHash);

        return hash;
    }

    internal void RenamePreset(Preset preset, string newName)
    {
        if (preset.name == newName)
        {
            return;
        }

        CommonUtils.CheckNameForUnique(ref newName, presets.ConvertAll(p => p.name));
        preset.name = newName;
    }

    public Preset CopyPreset(Preset preset, string prefix = "", string postfix = "")
    {
        var newPresetName = prefix + preset + postfix;
        CommonUtils.CheckNameForUnique(ref newPresetName, presets.ConvertAll(p => p.name));

        var newPreset = new Preset(preset)
        {
            name = newPresetName
        };

        userPresets.Add(newPreset);
        return newPreset;
    }

    /// <summary>Delete preset and return the index of deleted preset.</summary>
    public int DeletePreset(Preset preset)
    {
        if (preset == DEFAULT_PRESET)
        {
            return -1;
        }

        var presetIndex = userPresets.IndexOf(preset);
        if (presetIndex < 0)
        {
            return presetIndex;
        }

        userPresets.RemoveAt(presetIndex);

        return presetIndex + 1;
    }

    public void SetWorkTypeContentToDefault(Preset selectedPreset, string workTypeDefName)
    {
        var patternWT = DEFAULT_PRESET.workTypes.Find(wt => wt.defName == workTypeDefName);
        var pattern = patternWT.workGivers;

        selectedPreset.SetWorkTypeContentToSample(workTypeDefName, pattern);
    }

    public WorkType GetDefaultTypeOfGiver(WorkGiver workGiver)
    {
        return DEFAULT_PRESET.FindWorkTypeByContainedGiverName(workGiver.defName);
    }

    internal bool DeleteWorkTypeInPreset(Preset preset, string workTypeDefName)
    {
        var workType = preset.FindWorkTypeByDefName(workTypeDefName);

        if (workType == null || !workType.IsExtra())
        {
            return false;
        }

        var workGivers = workType.workGivers;

        foreach (var workGiver in workGivers)
        {
            var defaultParent = GetDefaultTypeOfGiver(workGiver);
            var targetParent = preset.FindWorkTypeByDefName(defaultParent.defName);
            targetParent.workGivers.Add(workGiver);
        }

        return true;
    }
}