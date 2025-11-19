#if false
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using static ModuleParachute;
//using KIS;
//using RealChute;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{

    public class ChecklistSystem
    {
        static public Checklist ActiveChecklist
        {
            get
            {
                if (activeChecklist == null)
                {
                    activeChecklist = new Checklist();
                }
                return activeChecklist;
            }
            //set { activeChecklist = value; }
        }


        // static public Checklist parsedChecklist = new Checklist();

        static Checklist activeChecklist;
        public Rect paramsWindow = new Rect(0, 0, 200, 0);
        public List<Part> partsToCheck;

        //GUIStyles
        public static GUIStyle normalLabel = new GUIStyle(HighLogic.Skin.label);
        public static GUIStyle orangeLabel = new GUIStyle(HighLogic.Skin.label) { normal = { textColor = new Color(1f, 0.5f, 0.2f) } };
        public static GUIStyle centredLabel = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleCenter };

#if false
        public bool LoadChecklists()
        {
            Log.Info("LoadChecklists");
            try
            {
                if (Checklist.cfgLoaded)
                {
                    ActiveChecklist.Clear();
                    ConfigNode node = Checklist.cfg.GetNode("MISSIONPLANNER");

                    ///Begining item cycle
                    foreach (ConfigNode itemNode in node.GetNodes("CHECKLIST_ITEM"))
                    {
                        Log.Info("parsing item " + itemNode.GetValue("name"));

                        ChecklistItem parsedItem = new ChecklistItem();
                        parsedItem.criteria = new List<Criterion>();

                        // the id is the same as the name, unless specified differently
                        // name is used for display, id corresponds to 
                        parsedItem.id = parsedItem.name = itemNode.GetValue("name");
                        itemNode.TryGetValue("id", ref parsedItem.id);

                        //if (!bool.TryParse(itemNode.GetValue("isManual"), out parsedItem.isManual))
                        //    parsedItem.isManual = false;
                        //if (!bool.TryParse(itemNode.GetValue("allRequired"), out parsedItem.allRequired))
                        //    parsedItem.allRequired = true; ;

                        //Beginning criterion cycle
                        Log.Info("Beginning criterion cycle");
                        foreach (ConfigNode criterionNode in itemNode.GetNodes("CRITERION"))
                        {
                            Criterion parsedCriterion = new Criterion(criterionNode);
                            if (parsedCriterion.valid)
                            {
                                parsedItem.criteria.Add(parsedCriterion);
                            }
                        }
                        //if (parsedItem.criteria.Count() > 0 || parsedItem.isManual)
                        ActiveChecklist.items.Add(parsedItem);
                    }

                }
                return true;
            }

            catch
            {
                Log.Warning("Error loading checklist. Please, check your cfg file.");
                return false;
            }
        }

        public void CheckVessel()
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR)
                CheckActiveVessel(FlightGlobals.ActiveVessel);
            else
                CheckVessel(EditorLogic.fetch.ship);
        }


        public void CheckVessel(ShipConstruct ship)
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR)
                return;

#if false
            if (EditorLogic.RootPart == null || (MainInstance.partSelection == null && MainInstance.checkSelected))
            {
                if (ActiveChecklist.items != null)
                    ActiveChecklist.items.ForEach(i => i.state = false);
                return;
            }

            if (MainInstance.checkSelected && MainInstance.partSelection != null && MainInstance.partSelection.selectedParts != null)
                partsToCheck = MainInstance.partSelection.selectedParts.Intersect(ship.Parts).ToList();
            else
#endif
            partsToCheck = ship.Parts;

