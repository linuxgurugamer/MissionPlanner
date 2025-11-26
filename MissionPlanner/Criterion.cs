namespace MissionPlanner
{
    public enum CriterionType
    {
        Batteries,
        ChargeRateTotal,
        ChecklistItem,
        Communication,
        ControlSource,
        CrewMemberTrait,
        CrewCount,
        Destination_asteroid,
        Destination_body,
        Destination_vessel,
        DockingPort,
        Drills,
        Engines,
        Flags,
        FuelCells,
        Generators,
        Lights,
        Maneuver,
        Module,
        Number,
        Part,
        Parachutes,
        Radiators,
        Range,
        RCS,
        ReactionWheels,
        Resource,
        SAS,
        SolarPanels,
        TrackedVessel,
        VABOrganizerCategory
    }

    public enum Maneuver
    {
        None,
        Launch,
        Reentry,
        Landing,                    // planet
        Splashdown,                 // planet
        ImpactAsteroid,                     // asteroid or planet
        TransferToAnotherPlanet,    // planet
        ChangeApoapsis,
        ChangeBothPeAndAp,
        ChangeInclination,
        ChangePeriapsis,
        ChangeSemiMajorAxis,
        FineTuneClosestApproachToVessel,    // vessel
        InterceptVessel,                  // vessel
        MatchPlanesWithVessel,                // vessel
        MatchVelocitiesWithVessel,            // vessel
        ReturnFromAMoon
    }
}
