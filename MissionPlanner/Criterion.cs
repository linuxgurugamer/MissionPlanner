using System;
using System.Collections.Generic;
using System.Linq;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public enum CriterionType
    {
#if false
        ChecklistItem,
        trackedVessel,
        Module,
        VABOrganizerSubcategory,
        CrewMemberTrait,

        toggle,
        number,
        range,
        crewCount,
        part,
        resource,

        SAS,
        RCS,
        Batteries,
        Communication,
        SolarPanels,
        FuelCells,
        Radiators,
        Lights,
        Parachutes,
        ControlSource,
        ReactionWheels,
        Engines,
        Flags, 
        Dest_vessel,
        Dest_body,
        Dest_asteroid
#else
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
        Engines,
        Flags,
        FuelCells, 
        Generators, 
        Lights,
        Module,
        Number,
        Part,
        Parachutes,
        RCS,
        Radiators,
        Range,
        ReactionWheels,
        Resource,
        SAS,
        SolarPanels, 
        TrackedVessel,
        VABOrganizerCategory

#endif
    }
}
