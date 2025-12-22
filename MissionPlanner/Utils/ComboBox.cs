// Contents of this file originated from Mechjeb
//
// Contents of this file are governed by the GPL v3
//
// MechJeb2 Copyright(C) 2013
//    This program comes with ABSOLUTELY NO WARRANTY!
//    This is free software, and you are welcome to redistribute it
//    under certain conditions, as outlined in the full content of
//    the GNU General Public License (GNU GPL), version 3, revision
//    date 29 June 2007.
//
//
//

using System;
using System.Collections.Generic;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner.Utils
{

    static public class ComboBox
    {

        static GUIStyle _yellowOnHover;
        public static GUIStyle yellowOnHover
        {
            get
            {
                if (_yellowOnHover == null)
                {
                    _yellowOnHover = new GUIStyle(GUI.skin.button);
                    _yellowOnHover.hover.textColor = Color.yellow;
                    Texture2D t = new Texture2D(1, 1);
                    _yellowOnHover.fontSize = (int)(12f * GameSettings.UI_SCALE);
                   // t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                   // t.Apply();
                   //_yellowOnHover.hover.background = t;
                }
                return _yellowOnHover;
            }
        }

        public class ComboBoxData
        {
            //int id;
            // Position of the popup
            internal Rect rect;
            // Identifier of the caller of the popup, null if nobody is waiting for a value
            internal object popupOwner = null;
            internal string[] entries;
            public float minWidth;
            internal bool popupActive = false;
            // Result to be returned to the owner
            internal int selectedItem;

        }

        static public Dictionary<int, ComboBoxData> comboBoxData = new Dictionary<int, ComboBoxData>();

        // Easy to use combobox class
        // ***** For users *****
        // Call the Box method with the latest selected item, list of text entries
        // and an object identifying who is making the request.
        // The result is the newly selected item.

        // Unity identifier of the window, just needs to be unique
        private static int id = GUIUtility.GetControlID(FocusType.Passive);

        // ComboBox GUI Style
        private static GUIStyle style;
        static  GUIStyle selectionStyle;

        static ComboBox()
        {
            UpdateStyles((int)(12f * GameSettings.UI_SCALE));
        }
        public static void UpdateStyles(int fontSize)
        {
            style = new GUIStyle(GUI.skin.window);
            style.normal.background = null;
            style.onNormal.background = null;
            style.border.top = style.border.bottom;
            style.padding.top = style.padding.bottom;
            style.fontSize = fontSize;
            _yellowOnHover = null;

            selectionStyle = new GUIStyle(GUI.skin.button);
            selectionStyle.fontSize = fontSize;

            _yellowOnHover = null;
            //popupActive = false;
        }

        public static void DrawGUI()
        {
            foreach (var c in comboBoxData.Values)
            {
                if (c.popupOwner == null || c.rect.height == 0 || !c.popupActive)
                    continue;
                var scaledScreenWidth = Screen.width;
                var scaledScreenHeight = Screen.height;

                if (style.normal.background == null)
                {
                    style = GUI.skin.button;
                }

                // Make sure the rectangle is fully on screen
                c.rect.x = Math.Max(0, Math.Min(c.rect.x, scaledScreenWidth - c.rect.width));
                c.rect.y = Math.Max(0, Math.Min(c.rect.y, scaledScreenHeight - c.rect.height));

                c.rect = GUILayout.Window(id, c.rect, identifier =>
                {
                    c.selectedItem = GUILayout.SelectionGrid(-1, c.entries, 1, yellowOnHover);
                    if (GUI.changed)
                        c.popupActive = false;
                }, "", style);

                //Cancel the popup if we click outside
                if (Event.current.type == EventType.MouseDown && !c.rect.Contains(Event.current.mousePosition))
                {
                    c.popupOwner = null;
                }
            }
        }

        public static int Box(int id, int selectedItem, string[] entries, object caller,float width , bool locked, bool expandWidth = true)
        {
            int oldSelectedItem = selectedItem;
            // Trivial cases (0-1 items)
            if (entries.Length == 0)
                return 0;
            if (entries.Length == 1)
            {
                GUILayout.Label(entries[0]);
                return 0;
            }

            if (selectedItem >= entries.Length)
                selectedItem = entries.Length - 1;

            if (!comboBoxData.ContainsKey(id))
            {
                comboBoxData[id] = new ComboBoxData();
            }
            //if (dontUseDropDownMenu)
            //    return ArrowSelector(selectedItem, entries.Length, entries[selectedItem], expandWidth);

            // A choice has been made, update the return value
            if (comboBoxData[id]. popupOwner == caller && !comboBoxData[id].popupActive)
            {
                comboBoxData[id].popupOwner = null;
                selectedItem = comboBoxData[id].selectedItem;
                GUI.changed = true;
            }

            if (selectedItem < 0)
                return 0;

            try
            {
                bool guiChanged = GUI.changed;
                if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓", HierarchicalStepsWindow.ScaledGUILayoutWidth(width) ))
                {
                    // We will set the changed status when we return from the menu instead
                    GUI.changed = guiChanged;
                    // Update the global state with the new items
                    comboBoxData[id].popupOwner = caller;
                    comboBoxData[id].popupActive = true;

                    comboBoxData[id].entries = entries;
                    // Magic value to force position update during repaint event
                    comboBoxData[id].rect = new Rect(0, 0, 0, 0);
                }
            }
            catch { }  // used to catch index errors when the skin changes

            // The GetLastRect method only works during repaint event, but the Button will return false during repaint
            if (Event.current.type == EventType.Repaint && comboBoxData[id].popupOwner == caller && comboBoxData[id].rect.height == 0)
            {
                comboBoxData[id].rect = GUILayoutUtility.GetLastRect();
                // But even worse, I can't find a clean way to convert from relative to absolute coordinates
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                Vector2 clippedMousePos = Event.current.mousePosition;
                comboBoxData[id].rect.x = (comboBoxData[id].rect.x + mousePos.x) / 1 - clippedMousePos.x;
                comboBoxData[id].rect.y = (comboBoxData[id].rect.y + mousePos.y) / 1 - clippedMousePos.y;
            }

            //Cancel the popup if we click outside
            if (Event.current.type == EventType.MouseDown && !comboBoxData[id].rect.Contains(Event.current.mousePosition))
            {
                comboBoxData[id].popupOwner = null;
            }
            if (locked)
                return oldSelectedItem;
            return selectedItem;
        }
    }

}
