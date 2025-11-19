using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MissionPlanner.HierarchicalStepsWindow;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner.Utils
{
    public class VABOrganizerUtils
    {
        static Dictionary<string, VABOrganizerUtils> dict = new Dictionary<string, VABOrganizerUtils>();
        string organizerSubcategory;
        HashSet<string> parts = new HashSet<string>();

        public VABOrganizerUtils(string organizerSubcategory)
        {
            this.organizerSubcategory = organizerSubcategory;
        }

        public static void AddPartToCategory(string organizerSubcategory, string partName)
        {
            if (!dict.ContainsKey(organizerSubcategory))
                dict[organizerSubcategory] = new VABOrganizerUtils(organizerSubcategory);

            dict[organizerSubcategory].parts.Add(partName);
        }

        public static bool IsPartInCategory(string organizerSubcategory, string partName)
        {
            if (dict.ContainsKey(organizerSubcategory))
            {
                return dict[organizerSubcategory].parts.Contains(partName);
            }
            return false;
        }

        public static List<string> SubCategories()
        {
            return dict.Keys.Select(k => k.ToString()).ToList();
            // Following does this without Linq
#if false
            List<string> result = new List<string>();
            foreach (var d in dict.Keys)
            {
                result.Add(d);
            }
            return result();
#endif
        }

#if DEBUG
        public static void Dump()
        {
            foreach (var d in dict)
            {

                Log.Info($"category: {d.Key}");
                foreach (var d1 in d.Value.parts)
                {
                    Log.Info($"category: {d.Key}   part: {d1}");
                }

            }
        }
#endif
    }
}
