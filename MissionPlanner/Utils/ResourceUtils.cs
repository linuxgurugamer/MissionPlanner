using System;
using System.Collections.Generic;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {

        /// <summary>
        /// Returns all resource definitions currently loaded by KSP1.
        /// </summary>
        static List<PartResourceDefinition> partResourceDefinitions = null;
        static List<string> partResourceDefinitionStrings = null;
        public static Dictionary<string, string> partResourceDisplayStrings = new Dictionary<string, string>();
        static Dictionary<string, int> partResourceIds = new Dictionary<string, int>();

        static List<PartResourceDefinition> GetAllResources()
        {
            if (partResourceDefinitions != null)
                return partResourceDefinitions;
            partResourceDefinitions = new List<PartResourceDefinition>();
            var lib = PartResourceLibrary.Instance;
            if (lib == null) return partResourceDefinitions;

            // PartResourceDefinitionList implements IEnumerable<PartResourceDefinition>
            foreach (var def in lib.resourceDefinitions)
            {
                if (def != null && def.name != "MJPropellant") partResourceDefinitions.Add(def);
            }

            return partResourceDefinitions;
        }

        public static List<String> GetAllResourcesStrings()
        {
            GetAllResources();
            if (partResourceDefinitionStrings != null)
                return partResourceDefinitionStrings;
            partResourceDefinitionStrings = new List<string>();

            partResourceDefinitionStrings.Add("(none)");
            partResourceIds["(none)"] = 0;

            if (partResourceDefinitions == null)
                return partResourceDefinitionStrings;

            foreach (var def in partResourceDefinitions)
            {
                if (def != null)
                {
                    partResourceDefinitionStrings.Add(def.name);
                    partResourceDisplayStrings[def.name] = def.displayName;
                    partResourceIds[def.name] = partResourceIds.Count;
                }
            }
            return partResourceDefinitionStrings;
        }

        public static int GetResourceId(string id)
        {
            if (id == "")
                return 0;
            GetAllResourcesStrings();
            try
            {
                return partResourceIds[id];
            }
            catch
            {
                Log.Error("Resource: " + id + " not found");
                return 0;
            }
        }
    }
}
