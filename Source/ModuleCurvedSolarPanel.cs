using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NearFutureSolar
{
    public class ModuleCurvedSolarPanel:PartModule
    {

        [KSPField(isPersistant = false)]
        public string PanelTransformName;

        [KSPField(isPersistant = false)]
        public string DeployAnimation;

        [KSPField(isPersistant = false)]
        public bool Deployable = false;

        [KSPField(isPersistant = false)]
        public float TotalEnergyRate = 5.0f;

        [KSPField(isPersistant = false)]
        public FloatCurve powerCurve = new FloatCurve();

        [KSPField(isPersistant = false)]
        public string ResourceName;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Sun Exposure")]
        public string SunExposure;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Energy Flow")]
        public string EnergyFlow;

        [KSPField(isPersistant = false)]
        public float energyFlow;

        public float sunExposure;

        // ACTIONS
        // -----------------
        // Deploy Panels
        [KSPEvent(guiActive = true, guiName = "Deploy Panel", active = true, guiActiveEditor = true)]
        public void DeployPanels()
        {
            Deploy();

        }
        // Retract Panels
        [KSPEvent(guiActive = true, guiName = "Retract Panel", active = false, guiActiveEditor = true)]
        public void RetractPanels()
        {
            Retract();

        }
        // Toggle Panels
        [KSPEvent(guiActive = false, guiName = "Toggle Panel", active = false)]
        public void TogglePanels()
        {

            Toggle();
        }

        [KSPAction("Deploy Panels")]
        public void DeployPanelsAction(KSPActionParam param)
        {
            DeployPanels();
        }

        [KSPAction("Retract Panels")]
        public void RetractPanelsAction(KSPActionParam param)
        {
            RetractPanels();
        }

        [KSPAction("Toggle Panels")]
        public void TogglePanelsAction(KSPActionParam param)
        {
            TogglePanels();
        }

        // Deploy Panels
        public void Deploy()
        {
            if (!Deployable)
                return;

            for (int i = 0; i < deployStates.Length ; i++)
            {
                deployStates[i].speed = 1;
            }
            State = ModuleDeployablePart.DeployState.EXTENDING;
        }

        // Retract Panels
        public void Retract()
        {
            if (!Deployable)
                return;

            for (int i = 0; i < deployStates.Length ; i++)
            {
                deployStates[i].speed = -1;
            }
            State = ModuleDeployablePart.DeployState.RETRACTING;
        }
        // Toggle Panels
        public void Toggle()
        {
            if (State == ModuleDeployablePart.DeployState.EXTENDED)
                Retract();
            else if (State == ModuleDeployablePart.DeployState.RETRACTED)
                Deploy();
            else
                return;
        }

        // Get the state
        public ModuleDeployablePart.DeployState State
        {
            get
            {
                try
                {
                    return (ModuleDeployablePart.DeployState)Enum.Parse(typeof(ModuleDeployablePart.DeployState), SavedState);
                }
                catch
                {
                    State = ModuleDeployablePart.DeployState.RETRACTED;
                    return State;
                }
            }
            set
            {
                SavedState = value.ToString();
            }
        }

        // Current panel state
        [KSPField(isPersistant = true)]
        public string SavedState;


        private AnimationState[] deployStates;

        private Transform[] panelTransforms;
        private int panelCount= 0;
        private float chargePerTransform;

        private bool flight = false;

        private Transform sunTransform;

        // Info for ui
        public override string GetInfo()
        {
            return String.Format("Ideal Charge Rate: {0:F2} Ec/s", TotalEnergyRate);
        }


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.moduleName = "Curved Solar Panel";
        }

        public override void OnStart(PartModule.StartState state)
        {

            panelTransforms = part.FindModelTransforms(PanelTransformName);
            panelCount = panelTransforms.Length;


            if (Deployable)
            {
                deployStates = Utils.SetUpAnimation(DeployAnimation, this.part);


                if (State == ModuleDeployablePart.DeployState.EXTENDED || State == ModuleDeployablePart.DeployState.EXTENDING)
                {
                    for (int i = 0; i < deployStates.Length ; i++)
                    {
                        deployStates[i].normalizedTime = 1f;
                    }
                }
                else if (State == ModuleDeployablePart.DeployState.RETRACTED || State == ModuleDeployablePart.DeployState.RETRACTING)
                {
                    for (int i = 0; i < deployStates.Length ; i++)
                    {
                        deployStates[i].normalizedTime = 0f;
                    }
                }
                else
                {
                    // broken! none for you!
                }
            }
            else
            {
                Events["DeployPanels"].active = false;
                Events["RetractPanels"].active = false;
                Events["TogglePanels"].active = false;
            }

            if (state != StartState.Editor)
            {
                sunTransform = FlightGlobals.Bodies[0].bodyTransform;
                flight = true;
            }
            else
            {
                flight = false;
            }
        }

        public void FixedUpdate()
        {
            if (flight)
            {
                if (!Deployable || (Deployable && (State == ModuleDeployablePart.DeployState.EXTENDED ) ))
                {
                    sunExposure = 0f;
                    energyFlow = 0f;

                    int blockedPartCount = 0;
                    int blockedBodyCount = 0;
                    string body = "";
                    string obscuringPart = "";

                    chargePerTransform = TotalEnergyRate / panelCount;

                    for (int i = 0; i < panelTransforms.Length; i++)
                    {
                        float angle = 0f;

                        if (SolarLOS(panelTransforms[i], out angle, out body))
                        {
                            if (PartLOS(panelTransforms[i], out obscuringPart))
                            {
                                sunExposure += Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad)) / panelCount;
                                energyFlow += Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad)) * chargePerTransform;
                            }
                            else
                            {
                                energyFlow += 0f;
                                sunExposure += 0f;
                                blockedPartCount++;
                            }
                        }
                        else
                        {
                            sunExposure += 0f;
                            energyFlow += 0f;
                            blockedBodyCount++;

                        }

                    }

                    double altAboveSun = FlightGlobals.getAltitudeAtPos(vessel.GetWorldPos3D(), FlightGlobals.Bodies[0]);
                    float realFlow = powerCurve.Evaluate((float)altAboveSun) * energyFlow;
                    //Debug.Log(altAboveSun.ToString() + ", gives " + realFlow);

                    EnergyFlow = String.Format("{0:F2}", realFlow);
                    SunExposure = String.Format("{0:F2}", sunExposure);

                    if (blockedPartCount >= panelCount)
                    {
                        SunExposure = "Blocked by " + obscuringPart + "!";
                    }
                    if (blockedBodyCount >= panelCount)
                    {
                        SunExposure = "Blocked by " + body + "!";
                    }

                    part.RequestResource(ResourceName, (-realFlow) * TimeWarp.fixedDeltaTime);
                } else if  (Deployable && (State == ModuleDeployablePart.DeployState.BROKEN ))
                {
                    SunExposure = "Broken!";
                    EnergyFlow = "Panels Retracted";
                }
                else if (Deployable && (State == ModuleDeployablePart.DeployState.RETRACTED))
                {
                    SunExposure = "Panels Retracted";
                    EnergyFlow = "Panels Retracted";
                }
            }
        }

        public void Update()
        {

            if (Deployable)
            {
                for (int i = 0; i < deployStates.Length ; i++)
                {
                    deployStates[i].normalizedTime = Mathf.Clamp01(deployStates[i].normalizedTime);
                }
                if (State == ModuleDeployablePart.DeployState.RETRACTING)
                {
                    if (EvalAnimationCompletionReversed(deployStates) == 0f)
                        State = ModuleDeployablePart.DeployState.RETRACTED;
                }

                if (State == ModuleDeployablePart.DeployState.EXTENDING)
                {
                    if (EvalAnimationCompletion(deployStates) == 1f)
                        State = ModuleDeployablePart.DeployState.EXTENDED;
                }

                if ((State == ModuleDeployablePart.DeployState.EXTENDED && Events["DeployPanels"].active) || (State == ModuleDeployablePart.DeployState.RETRACTED && Events["RetractPanels"].active))
                {
                    Events["DeployPanels"].active = !Events["DeployPanels"].active;
                    Events["RetractPanels"].active = !Events["RetractPanels"].active;
                }
            }
        }

        private bool PartLOS(Transform refXForm, out string obscuringPart)
        {
            bool sunVisible = true;
            obscuringPart = "nil";

            CelestialBody sun = FlightGlobals.Bodies[0];

            RaycastHit hit;
            if (Physics.Raycast(refXForm.position,  refXForm.position - sun.transform.position,out hit,2500f))
            {

                Transform hitObj = hit.transform;
                Part pt = hitObj.GetComponent<Part>();
                if (pt != null && pt != part)
                {
                    sunVisible = false;
                    obscuringPart = pt.partInfo.name;
                }
            }

            return sunVisible;
        }

        private bool SolarLOS(Transform refXForm, out float angle, out string obscuringBody)
        {
            bool sunVisible = true;
            angle = 0f;
            obscuringBody = "nil";

            CelestialBody sun = FlightGlobals.Bodies[0];
            CelestialBody currentBody = FlightGlobals.currentMainBody;

            angle = Vector3.Angle(refXForm.forward, sun.transform.position-refXForm.position);

            if (currentBody != sun)
            {

                Vector3 vT = sun.position - part.vessel.GetWorldPos3D();
                Vector3 vC = currentBody.position - part.vessel.GetWorldPos3D();
                // if true, behind horizon plane
                if (Vector3.Dot(vT, vC) > (vC.sqrMagnitude - currentBody.Radius * currentBody.Radius))
                {
                    // if true, obscured
                    if ((Mathf.Pow(Vector3.Dot(vT, vC), 2) / vT.sqrMagnitude) > (vC.sqrMagnitude - currentBody.Radius * currentBody.Radius))
                    {
                        sunVisible = false;
                        obscuringBody = currentBody.name;
                    }
                }
            }

            return sunVisible;

            // discard this for now; stock panels don't check against other than main body

            //foreach (CelestialBody planet in FlightGlobals.Bodies)
            //{
            //    if (planet == sun)
            //    {
            //        angle = Vector3.Angle(refXForm.forward, refXForm.position - planet.transform.position);
            //    }
            //    else
            //    {
            //        Vector3d vT = sun.position - part.vessel.GetWorldPos3D();
            //        Vector3d vC = planet.position - part.vessel.GetWorldPos3D();
            //        // if true, behind horizon plane
            //        if (Vector3d.Dot(vT, vC) > (vC.sqrMagnitude - planet.Radius*planet.Radius))
            //        {
            //            // if true, obsucred
            //            if ((Mathf.Pow(Vector3.Dot(vT, vC), 2) / vT.sqrMagnitude) > (vC.sqrMagnitude - planet.Radius * planet.Radius))
            //            {
            //                sunVisible = false;
            //                obscuringBody = planet.name;
            //            }
            //        }
            //    }

            //}


        }


        private float EvalAnimationCompletion(AnimationState[] states)
        {
            float checker = 0f;
            for (int i = 0; i < states.Length ; i++)
            {
                checker = Mathf.Max(states[i].normalizedTime, checker);
            }
            return checker;
        }
        private float EvalAnimationCompletionReversed(AnimationState[] states)
        {
            float checker = 1f;
            for (int i = 0; i < states.Length ; i++)
            {
                checker = Mathf.Min(states[i].normalizedTime, checker);
            }
            return checker;
        }

    }
}
