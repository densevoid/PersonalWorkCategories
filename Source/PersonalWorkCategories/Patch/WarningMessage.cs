using UnityEngine;
using Verse;

namespace HandyUI_PersonalWorkCategories.Patch;

internal class WarningMessage : Window
{
    private readonly string message;
    private readonly PreClose preClose;
    private readonly string title;

    public WarningMessage(string title, string message, PreClose preClose)
    {
        this.title = title;
        this.message = message;
        this.preClose = preClose;

        doCloseButton = true;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new Vector2(500f, 300f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(inRect, title);
        GUI.color = Color.gray;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(inRect.ContractedBy(25f, 65f), message);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    public override void PreClose()
    {
        base.PreClose();
        preClose();
    }
}