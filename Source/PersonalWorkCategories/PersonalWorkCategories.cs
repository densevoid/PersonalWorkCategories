using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace HandyUI_PersonalWorkCategories;

public class PersonalWorkCategories : Mod
{
    public static PersonalWorkCategoriesSettings Settings;

    public PersonalWorkCategories(ModContentPack content) : base(content)
    {
        Settings = GetSettings<PersonalWorkCategoriesSettings>();
        new Harmony("densevoid.hui.personalworkcat").PatchAll(Assembly.GetExecutingAssembly());
    }

    public override string SettingsCategory()
    {
        return "Personal Work Categories";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
        base.DoSettingsWindowContents(inRect);
    }
}