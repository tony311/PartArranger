﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TonyPartArranger
{
    public partial class PartArranger : MonoBehaviour
    {
        /// <summary>
        /// Defines available cursor types
        /// </summary>
        public enum CursorType
        {
            ResizeNS,
            Default
        }

        /// <summary>
        /// Runs when the plugin should render its UI
        /// </summary>
        public void OnGUI()
        {
            if (!windowShown || (Boolean)settings["HidePlugin"])
                return;

            GUI.skin = HighLogic.Skin;

            // Default button style
            buttonStyles["Normal"] = new GUIStyle(GUI.skin.button);
            buttonStyles["Normal"].padding = new RectOffset(1, 1, 1, 1);

            // Used for active or disabled buttons - current category's button, MoveUp button when you can't move up, etc.
            buttonStyles["Disabled"] = new GUIStyle(buttonStyles["Normal"]);
            buttonStyles["Disabled"].normal = buttonStyles["Disabled"].active;
            buttonStyles["Disabled"].hover = buttonStyles["Disabled"].active;

            // Used for parts in the part list
            buttonStyles["Part"] = new GUIStyle(buttonStyles["Normal"]);
            buttonStyles["Part"].normal.background = (Texture2D)buttonTextures["ButtonBGOff"];
            buttonStyles["Part"].active.background = (Texture2D)buttonTextures["ButtonBGActive"];
            buttonStyles["Part"].hover.background = (Texture2D)buttonTextures["ButtonBGHover"];
            buttonStyles["Part"].fontStyle = FontStyle.Normal;
            buttonStyles["Part"].alignment = TextAnchor.MiddleLeft;
            buttonStyles["Part"].padding = new RectOffset(5, 0, 4, 0);

            // Used for the currently selected part in the part list
            buttonStyles["PartActive"] = new GUIStyle(buttonStyles["Part"]);
            buttonStyles["PartActive"].active.textColor = new Color(1.0F, 0.5F, 0.0F);
            buttonStyles["PartActive"].normal = buttonStyles["PartActive"].active;
            buttonStyles["PartActive"].normal.background = (Texture2D)buttonTextures["ButtonBGOn"];

            buttonLayouts["Category"] = new GUILayoutOption[] { GUILayout.Width(38), GUILayout.Height(38) };

            windowRect = GUI.Window(1, windowRect, WindowHandler, "Part Arranger");

            if (tooltipText != "")
            {
                GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box);
                tooltipStyle.alignment = TextAnchor.MiddleLeft;
                tooltipStyle.padding = new RectOffset(8, 0, 0, 0);

                Rect tooltipRect = new Rect(windowRect.x, windowRect.y + windowRect.height, windowRect.width, 23);
                GUI.Box(tooltipRect, tooltipText, tooltipStyle);
            }

            PreventEditorClickthrough();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowID">ID of the window being rendered</param>
        public void WindowHandler(int windowID)
        {
            // Select the first part in the currently selected category if necessary (don't keep invisible parts selected)
            if (IsCategoryValid(CurrentCategory()) && selectedPart.Category != CurrentCategory())
                selectedPart = SortedPart.FindByCategory(CurrentCategory()).First();

            if (GUI.Button(new Rect(windowRect.width - 54, 3, 24, 24), (windowLocked ? new GUIContent(buttonTextures["Locked"], "Unlock window") : new GUIContent(buttonTextures["Unlocked"], "Lock window"))))
                windowLocked = !windowLocked;

            if (GUI.Button(new Rect(windowRect.width - 27, 3, 24, 24), buttonTextures["Close"]))
            {
                if (usingBlizzyToolbar)
                    blizzyButton.TexturePath = pluginDir + "/icons/BlizzyIconOff";
                else
                    appButton.SetFalse();

                windowShown = false;
                EditorLogic.fetch.Unlock("PartArrangerLock");
            }

            /*** Category buttons ***/
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(buttonTextures["CatPods"], "Pods"), (CurrentCategory() == PartCategories.Pods ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Pods);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatPropulsion"], "Propulsion"), (CurrentCategory() == (int)PartCategories.Propulsion ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Propulsion);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatControl"], "Control"), (CurrentCategory() == PartCategories.Control ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Control);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatStructural"], "Structural"), (CurrentCategory() == PartCategories.Structural ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Structural);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatAero"], "Aerodynamic"), (CurrentCategory() == PartCategories.Aero ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Aero);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatUtility"], "Utility"), (CurrentCategory() == PartCategories.Utility ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Utility);
            else if (GUILayout.Button(new GUIContent(buttonTextures["CatScience"], "Science"), (CurrentCategory() == PartCategories.Science ? buttonStyles["Disabled"] : buttonStyles["Normal"]), buttonLayouts["Category"]))
                ShowCategory(PartCategories.Science);
            GUILayout.EndHorizontal();

            /*** Part button scrollview ***/
            int viewHeight = SortedPart.FindByCategory(CurrentCategory()).Count * 23 + 3;
            partScrollPosition = GUI.BeginScrollView(new Rect(8, 68, 290, windowRect.height - 73), partScrollPosition, new Rect(0, 0, 271, viewHeight));
            if (IsCategoryValid(CurrentCategory()))
            {
                if (draggedPart != null && drawingDrag)
                {
                    foreach (SortedPart part in SortedPart.FindByCategory(CurrentCategory()))
                    {
                        if (part != draggedPart) // Don't draw the part we're dragging
                        {
                            Rect position = part.ButtonPosition;

                            if (draggedPart.CursorPosition < draggedPart.Position) // handle parts below empty
                            {
                                if (part.Position >= draggedPart.CursorPosition && part.Position < draggedPart.Position)
                                    position.y += 23;
                            }
                            else if (draggedPart.CursorPosition > draggedPart.Position) // handle parts above empty
                            {
                                if (part.Position <= draggedPart.CursorPosition && part.Position > draggedPart.Position)
                                    position.y -= 23;
                            }

                            if (GUI.Button(position, part.Title, buttonStyles["Part"]))
                                selectedPart = part;
                        }
                    }

                    // Draw dragged part
                    Rect dragPosition = draggedPart.ButtonPosition;
                    dragPosition.y = Mouse.screenPos.y - windowRect.y + partScrollPosition.y - 68 - 10;

                    if (dragPosition.y < 3)
                        dragPosition.y = 3;
                    else if (dragPosition.y > (SortedPart.FindByCategory(CurrentCategory()).Count-1)*23+3)
                        dragPosition.y = (SortedPart.FindByCategory(CurrentCategory()).Count-1)*23+3;

                    if (GUI.Button(dragPosition, draggedPart.Title, buttonStyles["PartActive"]))
                        return; // Don't do anything if it's actually clicked

                    /*** Scroll view up/down when dragging a part near the edges ***/
                    // Fix reversed y position in mouse coordinates
                    Vector3 mousePos = Input.mousePosition;
                    mousePos.y = Screen.height - mousePos.y;

                    if (mousePos.y > windowRect.y + 68 + (windowRect.height - 73) - 46)
                        partScrollPosition.y += (mousePos.y - (windowRect.y + 68 + (windowRect.height - 73) - 46))/10;
                    else if (mousePos.y < windowRect.y + 68 + 3 + 46)
                        partScrollPosition.y -= ((windowRect.y + 68 + 3 + 46) - mousePos.y)/10;
                }
                else
                {
                    foreach (SortedPart part in SortedPart.FindByCategory(CurrentCategory()))
                    {
                        if (GUI.Button(part.ButtonPosition, new GUIContent(part.Title, "Drag up or down to move"), (part.Name == selectedPart.Name ? buttonStyles["PartActive"] : buttonStyles["Part"])))
                            selectedPart = part;
                    }
                }
            }

            if (needToScroll)
            {
                GUI.ScrollTo(new Rect(0, selectedPart.Position * 23 - 23, 200, 23 * 3));
                needToScroll = false;
            }
            GUI.EndScrollView(true);

            /*** Move buttons ***/
            Boolean upButtonPressed = false;
            Boolean downButtonPressed = false;

            if (IsCategoryValid(CurrentCategory()))
            {
                // "Move to top" button
                if (GUI.Button(new Rect(windowRect.width - 46, 68, 42, 36), new GUIContent(buttonTextures["MoveTop"], "Move the selected part to the top"), (selectedPart.Position == 0 ? buttonStyles["Disabled"] : buttonStyles["Normal"])))
                    MovePart(selectedPart, MoveDirection.Top);

                // "Move up" button
                if (GUI.RepeatButton(new Rect(windowRect.width - 46, 108, 42, 36), new GUIContent(buttonTextures["MoveUp"], "Move the selected part up. Hold to move quickly"), (selectedPart.Position == 0 ? buttonStyles["Disabled"] : buttonStyles["Normal"])))
                {
                    upButtonPressed = true;
                    if (buttonClickTime == 0.0F)
                        MovePart(selectedPart, MoveDirection.Up);
                    else if (isButtonDelayed && buttonClickTime > moveRepeatDelay)
                    {
                        buttonClickTime = 0.0F;
                        isButtonDelayed = false;
                        MovePart(selectedPart, MoveDirection.Up);
                    }

                    else if (!isButtonDelayed && buttonClickTime > moveRepeatInterval)
                    {
                        buttonClickTime = 0.0F;
                        MovePart(selectedPart, MoveDirection.Up);
                    }

                    buttonClickTime += Time.deltaTime;
                }

                // "Move down" button
                if (GUI.RepeatButton(new Rect(windowRect.width - 46, 148, 42, 36), new GUIContent(buttonTextures["MoveDown"], "Move the selected part down. Hold to move quickly"), (selectedPart.Position == SortedPart.FindByCategory(selectedPart.Category).Count - 1 ? buttonStyles["Disabled"] : buttonStyles["Normal"])))
                {
                    downButtonPressed = true;
                    if (buttonClickTime == 0.0F)
                        MovePart(selectedPart, MoveDirection.Down);
                    else if (isButtonDelayed && buttonClickTime > moveRepeatDelay)
                    {
                        buttonClickTime = 0.0F;
                        isButtonDelayed = false;
                        MovePart(selectedPart, MoveDirection.Down);
                    }
                    else if (!isButtonDelayed && buttonClickTime > moveRepeatInterval)
                    {
                        buttonClickTime = 0.0F;
                        MovePart(selectedPart, MoveDirection.Down);
                    }
                    buttonClickTime += Time.deltaTime;
                }

                // "Move to bottom" button
                if (GUI.Button(new Rect(windowRect.width - 46, 188, 42, 36), new GUIContent(buttonTextures["MoveBottom"], "Move the selected part to the bottom"), (selectedPart.Position == SortedPart.FindByCategory(selectedPart.Category).Count - 1 ? buttonStyles["Disabled"] : buttonStyles["Normal"])))
                    MovePart(selectedPart, MoveDirection.Bottom);

                // "Reset category" button
                if (GUI.Button(new Rect(windowRect.width - 46, 228, 42, 36), new GUIContent(buttonTextures["MoveReset"], "Reset this category's sorting to default"), buttonStyles["Normal"]))
                    ResetCategory(CurrentCategory());
            }

            // If neither up/down button is being pressed and this is a "real" draw event, reset the button press timer
            if (!upButtonPressed && !downButtonPressed && Event.current.type == EventType.Repaint)
            {
                isButtonDelayed = true;
                buttonClickTime = 0.0F;
            }
            
            if (Event.current.type == EventType.Repaint)
                tooltipText = GUI.tooltip;

            // Register the window for mouse dragging
            if (!windowLocked && !resizing && draggedPart == null)
                GUI.DragWindow();
            //GUI.DragWindow(new Rect(0,0,windowRect.width,27)); // Titlebar only
        }

        // Idea from Kerbal Engineer + MechJeb
        /// <summary>
        /// Locks the editor if the cursor is inside the plugin's window
        /// </summary>
        public void PreventEditorClickthrough()
        {
            if (!windowShown || (Boolean)settings["HidePlugin"])
                return;

            bool mouseInWindow = windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)) && windowShown;

            if (!lockedEditor && mouseInWindow)
            {
                EditorLogic.fetch.Lock(true, false, true, "PartArrangerLock");
                lockedEditor = true;
            }

            if (lockedEditor && !mouseInWindow)
            {
                EditorLogic.fetch.Unlock("PartArrangerLock");
                lockedEditor = false;
            }
        }

        /// <summary>
        /// Sets the cursor texture
        /// </summary>
        /// <param name="type"></param>
        public void SetCursor(CursorType type)
        {
            if (type == CursorType.ResizeNS)
                Cursor.SetCursor((Texture2D)buttonTextures["CursorResizeNS"], new Vector2(11, 11), CursorMode.ForceSoftware);
            else if (type == CursorType.Default)
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}