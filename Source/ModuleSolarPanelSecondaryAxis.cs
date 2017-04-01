using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NearFutureSolar
{
    public class ModulSolarPanelSecondaryAxis:PartModule
    {

        [KSPField(isPersistant = false)]
        public string PanelTransformName;

        [KSPField(isPersistant = false)]
        public string PivotTransformName;

        private Transform pivotTransform;
        private Transform panelTransform;

        private Transform sunTransform;
        private ModuleDeployableSolarPanel host;

        public override void OnStart(PartModule.StartState state)
        {

            panelTransform = part.FindModelTransform(PanelTransformName);
            pivotTransform = part.FindModelTransform(PivotTransformName);
            host = part.GetComponent<ModuleDeployableSolarPanel>();


        }

        public void FixedUpdate()
        {
          if (FlightGlobals.CurrentSceneIsFlight && host != null)
          {
            if (host.State == ModuleDeployablePart.DeployState.EXTENDED)
            {
              if (SolarLOS(panelTransform))
                RotateToSun();
            }
            if (host.State == ModuleDeployablePart.DeployState.RETRACTING)
            {
              RotateToZero();
            }
          }
        }

        void RotateToSun()
        {

        }

        void RotateToZero()
        {

        }

        private bool SolarLOS(Transform refXForm)
        {
            bool sunVisible = true;

            CelestialBody sun = FlightGlobals.Bodies[0];
            CelestialBody currentBody = FlightGlobals.currentMainBody;
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

                    }
                }
            }

            return sunVisible;
        }



    }
}
