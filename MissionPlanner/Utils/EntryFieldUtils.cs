using UnityEngine;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        public int currentEntryFieldId = 0;
        public int oldEntryFieldId = 0;

        void GetCurrentEntryFieldID()
        {
            currentEntryFieldId = GUIUtility.GetControlID(FocusType.Keyboard);
        }

        private void IntField(string label, ref int value, bool locked, float width = 120, float labelWidth = 90)
        {
            IntField(new GUIContent(label), ref value, locked, width, labelWidth);
        }
        private void IntField(GUIContent label, ref int value, bool locked, float width = 120, float labelWidth = 90)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, ScaledGUILayoutWidth(labelWidth));
                GetCurrentEntryFieldID();
                string buf = GUILayout.TextField(value.ToString(), ScaledGUILayoutWidth(width));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                if (!locked && int.TryParse(buf, out int parsed)) value = parsed;
                GUILayout.FlexibleSpace();
            }
        }

        private float FloatField(GUIContent label, float value, int places, bool locked, string suffix = "", float width = 120, bool flex = true, float labelWidth=90)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, ScaledGUILayoutWidth(labelWidth)); //, ScaledGUILayoutWidth(120));
                string buf = "";
                GetCurrentEntryFieldID();
                if (places == 0)
                    buf = GUILayout.TextField(value.ToString("F0"), ScaledGUILayoutWidth(width));
                else
                    buf = GUILayout.TextField(value.ToString($"F{places}"), ScaledGUILayoutWidth(width));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                if (!locked && float.TryParse(buf, out float parsed))
                    value = parsed;
                GUILayout.Label(suffix);
                if (flex)
                    GUILayout.FlexibleSpace();
            }
            return value;

        }
        private float FloatField(string label, float value, int places, bool locked, string suffix = "", float width = 120, bool flex = true, float labelWidth = 90)
        {
            return FloatField(new GUIContent(label, ""), value, places, locked, suffix, width, flex);
#if false
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label); //, ScaledGUILayoutWidth(120));
                string buf = "";
                GetCurrentEntryFieldID();
                if (places == 0)
                    buf = GUILayout.TextField(value.ToString("F0"), ScaledGUILayoutWidth(width));
                else
                    buf = GUILayout.TextField(value.ToString($"F{places}"), ScaledGUILayoutWidth(width));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                if (!locked && float.TryParse(buf, out float parsed))
                    value = parsed;
                GUILayout.Label(suffix);
                if (flex)
                    GUILayout.FlexibleSpace();
            }
            return value;
#endif
        }

        private void FloatField(string label, ref float value, int places, bool locked, string suffix = "", float width = 120, bool flex = true)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label); //, ScaledGUILayoutWidth(120));
                string buf = "";
                GetCurrentEntryFieldID();
                if (places == 0)
                    buf = GUILayout.TextField(value.ToString("F0"), ScaledGUILayoutWidth(width));
                else
                    buf = GUILayout.TextField(value.ToString($"F{places}"), ScaledGUILayoutWidth(width));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                if (!locked && float.TryParse(buf, out float parsed))
                    value = parsed;
                GUILayout.Label(suffix);
                if (flex)
                    GUILayout.FlexibleSpace();
            }
        }

        private void DoubleField(string label, ref double value, bool locked, string suffix = "", float width = 120)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label); //, ScaledGUILayoutWidth(90));
                string buf = null;
                GetCurrentEntryFieldID();
                if (oldEntryFieldId != currentEntryFieldId)
                {
                    buf = GUILayout.TextField(Utils.AntennaUtils.FormatPower(value), ScaledGUILayoutWidth(width));
                }
                else
                {
                    buf = GUILayout.TextField(value.ToString(), ScaledGUILayoutWidth(width));
                    Log.Info("DoubleField, value: " + value + ", buf: " + buf);
                    if (TryParseWithSuffix(buf, out double parsed))
                    {
                        value = parsed;
                    }
                }
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                GUILayout.Label(suffix);
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Parses a string that may contain a suffix (k, M, G) and converts it to a float.
        /// Examples:
        ///   "500"  -> 500
        ///   "1.5k" -> 1500
        ///   "2M"   -> 2000000
        ///   "3G"   -> 3000000000
        /// </summary>
        private bool TryParseWithSuffix(string input, out double result)
        {
            result = 0f;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();
            char suffix = char.ToUpperInvariant(input[input.Length - 1]);

            double multiplier = 1f;
            string numericPart = input;

            switch (suffix)
            {
                case 'K':
                    multiplier = 1_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
                case 'M':
                    multiplier = 1_000_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
                case 'G':
                    multiplier = 1_000_000_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
                default:
                    break;
            }
            Log.Info("Before double.TryParse, numericPart: " + numericPart);
            if (double.TryParse(numericPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double baseValue))
            {
                result = baseValue * multiplier;
                return true;
            }

            return false;
        }


#if false
        private void IntRangeFields(ref int min, ref int max, bool locked)
        {
                       SetNextControlName();
 using (new GUILayout.HorizontalScope())
            {
            GUILayout.Label("Min (int)", ScaledGUILayoutWidth(90));
            string minBuf = GUILayout.TextField(min.ToString(), ScaledGUILayoutWidth(120));
            GUILayout.Space(12);
            GUILayout.Label("Max (int)", ScaledGUILayoutWidth(90));
            string maxBuf = GUILayout.TextField(max.ToString(), ScaledGUILayoutWidth(120));
            GUILayout.FlexibleSpace();
            }

            if (!locked)
            {
                int pmin, pmax;
                if (int.TryParse(minBuf, out pmin)) min = pmin;
                if (int.TryParse(maxBuf, out pmax)) max = pmax;
            }
        }
#endif

        private void FloatRangeFields(ref float min, ref float max, bool locked)
        {
            string minBuf, maxBuf;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Min (float)", ScaledGUILayoutWidth(90));
                GetCurrentEntryFieldID();
                if (oldEntryFieldId != currentEntryFieldId)
                    minBuf = GUILayout.TextField(min.ToString("G"), ScaledGUILayoutWidth(120));
                else
                    minBuf = GUILayout.TextField(min.ToString("F0"), ScaledGUILayoutWidth(120));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;

                GUILayout.Space(12);
                GUILayout.Label("Max (float)", ScaledGUILayoutWidth(90));
                GetCurrentEntryFieldID();
                if (oldEntryFieldId != currentEntryFieldId)
                    maxBuf = GUILayout.TextField(max.ToString("G"), ScaledGUILayoutWidth(120));
                else
                    maxBuf = GUILayout.TextField(max.ToString("F0"), ScaledGUILayoutWidth(120));
                oldEntryFieldId = (GUIUtility.keyboardControl - 1) == currentEntryFieldId ? currentEntryFieldId : oldEntryFieldId;
                GUILayout.FlexibleSpace();
            }

            if (!locked)
            {
                float pmin, pmax;
                if (float.TryParse(minBuf, out pmin)) min = pmin;
                if (float.TryParse(maxBuf, out pmax)) max = pmax;
            }
        }
    }
}
