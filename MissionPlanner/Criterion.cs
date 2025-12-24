namespace MissionPlanner
{
    public enum DestinationType
    {
        Asteroid,
        Body,
        Vessel
    }

    public enum PartGroup
    {
        Batteries,
        Communication,
        ControlSource,
        DockingPort,
        Drills,
        Engines,
        FuelCells,
        Generators,
        Lights,
        Parachutes,
        Radiators,
        RCS,
        ReactionWheels,
        SolarPanels
    }

    public enum CriterionType
    {
        ChargeRateTotal,
        ChecklistItem,
        CrewMemberTrait,
        CrewCount,
        Destination,
        Flags,
        Maneuver,
        Module,
        Part,
        PartGroup,
        Resource,
        SAS,
        Staging,
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
        FineTuneClosestApproach,    
        InterceptAsteroid,                 
        InterceptVessel,                 
        MatchPlanesWithVessel,                
        MatchVelocitiesWithVessel,           
        ReturnFromAMoon
    }
}
