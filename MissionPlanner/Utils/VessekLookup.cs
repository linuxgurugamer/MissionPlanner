using System;
using System.Linq;
using UnityEngine;
using KSP;

public static class VesselLookup
{
    /// <summary>
    /// Returns the Vessel with a specified persistentId and outputs the vessel's Guid.
    /// Works for loaded and unloaded vessels.
    /// </summary>
    public static Guid GetVesselGuidByPersistentId(ulong pid)
    {
        var vesselGuid = Guid.Empty;

        // 1. Check loaded vessels
        foreach (var v in FlightGlobals.Vessels)
        {
            if (v != null && v.persistentId == pid)
            {
                vesselGuid = v.id;
                return vesselGuid;
            }
        }

        // 2. Check proto vessels (unloaded)
        var game = HighLogic.CurrentGame;
        var protoList = game?.flightState?.protoVessels;

        if (protoList != null)
        {
            foreach (var pv in protoList)
            {
                if (pv != null && pv.persistentId == pid)
                {
                    vesselGuid = pv.vesselID;
                    return vesselGuid;
                }
            }
        }

        // Not found anywhere
        return vesselGuid;
    }
}
