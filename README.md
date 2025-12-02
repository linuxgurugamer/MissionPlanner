Mission Planner/Checklist

This mod is a tool to help in planning missions, and then use that plan as a checklist when actually doing the mission.
The planner is presented as an indented list, with three toggle buttons at the left.

A second mode is a simple checklist.  When creating a new mission, there will be a toggle where you can specify a simple checklist, if you select that, then the display will be abbreviated to a simple checklist without any of the extra detail.

The three toggle buttons on each line do the following (labeled at top of list), and have tooltips on each as a reference:

First one is for the user's use.  Let's this be used as a simple checklist.  If it doesn't have any children, the line will 
be marked at fulfilled or not based on the toggle.  

Some items will have a status shown, red means that the criteria for that line has not been met.

The following is the list of entries that can be specified  Most of them have active checks, meaning that checks are done during flight to verify each entry against the current vessel :


	Batteries				Updates status based on whether  specified capacity is available
	Checklist Item			Manual checklist item, no status updates
	Communication			Updates status on whether specified antenna power is available
	Control Source			Checks for parts with ModuleCommand
	Crew Member Trait		Updates status depending on whether a crew member with the desired trait is 
							on the vessel.  Supports USI and Civilian Population traits
	Crew Count				Checks for minimum crew
	Docking Ports			Checks for docking ports
	Engines					Has specific checks for TWR, dV and resources used by the engine
	Flags					Updates if one or more flags have been planted on the selected body.
	Lights					Checks for the required number of spotlights on the vessel
	Maneuver				Manual checklist item for various maneuvers.  Some of the maneuvers have additional fields:
							The following two maneuvers have orbital parameters and have active check for the Ap and Pe:
								Launch		
								Orbit
							The following maneuvers have fields to record the target of the maneuver.  These are not
							active checks (ie:  no flight checks are done):
								ImpactAsteroid
								InterceptAsteroid
								FineTuneClosestApproachToVessel
								InterceptVessel
								MatchPlanesWithVessel
								MatchVelocitiesWithVessel
								Landing
								Splashdown
								TransferToAnotherPlanet
	Module					Checks for a specified part module on the vessel
	Number					A checklist item, given it's own name due to a desire to have explicit step for this
	Part					Checks for a specified part on the vessel
	Parachutes				Checks for a specified number of parachutes; supports Real Chutes
	RCS						Checks that RCS is available.
	Radiators				Checks that the specified cooling rate is available via radiators
	Range					A checklist item, given it's own name due to a desire to have explicit step for this
	ReactionWheels			Checks that the specified number of reaction wheels are available
	Resource				Checks that both a minimum capacity and minimum amount is on the vessel
	SAS						Checks that the requested SAS mode is available, this takes into account crew abilities
	TrackedVessel			Tracks a vessel.  See below for details
	VABOrganizer Category	Supports the VABOrganizer mod.  If installed, will check to see if a part from that 
							category is on the vessel

		The following three criteria each specify a destination.  The asteroid and body destinations has an 
		optional landing, the vessel destination has an optional docking.
		Updates whether the destination has been visited (and landed upon/docked)

	Destination Asteroid		
	Destination Body
	Destination Vessel

		The following three criteria consist of those parts that generate EC (stock only)

	Fuel Cells				Updates if there is a minimum charge rate supplied by fuel cells
	Generarators			Checks that the minimum charge rate is available from Generators (RTGs, etc).  
							Supports Near Future Electrical (ModuleSystemHeatFissionReactor)
	SolarPanels				Checks that the minimum charge rate is available from Solar Panels
	ChargeRateTotal			Checks that there is a minimum charge rate from Fuel Cells, Generators & Solar Panels, 


Checklist Items

	Checklist items are there for use as a manual checklist


Controls available on the main window

	Add Objective/goal (button)		Adds a new top-level entry 
	Expand All (button)				Expands all steps with children
	Collapse All (button)			Collapses all steps (hides all children)
	View (dropdown combo box)		Changes the view to one of:
										Tiny		Hides all controls on the items. List items are not clickable
										Compact		Enables double-clicking on the step to allow editing of the step
										Full		Full control, allows new steps to be added, steps to be moved
	Show Summary (toggle)			Shows the mission summary
	Show Detail (toggle)			Shows an additional line with the criterion type and optional parameters
	Use KSP Skin (toggle)

	New (button)					Creates a new mission
	Clear All (button)				Clears all the steps, but keeps the mission info
	Save (button)					Save to existing file
	Save As... (button)				Save to new file
	Load/Import... (button)			Load or import a mission.  Imports get added to the end
	Close (button)

