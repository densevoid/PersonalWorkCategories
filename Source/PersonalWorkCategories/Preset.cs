using System;
using System.Collections.Generic;
using HandyUI_PersonalWorkCategories.Utils;
using Verse;

namespace HandyUI_PersonalWorkCategories;

public class Preset : IExposable, ICloneable
{
    public string hash;
    public bool isAdvanced;
    public bool isBuildingWorksSplitted;
    public string name;

    public List<WorkType> workTypes = new List<WorkType>();

    public Preset()
    {
    }

    public Preset(Preset preset)
    {
        name = preset.name;
        hash = preset.hash;
        isAdvanced = preset.isAdvanced;

        foreach (var workType in preset.workTypes)
        {
            var workTypeCopy = new WorkType(workType)
            {
                workGivers = workType.workGivers.ConvertAll(wg => new WorkGiver(wg))
            };

            workTypes.Add(workTypeCopy);
        }
    }

    public object Clone()
    {
        return new Preset(this);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref name, "Name");
        Scribe_Values.Look(ref hash, "Hash");
        Scribe_Values.Look(ref isAdvanced, "IsAdvanced");
        Scribe_Values.Look(ref isBuildingWorksSplitted, "IsBuildingWorksSplitted");
        Scribe_Collections.Look(ref workTypes, "WorkTypes");
    }

    public WorkGiver FindWorkGiverByDefName(string defName)
    {
        foreach (var workType in workTypes)
        {
            var workGiver = workType.workGivers.Find(wg => wg.defName == defName);
            if (workGiver != null)
            {
                return workGiver;
            }
        }

        return null;
    }

    internal bool SetWorkTypeContentToSample(string workTypeDefName, List<WorkGiver> pattern)
    {
        var target = workTypes.Find(wt => wt.defName == workTypeDefName);
        if (target == null)
        {
            return false;
        }

        foreach (var wt in workTypes)
        {
            foreach (var wg in wt.workGivers.ListFullCopy())
            {
                if (pattern.Find(pwg => pwg.defName == wg.defName) == null)
                {
                    continue;
                }

                wt.workGivers.Remove(wg);
                target.workGivers.Add(wg);
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
        foreach (var workType in workTypes)
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
        foreach (var workType in workTypes)
        {
            if (workType.workGivers.Contains(workGiver))
            {
                return workType;
            }
        }

        return null;
    }

    public void MoveWorkTypeToPosition(WorkType targetWorkType, WorkType positionWorkType)
    {
        if (!workTypes.Contains(targetWorkType) || !workTypes.Contains(positionWorkType))
        {
            return;
        }

        workTypes.Remove(targetWorkType);
        var index = workTypes.IndexOf(positionWorkType) + 1;
        workTypes.Insert(index, targetWorkType);
    }

    public WorkType SplitWorkType(string workTypeDefName)
    {
        var workType = FindWorkTypeByDefName(workTypeDefName);
        if (workType == null || workType.IsExtra())
        {
            return null;
        }

        var extraWorkTypeDefName = workType.defName;
        CommonUtils.CheckNameForUnique(ref extraWorkTypeDefName, workTypes.ConvertAll(wt => wt.defName), false);

        var extraWorkTypeLabel = workType.GetLabel();
        CommonUtils.CheckNameForUnique(ref extraWorkTypeLabel, workTypes.ConvertAll(wt => wt.GetLabel()));

        var extraWorkType = new WorkType(extraWorkTypeDefName, workType.defName, extraWorkTypeLabel);

        var newWorkTypeIndex = workTypes.IndexOf(workType) + 1;
        workTypes.Insert(newWorkTypeIndex, extraWorkType);

        return extraWorkType;
    }

    public WorkType CreateNewCustomWorkType()
    {
        var extraWorkTypeDefName = "custom";
        CommonUtils.CheckNameForUnique(ref extraWorkTypeDefName, workTypes.ConvertAll(wt => wt.defName), false);

        string extraWorkTypeLabel = "personalWorkCategories_defaultGroupLabel".Translate();
        CommonUtils.CheckNameForUnique(ref extraWorkTypeLabel, workTypes.ConvertAll(wt => wt.GetLabel()));

        var extraWorkType = new WorkType(extraWorkTypeDefName, true)
        {
            extraData =
            {
                labelShort = extraWorkTypeLabel
            }
        };

        workTypes.Add(extraWorkType);

        return extraWorkType;
    }

    public void InsertWorkTypeByPriority(WorkType workType)
    {
        var insertedPriority = DefDatabase<WorkTypeDef>.GetNamed(workType.defName).naturalPriority;

        var insertIndex = -1;
        foreach (var compareWorkType in workTypes)
        {
            var comparePriority = DefDatabase<WorkTypeDef>.GetNamed(compareWorkType.defName).naturalPriority;
            if (insertedPriority <= comparePriority)
            {
                continue;
            }

            insertIndex = workTypes.IndexOf(compareWorkType);
            break;
        }

        if (insertIndex >= 0)
        {
            workTypes.Insert(insertIndex, workType);
        }
        else
        {
            workTypes.Add(workType);
        }
    }

    public override string ToString()
    {
        return name;
    }
}