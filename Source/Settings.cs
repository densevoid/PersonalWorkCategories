using HandyUI_PersonalWorkCategories.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HandyUI_PersonalWorkCategories
{
    public class PersonalWorkCategoriesSettings : ModSettings
    {
        int version = 2;

        public enum ContentCategory
        {
            Undefined,
            WorkType,
            WorkGiver
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

        public enum ElementStatus
        {
            Available,
            Forbidden,
            Normal
        }

        public Preset selectedPreset;
        private string editablePresetName;

        public PresetManager PM;

        private Type currentMouseOverColumnType;
        internal WorkType selectedWorkType;

        internal FloatingElement draggedElement;
        private DropTarget dropTarget = new DropTarget();

        private float elementsColumnHeight = 0;
        private Vector2 workTypesScrollPosition = Vector2.zero;
        private Vector2 workGiversScrollPosition = Vector2.zero;

        delegate void DrawElementFunc<W>(Rect rowRect, W work, ElementStatus status) where W : WorkCommon;

        private float presetsContainerHeight = 0f;
        private float workTypesContainerHeight = 0f;
        internal bool isWorksListWasChanged;
        internal bool isSavedDataVersionDeprecated;

        const float elementHeight = 50f;
        const float elementGap = 4f;

        public override void ExposeData()
        {
            base.ExposeData();
            
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    if (PM == null) return;
                    PM.RenamePreset(selectedPreset, editablePresetName);
                    Scribe_Values.Look<int>(ref version, "Version");
                }
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    int savedVersion = version;
                    Scribe_Values.Look<int>(ref savedVersion, "Version", -1);
                    if (savedVersion != version)
                    {
                        isSavedDataVersionDeprecated = true;
                        return;
                    }
                }

                string selectedPresetName = null;
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    selectedPresetName = selectedPreset.name;
                }

                Scribe_Values.Look<string>(ref selectedPresetName, "selectedPresetIndex");
                Scribe_Deep.Look<PresetManager>(ref PM, "PresetsData");

                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    setSelectedPreset(PM.presets.Find(p => p.name == selectedPresetName));
                }
            }
        }

        internal void DoWindowContents(Rect inRect)
        {
            //Log.Clear();
            inRect.yMax -= 10f;

            float upperY = 45f;
            float curX = 0f; ;
            float curY = upperY;
            float centerX = inRect.xMax / 2f;
            const float COLUMN_GAP = 5f;
            const float HALFS_GAP = 10f;
            const float BUTTONS_GAP = 5f;
            float columnWidth = (centerX - COLUMN_GAP) / 2f;
            const float CONTAINER_PADDING = 10f;

            Rect presetsContainer = new Rect(curX, upperY, centerX - HALFS_GAP, presetsContainerHeight);
            Widgets.DrawLightHighlight(presetsContainer);

            curX += CONTAINER_PADDING;
            curY += CONTAINER_PADDING;

            float standartButtonWidth = (centerX - HALFS_GAP - 2f * CONTAINER_PADDING - BUTTONS_GAP * 2f) / 3f;
            float standartButtonHeight = 35f;

            Rect firstButRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);

            if (Widgets.ButtonText(firstButRect, "personalWorkCategories_selectPreset".Translate(), true, true, true))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                foreach (Preset preset in PM.presets)
                {
                    list.Add(new FloatMenuOption(preset.name, delegate ()
                    {
                        SwitchPresetTo(preset);
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (Widgets.ButtonText(new Rect(firstButRect) { x = firstButRect.x + firstButRect.width + BUTTONS_GAP }, "personalWorkCategories_copyPreset".Translate(), true, true, true))
            {
                Preset newPreset = PM.CopyPreset(selectedPreset, "", " " + "personalWorkCategories_copy".Translate());
                SwitchPresetTo(newPreset);
            }

            bool isDefaultPreset = selectedPreset == PM.DEFAULT_PRESET;
            Rect deleteButRect = new Rect(firstButRect) { x = firstButRect.x + (firstButRect.width + BUTTONS_GAP) * 2 };

            if (!isDefaultPreset)
            {
                if (Widgets.ButtonText(deleteButRect, "personalWorkCategories_deletePreset".Translate(), true, true, !isDefaultPreset))
                {
                    int index = PM.DeletePreset(selectedPreset);
                    if (index > 0)
                        SwitchPresetTo(PM.presets[index - 1]);
                    else
                        SwitchPresetTo(PM.DEFAULT_PRESET);
                }
            }

            curY += 45f;

            Rect presetLabelRect = new Rect(curX, curY + 5f, 66f, 30f);
            Widgets.Label(presetLabelRect, "personalWorkCategories_current".Translate() + ": ");

            Rect presetNameRect = new Rect(presetLabelRect.xMax + 5f, curY, centerX - HALFS_GAP - 2 * CONTAINER_PADDING - presetLabelRect.xMax, 30f);
            if (!isDefaultPreset)
            {
                if (editablePresetName == null) editablePresetName = selectedPreset.name;
                editablePresetName = Widgets.TextField(presetNameRect, editablePresetName, 30, Outfit.ValidNameRegex);
            }
            else
                Widgets.Label(new Rect(presetNameRect) { x = presetNameRect.x + 3f, y = presetNameRect.y + 5f, height = presetNameRect.height - 5f }, selectedPreset.name);

            curY += 30f;

            curX -= CONTAINER_PADDING;
            curY += CONTAINER_PADDING;

            presetsContainerHeight = curY - upperY;

            if (!isDefaultPreset)
            {
                string defaultHash = PM.DEFAULT_PRESET.hash;
                string currentHash = selectedPreset.hash;

                if (defaultHash == currentHash)
                {
                    if (selectedWorkType != null)
                    {
                        curY += 10f;
                        upperY = curY;

                        Rect workTypesContainer = new Rect(presetsContainer) { y = curY, height = workTypesContainerHeight };
                        Widgets.DrawLightHighlight(workTypesContainer);

                        curX += CONTAINER_PADDING;
                        curY += CONTAINER_PADDING;

                        Text.Font = GameFont.Medium;
                        Rect selectedTypeLabelRect = new Rect(curX, curY + 5f, centerX - HALFS_GAP - 2 * CONTAINER_PADDING, 50f);
                        Widgets.Label(selectedTypeLabelRect, selectedWorkType.GetLabel());
                        Text.Font = GameFont.Small;

                        curY += selectedTypeLabelRect.height;

                        if (!selectedWorkType.IsExtra())
                        {
                            Rect splitWorkTypeRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);
                            if (Widgets.ButtonText(splitWorkTypeRect, "personalWorkCategories_splitGroup".Translate(), true, true, true))
                            {
                                if (selectedPreset.SplitWorkType(selectedWorkType.defName) is WorkType newWorkType)
                                {
                                    SetSelectedWorkType(newWorkType);
                                }
                            }

                            Rect resetToDefaultRect = new Rect(splitWorkTypeRect) { x = splitWorkTypeRect.x + (splitWorkTypeRect.width + BUTTONS_GAP) * 1 };
                            if (Widgets.ButtonText(resetToDefaultRect, "personalWorkCategories_resetToDefault".Translate(), true, true, true))
                            {
                                PM.SetWorkTypeContentToDefault(selectedPreset, selectedWorkType.defName);
                            }

                            curY += splitWorkTypeRect.height;
                        }
                        else
                        {
                            WorkType.ExtraData extraData = selectedWorkType.extraData;

                            Rect initRect = new Rect(curX, curY, centerX - HALFS_GAP - 2 * CONTAINER_PADDING, 30f);
                            Rect leftPart = initRect.LeftPart(0.3f);
                            leftPart.y += 5f;
                            Rect rightPart = initRect.RightPart(0.7f);

                            float rowHeight = 35f;

                            Widgets.Label(leftPart, "personalWorkCategories_groupLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight }, "personalWorkCategories_pawnLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight * 2 }, "personalWorkCategories_gerungLabel".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight * 3 }, "personalWorkCategories_description".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight * 4 }, "personalWorkCategories_verb".Translate() + ":");
                            Widgets.Label(new Rect(leftPart) { y = leftPart.y + rowHeight * 5 }, "personalWorkCategories_skills".Translate() + ":");

                            extraData.labelShort = Widgets.TextField(rightPart, extraData.labelShort);
                            extraData.pawnLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + rowHeight }, extraData.pawnLabel);
                            extraData.gerundLabel = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + rowHeight * 2 }, extraData.gerundLabel);
                            extraData.description = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + rowHeight * 3 }, extraData.description);
                            extraData.verb = Widgets.TextField(new Rect(rightPart) { y = rightPart.y + rowHeight * 4 }, extraData.verb);

                            Rect skillsRect = new Rect(rightPart) { y = rightPart.y + rowHeight * 5, width = rightPart.width - 32f, height = rightPart.height - 5f };
                            Widgets.Label(new Rect(skillsRect) { y = skillsRect.y + 5f }, extraData.skills.ToStringSafeEnumerable());

                            if (selectedPreset.isAdvanced)
                            {
                                Rect changeRect = new Rect(skillsRect) { width = 15f, x = skillsRect.xMax + 2f };
                                if (Widgets.ButtonText(changeRect, "+"))
                                {
                                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                                    foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefs.Where(sd => !extraData.skills.Contains(sd.defName)))
                                    {
                                        list.Add(new FloatMenuOption(skillDef.label, delegate ()
                                        {
                                            extraData.skills.Add(skillDef.defName);
                                        }));
                                    }

                                    if (list.Count > 0)
                                        Find.WindowStack.Add(new FloatMenu(list));
                                }

                                if (Widgets.ButtonText(new Rect(changeRect) { x = changeRect.xMax + 2f }, "-"))
                                {
                                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                                    foreach (string skillDefName in extraData.skills)
                                    {
                                        list.Add(new FloatMenuOption(DefDatabase<SkillDef>.GetNamed(skillDefName).label, delegate ()
                                        {
                                            extraData.skills.Remove(skillDefName);
                                        }));
                                    }

                                    if (list.Count > 0)
                                        Find.WindowStack.Add(new FloatMenu(list));
                                }
                            }

                            curY += 220f;

                            Rect deleteGroupButRect = new Rect(curX, curY, standartButtonWidth, standartButtonHeight);
                            if (Widgets.ButtonText(deleteGroupButRect, "personalWorkCategories_deleteGroup".Translate(), true, true, true))
                            {
                                DeleteSelectedType();
                            }
                            curY += deleteGroupButRect.height;
                        }


                        curX -= CONTAINER_PADDING;
                        curY += CONTAINER_PADDING;

                        workTypesContainerHeight = curY - upperY;
                    }

                    curY = inRect.yMax - (standartButtonHeight * 3 + 15f);

                    if (selectedPreset.isAdvanced)
                    {
                        Rect buttonRect = new Rect(0, inRect.yMax - standartButtonHeight * 2 - 5f, centerX - HALFS_GAP, standartButtonHeight);
                        curY += Button_CreateCustomGroup(buttonRect);
                    }
                    else
                    {

                        Rect buttonRect = new Rect(0, curY, centerX - HALFS_GAP, standartButtonHeight);
                        curY += Button_EnableAdvancedMode(buttonRect);
                    }

                    // right window part
                    curY = 45f;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect() { x = centerX, y = curY, xMax = inRect.xMax - CONTAINER_PADDING, height = 35f }, "personalWorkCategories_dragTheWorks".Translate());
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;

                    curY += 35f;
                    Rect workTypesRect = new Rect(inRect) { x = centerX, y = curY, width = columnWidth, yMax = inRect.yMax };
                    elementsColumnHeight = workTypesRect.height;

                    dropTarget.Reset();
                    currentMouseOverColumnType = null;

                    DrawList<WorkType>(workTypesRect, ref workTypesScrollPosition, selectedPreset.workTypes,
                        (elemtnRect, work, status) => { DrawWorkTypeElement(elemtnRect, work, status); });

                    Rect workGiversRect = new Rect(workTypesRect) { x = workTypesRect.xMax + COLUMN_GAP };

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
                        DrawList<WorkGiver>(workGiversRect, ref workGiversScrollPosition, selectedWorkType.workGivers,
                            (elemtnRect, work, status) => { DrawWorkGiverElement(elemtnRect, work, status); });
                    }
                }
                else
                {
                    Rect rect = new Rect(0f, curY, inRect.xMax, inRect.yMax - curY);

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "personalWorkCategories_presetDoesNotMatch".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;

                    Vector2 center = rect.center;
                    if (Widgets.ButtonText(new Rect()
                    {
                        x = center.x - standartButtonWidth,
                        y = center.y - standartButtonHeight / 2 + 50f,
                        width = standartButtonWidth * 2f,
                        height = standartButtonHeight
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
                Widgets.Label(new Rect(0f, curY, inRect.xMax, inRect.yMax - curY), "personalWorkCategories_cantChangeDefault".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(
                new Rect(0, inRect.yMax - standartButtonHeight, centerX - HALFS_GAP, standartButtonHeight),
                "personalWorkCategories_rebootGame".Translate()))
            {
                Write();
                GenCommandLine.Restart();
            }
        }

        private void DrawList<W>(Rect inRect, ref Vector2 scrollPosition, List<W> works, DrawElementFunc<W> drawElement) where W : WorkCommon 
        {
            Type contentType = typeof(W);

            bool isMouseOver = Mouse.IsOver(inRect);
            if (isMouseOver) currentMouseOverColumnType = contentType;

            Rect elementRect = new Rect(0f, 0f, inRect.width - 18f, elementHeight);
            Rect viewRect = new Rect(elementRect) { height = works.Count * (elementRect.height + elementGap) };

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            float yPosition = 0f;

            foreach (W work in works)
            {
                bool isSource = false;
                bool isTarget = false;
                FloatingElement.DragReaction reaction = FloatingElement.DragReaction.Nothing;
                ElementStatus status = ElementStatus.Normal;

                if (draggedElement != null)
                {
                    bool isAvailable = false;

                    if (work is WorkType workType && draggedElement.element is WorkGiver draggedGiver)
                    {
                        if (selectedPreset.isAdvanced) isAvailable = true;
                        else
                        {
                            WorkType workTypeOfDraggedGiver = PM.GetDefaultTypeOfGiver(draggedGiver);

                            bool isSameGroup = workType.defName == workTypeOfDraggedGiver.defName || 
                                (workType.IsRooted() && workType.extraData.root == workTypeOfDraggedGiver.defName);

                            isAvailable = isSameGroup || workType.IsCustom();
                        }

                        if (isAvailable) status = ElementStatus.Available;
                        else status = ElementStatus.Forbidden;
                    }

                    Vector2 mp = Event.current.mousePosition;
                    //TODO: it's possible error place
                    isSource = draggedElement.element.GetType() == contentType && draggedElement.element.defName == work.defName;
                    float y = mp.y - yPosition;
                    isTarget = isMouseOver && y >= 0f && y < elementRect.height + 4f;
                    if (isTarget)
                    {
                        reaction = draggedElement.DoDragReaction(work, status);
                        dropTarget.Set(work, status);
                    }
                }

                if (!isSource)
                {
                    if (isTarget)
                    {
                        if (reaction == FloatingElement.DragReaction.Spread)
                        {
                            yPosition += elementRect.yMax + elementGap;
                        }
                    }
                    drawElement(new Rect(elementRect) { y = yPosition }, work, status);
                    yPosition += elementRect.yMax + elementGap;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawWorkTypeElement(Rect rowRect, WorkType workType, ElementStatus status)
        {
            Widgets.DraggableResult dragResult = Widgets.ButtonInvisibleDraggable(rowRect, status != ElementStatus.Forbidden);
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

        internal void DrawWorkTypeContent(Rect inRect, WorkType workType, bool isHighlight = false, ElementStatus status = ElementStatus.Normal)
        {
            if (selectedWorkType == workType)
            {
                Widgets.DrawHighlightSelected(inRect);
            }
            else if ((isHighlight && status != ElementStatus.Forbidden) || status == ElementStatus.Available)
            {
                Widgets.DrawHighlight(inRect);
            }
            else
            {
                Widgets.DrawLightHighlight(inRect);
            }

            Rect labelRect = inRect.ContractedBy(3f);
            if (status == ElementStatus.Forbidden) GUI.color = Color.gray;
            Widgets.Label(labelRect, workType.GetLabel());

            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            int count = workType.workGivers.Count;
            string giversCount = count.ToString() + (count == 0 ? " (" + "personalWorkCategories_hidden".Translate().RawText + ")" : "");

            Widgets.Label(labelRect, giversCount);

            if (workType.IsExtra())
            {
                Text.Anchor = TextAnchor.LowerRight;
                string littleGrayText = "";
                if (workType.IsCustom())
                    littleGrayText = "personalWorkCategories_customGroup".Translate();
                else
                {
                    string rootLabel = selectedPreset.FindWorkTypeByDefName(workType.extraData.root).GetLabel();
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
            Widgets.DraggableResult dragResult = Widgets.ButtonInvisibleDraggable(rowRect, true);

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
            if (draggedElement == null) return;

            WorkCommon source = draggedElement.element;
            WorkCommon target = dropTarget.element;

            draggedElement = null;

            if (currentMouseOverColumnType == null || source.Equals(target)) return;

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
            if (target is WorkGiver) return;

            List<WorkGiver> workGiverList = source.workGivers;

            if (target is WorkType targetWorkType)
            {
                selectedPreset.MoveWorkTypeToPosition(source, targetWorkType);
            }
            else if (currentMouseOverColumnType == typeof(WorkType))
            {
                selectedPreset.workTypes.Remove(source);
                selectedPreset.workTypes.Add(source);
            }
        }

        private void MoveWorkGiver(WorkGiver sourceWorkGiver, WorkCommon target)
        {
            switch (target)
            {
                case WorkType targetWorkType:
                {
                    WorkType sourceParent = selectedPreset.FindWorkTypeOfWorkGiver(sourceWorkGiver);
                    sourceParent.workGivers.Remove(sourceWorkGiver);
                    targetWorkType.workGivers.Add(sourceWorkGiver);
                    break;
                }

                case WorkGiver targetWorkGiver:
                {
                    WorkType sourceWorkType = selectedPreset.FindWorkTypeOfWorkGiver(sourceWorkGiver);
                    WorkType targetWorkType = selectedPreset.FindWorkTypeOfWorkGiver(targetWorkGiver);

                    sourceWorkType.workGivers.Remove(sourceWorkGiver);
                    int index = targetWorkType.workGivers.IndexOf(targetWorkGiver);
                    targetWorkType.workGivers.Insert(index, sourceWorkGiver);
                    break;
                }
            }
        }

        internal bool Initialize(List<WorkTypeDef> defaultWorkTypes, List<WorkGiverDef> defaultWorkGivers)
        {
            defaultWorkTypes.Sort((a, b) => a.naturalPriority > b.naturalPriority ? -1 : 1);
            defaultWorkGivers.Sort((a, b) => a.priorityInType > b.priorityInType ? -1 : 1);

            string defaultHash;

            if (PM == null)
            {
                PM = new PresetManager(defaultWorkTypes, defaultWorkGivers);
                setSelectedPreset(PM.DEFAULT_PRESET);

                defaultHash = PM.DEFAULT_PRESET.hash;
            }
            else
            {
                defaultHash = PM.ComputePresetHash(defaultWorkTypes, defaultWorkGivers);
            }

            bool defaultPresetDeprecate = false;
            bool selectedCustomPresetDeprecate = false;

            if (PM.DEFAULT_PRESET.hash != defaultHash)
            {
                defaultPresetDeprecate = true;
                PM.UpdateDefaultPreset(defaultWorkTypes, defaultWorkGivers);
            }

            if (selectedPreset != PM.DEFAULT_PRESET)
            {
                if (selectedPreset.hash != defaultHash)
                {
                    selectedCustomPresetDeprecate = true;
                    TryToFixSelectedPreset(false);
                }
            }

            if (defaultPresetDeprecate || selectedCustomPresetDeprecate)
            {
                Write();

                if (selectedCustomPresetDeprecate)
                {
                    isWorksListWasChanged = true;
                }
            }

            //is changes needed
            return selectedPreset != PM.DEFAULT_PRESET;
        }

        internal void SwitchPresetTo(Preset target)
        {
            if (selectedPreset == target) return;
            if (selectedPreset != PM.DEFAULT_PRESET)
                PM.RenamePreset(selectedPreset, editablePresetName);

            SetSelectedWorkType(null);
            setSelectedPreset(target);
        }

        internal void setSelectedPreset(Preset preset)
        {
            selectedPreset = preset;
            editablePresetName = preset.name;
        }

        private void DeleteSelectedType()
        {
            if (PM.DeleteWorkTypeInPreset(selectedPreset, selectedWorkType.defName) == false) return;

            selectedPreset.workTypes.Remove(selectedWorkType);
            SetSelectedWorkType(null);
        }

        private void TryToFixSelectedPreset(bool withCopying = true)
        {
            if (withCopying)
            {
                Preset newPreset = PM.CopyPreset(selectedPreset, "personalWorkCategories_fixed".Translate() + " ");
                SwitchPresetTo(newPreset);
            }

            //Delete deprecated work types and givers
            foreach (WorkType workType in selectedPreset.workTypes.ListFullCopy())
            {
                //check is work type deprecate
                bool isWTDeprecate = true;

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
                        if (workType.IsCustom() || (workType.IsRooted() && PM.DEFAULT_PRESET.FindWorkTypeByDefName(workType.extraData.root) != null))
                        {
                            isWTDeprecate = false;
                        }
                    }
                }

                if (isWTDeprecate) selectedPreset.workTypes.Remove(workType);
                else
                {
                    // check givers of current type for deprecated
                    foreach (WorkGiver workGiver in workType.workGivers.ListFullCopy())
                    {
                        WorkType defaultTypeOfGiver = PM.GetDefaultTypeOfGiver(workGiver);

                        // if WorkGiver deprecated then remove it
                        if (!isWTDeprecate && defaultTypeOfGiver == null) workType.workGivers.Remove(workGiver);
                    }
                }
            }

            //Add new work types

            foreach (WorkType defaultWorkType in PM.DEFAULT_PRESET.workTypes)
            {
                WorkType newPresetWorkType = selectedPreset.FindWorkTypeByDefName(defaultWorkType.defName);

                if (newPresetWorkType != null)
                {
                    foreach (WorkGiver defaultWorkGiver in defaultWorkType.workGivers)
                    {
                        WorkGiver detectedWorkType = null;

                        foreach (WorkType checkedWorkType in selectedPreset.workTypes)
                        {
                            detectedWorkType = checkedWorkType.workGivers.Find(wg => wg.defName == defaultWorkGiver.defName);

                            if (detectedWorkType != null) break;
                        }

                        if (detectedWorkType != null) continue;

                        WorkType workTypeForInserting = selectedPreset.FindWorkTypeByDefName(defaultWorkType.defName);
                        workTypeForInserting.InsertWorkGiverByPriority(defaultWorkGiver);
                    }
                }
                else
                {
                    selectedPreset.InsertWorkTypeByPriority(defaultWorkType);
                }
            }

            selectedPreset.hash = PM.DEFAULT_PRESET.hash;
        }

        public void SetSelectedWorkType(WorkType workTypeToSelect)
        {
            selectedWorkType = workTypeToSelect;

            if (workTypeToSelect == null) return;

            float yPos = selectedPreset.workTypes.IndexOf(selectedWorkType) * (elementHeight + elementGap);

            if (yPos < workTypesScrollPosition.y)
            {
                workTypesScrollPosition.y = yPos;
            }
            else if (yPos + elementHeight + elementGap > workTypesScrollPosition.y + elementsColumnHeight)
            {
                workTypesScrollPosition.y = yPos - elementsColumnHeight + elementHeight + elementGap;
            }
        }

        private float Button_CreateCustomGroup(Rect buttonRect)
        {
            if (Widgets.ButtonText(buttonRect, "personalWorkCategories_createCustomGroup".Translate()))
            {
                if (selectedPreset.CreateNewCustomWorkType() is WorkType customWorkType)
                {
                    selectedPreset.MoveWorkTypeToPosition(customWorkType, selectedWorkType);
                    SetSelectedWorkType(customWorkType);
                }
            }

            return buttonRect.height + 5f;
        }

        private float Button_EnableAdvancedMode(Rect buttonRect)
        {
            if (Widgets.ButtonText(buttonRect, "personalWorkCategories_enableAdvancedMode".Translate()))
            {
                selectedPreset.isAdvanced = true;
            }

            Widgets.DrawBoxSolid(buttonRect, new Color(1.0f, 0.35f, 0.0f, 0.3f));

            float height = buttonRect.height;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(buttonRect.x, buttonRect.y + height, buttonRect.width, 100f),
                "personalWorkCategories_beCarefulWithAdvancedMode".Translate()); ;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            height += buttonRect.height + 5f;

            return height;
        }
    }
}