Save As… prompts for a new mission name; overwrite/auto-increment lives behind Save As (plain Save overwrites silently)

Crew-count validator button in Details (when in flight)

Fixed-width Title field in Details; tighter Type selector arrows

Column width tuning sliders + persist UI (positions, skin, tuning)

Auto-save on details close/switch + save indicator (red on error)

Load dialog with delete, show-all toggle

Toolbar button; hides on pause; drag from anywhere (DragWindow at end)


Note regarding docking ports:
	Most known docking ports are recognized.  Specifically, the following part modules are recognized as being docking ports:

			Mod								Part Modules
            Stock							ModuleDockingNode
            USI Konstruction				ModuleWeldablePort
            KAS								ModuleKASPort, ModuleKASJointDock
            SSTU							SSTUDockingPort, SSTUAnimateControlledDockingNode
            Tundra Exploration				ModuleGimbalDockingPort
            B9-style						ModuleDockingNodeHinge
            Kerbal Reusability Expansion	ModuleKREDockingPort

Note regarding Autosaving:
	There is an option to autosave after every edit, it defaults to on, but you can disable that in the settings so it only saves when you click the Save buttons

Several sample mission plans have been provided for your use and to provide some guidance how to use the mod:

	Create Orbital Fueling Station	This is a checklist used to design and build an orbital fueling station.  
									It includes sections for building the station, building the launch vehicle, 
									launching and resupply

	Land on the Mun					This checklist details everything needed to land on the Mun

	Grand Tour						This checklist will help  you do a grand tour of the system

Note:  The contents of the file ComboBox2.cs are a derivitive of the combobox from Mechjeb.
		The license for this file is the GPLv3

Note:  The Mechanical Jeb - Pod version 2.0 from MechJeb is ignored, as it's not used anymore


Delta V & Planet Packs
	The following planet packs are recognized and have delta V tables :

		Stock
		JNSQ
		GPP

		Promised Worlds is idential to Stock, so if Promised Worlds is detected, it will use Stock instead

	Additional tables are welcome, can be made using the provided editor

Delta-V Editor
	The supplied DeltaV charts are using values from the available Delta-V graphs, showing the 
	needed delta-V from Kerbal to all bodies.  Bodies which are moons of other bodies (ie:  Bop is
	a moon of Jool) show the info starting from the point of being captured in the parent body's 
	SOI

	After entering the editor, you will be able to load any existing delta-v tables.

	There is a toggle called "Use Loaded Planet Pack", which if selected will use the currently loaded bodies
	to set the isMoon and parent values.

	The editor has the ability to generate initial line entries for whatever planet-pack is loaded.  
	There are three options:

		Homeworld to all	Generate initial lines for each body in the system, starting at the 
							homeworld.  They will be filled with default values of -1, just fill 
							in the values that you ant		
		All to all			Generate initial lines going from each body to every other body
		All to Homeworld	Generate initial  lines for each body in the system going to the homeworld,
							starting at the body.

	These options are additive, but will only add a line if the corresponding line isn't there.

	Each lines has entry fields for the following:

		Origin						Starting body
		Destination					Destination body
		dV_to_low_orbit				Delta V to low orbit
		injection_dV				Delta V for injection maneuver from low orbit 
		capture_dV					Dv needed to capture in SOI of destination
		transfer_to_low_orbit_dV	Dv needed to transfer to low orbit
		total_capture_dV			This is a total of the capture_dV and transfer_to_low_orbit_dV
		dV_low_orbit_to_surface		Dv needed to land on the planet.  For planets with an atmosphere, 
									will be what's needed to low down for reentry
		ascent_dV					Dv needed to launch from the planet to low orbit
		plane_change_dV				Max Dv needed for any plane change needed
		isMoon						Is this body a moon of another body
		parent						If a moon, the parent of the body

	When using the Populate buttons to fill the initial data, the Origin, Destination, isMoon and 
	parent fields are populated, all the other fields will be initialized with values of -1.

	The editor can be enabled by going into the stock settings and enabling the editor option
	in the Mission Planner pane

	There will be the following controls on each line in the editor:  
	
		Del							Delete the line
		Dup							Duplicate the line

	The following columns can be sorted by clicking on the button at the top of the column:

		Origin
		Destination
		Parent
