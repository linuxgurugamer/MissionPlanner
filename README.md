Mission Planner/Checklist

This mod is a tool to help in planning missions, and then use that plan as a checklist when actually doing the mission.
The planner is presented as an indented list, with three toggle buttons at the left.

A second mode is a simple checklist.  When creating a new mission, there will be a toggle where you can specify a simple checklist, if you select that, then the display will be abbreviated to a simple checklist without any of the extra detail.

The three toggle buttons on each line do the following (labeled at top of list), and have tooltips on each as a reference:

First one is for the user's use.  Let's this be used as a simple checklist.  If it doesn't have any children, the line will 
be marked at fulfilled or not based on the toggle.  

Some items will have a status shown, red means that the criteria for that line has not been met.

The following is the list of entries that can be specified:


	Batteries				Updates status based on whether  specified capacity is available
	Checklist Item			Manual checklist item, no status updates
	Communication			Updates status on whether specified antenna power is available
	Control Source			A checklist item, given it's own name due to a desire to have explicit step for this
	Crew Member Trait		Updates status depending on whether a crew member with the desired trait is 
							on the vessel.  Supports USI and Civilian Population traits
	Crew Count				Checks for minimum crew
	Engines					A checklist item, currently no special checks are implemented
	Flags					Updates if one or more flags have been planted on the selected body.
	Lights					Checks for the required number of spotlights on the vessel
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


Note:  The contents of the file ComboBox2.cs are a derivitive of the combobox from Mechjeb.
		The license for this file is the GPLv3


		The Mechanical Jeb - Pod version 2.0 from MechJeb is ignored, as it's not used anymore