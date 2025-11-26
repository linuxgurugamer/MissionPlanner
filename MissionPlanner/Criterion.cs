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
        Launch,     // ap/pe
        Orbit,      // ap/pe
        Reentry,
        Landing,   
        ResourceTransfer,
        Splashdown,                 
        ImpactAsteroid,                     
        TransferToAnotherPlanet,    
        ChangeApoapsis,
        ChangeBothPeAndAp,
        ChangeInclination,
        ChangePeriapsis,
        ChangeSemiMajorAxis,
        FineTuneClosestApproachToVessel,    
        InterceptAsteroid,                 
        InterceptVessel,                 
        MatchPlanesWithVessel,                
        MatchVelocitiesWithVessel,           
        ReturnFromAMoon
    }
}
