using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionPlanner
{
    public enum CriterionType
    {
        none,
        Module,
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
        Flags, // bool or cnt only
        Dest_vessel,
        Dest_body,
        Dest_asteroid
    }

    public class Criterion
    {


        public CriterionType type;
        public bool met;
        public Func<Criterion, object> paramsGUIFunction;
        public object parameter;
        public object tempParam;
        public Type parameterType;
        public bool hasParameter;
        public string valuesShortened;
        public string measure;
        public bool paramValid;
        public string tooltip;
        public string valuesFull;
        public string resourceName;
        public List<string> parts;
        public List<string> modules;
        public string experienceTrait;

        public string reqModName;
        public bool valid;

        public bool toggleValue;
        public int number; // also used for crewCount, and resource amount
        public int range_lower;
        public int range_upper;
        public string partId;
        
        public Criterion ShallowClone()
        {
            return (Criterion)this.MemberwiseClone();
        }

#warning need to update this entire file to use the data stored in the Step list

        public Criterion(ConfigNode node)
        {
            this.type = (CriterionType)Enum.Parse(typeof(CriterionType), node.GetValue("type"));
            if (type == CriterionType.part || type == CriterionType.Module || type == CriterionType.resource || type == CriterionType.CrewMemberTrait)
            {
                paramsGUIFunction = ChecklistSystem.ParamsTextField;
                parameter = 1;
                tempParam = 1;
                parameterType = typeof(int);
                hasParameter = true;
            }

            if (node.HasValue("defaultParameter"))
            {
                int i;
                if (int.TryParse(node.GetValue("defaultParameter"), out i))
                {
                    this.parameter = i;
                    this.tempParam = this.parameter;
                }
            }
#if false
            if (node.HasValue("requiredMod"))
            {
                Log.Info("requiredMod found");
                reqModName = node.GetValue("requiredMod");
                Log.Info("reqModName: " + reqModName);
                if (!WernherChecker.hasMod(reqModName))
                {
                    Log.Info("required mod not found");
                    valid = false;
                    return;
                }

            }
            else
#endif
            reqModName = null;
            valid = true;
            
            switch (this.type)
            {
                case CriterionType.toggle:
                case CriterionType.number:
                case CriterionType.range:
                case CriterionType.crewCount:
                    break;

                case CriterionType.Module:
                    this.modules = node.GetValue("modules").Trim().Split(',').ToList<string>();
                    this.measure = "QTY";
                    this.valuesFull = string.Join(", ", this.modules.ToArray());
                    this.valuesShortened = this.modules.First() + (this.modules.Count == 1 ? string.Empty : ",...");
                    this.tooltip = "How many of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
                    break;

                case CriterionType.part:
                    this.parts = node.GetValue("parts").Trim().Split(',').ToList<string>();
                    this.measure = "QTY";
                    this.valuesFull = string.Join(", ", this.parts.ToArray());
                    this.valuesShortened = this.parts.First() + (this.parts.Count == 1 ? string.Empty : ",...");
                    this.tooltip = "How many of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
                    break;

                case CriterionType.resource: // min resource level:
                    this.resourceName = node.GetValue("resourceName");
                    this.measure = "AMT";
                    this.valuesFull = this.resourceName;
                    this.valuesShortened = this.resourceName;
                    this.tooltip = "How much of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel contain";
             
                //case CriterionType.MinResourceCapacity:
                    this.resourceName = node.GetValue("resourceName");
                    this.measure = "CAPY";
                    this.valuesFull = this.resourceName;
                    this.valuesShortened = this.resourceName;
                    this.tooltip = "How much of <b><color=#90FF3E>" + this.valuesFull + "</color></b> should your vessel has capacity for";
                    break;
                case CriterionType.CrewMemberTrait:
                    this.experienceTrait = node.GetValue("experienceTrait");
                    this.measure = "LVL";
                    this.valuesFull = this.experienceTrait;
                    this.valuesShortened = this.experienceTrait;
                    this.tooltip = "Minimum experience level of your <b><color=#90FF3E>" + this.valuesFull + "</color></b>";
                    break;
            }
        }

        public Criterion(CriterionType type)
        {
            this.type = type;
            if (type == CriterionType.part || type == CriterionType.Module || type == CriterionType.resource|| type == CriterionType.CrewMemberTrait)
            {
                paramsGUIFunction = ChecklistSystem.ParamsTextField;
                parameter = 1;
                tempParam = 1;
                parameterType = typeof(int);
                hasParameter = true;
            }
        } 
    }
}
