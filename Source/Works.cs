using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HandyUI_PersonalWorkCategories
{
    abstract public class WorkCommon : ICloneable, IExposable
    {
        public string defName;

        public WorkCommon() { }

        public WorkCommon(string defName)
        {
            this.defName = defName;
        }

        public WorkCommon(WorkCommon source)
        {
            this.defName = source.defName;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look<string>(ref defName, "defName");
        }

        abstract public string GetLabel();

        abstract public object Clone();
    }
    public class WorkType : WorkCommon
    {
        public ExtraData extraData;
        public List<WorkGiver> workGivers = new List<WorkGiver>();

        public WorkType() {}

        public WorkType(string defName, bool isCustom = false) : base (defName) 
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
                extraData = new ExtraData(source.extraData);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ExtraData>(ref extraData, "ExtraData", false);
            Scribe_Collections.Look<WorkGiver>(ref workGivers, "WorkGivers", LookMode.Deep);
        }

        public void InsertWorkGiverByPriority(WorkGiver workGiver)
        {
            int insertedPriority = DefDatabase<WorkGiverDef>.GetNamed(workGiver.defName).priorityInType;
            bool isInserted = false;

            for (int i = workGivers.Count - 1; i >= 0; i--)
            {
                WorkGiverDef workGiverDef = DefDatabase<WorkGiverDef>.GetNamed(workGivers[i].defName);
                if (workGiverDef == null)
                {
                    Log.Message("Can'find WorkGiverDef: " + workGivers[i].defName);
                    continue;
                }

                if (workGiverDef.priorityInType >= insertedPriority)
                {
                    workGivers.Insert(i + 1, workGiver);
                    isInserted = true;
                    break;
                }

                if (i == 0)
                {
                    workGivers.Insert(0, workGiver);
                    isInserted = true;
                    break;
                }
            }

            if (!isInserted)
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
            else
            {
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamed(defName);
                if (def == null) return defName;

                return def.labelShort;
            }
        }

        public bool IsExtra()
        {
            return extraData != null;
        }

        public bool IsCustom()
        {
            return extraData != null && extraData.root == null;
        }

        public bool IsRooted()
        {
            return extraData != null && extraData.root != null;
        }

        public override object Clone()
        {
            return new WorkType(this);
        }

        public override string ToString()
        {
            return defName + " (" + workGivers.ToStringSafeEnumerable() + ")";
        }

        public class ExtraData : IExposable
        {
            public string root;
            public string labelShort;
            public string pawnLabel;
            public string gerundLabel;
            public string description;
            public string verb;
            public List<string> skills = new List<string>();

            public ExtraData(bool setDefaultValues = false)
            {
                if (setDefaultValues)
                {
                    this.labelShort = "personalWorkCategories_defaultGroupLabel".Translate();
                    this.pawnLabel = "personalWorkCategories_defaultPawnLabel".Translate();
                    this.gerundLabel = "personalWorkCategories_defaultGerungLabel".Translate();
                    this.description = "personalWorkCategories_defaultDescription".Translate();
                    this.verb = "personalWorkCategories_defaultVerb".Translate();
                }
            }

            public ExtraData(ExtraData source)
            {
                this.root = source.root;
                this.labelShort = source.labelShort;
                this.pawnLabel = source.pawnLabel;
                this.gerundLabel = source.gerundLabel;
                this.description = source.description;
                this.verb = source.verb;
                this.skills = source.skills.ListFullCopy();
            }

            public ExtraData(string root, string lable = null)
            {
                WorkTypeDef wtDef = DefDatabase<WorkTypeDef>.GetNamed(root);

                this.root = root;
                if (lable == null)
                    this.labelShort = wtDef.labelShort;
                else
                    this.labelShort = lable;
                this.pawnLabel = wtDef.pawnLabel;
                this.gerundLabel = wtDef.gerundLabel;
                this.description = wtDef.description;
                this.verb = wtDef.verb;
                this.skills = wtDef.relevantSkills.ConvertAll(sd => sd.defName);
            }

            public void ExposeData()
            {
                Scribe_Values.Look<string>(ref root, "root");
                Scribe_Values.Look<string>(ref labelShort, "labelShort");
                Scribe_Values.Look<string>(ref pawnLabel, "pawnLabel");
                Scribe_Values.Look<string>(ref gerundLabel, "gerundLabel");
                Scribe_Values.Look<string>(ref description, "description");
                Scribe_Values.Look<string>(ref verb, "verb");
                Scribe_Collections.Look<string>(ref skills, "skills", LookMode.Value);
            }
        }
    }

    public class WorkGiver : WorkCommon
    {
        public WorkGiver() {}

        public WorkGiver(WorkCommon source) : base(source) {}

        public WorkGiver(WorkGiverDef wgDef)
        {
            this.defName = wgDef.defName;
        }

        public override string GetLabel()
        {
            WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamed(defName);
            if (def == null) return defName;

            return def.label + (def.emergency ? " (E)" : "");
        }

        public override object Clone()
        {
            return new WorkGiver(this);
        }

        public override string ToString()
        {
            return defName;
        }
    }
}
