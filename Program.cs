using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

/* TODO:
 * - welders policy is to aggressive, needs a lot of refinement
 * - remove magnetic field from connectors of machine and bridge
 * - rotor should not spin if in presence of rails
 * 
 */


namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const Single rotorShaftDefaultDrillingSpeed = 2.0f;
        private const Single rotorShaftStoppingSpeed = 2.0f;
        private const Single minPistonDistance = 1.05f;
        private const Single maxPistonDistance = 9.85f;
        private const Single pistonDefaultDrillingSpeed = 0.015f;
        private const Single pistonRetractSpeed = 0.25f;

        private List<IMyTerminalBlock> allBlocks;

        string weldersGroupName = "Autominer - Welders";
        private IMyBlockGroup weldersGroup;
        private List<IMyShipWelder> weldersBlocs;

        string drillsGroupName = "Autominer - Drills";
        private IMyBlockGroup drillsGroup;
        private List<IMyShipDrill> drillBlocks;

        string grindersGroupName = "Autominer - Grinders";
        private IMyBlockGroup grindersGroup;
        private List<IMyShipGrinder> grinderBlocks;

        string pistonsGroupName = "Autominer - Pistons";
        private IMyBlockGroup pistonsGroup;
        private List<IMyPistonBase> pistonBlocks;

        string rotorDrillShaftName = "Autominer - Advanced Rotor Drill";
        private IMyMotorAdvancedStator rotorDrillShaft;

        string mbBackName = "Autominer - MB back";
        private IMyShipMergeBlock mbBack;

        string mbFrontName = "Autominer - MB front";
        private IMyShipMergeBlock mbFront;

        string connectorBackName = "Autominer - Connector back";
        private IMyShipConnector connectorBack;

        string connectorFrontName = "Autominer - Connector front";
        private IMyShipConnector connectorFront;

        string mainProjectorName = "Autominer - Projector";
        private IMyProjector mainProjector;

        string rigInteriorLightsGroupName = "Autominer - Interior lights";
        private IMyBlockGroup rigInteriorLightsGroup;
        private List<IMyInteriorLight> rigInteriorLights;

        private List<bool> connectorsStatus;

        private Dictionary<string, Color> colors = new Dictionary<string, Color>();
    


        private Status status;

        //IMyShipDrill : IMyFunctionalBlock, IMyTerminalBlock, IMyCubeBlock, IMyEntity

        MyCommandLine _commandLine = new MyCommandLine();
        Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);


        public Program()
        {
            // Associate the methods with the commands
            _commands["automine"] = Automine;
            _commands["stop"] = Stop;
            _commands["rename"] = Rename;
            _commands["extend"] = Extend;
            _commands["retract"] = Retract;
            _commands["park"] = Park;
            _commands["abort"] = Abort;
            _commands["set"] = Set;



            allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(allBlocks);

            // welders
            weldersGroup = GridTerminalSystem.GetBlockGroupWithName(weldersGroupName);
            weldersBlocs = new List<IMyShipWelder>();
            weldersGroup.GetBlocksOfType<IMyShipWelder>(weldersBlocs);

            // grindrs
            grindersGroup = GridTerminalSystem.GetBlockGroupWithName(grindersGroupName);
            grinderBlocks = new List<IMyShipGrinder>();
            grindersGroup.GetBlocksOfType<IMyShipGrinder>(grinderBlocks);

            // drills
            drillsGroup = GridTerminalSystem.GetBlockGroupWithName(drillsGroupName);
            drillBlocks = new List<IMyShipDrill>();
            drillsGroup.GetBlocksOfType<IMyShipDrill>(drillBlocks);


            // pistons
            pistonsGroup = GridTerminalSystem.GetBlockGroupWithName(pistonsGroupName);
            pistonBlocks = new List<IMyPistonBase>();
            pistonsGroup.GetBlocksOfType<IMyPistonBase>(pistonBlocks);

            rotorDrillShaft = GridTerminalSystem.GetBlockWithName(rotorDrillShaftName) as IMyMotorAdvancedStator;
            mbBack = GridTerminalSystem.GetBlockWithName(mbBackName) as IMyShipMergeBlock;
            
            mbFront = GridTerminalSystem.GetBlockWithName(mbFrontName) as IMyShipMergeBlock;
            connectorBack = GridTerminalSystem.GetBlockWithName(connectorBackName) as IMyShipConnector;
            connectorFront = GridTerminalSystem.GetBlockWithName(connectorFrontName) as IMyShipConnector;
            mainProjector = GridTerminalSystem.GetBlockWithName(mainProjectorName) as IMyProjector;

            rigInteriorLightsGroup = GridTerminalSystem.GetBlockGroupWithName(rigInteriorLightsGroupName);
            rigInteriorLights = new List<IMyInteriorLight>();
            rigInteriorLightsGroup.GetBlocksOfType<IMyInteriorLight>(rigInteriorLights);

            if (!String.IsNullOrEmpty(Storage))
            {
                status = new Status(Storage);
            }
            else
            {
                status = new Status();
                status.DrillingRotorSpeed = rotorShaftDefaultDrillingSpeed;
                status.PistonsDrillingSpeed = pistonDefaultDrillingSpeed;
            }
            connectorsStatus = new List<bool> { false, false, false, false };

            colors["automine"] = new Color(252, 114, 0);
            colors["standby"] = new Color(255, 251, 247);
            colors["emergency"] = new Color(255, 8, 0);

            // set the frequency update
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            //IMyShipController.MoveIndicator
            Echo("Compilation successful");
            Echo($"connector back status = {connectorBack.Status}");


        }

        public void Save()
        {
            Storage = status.Save();
        }

        public void Main(string argument, UpdateType updateType)
        {
            // If the update source is from a trigger or a terminal,
            // this is an interactive command.
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                if (_commandLine.TryParse(argument))
                {
                    Action commandAction;

                    // Retrieve the first argument. Switches are ignored.
                    string command = _commandLine.Argument(0);

                    // Now we must validate that the first argument is actually specified, 
                    // then attempt to find the matching command delegate.
                    if (command == null)
                    {
                        Echo("No command specified");
                    }
                    else if (_commands.TryGetValue(command, out commandAction))
                    {
                        // We have found a command. Invoke it.
                        commandAction();
                    }
                    else
                    {
                        Echo($"Unknown command {command}");
                    }
                }
            }

            // If the update source has this update flag, it means
            // that it's run from the frequency system, and we should
            // update our continuous logic.
            if ((updateType & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) != 0)
            {
                UpdateConnectorStatus();
                UpdateDrill();
                UpdateWeld();
                UpdateGrind();
                UpdatePistons();
                UpdateAutomine();
                UpdateLights();
            }

        }

        public void Rename()
        {
            string myNewPrefix = _commandLine.Argument(1);
            string myRegex = "^" + myNewPrefix + " - *";
            Echo("Renaming blocs");
            foreach (IMyTerminalBlock bloc in allBlocks)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(bloc.CustomName, myRegex))
                {
                    string newName = myNewPrefix + " - " + bloc.CustomName;
                    Echo($"- {bloc.CustomName} -> {newName}");
                    bloc.CustomName = newName;
                }
            }
            return;
        }

        public void Set()
        {
            string property = _commandLine.Argument(1);
            float value = float.Parse(_commandLine.Argument(2));
            switch (property)
            {
                case "pistonspeed":
                    status.PistonsDrillingSpeed = value;
                    break;
                case "rotorspeed":
                    status.DrillingRotorSpeed = value;
                    break;
            }

        }

        public void Extend()
        {
            SetDrillStatus(true, true);
            SetWeldersStatus(true);
            SetGrindsStatus(false);
            SetExtendingStatus(true, false);
            OnOff(mainProjector, true);
            status.AbortRequested = false;
        }

        public void Retract()
        {
            SetDrillStatus(false, false);
            SetWeldersStatus(false);
            SetGrindsStatus(true);
            SetExtendingStatus(false, true);
            OnOff(mainProjector, false);
            status.AbortRequested = false;
        }

        public void Park()
        {
            Automine();
            status.ParkingRequested = true;
            Echo($"ParkingRequested() = {status.ParkingRequested}");
        }

        public void Abort()
        {
            status.IsAutomining = false;
            SetDrillStatus(false, false);
            SetWeldersStatus(false);
            SetGrindsStatus(false);
            SetExtendingStatus(false, false);
            OnOff(mainProjector, false);
            status.AbortRequested = true;
        }

        public bool AreWeldersFree()
        {
            bool sensorStatus = true;
            //foreach (IMySensorBlock mySensor in sensorsOnRotorBlocs)
            //{
            //    if (mySensor.IsActive)
            //    {
            //        sensorStatus = false;
            //    }
            //}
            Echo($"INFO: Rotor is free = {sensorStatus}");
            status.IsWeldingPossible = sensorStatus;
            return sensorStatus;
        }


        public void Automine()
        {
            Echo("Starting auto-mining program");
            status.ParkingRequested = false;
            status.IsAutomining = true;
            status.AbortRequested = false;
            Echo($"PistonsExtended() = {PistonsExtended()}");
            Echo($"PistonRetracted() = {PistonRetracted()}");
        }

        public void Stop()
        {
            Echo("Emergency STOP requested");
            status.IsAutomining = false;
            SetWeldersStatus(false);
            SetDrillStatus(false, false);
            StopPistons();
            SetExtendingStatus(false, false);
        }

        

        public void SetDrillStatus(bool isDrilling, bool isSpinning)
        {
            status.IsDrilling = isDrilling;
            status.IsSpinning = isSpinning;
        }
        public void SetWeldersStatus(bool isActive)
        {
            status.IsWelding = isActive;
        }
        public void SetGrindsStatus(bool isActive)
        {
            status.IsGrinding = isActive;
        }

        public void SetExtendingStatus(bool isExtending, bool isRetracting)
        {
            status.IsExtending = isExtending;
            status.IsRetracting = isRetracting;
        }
        public void UpdateDrill()
        {
            OnOff(drillBlocks, status.IsDrilling);
            switch (status.IsSpinning)
            {
                case false:
                    rotorDrillShaft.LowerLimitDeg = -1f;
                    rotorDrillShaft.UpperLimitDeg = 0f;
                    rotorDrillShaft.SetValue("Velocity", rotorShaftStoppingSpeed);
                    break;
                case true:
                    rotorDrillShaft.LowerLimitDeg = -361f;
                    rotorDrillShaft.UpperLimitDeg = 361f;
                    rotorDrillShaft.SetValue("Velocity", status.DrillingRotorSpeed);
                    break;
            }
        }

        public void UpdateWeld()
        {
            switch (status.IsWelding)
            {
                case false:
                    OnOff(weldersBlocs, false);
                    break;
                case true:
                    AreWeldersFree();
                    if (status.IsWeldingPossible)
                    {
                        OnOff(weldersBlocs, true);
                        return;
                    }
                    else
                    {
                        OnOff(weldersBlocs, false);
                    }
                    break;
            }
        }
        public void UpdateGrind()
        {
            switch (status.IsGrinding)
            {
                case false:
                    OnOff(grinderBlocks, false);
                    break;
                case true:
                    OnOff(grinderBlocks, true);
                    break;
            }
        }
        public void UpdateLights()
        {
            foreach (IMyInteriorLight light in rigInteriorLights)
            {
                if (status.IsAutomining)
                {
                    light.SetValue<Color>("Color", colors["automine"]); // set color    
                }
                else
                {
                    light.SetValue<Color>("Color", colors["standby"]); // set color    
                }
            }
        }

        public bool PistonRetracted()
        {
            List<bool> tmpStatus = new List<bool>{ false, false};
            int c = 0;
            foreach (IMyPistonBase bloc in pistonBlocks)
            {
                if (bloc.CurrentPosition <= (minPistonDistance + 0.005))
                {
                    tmpStatus[c] = true;
                }
                c++;
            }
            return !tmpStatus.Contains(false);
        }

        public bool PistonsExtended()
        {
            List<bool> tmpStatus = new List<bool> { false, false };
            int c = 0;
            foreach (IMyPistonBase bloc in pistonBlocks)
            {
                if (bloc.CurrentPosition >= (maxPistonDistance - 0.005))
                {
                    tmpStatus[c] = true;
                }
                c++;
            }
            return !tmpStatus.Contains(false);
        }

        public void UpdateAutomine()
        {
            if (status.IsAutomining)
            {
                if (PistonsExtended() && status.IsExtending)
                {
                    Retract();
                }
                else if (PistonRetracted() && status.IsRetracting)
                {
                    Extend();
                }
                else if (PistonsExtended() && !status.IsExtending && !status.IsRetracting)
                {
                    Retract();
                }
                else if (PistonRetracted() && !status.IsExtending && !status.IsRetracting)
                {
                    Extend();
                }
            }
        }
        
        public void UpdateConnectorStatus()
        {
            connectorsStatus[0] = connectorFront.Status == MyShipConnectorStatus.Connected;
            connectorsStatus[1] = connectorBack.Status == MyShipConnectorStatus.Connected;
            connectorsStatus[2] = mbFront.IsConnected;
            connectorsStatus[3] = mbBack.IsConnected;
        }
        public void PrepareRetraction()
        {
            if (connectorsStatus[0] && connectorsStatus[1] && connectorsStatus[2] && connectorsStatus[3])
            {
                // all are connected, disconnect all but front connector
                OnOff(mbBack, false);
                OnOff(mbFront, false);
                connectorBack.Disconnect();
            }
            else if (!connectorsStatus[0] && connectorsStatus[1] && !connectorsStatus[2] && connectorsStatus[3])
            {
                // expected status after extension, connect front
                OnOff(mbFront, true);
            }
            else if (!connectorsStatus[0] && connectorsStatus[1] && connectorsStatus[2] && connectorsStatus[3])
            {
                if (connectorFront.Status == MyShipConnectorStatus.Connectable)
                {
                    connectorFront.Connect();
                }
                if (connectorBack.Status == MyShipConnectorStatus.Connected)
                {
                    connectorBack.Disconnect();
                }
                OnOff(mbBack, false);
            }
            else if (connectorsStatus[0] && (connectorsStatus[1] || connectorsStatus[2] || connectorsStatus[3]))
            {
                if (connectorBack.Status == MyShipConnectorStatus.Connected)
                {
                    connectorBack.Disconnect();
                }
                OnOff(mbBack, false);
                OnOff(mbFront, false);
            }
        }
        public void PrepareExtension()
        {
            if (status.ParkingRequested 
                && connectorsStatus[1] 
                && connectorsStatus[3]
                && !connectorsStatus[0]
                && !connectorsStatus[2])
            {
                // perfect for parking
                Abort();
            }

            if (connectorsStatus[0] && connectorsStatus[1] && connectorsStatus[2] && connectorsStatus[3])
            {
                // all are connected, disconnect the front
                OnOff(mbFront, false);
                connectorFront.Disconnect();
            } 
            else if (connectorsStatus[0] && !connectorsStatus[1] && !connectorsStatus[2] && !connectorsStatus[3])
            {
                // only connectorFront is connected, so connect the back MB
                OnOff(mbBack, true);
            } 
            else if (connectorsStatus[0] && !connectorsStatus[1] && !connectorsStatus[2] && connectorsStatus[3])
            {
                connectorBack.Connect();
            } 
            else if (connectorsStatus[0] && !connectorsStatus[1] && connectorsStatus[2] && connectorsStatus[3])
            {
                connectorFront.Disconnect();
            }
            else if (connectorsStatus[1] && connectorsStatus[3])
            {
                connectorFront.Disconnect();
                OnOff(mbFront, false);
            }
        }


        
        public void UpdatePistons()
        {
            if (status.AbortRequested)
            {
                RetractPistons();
            }
            else if (status.IsExtending)
            {
                if (!connectorsStatus[0] && connectorsStatus[1] && !connectorsStatus[2] && connectorsStatus[3])
                {
                    ExtendPistons();
                }
                else
                {
                    PrepareExtension();
                }
            }
            else if (status.IsRetracting)
            {
                if (connectorsStatus[0] && !connectorsStatus[1] && !connectorsStatus[2] && !connectorsStatus[3])
                {
                    RetractPistons();
                }
                else
                {
                    PrepareRetraction();
                }
            }
            else
            {
                StopPistons();
            }

        }
        public void ExtendPistons()
        {
            foreach (IMyPistonBase bloc in pistonBlocks)
            {
                bloc.MaxLimit = maxPistonDistance;
                bloc.Velocity = status.PistonsDrillingSpeed;
            }
            return;
        }

        public void StopPistons()
        {
            foreach (IMyPistonBase bloc in pistonBlocks)
            {
                bloc.Velocity = 0;
            }
            return;
        }

        public void RetractPistons()
        {
            foreach (IMyPistonBase bloc in pistonBlocks)
            {
                bloc.MinLimit = minPistonDistance;
                bloc.Velocity = -pistonRetractSpeed;
            }
            return;
        }

        public void OnOff<T>(T bloc, bool isOn) where T : IMyFunctionalBlock
        {
            if (isOn)
            {
                bloc.ApplyAction("OnOff_On");
                bloc.Enabled = true;
            }
            else
            {
                bloc.ApplyAction("OnOff_Off");
                bloc.Enabled = false;
            }
            return;
        }

        public void OnOff<T>(List<T> blocs, bool isOn) where T : IMyFunctionalBlock
        {
            foreach (T bloc in blocs)
            {
                if (isOn)
                {
                    bloc.ApplyAction("OnOff_On");
                    bloc.Enabled = true;
                }
                else
                {
                    bloc.ApplyAction("OnOff_Off");
                    bloc.Enabled = false;
                }
            }
            return;
        }

        public void SetRotorVelocity(List<IMyTerminalBlock> rotors, float speed, bool isReversed)
        {
            if (isReversed)
            {
                speed = -speed;
            }
            foreach (IMyTerminalBlock rotor in rotors)
            {
                rotor.SetValue("Velocity", speed);
            }
            return;
        }


    }
}
