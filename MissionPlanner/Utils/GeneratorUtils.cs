
// File: GeneratorUtils.cs
// Detect and summarize ModuleGenerator usage (stock RTGs and similar)
// C# 7.3 compatible

using MissionPlanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class GeneratorUtils
{
    // ---------- EC generation summary struct ----------
    public struct GeneratorSummary
    {
        public double TotalECps;
        public int generatorCnt;
    }


    // =========================
    // ===== Presence / Count ==
    // =========================

    public static bool HasGeneratorsFlight(Vessel v)
    {
        if (v == null) return false;
        return v.Parts.Any(p => p != null && p.FindModulesImplementing<ModuleGenerator>().Count > 0);
    }

    public static bool HasGeneratorsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;
        return ship.parts.Any(p => p != null && p.FindModulesImplementing<ModuleGenerator>().Count > 0);
    }

    public static int CountGeneratorsFlight(Vessel v)
    {
        if (v == null) return 0;
        int n = 0;
        foreach (var p in v.Parts)
            n += p.FindModulesImplementing<ModuleGenerator>().Count;
        return n;
    }

    public static int CountGeneratorsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return 0;
        int n = 0;
        foreach (var p in ship.parts)
            n += p.FindModulesImplementing<ModuleGenerator>().Count;
        return n;
    }

    // =========================
    // ===== Part lists ========
    // =========================

    public static List<Part> GetGeneratorPartsFlight(Vessel v)
    {
        if (v == null) return new List<Part>();
        return v.Parts.Where(p => p.FindModulesImplementing<ModuleGenerator>().Count > 0).ToList();
    }

    public static List<Part> GetGeneratorPartsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return new List<Part>();
        return ship.parts.Where(p => p.FindModulesImplementing<ModuleGenerator>().Count > 0).ToList();
    }

    // =========================
    // ===== EC/s summaries ====
    // =========================

    /// <summary>
    /// Editor: nominal ElectricCharge/s from all ModuleGenerator instances (assumes active).
    /// </summary>
    public static GeneratorSummary GetTotalECGeneratorsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return new GeneratorSummary();

        return GetTotalECGenerators(ship.parts);
    }

    /// <summary>
    /// Flight: ElectricCharge/s from all *active* ModuleGenerator instances (best effort).
    /// </summary>
    public static GeneratorSummary GetTotalECGeneratorsFlight(Vessel v)
    {
        if (v == null) return new GeneratorSummary();

        return GetTotalECGenerators(v.parts);
    }

    public static GeneratorSummary GetTotalECGenerators(List<Part> parts)
    {
        GeneratorSummary ggs = new GeneratorSummary();
        //double total = 0;

        foreach (var p in parts)
        {
            foreach (PartModule tmpPM in p.Modules)
            {
                switch (tmpPM.moduleName)
                {
                    case "ModuleGenerator":
                        {
                            ModuleGenerator tmpGen = (ModuleGenerator)tmpPM;

                            foreach (ModuleResource outp in tmpGen.resHandler.outputResources)
                            {
                                if (outp.name == "ElectricCharge")
                                {
                                    ggs.TotalECps += outp.rate;
                                    ggs.generatorCnt++;
                                }
                            }
                        }
                        break;

                    case "ModuleResourceConverter":
                    case "FissionReactor":
                    case "KFAPUController":
                        {
                            ModuleResourceConverter tmpGen = (ModuleResourceConverter)tmpPM;
                            foreach (ResourceRatio outp in tmpGen.outputList)
                            {
                                if (outp.ResourceName == "ElectricCharge")
                                {
                                    ggs.TotalECps += outp.Ratio;
                                    ggs.generatorCnt++;
                                }
                            }
                        }
                        break;
                        // ModuleSystemHeatFissionReactor is dealt with below, since it gets the data from ConfigNodes
                    //case "ModuleSystemHeatFissionReactor":
                    //    break;

                }
            }
            if (Initialization.systemHeat)
            {
                var a = SystemHeatConfigUtils.GetElectricalGenerationNodes(p, out float maxEC);
                if (a.Count > 0)
                {
                    ggs.TotalECps += maxEC;
                    ggs.generatorCnt++;
                }
            }
        }
        return ggs; // EC/s
    }
}

