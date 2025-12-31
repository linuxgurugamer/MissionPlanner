using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    internal class ResizeHandle
    {
        private bool resizing;
        private Vector2 lastPosition = new Vector2(0, 0);
        private const float resizeBoxSize = 18;
        private const float resizeBoxMargin = 2;

        internal void Draw(ref Rect winRect)
        {
            var resizeBoxStyle = new GUIStyle(GUI.skin.box);
            resizeBoxStyle.fontSize = 10;
            resizeBoxStyle.normal.textColor = XKCDColors.LightGrey;

            var resizer = new Rect(winRect.width - resizeBoxSize - resizeBoxMargin - 2, winRect.height - resizeBoxSize - 2, resizeBoxSize, resizeBoxSize);
            GUI.Box(resizer, "//", resizeBoxStyle);

            if (!Event.current.isMouse)
            {
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                resizer.Contains(Event.current.mousePosition))
            {
                this.resizing = true;
                this.lastPosition.x = Input.mousePosition.x;
                this.lastPosition.y = Input.mousePosition.y;

                Event.current.Use();
            }
        }

        internal void DoResize(ref Rect winRect)
        {
            if (!this.resizing)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                var deltaX = Input.mousePosition.x - this.lastPosition.x;
                var deltaY = Input.mousePosition.y - this.lastPosition.y;

                //Event.current.delta does not make resizing very smooth.

                this.lastPosition.x = Input.mousePosition.x;
                this.lastPosition.y = Input.mousePosition.y;

                winRect.xMax += deltaX;
                winRect.yMax -= deltaY;

                if (Event.current.isMouse)
                {
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                this.resizing = false;

                Event.current.Use();
            }
        }
    } // ResizeHandle


}
