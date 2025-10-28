using System.Linq;
using UnityEngine;

public static class RCSUtils
{
    /// <summary>
    /// Returns true if the vessel currently has RCS toggled ON in flight.
    /// </summary>
    public static bool IsRCSEngaged(Vessel v)
    {
        return v != null && v.ActionGroups[KSPActionGroup.RCS];
    }

    /// <summary>
    /// Returns true if the vessel has at least one RCS thruster module and is controllable in flight.
    /// </summary>
    public static bool IsRCSAvailableFlight(Vessel v)
    {
        if (v == null) return false;
        if (!v.IsControllable) return false;

        // RCS modules can be either ModuleRCS or ModuleRCSFX
        return v.Parts.Any(p =>
            p.FindModuleImplementing<ModuleRCS>() != null ||
            p.FindModuleImplementing<ModuleRCSFX>() != null
        );
    }

    /// <summary>
    /// Returns true if the editor craft has at least one RCS thruster module.
    /// </summary>
    public static bool IsRCSAvailableEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;

        foreach (var p in ship.parts)
        {
            if (p.FindModuleImplementing<ModuleRCS>() != null ||
                p.FindModuleImplementing<ModuleRCSFX>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
