using UnityEngine;
using Verse;
using static HandyUI_PersonalWorkCategories.PersonalWorkCategoriesSettings;

namespace HandyUI_PersonalWorkCategories;

internal abstract class FloatingElement : Window
{
    public enum DragReaction
    {
        Nothing,
        Insert,
        Spread
    }

    protected static Vector2? dragOffset = new Vector2(-5f, -5f);
    public readonly WorkCommon element;
    protected readonly PersonalWorkCategoriesSettings settings;
    protected Rect rect;

    public FloatingElement(PersonalWorkCategoriesSettings settings, Rect rect, WorkCommon element,
        Vector2? dragOffset = null)
    {
        this.settings = settings;
        this.rect = rect;
        this.element = element;

        layer = WindowLayer.Super;
        closeOnClickedOutside = true;
        doWindowBackground = false;
        drawShadow = true;
    }

    protected override float Margin => 0f;

    public override Vector2 InitialSize => rect.size;

    public abstract DragReaction DoDragReaction(WorkCommon target, ElementStatus status);

    protected override void SetInitialSizeAndPosition()
    {
        var vector = UI.MousePositionOnUIInverted;
        if (dragOffset != null)
        {
            vector -= dragOffset.Value;
        }

        if (vector.x + InitialSize.x > UI.screenWidth)
        {
            vector.x = UI.screenWidth - InitialSize.x;
        }

        if (vector.y + InitialSize.y > UI.screenHeight)
        {
            vector.y = UI.screenHeight - InitialSize.y;
        }

        windowRect = new Rect(vector.x, vector.y, InitialSize.x, InitialSize.y);
    }

    public override void WindowUpdate()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Close();
            return;
        }

        SetInitialSizeAndPosition();
        base.WindowUpdate();
    }

    public override void PostClose()
    {
        settings.DraggedElementDropped();
        base.PostClose();
    }
}