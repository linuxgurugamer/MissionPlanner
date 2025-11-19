using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

using static SpaceTuxUtility.ConfigNodeUtils;

public static class SystemHeatConfigUtils
{
    public struct SHElectricalGeneration
    {
        public int level;
        public float ecGeneration;

        public SHElectricalGeneration(string[] ar)
        {
            level = int.Parse(ar[0]);
            ecGeneration = float.Parse(ar[1]);
        }
    }

    private static char[] delimiters = new char[4] { ' ', ',', ';', '\t' };

    /// <summary>
    /// Returns all ELECTRICAL GENERATION nodes inside the 
    /// ModuleSystemHeatFissionReactor config for this part.
    /// </summary>
    public static List<SHElectricalGeneration> GetElectricalGenerationNodes(Part part, out float maxEC)
    {
        List<SHElectricalGeneration> ecList = new List<SHElectricalGeneration>();
        maxEC = 0f;
        if (part == null || part.partInfo == null)
            return ecList;

        // Get the original, unmodified prefab part config
        ConfigNode partConfig = GameDatabase.Instance
            .GetConfigNode(part.partInfo.partUrl);

        if (partConfig == null)
            return ecList;

        // Locate the module node(s)
        ConfigNode[] moduleNodes = partConfig.GetNodes("MODULE");
        if (moduleNodes == null || moduleNodes.Length == 0)
            return ecList;

        foreach (ConfigNode m in moduleNodes)
        {
            string name = m.SafeLoad("name", "");
            //string name = m.GetValue("name");
            if (!string.Equals(name, "ModuleSystemHeatFissionReactor"))
                continue;

            // Inside the module, SystemHeat defines one or more:
            //     ELECTRICALGENERATION
            // blocks (case-insensitive safe)
            ConfigNode[] eNodes = m.GetNodes("ElectricalGeneration");
            if (eNodes != null && eNodes.Length > 0)
            {
                foreach (var e in eNodes)
                {
                    var n = e.GetValues("key");
                    foreach (var n2 in n)
                    {
                        string[] array = n2.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        ecList.Add(new SHElectricalGeneration(array));
                        maxEC = Math.Max(maxEC, ecList[ecList.Count].ecGeneration);
                    }
                }
            }
        }

        return ecList;
    }
}
