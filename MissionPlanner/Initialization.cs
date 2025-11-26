using MissionPlanner.Utils;
using SpaceTuxUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MissionPlanner.HierarchicalStepsWindow;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{

    internal class EngineTypeInfo
    {
        internal string engineType;
        internal List<string> Propellants = new List<string>();

        internal EngineTypeInfo(string engineType)
        {
            this.engineType = engineType;
        }
        internal string Key
        {
            get
            {
                string str = engineType + ":";
                foreach (var s in Propellants)
                    str += s + ":";
                return str;
            }
        }
        internal string EngineType { get { return engineType; } }
    }

    internal class Initialization : MonoBehaviour
    {
        static bool initialized = false;

        internal static string[] engineTypesAr;
        internal static string[] engineTypesDisplayAr;
        internal static Dictionary<string, EngineTypeInfo> engineTypeDict = new Dictionary<string, EngineTypeInfo>();

        internal static string[] rcsTypesAr;
        internal static string[] rcsTypesDisplayAr;
        internal static Dictionary<string, EngineTypeInfo> rcsTypeDict = new Dictionary<string, EngineTypeInfo>();

        internal static bool systemHeat;
        internal static IEnumerator BackgroundInitialize()
        {
            if (!initialized)
            {

                initialized = true;


                systemHeat = SpaceTuxUtility.HasMod.hasMod("SystemHeat");
                vabOrganizer = SpaceTuxUtility.HasMod.hasMod("VABOrganizer");
                HierarchicalStepsWindow.criterionTypeStrings = Enum.GetNames(typeof(CriterionType));
                for (int i = 0; i < criterionTypeStrings.Length; i++)
                {
                    criterionTypeStrings[i] = StringFormatter.BeautifyName(criterionTypeStrings[i]);
                }

                HierarchicalStepsWindow.maneuverStrings = Enum.GetNames(typeof(Maneuver));
                for (int i = 0; i < maneuverStrings.Length; i++)
                {
                    maneuverStrings[i] = StringFormatter.BeautifyName(maneuverStrings[i]);
                }



                ResourceStrings = GetAllResourcesStrings().ToArray();

                // *******************************************************************
                {
                    foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                    {
                        if (HierarchicalStepsWindow.IsBannedPart(ap))
                            continue;
                        
                        ConfigNode[] modules = ap.partConfig.GetNodes("MODULE");
                        foreach (var m in modules)
                        {
                            string name = m.SafeLoad("name", "");
                            if (name.StartsWith("ModuleEngines"))
                            {
                                string engineType = m.SafeLoad("EngineType", "");
                                if (engineType != "")
                                {
                                    EngineTypeInfo eti = new EngineTypeInfo(engineType);
                                    var propellantNodes = m.GetNodes("PROPELLANT");
                                    foreach (var p in propellantNodes)
                                    {
                                        string pname = p.SafeLoad("name", "");
                                        if (pname != "" && pname != "IntakeAir" && !eti.Propellants.Contains(pname))
                                            eti.Propellants.Add(pname);
                                    }
                                    if (!engineTypeDict.ContainsKey(eti.Key))
                                        engineTypeDict.Add(eti.Key, eti);
                                }
                            }

                            if (name.StartsWith("ModuleRCS"))
                            {
                                string resourceName = m.SafeLoad("resourceName", "");

                                {
                                    EngineTypeInfo eti = new EngineTypeInfo("RCS");
                                    if (resourceName != "MonoPropellant")
                                    {
                                        var propellantNodes = m.GetNodes("PROPELLANT");
                                        foreach (var p in propellantNodes)
                                        {
                                            string pname = p.SafeLoad("name", "");
                                            if (pname != "" && !eti.Propellants.Contains(pname))
                                                eti.Propellants.Add(pname);
                                        }
                                    }
                                    else
                                    {
                                        eti.Propellants.Add(resourceName);
                                    }
                                    if (!rcsTypeDict.ContainsKey(eti.Key))
                                        rcsTypeDict.Add(eti.Key, eti);
                                }
                            }

                        }


                        if (vabOrganizer && ap.partConfig.HasNode("VABORGANIZER"))
                        {
                            var nodes = ap.partConfig.GetNodes("VABORGANIZER");
                            foreach (var node in nodes)
                            {
                                foreach (var s in node.GetValues("organizerSubcategory"))
                                {
                                    VABOrganizerUtils.AddPartToCategory(s, ap.name);
                                }
                            }
                        }
                        //VABOrganizerUtils.Dump();
                    }
                    engineTypesAr = engineTypeDict.Keys
                        .OrderBy(s => s)
                        .ToArray();
                    if (engineTypesAr.Length > 0)
                    {
                        List<string> strings = new List<string>();
                        foreach (var e in engineTypesAr)
                        {
                            var split = e.Split(':');
                            string str = split[0] + " (";
                            for (int i = 1; i < split.Length; i++)
                            {
                                str += split[i];
                                if (i < split.Length - 2)
                                    str += ", ";
                            }
                            str += ")";
                            strings.Add(str);
                        }
                        engineTypesDisplayAr = strings.ToArray();
                    }
                    else
                        Log.Error("engineTypesAr is empty");

                    //rcsTypesAr = rcsTypeDict.Keys.ToArray();
                    rcsTypesAr = rcsTypeDict.Keys
                        .OrderBy(s => s)
                        .ToArray();
                    if (rcsTypesAr.Length > 0)
                    {
                        List<string> strings = new List<string>();
                        foreach (var e in rcsTypesAr)
                        {
                            var split = e.Split(':');
                            string str = split[0] + " (";
                            for (int i = 1; i < split.Length; i++)
                            {
                                str += split[i];
                                if (i < split.Length - 2)
                                    str += ", ";
                            }
                            str += ")";
                            strings.Add(str);
                        }
                        rcsTypesDisplayAr = strings.ToArray();
                    }
                    else
                        Log.Error("rcsTypesAr is empty");
                }
            }
            yield return null;
        }
    }
}

