using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HandyUI_PersonalWorkCategories;

public class PersonalWorkCategoriesSettings : ModSettings
{
    public enum ContentCategory
    {
        Undefined,
        WorkType,
        WorkGiver
    }

    public enum ElementStatus
    {
        Available,
        Forbidden,
        Normal
    }

    private const float ELEMENT_HEIGHT = 50f;
    private const float ELEMENT_GAP = 4f;

    private const float COLUMN_GAP = 5f;
    private const float HALFS_GAP = 10f;
    private const float BUTTONS_GAP = 5f;
    private const float CONTAINER_PADDING = 10f;
    private const float CONTAINERS_GAP = 10f;

    private const float BUTTON_HEIGHT = 30f;
    private readonly DropTarget dropTarget = new DropTarget();

    private Type currentMouseOverColumnType;

    internal FloatingElement draggedElement;
    private string editablePresetName;

    private float elementsColumnHeight;

    private bool isDefaultPreset;
    private bool isHashesMatchUp;
    public bool isSavedDataVersionDeprecated;

    public bool isWorksListWasChanged;

    private MainMenuContent mainMenuContent = MainMenuContent.empty;

    private PresetManager PM;

    public Preset selectedPreset;
    internal WorkType selectedWorkType;
    private int version = 2;
    private Vector2 workGiversScrollPosition = Vector2.zero;
    private Vector2 workTypesScrollPosition = Vector2.zero;

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode != LoadSaveMode.LoadingVars && Scribe.mode != LoadSaveMode.Saving)
        {
            return;
        }

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (PM == null)
            {
                return;
            }

            PM.RenamePreset(selectedPreset, editablePresetName);
            Scribe_Values.Look(ref version, "Version");
        }
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            var savedVersion = version;
            Scribe_Values.Look(ref savedVersion, "Version", -1);
            if (savedVersion != version)
            {
                return;
                /*
                    switch (savedVersion)
                    {
                        case -1:
                            string selectedPreset = null;
                            Scribe_Values.Look<string>(ref selectedPreset, "SelectedPreset");
                            if (selectedPreset != null)
                            {
                                isSavedDataVersionDeprecated = true;
                                return;
                            }
                            break;
                    }
                    return;
                    */
            }
        }

        string selectedPresetName = null;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            selectedPresetName = selectedPreset.name;
        }

        Scribe_Values.Look(ref selectedPresetName, "selectedPresetIndex");
        Scribe_Deep.Look(ref PM, "PresetsData");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            SetSelectedPreset(PM.presets.Find(p => p.name == selectedPresetName));
        }
    }

    internal void DoWindowContents(Rect inRect)
    {
        if (PM == null)
        {
            return;
        }

        //Log.Clear();
        inRect.yMax -= 10f;

        isDefaultPreset = selectedPreset == PM.DEFAULT_PRESET;
        isHashesMatchUp = PM.DEFAULT_PRESET.hash == selectedPreset.hash;

        var leftPartRect = inRect.LeftHalf();
        leftPartRect.xMax -= HALFS_GAP / 2;

        DrawPresetsContainer(ref leftPartRect);

        if (!isDefaultPreset)
        {
            if (isHashesMatchUp)
            {
                DrawMainMenu(ref leftPartRect);
                DrawFooter(ref leftPartRect);

                var rightPartRect = inRect.RightHalf();
                rightPartRect.xMin += HALFS_GAP / 2;
                DrawElementsMenu(ref rightPartRect);
            }
            else
            {
                var rect = new Rect(inRect) { yMin = leftPartRect.yMin };

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                GUI.color = Color.gray;
                Widgets.Label(rect, "personalWorkCategories_presetDoesNotMatch".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                var buttonWidth = 200f;
                var center = rect.center;
                if (Widgets.ButtonText(new Rect
                    {
                        x = center.x - (buttonWidth / 2),
                        y = center.y - (BUTTON_HEIGHT / 2) + 50f,
                        width = buttonWidth,
                        height = BUTTON_HEIGHT
                    }, "personalWorkCategories_createCopyWithChanges".Translate()))
                {
                    TryToFixSelectedPreset();
                }
            }
        }
        else
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            GUI.color = Color.gray;
            var rect = new Rect(inRect) { yMin = leftPartRect.yMin };
            Widgets.Label(rect, "personalWorkCategories_cantChangeDefault".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }

    private void DrawElementsMenu(ref Rect contentRect)
    {
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Text.Anchor = TextAnchor.MiddleCenter;
        var smallInstructionRect = new Rect(contentRect) { height = 35f };
        Widgets.Label(smallInstructionRect, "personalWorkCategories_dragTheWorks".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        var workTypesRect = new Rect(contentRect)
            { y = smallInstructionRect.yMax, width = (contentRect.width - COLUMN_GAP) / 2, yMax = contentRect.yMax };

        dropTarget.Reset();
        currentMouseOverColumnType = null;
        elementsColumnHeight = workTypesRect.height;

        DrawList(workTypesRect, ref workTypesScrollPosition, selectedPreset.workTypes,
            DrawWorkTypeElement);

        var workGiversRect = new Rect(workTypesRect) { x = workTypesRect.xMax + COLUMN_GAP };

        if (selectedWorkType == null)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Widgets.Label(workGiversRect, "personalWorkCategories_selectWorkType".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
        else
        {
            DrawList(workGiversRect, ref workGiversScrollPosition, selectedWorkType.workGivers,
                DrawWorkGiverElement);
        }
    }

    private void DrawPresetsContainer(ref Rect contentRect)
    {
        var presetsContainer = new Rect(contentRect) { height = 100f };
        Widgets.DrawLightHighlight(presetsContainer);

        contentRect.yMin = presetsContainer.yMax + CONTAINERS_GAP;

        var paddedRect = presetsContainer.ContractedBy(CONTAINER_PADDING);

        var buttonWidth = (paddedRect.width - (BUTTONS_GAP * 2)) / 3f;

        var selectPresetRect = new Rect(paddedRect.x, paddedRect.y, buttonWidth, BUTTON_HEIGHT);
        if (Widgets.ButtonText(selectPresetRect, "personalWorkCategories_selectPreset".Translate()))
        {
            var list = new List<FloatMenuOption>();

            foreach (var preset in PM.presets)
            {
                list.Add(new FloatMenuOption(preset.name, delegate { SwitchPresetTo(preset); }));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        if (Widgets.ButtonText(
                new Rect(selectPresetRect) { x = selectPresetRect.x + selectPresetRect.width + BUTTONS_GAP },
                "personalWorkCategories_copyPreset".Translate()))
        {
            var newPreset = PM.CopyPreset(selectedPreset, "", " " + "personalWorkCategories_copy".Translate());
            SwitchPresetTo(newPreset);
        }

        var deleteButRect = new Rect(selectPresetRect)
            { x = selectPresetRect.x + ((selectPresetRect.width + BUTTONS_GAP) * 2) };

        if (!isDefaultPreset)
        {
            if (Widgets.ButtonText(deleteButRect, "personalWorkCategories_deletePreset".Translate()))
            {
                var index = PM.DeletePreset(selectedPreset);
                SwitchPresetTo(index <= 0 ? PM.DEFAULT_PRESET : PM.presets[index - 1]);
            }
        }

        var presetLabelRect = new Rect(selectPresetRect.x, selectPresetRect.y + 50f, 66f, BUTTON_HEIGHT);
        Widgets.Label(presetLabelRect, "personalWorkCategories_current".Translate() + ": ");

        var presetNameRect = new Rect(presetLabelRect)
        {
            x = presetLabelRect.xMax + 5f,
            y = presetLabelRect.y - 5f,
            xMax = paddedRect.xMax
        };
        if (!isDefaultPreset)
        {
            if (editablePresetName == null)
            {
                editablePresetName = selectedPreset.name;
            }

            var editablePresetFieldRect = new Rect(presetNameRect)
                { xMax = paddedRect.xMax - buttonWidth - BUTTONS_GAP };
            editablePresetName =
                Widgets.TextField(editablePresetFieldRect, editablePresetName, 30, Outfit.ValidNameRegex);

            if (Widgets.ButtonText(
                    new Rect(editablePresetFieldRect)
                        { xMin = editablePresetFieldRect.xMax + BUTTONS_GAP, width = buttonWidth },
                    "personalWorkCategories_presetSettigns".Translate()))
            {
                mainMenuContent = MainMenuContent.preset;
            }
        }
        else
        {
            Widgets.Label(
                new Rect(presetNameRect)
                    { x = presetNameRect.x + 3f, y = presetNameRect.y + 5f, height = presetNameRect.height - 5f },
                selectedPreset.name);
        }
    }

    private void DrawFooter(ref Rect contentRect)
    {
        var rebootGameRect = new Rect(contentRect) { y = contentRect.yMax - BUTTON_HEIGHT, height = BUTTON_HEIGHT };
        if (Widgets.ButtonText(rebootGameRect, "personalWorkCategories_rebootGame".Translate()))
        {
            Write();
            GenCommandLine.Restart();
        }

        if (!selectedPreset.isAdvanced)
        {
            return;
        }

        var createCustomRect = new Rect(rebootGameRect) { y = rebootGameRect.yMin - BUTTON_HEIGHT - BUTTONS_GAP };
        Button_CreateCustomGroup(createCustomRect);
    }

    private void DrawMainMenu(ref Rect contentRect)
    {
        if (mainMenuContent == MainMenuContent.empty)
        {
            return;
        }

        var mainMenuContainer = new Rect(contentRect) { height = 330f };
        Widgets.DrawLightHighlight(mainMenuContainer);

        contentRect.yMin = mainMenuContainer.yMax + CONTAINERS_GAP;

        var paddedRect = mainMenuContainer.ContractedBy(CONTAINER_PADDING);

        switch (mainMenuContent)
        {
            case MainMenuContent.workType:
                DrawWorkTypeSettings(paddedRect);
                break;

            case MainMenuContent.preset:
                DrawPresetSettings(paddedRect);
                break;
        }
    }

    private void DrawPresetSettings(Rect paddedRect)
    {
        Button_EnableAdvancedMode(ref paddedRect);
    }

    private void DrawWorkTypeSettings(Rect contentRect)
    {
        if (selectedWorkType == null)
        {
            return;
        }

        Text.Font = GameFont.Medium;
        var selectedTypeLabelRect = new Rect(contentRect) { height = 50f };
        Widgets.Label(selectedTypeLabelRect, selectedWorkType.GetLabel());
        Text.Font = GameFont.Small;

        var buttonWidth = (contentRect.width - (BUTTONS_GAP * 2)) / 3f;

        var initRect = new Rect(contentRect) { y = selectedTypeLabelRect.yMax, height = BUTTON_HEIGHT };
        var leftPart = initRect.LeftPart(0.3f);
        leftPart.y += 5f;
        var rightPart = initRect.RightPart(0.7f);

        var rowHeight = BUTTON_HEIGHT + 5f;

        Widgets.Label(leftPart, "personalWorkCategories_groupLabel".Translate() + ":");
        Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight },
            "personalWorkCategories_pawnLabel".Translate() + ":");
        Widgets.Label(new Rect(leftPart) { y = leftPart.y + (rowHeight * 2) },
            "personalWorkCategories_gerungLabel".Translate() + ":");
        Widgets.Label(new Rect(leftPart) { y = leftPart.y + (rowHeight * 3) },
            "personalWorkCategories_description".Translate() + ":");
        Widgets.Label(new Rect(leftPart) { y = leftPart.y + (rowHeight * 4) },
            "personalWorkCategories_verb".Translate() + ":");
        Widgets.Label(new Rect(leftPart) { y = leftPart.y + (rowHeight * 5) },
            "personalWorkCategories_skills".Translate() + ":");

        if (!selectedWorkType.IsExtra())
        {
            rightPart.y += 5f;
            rightPart.height = 20f;

            var workTypeDef = DefDatabase<WorkTypeDef>.GetNamed(selectedWorkType.defName);

            Widgets.Label(rightPart, workTypeDef.labelShort);
            Widgets.Label(new Rect(rightPart) { y = rightPart.y + rowHeight }, workTypeDef.pawnLabel);
            Widgets.Label(new Rect(rightPart) { y = rightPart.y + (rowHeight * 2) }, workTypeDef.gerundLabel);
            Widgets.Label(new Rect(rightPart) { y = rightPart.y + (rowHeight * 3) }, workTypeDef.description);
            Widgets.Label(new Rect(rightPart) { y = rightPart.y + (rowHeight * 4) }, workTypeDef.verb);

            Widgets.Label(new Rect(rightPart) { y = rightPart.y + (rowHeight * 5) },
                workTypeDef.relevantSkills.ConvertAll(sd => sd.label).ToStringSafeEnumerable());

            var splitWorkTypeRect = new Rect(contentRect.x, contentRect.yMax - BUTTON_HEIGHT, buttonWidth,
                BUTTON_HEIGHT);
            if (Widgets.ButtonText(splitWorkTypeRect, "personalWorkCategories_splitGroup".Translate()))
            {
                if (selectedPreset.SplitWorkType(selectedWorkType.defName) is { } newWorkType)
                {
                    SetSelectedWorkType(newWorkType);
                }
            }

            var resetToDefaultRect = new Rect(splitWorkTypeRect)
                { x = splitWorkTypeRect.x + ((splitWorkTypeRect.width + BUTTONS_GAP) * 1) };
            if (Widgets.ButtonText(resetToDefaultRect, "personalWorkCategories_resetToDefault".Translate()))
            {
                PM.SetWorkTypeContentToDefault(selectedPreset, selectedWorkType.defName);
            }
        }
        else
        {
            var extraData = selectedWorkType.extraData;

            extraData.labelShort = Widgets.TextField(rightPart, extraData.labelShort);
            extraData.pawnLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + rowHeight },
                extraData.pawnLabel);
            extraData.gerundLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + (rowHeight * 2) },
                extraData.gerundLabel);
            extraData.description = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + (rowHeight * 3) },
                extraData.description);
            extraData.verb = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + (rowHeight * 4) },
                extraData.verb);

            var skillsRect = new Rect(rightPart)
            {
                y = rightPart.y + (rowHeight * 5), width = rightPart.width - 32f, height = rightPart.height - 5f
            };
            Widgets.Label(new Rect(skillsRect) { y = skillsRect.y + 5f },
                extraData.skills.ConvertAll(s => DefDatabase<SkillDef>.GetNamed(s).label).ToStringSafeEnumerable());

            if (selectedPreset.isAdvanced)
            {
                var changeRect = new Rect(skillsRect) { width = 15f, x = skillsRect.xMax + 2f };
                if (Widgets.ButtonText(changeRect, "+"))
                {
                    var list = new List<FloatMenuOption>();

                    foreach (var skillDef in DefDatabase<SkillDef>.AllDefs.Where(sd =>
                                 !extraData.skills.Contains(sd.defName)))
                    {
                        list.Add(new FloatMenuOption(skillDef.label,
                            delegate { extraData.skills.Add(skillDef.defName); }));
                    }

                    if (list.Count > 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                }

                if (Widgets.ButtonText(new Rect(changeRect) { x = changeRect.xMax + 2f }, "-"))
                {
                    var list = new List<FloatMenuOption>();

                    foreach (var skillDefName in extraData.skills)
                    {
                        list.Add(new FloatMenuOption(DefDatabase<SkillDef>.GetNamed(skillDefName).label,
                            delegate { extraData.skills.Remove(skillDefName); }));
                    }

                    if (list.Count > 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                }
            }

            var deleteGroupButRect = new Rect(contentRect.x, contentRect.yMax - BUTTON_HEIGHT, buttonWidth,
                BUTTON_HEIGHT);
            if (Widgets.ButtonText(deleteGroupButRect, "personalWorkCategories_deleteGroup".Translate()))
            {
                DeleteSelectedType();
            }
        }
    }

    private void DrawList<W>(Rect inRect, ref Vector2 scrollPosition, List<W> works, DrawElementFunc<W> drawElement)
        where W : WorkCommon
    {
        var contentType = typeof(W);

        var isMouseOver = Mouse.IsOver(inRect);
        if (isMouseOver)
        {
            currentMouseOverColumnType = contentType;
        }

        var elementRect = new Rect(0f, 0f, inRect.width - 18f, ELEMENT_HEIGHT);
        var viewRect = new Rect(elementRect) { height = works.Count * (elementRect.height + ELEMENT_GAP) };

        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
        var yPosition = 0f;

        foreach (var work in works)
        {
            var isSource = false;
            var isTarget = false;
            var reaction = FloatingElement.DragReaction.Nothing;
            var status = ElementStatus.Normal;

            if (draggedElement != null)
            {
                if (work is WorkType workType && draggedElement.element is WorkGiver draggedGiver)
                {
                    bool isAvailable;
                    if (selectedPreset.isAdvanced)
                    {
                        isAvailable = true;
                    }
                    else
                    {
                        var workTypeOfDraggedGiver = PM.GetDefaultTypeOfGiver(draggedGiver);

                        var isSameGroup = workType.defName == workTypeOfDraggedGiver.defName ||
                                          workType.IsRooted() &&
                                          workType.extraData.root == workTypeOfDraggedGiver.defName;

                        isAvailable = isSameGroup || workType.IsCustom();
                    }

                    status = isAvailable ? ElementStatus.Available : ElementStatus.Forbidden;
                }

                var mp = Event.current.mousePosition;
                //TODO: it's possible error place
                isSource = draggedElement.element.GetType() == contentType &&
                           draggedElement.element.defName == work.defName;
                var y = mp.y - yPosition;
                isTarget = isMouseOver && y >= 0f && y < elementRect.height + 4f;
                if (isTarget)
                {
                    reaction = draggedElement.DoDragReaction(work, status);
                    dropTarget.Set(work, status);
                }
            }

            if (isSource)
            {
                continue;
            }

            if (isTarget)
            {
                if (reaction == FloatingElement.DragReaction.Spread)
                {
                    yPosition += elementRect.yMax + ELEMENT_GAP;
                }
            }

            drawElement(new Rect(elementRect) { y = yPosition }, work, status);
            yPosition += elementRect.yMax + ELEMENT_GAP;
        }

        Widgets.EndScrollView();
    }

    private void DrawWorkTypeElement(Rect rowRect, WorkType workType, ElementStatus status)
    {
        var dragResult = Widgets.ButtonInvisibleDraggable(rowRect, status != ElementStatus.Forbidden);
        switch (dragResult)
        {
            case Widgets.DraggableResult.Pressed:
                SoundDefOf.Click.PlayOneShotOnCamera();
                SetSelectedWorkType(workType);
                break;

            case Widgets.DraggableResult.Dragged:
                GUI.FocusControl(null);
                Find.WindowStack.Add(draggedElement = new WorkTypeFloatingElement(this, rowRect, workType));
                break;
        }

        DrawWorkTypeContent(rowRect, workType, Mouse.IsOver(rowRect), status);
    }

    internal void DrawWorkTypeContent(Rect inRect, WorkType workType, bool isHighlight = false,
        ElementStatus status = ElementStatus.Normal)
    {
        if (selectedWorkType == workType)
        {
            Widgets.DrawHighlightSelected(inRect);
        }
        else if (isHighlight && status != ElementStatus.Forbidden || status == ElementStatus.Available)
        {
            Widgets.DrawHighlight(inRect);
        }
        else
        {
            Widgets.DrawLightHighlight(inRect);
        }

        var labelRect = inRect.ContractedBy(3f);
        if (status == ElementStatus.Forbidden)
        {
            GUI.color = Color.gray;
        }

        Widgets.Label(labelRect, workType.GetLabel());

        Text.Anchor = TextAnchor.UpperRight;
        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;
        var count = workType.workGivers.Count;
        var giversCount = count + (count == 0
            ? $" ({"personalWorkCategories_hidden".Translate().RawText})"
            : "");

        Widgets.Label(labelRect, giversCount);

        if (workType.IsExtra())
        {
            Text.Anchor = TextAnchor.LowerRight;
            string littleGrayText;
            if (workType.IsCustom())
            {
                littleGrayText = "personalWorkCategories_customGroup".Translate();
            }
            else
            {
                var rootLabel = selectedPreset.FindWorkTypeByDefName(workType.extraData.root).GetLabel();
                littleGrayText = "personalWorkCategories_root".Translate() + ": " + rootLabel;
            }

            Widgets.Label(labelRect, littleGrayText);
        }

        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawWorkGiverElement(Rect rowRect, WorkGiver workGiver, ElementStatus status)
    {
        var dragResult = Widgets.ButtonInvisibleDraggable(rowRect, true);

        switch (dragResult)
        {
            case Widgets.DraggableResult.Dragged:
                GUI.FocusControl(null);
                Find.WindowStack.Add(draggedElement = new WorkGiverFloatingElement(this, rowRect, workGiver));
                break;
        }

        DrawWorkGiverContent(rowRect, workGiver, Mouse.IsOver(rowRect));
    }

    internal void DrawWorkGiverContent(Rect inRect, WorkGiver workGiver, bool isHighlight = false)
    {
        if (isHighlight)
        {
            Widgets.DrawHighlight(inRect);
        }
        else
        {
            Widgets.DrawLightHighlight(inRect);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(inRect) { xMin = inRect.x + 5f }, workGiver.GetLabel());
        Text.Anchor = TextAnchor.UpperLeft;
    }

    internal void DraggedElementDropped()
    {
        if (draggedElement == null)
        {
            return;
        }

        var source = draggedElement.element;
        var target = dropTarget.element;

        draggedElement = null;

        if (currentMouseOverColumnType == null || source.Equals(target))
        {
            return;
        }

        switch (source)
        {
            case WorkType workType:
                MoveWorkType(workType, target);
                break;
            case WorkGiver workGiver:
                MoveWorkGiver(workGiver, target);
                break;
        }
    }

    private void MoveWorkType(WorkType source, WorkCommon target)
    {
        if (target is WorkGiver)
        {
            return;
        }

        var unused = source.workGivers;

        if (target is WorkType targetWorkType)
        {
            selectedPreset.MoveWorkTypeToPosition(source, targetWorkType);
        }
    }

    private void MoveWorkGiver(WorkGiver sourceWorkGiver, WorkCommon target)
    {
        switch (target)
        {
            case WorkType targetWorkType:
            {
                var sourceParent = selectedPreset.FindWorkTypeOfWorkGiver(sourceWorkGiver);
                sourceParent.workGivers.Remove(sourceWorkGiver);
                targetWorkType.workGivers.Add(sourceWorkGiver);
                break;
            }

            case WorkGiver targetWorkGiver:
            {
                var sourceWorkType = selectedPreset.FindWorkTypeOfWorkGiver(sourceWorkGiver);
                var targetWorkType = selectedPreset.FindWorkTypeOfWorkGiver(targetWorkGiver);

                sourceWorkType.workGivers.Remove(sourceWorkGiver);
                var index = targetWorkType.workGivers.IndexOf(targetWorkGiver);
                targetWorkType.workGivers.Insert(index, sourceWorkGiver);
                break;
            }
        }
    }

    internal bool InitModSettings(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
    {
        string newHash;

        if (PM == null)
        {
            PM = new PresetManager(defaultWorkTypes, defaultWorkGivers);
            SetSelectedPreset(PM.DEFAULT_PRESET);

            newHash = PM.DEFAULT_PRESET.hash;
        }
        else
        {
            newHash = PM.ComputePresetHash(defaultWorkTypes, defaultWorkGivers);
        }

        defaultWorkTypes.Sort((a, b) => a.naturalPriority >= b.naturalPriority ? -1 : 1);

        var selectedPresetDeprecate = false;
        var defaultPresetDeprecate = false;

        if (selectedPreset != PM.DEFAULT_PRESET)
        {
            if (selectedPreset.hash != newHash)
            {
                SwitchPresetTo(PM.DEFAULT_PRESET);
                selectedPresetDeprecate = true;
            }
        }

        if (PM.DEFAULT_PRESET.hash != newHash)
        {
            defaultPresetDeprecate = true;
        }

        if (!defaultPresetDeprecate)
        {
            return true;
        }

        if (selectedPresetDeprecate)
        {
            isWorksListWasChanged = true;
        }

        Write();
        return false;
    }

    internal void SwitchPresetTo(Preset target)
    {
        if (selectedPreset == target)
        {
            return;
        }

        if (selectedPreset != PM.DEFAULT_PRESET)
        {
            PM.RenamePreset(selectedPreset, editablePresetName);
        }

        SetSelectedWorkType(null);
        SetSelectedPreset(target);
    }

    internal void SetSelectedPreset(Preset preset)
    {
        selectedPreset = preset;
        editablePresetName = preset.name;
    }

    private void DeleteSelectedType()
    {
        if (PM.DeleteWorkTypeInPreset(selectedPreset, selectedWorkType.defName) == false)
        {
            return;
        }

        selectedPreset.workTypes.Remove(selectedWorkType);
        SetSelectedWorkType(null);
    }

    private void TryToFixSelectedPreset()
    {
        var newPreset = PM.CopyPreset(selectedPreset, "personalWorkCategories_fixed".Translate() + " ");
        newPreset.hash = PM.DEFAULT_PRESET.hash;
        SwitchPresetTo(newPreset);

        //Delete deprecated work types and givers
        foreach (var workType in newPreset.workTypes.ListFullCopy())
        {
            //check is work type deprecate
            var isWTDeprecate = true;

            //if it is default group then not deprecated
            if (PM.DEFAULT_PRESET.FindWorkTypeByDefName(workType.defName) != null)
            {
                isWTDeprecate = false;
            }
            else
            {
                //if separated from the defaul group or is a custom group then not deprecated
                if (workType.IsExtra())
                {
                    if (workType.IsCustom() || workType.IsRooted() &&
                        PM.DEFAULT_PRESET.FindWorkTypeByDefName(workType.extraData.root) != null)
                    {
                        isWTDeprecate = false;
                    }
                }
            }

            // check givers of current type for deprecated
            foreach (var workGiver in workType.workGivers.ListFullCopy())
            {
                var defaultTypeOfGiver = PM.GetDefaultTypeOfGiver(workGiver);

                // if WorkGiver must be rescued from destroing WorkType
                if (isWTDeprecate && defaultTypeOfGiver != null)
                {
                    workType.workGivers.Remove(workGiver);
                    newPreset.FindWorkTypeByDefName(defaultTypeOfGiver.defName).InsertWorkGiverByPriority(workGiver);
                }
                // if WorkGiver deprecated then remove it
                else if (!isWTDeprecate && defaultTypeOfGiver == null)
                {
                    workType.workGivers.Remove(workGiver);
                }
            }


            if (isWTDeprecate)
            {
                newPreset.workTypes.Remove(workType);
            }
        }

        //Add new works
        foreach (var mayBeNewWorkType in PM.DEFAULT_PRESET.workTypes)
        {
            if (newPreset.FindWorkTypeByDefName(mayBeNewWorkType.defName) == null)
            {
                continue;
            }

            newPreset.InsertWorkTypeByPriority(mayBeNewWorkType);
        }
    }

    public void SetSelectedWorkType(WorkType workTypeToSelect)
    {
        selectedWorkType = workTypeToSelect;

        if (workTypeToSelect == null)
        {
            mainMenuContent = MainMenuContent.empty;
            return;
        }

        mainMenuContent = MainMenuContent.workType;

        var yPos = selectedPreset.workTypes.IndexOf(selectedWorkType) * (ELEMENT_HEIGHT + ELEMENT_GAP);

        Log.Message($"scroll: {workTypesScrollPosition.y} pos: {yPos} height: {elementsColumnHeight}");
        if (yPos < workTypesScrollPosition.y)
        {
            workTypesScrollPosition.y = yPos;
        }
        else if (yPos + ELEMENT_HEIGHT + ELEMENT_GAP > workTypesScrollPosition.y + elementsColumnHeight)
        {
            workTypesScrollPosition.y = yPos - elementsColumnHeight + ELEMENT_HEIGHT + ELEMENT_GAP;
        }
    }

    private void Button_CreateCustomGroup(Rect buttonRect)
    {
        if (!Widgets.ButtonText(buttonRect, "personalWorkCategories_createCustomGroup".Translate()))
        {
            return;
        }

        if (selectedPreset.CreateNewCustomWorkType() is not { } customWorkType)
        {
            return;
        }

        selectedPreset.MoveWorkTypeToPosition(customWorkType, selectedWorkType);
        SetSelectedWorkType(customWorkType);
    }

    private void Button_EnableAdvancedMode(ref Rect contentRect)
    {
        var buttonRect = new Rect(contentRect) { height = BUTTON_HEIGHT };
        if (!selectedPreset.isAdvanced)
        {
            if (Widgets.ButtonText(buttonRect, "personalWorkCategories_enableAdvancedMode".Translate()))
            {
                selectedPreset.isAdvanced = true;
            }

            Widgets.DrawBoxSolid(buttonRect, new Color(1.0f, 0.35f, 0.0f, 0.3f));
        }
        else
        {
            Text.Font = GameFont.Medium;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(buttonRect, "personalWorkCategories_advancedModeEnabled".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        var advancedWorkWarningMode = new Rect(buttonRect) { y = buttonRect.yMax, height = 50f };
        Widgets.Label(advancedWorkWarningMode, "personalWorkCategories_beCarefulWithAdvancedMode".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        var separateBuildWorksRect = new Rect(advancedWorkWarningMode)
            { y = advancedWorkWarningMode.yMax, height = BUTTON_HEIGHT };


#if DEBUG
        var checkboxValue = selectedPreset.isBuildingWorksSplitted;
        Widgets.CheckboxLabeled(separateBuildWorksRect, "personalWorkCategories_splitBuildingWorks".Translate(),
            ref checkboxValue);
        TooltipHandler.TipRegion(separateBuildWorksRect,
            "personalWorkCategories_splitBuildingWorksTooltip".Translate());

        if (checkboxValue != selectedPreset.isBuildingWorksSplitted)
        {
            if (checkboxValue)
            {
                var placeFrame = selectedPreset.FindWorkGiverByDefName(Const.PLACE_FRAME_DEF_NAME);
                var placeFrameGroup = selectedPreset.FindWorkTypeOfWorkGiver(placeFrame);
                var placeQualityFrame = new WorkGiver(placeFrame) { defName = Const.PLACE_QUALITY_FRAME_DEF_NAME };

                var index = placeFrameGroup.workGivers.IndexOf(placeFrame);
                placeFrameGroup.workGivers.Insert(index, placeQualityFrame);
            }
            else
            {
                var placeQualityFrame = selectedPreset.FindWorkGiverByDefName(Const.PLACE_QUALITY_FRAME_DEF_NAME);
                var placeFrameGroup = selectedPreset.FindWorkTypeOfWorkGiver(placeQualityFrame);
                placeFrameGroup.workGivers.Remove(placeQualityFrame);
            }

            selectedPreset.isBuildingWorksSplitted = checkboxValue;
        }
#endif

        contentRect.yMin = separateBuildWorksRect.yMax;
    }

    public class DropTarget
    {
        public WorkCommon element;
        public ElementStatus status;

        internal void Set(WorkCommon element, ElementStatus status)
        {
            this.element = element;
            this.status = status;
        }

        internal void Reset()
        {
            element = null;
            status = ElementStatus.Normal;
        }
    }

    private enum MainMenuContent
    {
        empty,
        preset,
        workType
    }

    private delegate void DrawElementFunc<W>(Rect rowRect, W work, ElementStatus status) where W : WorkCommon;
}