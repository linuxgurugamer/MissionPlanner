using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class CrewUtils
{
    /// <summary>
    /// Returns true if any Kerbal on the vessel has the specified trait
    /// (e.g., "Pilot", "Engineer", "Scientist").
    /// </summary>
    public static bool VesselHasTrait(Vessel vessel, string traitName)
    {
        if (vessel == null)
            return false;

        List<ProtoCrewMember> crew = vessel.GetVesselCrew();
        if (crew == null || crew.Count == 0)
            return false;

        return crew.Any(c =>
            c.experienceTrait != null &&
            c.experienceTrait.TypeName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Returns all Kerbals on the vessel who match the specified trait.
    /// </summary>
    public static ProtoCrewMember[] GetCrewWithTrait(Vessel vessel, string traitName)
    {
        if (vessel == null)
            return new ProtoCrewMember[0];

        List<ProtoCrewMember> crew = vessel.GetVesselCrew();
        if (crew == null || crew.Count == 0)
            return new ProtoCrewMember[0];

        return crew
            .Where(c => c.experienceTrait != null &&
                        c.experienceTrait.TypeName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
