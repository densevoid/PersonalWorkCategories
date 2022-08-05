using UnityEngine;

namespace HandyUI_PersonalWorkCategories;

internal class WorkGiverFloatingElement : FloatingElement
{
    public WorkGiverFloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, WorkGiver workGiver) : base(
        settings, rect, workGiver)
    {
    }

    public override void DoWindowContents(Rect inRect)
    {
        settings.DrawWorkGiverContent(inRect, (WorkGiver)element);
    }

    public override DragReaction DoDragReaction(WorkCommon target, PersonalWorkCategoriesSettings.ElementStatus status)
    {
        if (target is WorkType targetAsWorkType)
        {
            if (status == PersonalWorkCategoriesSettings.ElementStatus.Available)
            {
                settings.SetSelectedWorkType(targetAsWorkType);
                return DragReaction.Insert;
            }
        }

        if (target is WorkGiver)
        {
            return DragReaction.Spread;
        }

        return DragReaction.Nothing;
    }
}