using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HandyUI_PersonalWorkCategories;

public class WorkType : WorkCommon
{
    public ExtraData extraData;
    public List<WorkGiver> workGivers = new List<WorkGiver>();

    public WorkType()
    {
    }

    public WorkType(string defName, bool isCustom = false) : base(defName)
    {
        if (isCustom)
        {
            extraData = new ExtraData(true);
        }
    }

    public WorkType(string defName, string rootDefName, string label = null) : this(defName)
    {
        extraData = new ExtraData(rootDefName, label);
    }

    public WorkType(WorkType source) : this(source.defName)
    {
        if (source.extraData != null)
        {
            extraData = new ExtraData(source.extraData);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref extraData, "ExtraData", false);
        Scribe_Collections.Look(ref workGivers, "WorkGivers", LookMode.Deep);
    }

    public void InsertWorkGiverByPriority(WorkGiver workGiver)
    {
        var insertedPriority = DefDatabase<WorkGiverDef>.GetNamed(workGiver.defName).priorityInType;

        var insertIndex = -1;
        foreach (var compareWorkGiver in workGivers)
        {
            var comparePriority = DefDatabase<WorkGiverDef>.GetNamed(compareWorkGiver.defName).priorityInType;
            if (insertedPriority <= comparePriority)
            {
                continue;
            }

            insertIndex = workGivers.IndexOf(compareWorkGiver);
            break;
        }

        if (insertIndex >= 0)
        {
            workGivers.Insert(insertIndex, workGiver);
        }
        else
        {
            workGivers.Add(workGiver);
        }
    }

    public override string GetLabel()
    {
        if (extraData != null)
        {
            return extraData.labelShort;
        }

        var def = DefDatabase<WorkTypeDef>.GetNamed(defName);
        if (def == null)
        {
            return defName;
        }

        return def.labelShort;
    }

    public bool IsExtra()
    {
        return extraData != null;
    }

    public bool IsCustom()
    {
        return extraData is { root: null };
    }

    public bool IsRooted()
    {
        return extraData is { root: { } };
    }

    public override object Clone()
    {
        return new WorkType(this);
    }

    public class ExtraData : IExposable
    {
        public string description;
        public string gerundLabel;
        public string labelShort;
        public string pawnLabel;
        public string root;
        public List<string> skills = new List<string>();
        public string verb;

        public ExtraData(bool setDefaultValues = false)
        {
            if (!setDefaultValues)
            {
                return;
            }

            labelShort = "personalWorkCategories_defaultGroupLabel".Translate();
            pawnLabel = "personalWorkCategories_defaultPawnLabel".Translate();
            gerundLabel = "personalWorkCategories_defaultGerungLabel".Translate();
            description = "personalWorkCategories_defaultDescription".Translate();
            verb = "personalWorkCategories_defaultVerb".Translate();
        }

        public ExtraData(ExtraData source)
        {
            root = source.root;
            labelShort = source.labelShort;
            pawnLabel = source.pawnLabel;
            gerundLabel = source.gerundLabel;
            description = source.description;
            verb = source.verb;
            skills = source.skills.ListFullCopy();
        }

        public ExtraData(string root, string lable = null)
        {
            var wtDef = DefDatabase<WorkTypeDef>.GetNamed(root);

            this.root = root;
            if (lable == null)
            {
                labelShort = wtDef.labelShort;
            }
            else
            {
                labelShort = lable;
            }

            pawnLabel = wtDef.pawnLabel;
            gerundLabel = wtDef.gerundLabel;
            description = wtDef.description;
            verb = wtDef.verb;
            skills = wtDef.relevantSkills.ConvertAll(sd => sd.defName);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref root, "root");
            Scribe_Values.Look(ref labelShort, "labelShort");
            Scribe_Values.Look(ref pawnLabel, "pawnLabel");
            Scribe_Values.Look(ref gerundLabel, "gerundLabel");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref verb, "verb");
            Scribe_Collections.Look(ref skills, "skills", LookMode.Value);
        }
    }
}