#warning need to update this entire file to use the data stored in the Step list
            for (int j = 0; j < activeChecklist.items.Count; j++)
            {
                ChecklistItem item = activeChecklist.items[j];
                if (item.isManual)
                    continue;

                item.state = true;
                for (int i = 0; i < item.criteria.Count; i++)
                {
                    Criterion crton = item.criteria[i];
                    switch (crton.type)
                    {
                        case CriterionType.Module:
                            crton.met = CheckForModules(crton);
                            break;
                        case CriterionType.part:
                            crton.met = CheckForParts(crton);
                            break;
                        case CriterionType.resource:
                            crton.met = CheckForResourceLevel(crton);
                            crton.met = CheckForResourceCapacity(crton);
                            break;
                        case CriterionType.CrewMemberTrait:
                            crton.met = CheckForCrewMember(crton);
                            break;
                    }
                    item.criteria[i] = crton;
                }
                if (!item.allRequired)
                {
                    if (item.criteria.TrueForAll(c => !c.met))
                        item.state = false;
                }
                else if (item.criteria.Any(c => !c.met))
                    item.state = false;

                activeChecklist.items[j] = item;
                continue;
            }
        }

        public void CheckActiveVessel(Vessel ship)
        {
            Log.Info("CheckActiveVessel");

#if false
            if (MainInstance.checkSelected && MainInstance.partSelection != null)
                partsToCheck = MainInstance.partSelection.selectedParts.Intersect(ship.Parts).ToList();
            else
#endif
            partsToCheck = ship.Parts;

            for (int j = 0; j < activeChecklist.items.Count; j++)
            {
                ChecklistItem item = activeChecklist.items[j];
                if (item.isManual)
                    continue;

                item.state = true;
                for (int i = 0; i < item.criteria.Count; i++)
                {
                    Criterion crton = item.criteria[i];
                    switch (crton.type)
                    {
                        case CriterionType.Module:
                            crton.met = CheckForModules(crton);
                            break;
                        case CriterionType.part:
                            crton.met = CheckForParts(crton);
                            break;
                        case CriterionType.resource:
                            crton.met = CheckForResourceLevel(crton);
                            crton.met = CheckForResourceCapacity(crton);
                            break;
                        case CriterionType.CrewMemberTrait:
                            crton.met = CheckForCrewMember(crton);
                            break;
                    }
                    item.criteria[i] = crton;
                }
                if (!item.allRequired)
                {
                    if (item.criteria.TrueForAll(c => !c.met))
                        item.state = false;
                }
                else if (item.criteria.Any(c => !c.met))
                    item.state = false;

                activeChecklist.items[j] = item;
                continue;
            }
        }


