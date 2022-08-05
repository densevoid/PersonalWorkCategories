using RimWorld;
using Verse;

namespace HandyUI_PersonalWorkCategories;

public class WorkGiver : WorkCommon
{
    public WorkGiver()
    {
    }

    public WorkGiver(WorkCommon source) : base(source)
    {
    }

    public WorkGiver(WorkGiverDef wgDef)
    {
        defName = wgDef.defName;
    }

    public override string GetLabel()
    {
        var def = DefDatabase<WorkGiverDef>.GetNamed(defName);
        if (def == null)
        {
            return defName;
        }

        return def.label + (def.emergency ? " (E)" : "");
    }

    public override object Clone()
    {
        return new WorkGiver(this);
    }
}