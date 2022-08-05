using System;
using Verse;

namespace HandyUI_PersonalWorkCategories;

public abstract class WorkCommon : ICloneable, IExposable
{
    public string defName;

    public WorkCommon()
    {
    }

    public WorkCommon(string defName)
    {
        this.defName = defName;
    }

    public WorkCommon(WorkCommon source)
    {
        defName = source.defName;
    }

    public abstract object Clone();

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName");
    }

    public abstract string GetLabel();
}