#if false
        bool CheckForKISModules(Criterion crton)
        {
            Log.Info("CheckForKISModules");

            int quantity = 0;
            foreach (string module in crton.modules)
            {
                foreach (var p in partsToCheck.Where(p => p.Modules.Contains("ModuleKISInventory")))
                {
                    var inv = p.FindModuleImplementing<ModuleKISInventory>();

                    foreach (var i in inv.items)
                    {
                        if (i.Value.equippedPart.Modules.Contains(module))
                            quantity++;
                    }

                }
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        int checkForRealChutes(Part p1)
        {
            int quantity = 0;
            foreach (RealChuteModule m in p1.Modules.GetModules<RealChuteModule>())
            {
                foreach (var par in m.parachutes)
                    if (par.DeploymentState == DeploymentStates.STOWED)
                        quantity++;
            }
            Log.Info("RealChuteModule: " + quantity.ToString());
            return quantity;
        }
#endif

        int CheckForParachutes(string module)
        {
            Log.Info("CheckForParachutes");
            int quantity = 0;
            if (partsToCheck.Where(p => p.Modules.Contains(module)).Count() > 0)
            {
                foreach (Part p1 in partsToCheck.Where(p => p.Modules.Contains(module)))
                {
                    Log.Info("part: " + p1.partInfo.name);
                    switch (module)
                    {
                        case "ModuleParachute":
                            foreach (ModuleParachute m in p1.Modules.GetModules<ModuleParachute>())
                            {
                                if (m.deploymentState == ModuleParachute.deploymentStates.STOWED)
                                    quantity++;
                            }
                            Log.Info("ModuleParachute: " + quantity.ToString());
                            break;
#if false
                        case "RealChuteModule":
                            if (WernherChecker.hasMod("RealChute"))
                                quantity += checkForRealChutes(p1);

                            break;
                        case "RealChuteFAR":
                            break;
#endif
                    }


                }
            }
            Log.Info("CheckForParachutes, module: " + module + "   quantity: " + quantity.ToString());
            return quantity;
        }
        bool CheckForModules(Criterion crton)
        {
            Log.Info("CheckForModules");

#if false
            if (crton.reqModName == "KIS")
            {
                if (!InstallChecker.KISisOK)
                    return false;
                return CheckForKISModules(crton);
            }
#endif
            int quantity = 0;
            foreach (string module in crton.modules)
            {
                Log.Info("module: " + module);
                if (module == "ModuleParachute" || module == "RealChuteModule" || module == "RealChuteFAR")
                    quantity += CheckForParachutes(module);
                else
                    quantity += partsToCheck.Where(p => p.Modules.Contains(module)).Count();
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForParts(Criterion crton)
        {
            int quantity = 0;
            foreach (string part in crton.parts)
            {
                quantity += partsToCheck.Where(p => p.name == part).Count();
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForResourceLevel(Criterion crton)
        {
            double quantity = 0;
            foreach (Part part in partsToCheck.Where(p => p.Resources.Contains(crton.resourceName)))
            {
                quantity += part.Resources.Get(crton.resourceName).amount;
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForResourceCapacity(Criterion crton)
        {
            double quantity = 0;
            foreach (Part part in partsToCheck.Where(p => p.Resources.Contains(crton.resourceName)))
            {
                quantity += part.Resources.Get(crton.resourceName).maxAmount;
            }
            if (quantity >= int.Parse(crton.parameter.ToString()))
                return true;

            return false;
        }

        bool CheckForCrewMember(Criterion crton)
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR)
            {
                foreach (Part p in FlightGlobals.ActiveVessel.parts)
                {
                    if (p.protoModuleCrew.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                if (KSP.UI.CrewAssignmentDialog.Instance == null || KSP.UI.CrewAssignmentDialog.Instance.GetManifest() == null)
                    return false;

                try
                {
                    foreach (PartCrewManifest part in KSP.UI.CrewAssignmentDialog.Instance.GetManifest().GetCrewableParts().Where(p => partsToCheck.Exists(pt => pt.partInfo == p.PartInfo)))
                    {
                        if (part.GetPartCrew().Where(c => c != null).Any(c => c.experienceTrait.Title == crton.experienceTrait && c.experienceLevel >= int.Parse(crton.parameter.ToString())))
                        {
                            //Log.Info("Crew OK");
                            return true;
                        }
                    }
                    //Log.Info("Crew KO");
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Warning("Error checking crew:\n" + ex + "\n\n<b><color=lime>Please note, that this can sometimes happen after entering the editor and attaching the part for the first time.</color> <color=#ff4444ff>If this is not the case, please, report it.</color></b>");
                    return false;
                }
            }
        }

        public void DrawParamsWindow(int WindowID)
        {
            ChecklistItem item = activeChecklist.items.Find(p => p.paramsDisplayed);
            GUILayout.BeginVertical(ScaledGUILayoutWidth(200));
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            GUILayout.Label("Item: " + item.name + "\n<color=#ffd333ff>" + (item.allRequired ? "All criteria met required" : "One criterion met suffices") + "</color>");
            GUILayout.FlexibleSpace();
            GUILayout.Label("►");
            GUILayout.EndHorizontal();
            item.criteria.ForEach(c => c.tempParam = c.paramsGUIFunction(c));

            if (item.criteria.TrueForAll(c => c.paramValid))
            {
                if (GUILayout.Button("Done", HighLogic.Skin.button))
                {
                    item.paramsDisplayed = false;
                    item.criteria.ForEach(c => c.parameter = c.tempParam);
                    CheckVessel();
                }
            }
            else
            {
                GUILayout.Label("<color=#FF1111FF><b> ! Some paramaters are invalid !</b></color>", centredLabel);
            }
            activeChecklist.items[activeChecklist.items.IndexOf(item)] = item;
            GUILayout.EndVertical();
            //MainInstance.SetTooltipText();
        }

        public static object ParamsTextField(Criterion crton)
        {
            int i;
            if (int.TryParse(crton.tempParam.ToString(), out i))
                crton.paramValid = true;
            else
                crton.paramValid = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(crton.valuesShortened + " " + crton.measure, crton.tooltip), crton.paramValid ? normalLabel : orangeLabel);
            GUILayout.FlexibleSpace();
            crton.tempParam = GUILayout.TextField(crton.tempParam.ToString(), 11, HighLogic.Skin.textField, ScaledGUILayoutWidth(68f));
            GUILayout.EndHorizontal();

            return crton.tempParam;
        }

        /*public static object ParamsContractSelect(Criterion crton)
        {
            if (Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().Count() == 0)
                return null;

            GUILayout.Label(crton.type.ToString(), normalLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().First<Contracts.Contract>().Title, HighLogic.Skin.button))
            {
                //GUILayout.BeginArea(paramInspector, "Select Parameter", HighLogic.Skin.window);
                foreach (Contracts.Contract contract in Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract>().Where(c => c.AllParameters.Any(p => p.GetType() == typeof(Contracts.Parameters.PartTest) || p.GetType() == typeof(FinePrint.Contracts.Parameters.CrewCapacityParameter) || p.GetType() == typeof(FinePrint.Contracts.Parameters.PartRequestParameter))))
                    Log.Info(contract.Title);
            }
            return crton.tempParam;
        }*/
#endif
    }
}
#endif