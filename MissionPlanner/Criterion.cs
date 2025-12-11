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
        //Batteries,
        ChargeRateTotal,
        ChecklistItem,
        //Communication,
        ControlSource,
        CrewMemberTrait,
        CrewCount,
        Destination,
#if false
        Destination_asteroid,
        Destination_body,
        Destination_vessel,
#endif
        //DockingPort,
        //Drills,
        //Engines,
        Flags,
        //FuelCells,
        //Generators,
        //Lights,
        Maneuver,
        Module,
        //Number,
        Part,
        PartGroup,
        //Parachutes,
        //Radiators,
        //Range,
        //RCS,
        //ReactionWheels,
        Resource,
        SAS,
        //Sum,
        Staging,
        //SolarPanels,
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
