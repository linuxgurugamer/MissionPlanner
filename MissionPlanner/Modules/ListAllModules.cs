using MissionPlanner;
using System.Collections.Generic;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public class ListAllModules : MonoBehaviour
{
    static public HashSet<string> uniqueModules = new HashSet<string>();
    public void Start()
    {

        foreach (AvailablePart ap in PartLoader.LoadedPartsList)
        {
            if (HierarchicalStepsWindow.IsBannedPart(ap))
                continue;

            foreach (PartModule pm in ap.partPrefab.Modules)
            {
                if (pm == null) continue;

                uniqueModules.Add(pm.moduleName);
            }
        }

        Debug.Log("[ModuleLister] ==============================");
        Debug.Log($"[ModuleLister] Total unique module types found: {uniqueModules.Count}");
        foreach (string name in uniqueModules)
        {
            Debug.Log($"[ModuleLister] {name}");
        }
        Debug.Log("[ModuleLister] ==============================");
    }
}
