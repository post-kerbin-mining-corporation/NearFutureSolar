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
        public float TotalEnergyRate = 5.0f;

        [KSPField(isPersistant = false)]
        public FloatCurve powerCurve;

        [KSPField(isPersistant = false)]
        public string ResourceName;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Sun Exposure")]
        public string SunExposure;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Energy Flow")]
        public string EnergyFlow;

        public float energyFlow;
        public float sunExposure;

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
            chargePerTransform = TotalEnergyRate / panelCount;

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
                sunExposure = 0f;
                energyFlow = 0f;

                int blockedPartCount = 0;
                int blockedBodyCount = 0;
                string body = "";
                string obscuringPart = "";

                foreach (Transform panel in panelTransforms)
                {
                    float angle = 0f;

                    if (SolarLOS(panel, out angle,out body))
                    {
                        if (PartLOS(panel, out obscuringPart))
                        {
                            sunExposure += Mathf.Clamp01(Mathf.Cos(angle*Mathf.Deg2Rad)) / panelCount;
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
                float realFlow = powerCurve.Evaluate((float)altAboveSun)*energyFlow;
                Debug.Log(altAboveSun.ToString() +", gives " + realFlow);

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

                part.RequestResource(ResourceName,  (-realFlow)*TimeWarp.fixedDeltaTime);
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

                Vector3d vT = sun.position - part.vessel.GetWorldPos3D();
                Vector3d vC = currentBody.position - part.vessel.GetWorldPos3D();
                // if true, behind horizon plane
                if (Vector3d.Dot(vT, vC) > (vC.sqrMagnitude - currentBody.Radius * currentBody.Radius))
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
    }
}
