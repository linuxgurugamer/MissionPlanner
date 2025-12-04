using System.Collections.Generic;

public static class StageUtility
{
    /// <summary>
    /// Checks whether the specified stage contains a decoupler or separator.
    /// Works in both Editor and Flight.
    /// </summary>
    public static bool StageHasDecouplerOrSeparator(int stage, bool includeDockingPorts = false)
    {
        List<Part> parts = null;

        if (HighLogic.LoadedSceneIsEditor)
            parts = EditorLogic.fetch.ship.Parts;
        if (HighLogic.LoadedSceneIsFlight)
            parts = FlightGlobals.ActiveVessel.Parts;

        if (parts == null)
            return false;

        foreach (Part p in parts)
        {
            // Only check parts in the specified stage
            if (p.inverseStage != stage)
                continue;

            // Stock decouplers
            if (p.Modules.GetModules<ModuleDecouple>().Count > 0)
                return true;

            if (p.Modules.GetModules<ModuleAnchoredDecoupler>().Count > 0)
                return true;

            if (includeDockingPorts && p.Modules.GetModules<ModuleDockingNode>().Count > 0)
                return true;
        }

        return false;
    }
}
