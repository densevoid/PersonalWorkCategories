using UnityEngine;

namespace HandyUI_PersonalWorkCategories;

internal class WorkTypeFloatingElement : FloatingElement
{
    public WorkTypeFloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, WorkType workType) : base(
        settings, rect, workType)
    {
    }

    public override void DoWindowContents(Rect inRect)
    {
        settings.DrawWorkTypeContent(inRect, (WorkType)element);
    }

    public override DragReaction DoDragReaction(WorkCommon target, PersonalWorkCategoriesSettings.ElementStatus status)
    {
        if (target is WorkType)
        {
            return DragReaction.Spread;
        }

        return DragReaction.Nothing;
    }
}