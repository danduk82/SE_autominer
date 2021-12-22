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

namespace IngameScript
{
    partial class Program
    {
        public class Status
        {
            private bool _isWelding;
            private bool _isGrinding;
            private bool _isWeldingPossible;
            private bool _isDrilling;
            private float _wheelRotorsSpeed;
            private float _drillingRotorSpeed;
            private float _pistonsDrillingSpeed;
            private bool _isSpinning;
            private bool _isExtending;
            private bool _isRetracting;
            private bool _isReadyToExtend;
            private bool _isReadyToRetract;
            private bool _abortRequested;
            private bool _parkingRequested;
            private bool _isAutomining;


            public bool IsWelding
            {
                get
                {
                    return _isWelding;
                }

                set
                {
                    _isWelding = value;
                }
            }

            public bool IsDrilling
            {
                get
                {
                    return _isDrilling;
                }

                set
                {
                    _isDrilling = value;
                }
            }

            public float WheelRotorsSpeed
            {
                get
                {
                    return _wheelRotorsSpeed;
                }

                set
                {
                    _wheelRotorsSpeed = value;
                }
            }

            public bool IsWeldingPossible
            {
                get
                {
                    return _isWeldingPossible;
                }

                set
                {
                    _isWeldingPossible = value;
                }
            }

            public bool IsSpinning
            {
                get
                {
                    return _isSpinning;
                }

                set
                {
                    _isSpinning = value;
                }
            }

            public bool IsGrinding
            {
                get
                {
                    return _isGrinding;
                }

                set
                {
                    _isGrinding = value;
                }
            }

            public bool IsExtending
            {
                get
                {
                    return _isExtending;
                }

                set
                {
                    _isExtending = value;
                }
            }

            public bool IsRetracting
            {
                get
                {
                    return _isRetracting;
                }

                set
                {
                    _isRetracting = value;
                }
            }

            public bool IsReadyToExtend
            {
                get
                {
                    return _isReadyToExtend;
                }

                set
                {
                    _isReadyToExtend = value;
                }
            }

            public bool IsReadyToRetract
            {
                get
                {
                    return _isReadyToRetract;
                }

                set
                {
                    _isReadyToRetract = value;
                }
            }

            public bool AbortRequested
            {
                get
                {
                    return _abortRequested;
                }

                set
                {
                    _abortRequested = value;
                }
            }

            public bool IsAutomining
            {
                get
                {
                    return _isAutomining;
                }

                set
                {
                    _isAutomining = value;
                }
            }

            public bool ParkingRequested
            {
                get
                {
                    return _parkingRequested;
                }

                set
                {
                    _parkingRequested = value;
                }
            }

            public float DrillingRotorSpeed
            {
                get
                {
                    return _drillingRotorSpeed;
                }

                set
                {
                    _drillingRotorSpeed = value;
                }
            }

            public float PistonsDrillingSpeed
            {
                get
                {
                    return _pistonsDrillingSpeed;
                }

                set
                {
                    _pistonsDrillingSpeed = value;
                }
            }

            public Status(
                    bool IsWelding = false,
                    bool IsWeldingPossible = true,
                    bool IsDrilling = false,
                    bool IsSpinning = false,
                    bool IsGrinding = false,
                    bool IsExtending = false,
                    bool IsRetracting = false,
                    float WheelRotorsSpeed = 0.0f,
                    bool IsReadyToExtend = false,
                    bool IsReadyToRetract = false,
                    bool AbortRequested = false,
                    bool IsAutomining = false,
                    bool ParkingRequested = false,
                    float DrillingRotorSpeed = 0.0f,
                    float PistonsDrillingSpeed = 0.0f)
            {
                this.IsWelding = IsWelding;
                this.IsWeldingPossible = IsWeldingPossible;
                this.IsDrilling = IsDrilling;
                this.IsSpinning = IsSpinning;
                this.IsGrinding = IsGrinding;
                this.IsExtending = IsExtending;
                this.IsRetracting = IsRetracting;
                this.WheelRotorsSpeed = WheelRotorsSpeed;
                this.IsReadyToExtend = IsReadyToExtend;
                this.IsReadyToRetract = IsReadyToRetract;
                this.AbortRequested = AbortRequested;
                this.IsAutomining = IsAutomining;
                this.ParkingRequested = ParkingRequested;
                this.DrillingRotorSpeed = DrillingRotorSpeed;
                this.PistonsDrillingSpeed = PistonsDrillingSpeed;
            }

            public Status(string StorageString)
            {
                this.Load(StorageString);
            }

            public string Save()
            {

                string saveStatus = $"IsWelding={this.IsWelding};IsWeldingPossible={this.IsWeldingPossible};IsDrilling={this.IsDrilling};IsSpinning={this.IsSpinning};IsGrinding={this.IsGrinding};IsExtending={this.IsExtending};IsRetracting={this.IsRetracting};WheelRotorsSpeed={this.WheelRotorsSpeed};IsReadyToExtend={this.IsReadyToExtend};IsReadyToRetract={this.IsReadyToRetract};AbortRequested={this.AbortRequested};IsAutomining={this.IsAutomining};ParkingRequested={this.ParkingRequested}";
                return saveStatus;
            }
            public void Load(string saveStatus)
            {
                string[] statusArray = saveStatus.Split(';');
                string key, value;
                try
                {
                    foreach (string s in statusArray)
                    {
                        string[] x = s.Split('=');

                        key = x[0];
                        value = x[1];
                        switch (key)
                        {
                            case "IsWelding":
                                this.IsWelding = bool.Parse(value);
                                break;
                            case "IsDrilling":
                                this.IsDrilling = bool.Parse(value);
                                break;
                            case "IsSpinning":
                                this.IsSpinning = bool.Parse(value);
                                break;
                            case "IsGrinding":
                                this.IsGrinding = bool.Parse(value);
                                break;
                            case "IsExtending":
                                this.IsExtending = bool.Parse(value);
                                break;
                            case "IsRetracting":
                                this.IsRetracting = bool.Parse(value);
                                break;
                            case "AbortRequested":
                                this.AbortRequested = bool.Parse(value);
                                break;
                            case "IsAutomining":
                                this.IsAutomining = bool.Parse(value);
                                break;
                            case "WheelRotorsSpeed":
                                this.WheelRotorsSpeed = float.Parse(value);
                                break;
                            case "IsReadyToExtend":
                                this.IsReadyToExtend = bool.Parse(value);
                                break;
                            case "IsReadyToRetract":
                                this.IsReadyToRetract = bool.Parse(value);
                                break;
                            case "ParkingRequested":
                                this.ParkingRequested = bool.Parse(value);
                                break;
                            case "DrillingRotorSpeed":
                                this.DrillingRotorSpeed = float.Parse(value);
                                break;
                            case "PistonsDrillingSpeed":
                                this.PistonsDrillingSpeed = float.Parse(value);
                                break;
                        }
                    }
                }
                catch (Exception) { }

            }
        }
    }
}
