// HLBallastPart
// by Hooligan Labs

// This is completely unlicensed.  Feel free to share your own projects with Hooligan Labs.  We would love to see it!

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HLBallastSpace
{
    public class HLBallastPartModule : PartModule
    {
        // Private variables cannot be changed by the part.cfg file
        #region KSPFields
        // These public values can be overwritten in the part.cfg files.
        // The limit for buoyant force (0 to disable). Allows operation at low Depths even with large Ballast volumes, though perhaps gas could not be compressed this much.
        // The minimum atmospheric pressure the blimp can operate in.
        // Used for inflateable parachutes
        // The maximum speed at which the vessel can be made stationary

        [KSPField(isPersistant = true, guiActive = false)]
        public float makeStationarySpeedMax = 1f, makeStationarySpeedClamp = 0.0f;

        [KSPField(isPersistant = true, guiActive = false, guiName = "GUI On")]
        public bool guiOn = true;

        // The Ballast volume.  This has been calculated based on model measurements done in Blender
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Ballast Volume", guiUnits = "m3", guiFormat = "F2")]
        public float BallastVolume = 3f; // assuming 3 cubic meters of uncompressed air inside for crew modules

        // Rate of change per second
        [KSPField(isPersistant = true, guiActive = false)]
        public float airRate = 0.1f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rate of Gaining Ballast", guiUnits = "% / sec", guiFormat = "F2")]
        public float ballastRate = 0.1f;

        // Vessel combined Ballast
        [KSPField(isPersistant = true, guiActive = true, guiName = "External Liquid Density", guiUnits = "ton/m3", guiFormat = "F3")]
        public float liquidDensity = 1.000f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Surface Liquid Density Kerbin", guiUnits = "ton/m3", guiFormat = "F3")]
        public float liquidDensityKerbin = 1.025f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Surface Liquid Density Eve", guiUnits = "ton/m3", guiFormat = "F3")]
        public float liquidDensityEve = 1.021f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Net Force on Part", guiUnits = "Nm", guiFormat = "F3")]
        public float netForceMagnitude = 0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Net Force Max", guiUnits = "Nm", guiFormat = "F3")]
        public float netForceMagnitudeMax = 0f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Vessel Buoyant Force Total", guiUnits = "Nm", guiFormat = "F3")]
        public float vesselNetForceTotal = 0f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Vessel Buoyant Force Unchanging", guiUnits = "Nm", guiFormat = "F3")]
        public float vesselNetForceUnchanging = 0f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Vessel Buoyant Force Max", guiUnits = "Nm", guiFormat = "F3")]
        public float vesselNetForceMax = 0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Vessel Mass", guiFormat = "F2")]
        public float totalMass = 0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Vessel Gravity Force", guiFormat = "F2")]
        public float totalGravityForce = 0f;
        // Target buoyant force (effectively specific volume) fraction of maximum possible, set by GUI
        [KSPField(isPersistant = true, guiActive = false, guiName = "Vessel %", guiFormat = "F2")]
        public float targetBallastVessel = 0.0f;
        // Fraction of lifting gas specific volume the Ballast
        [KSPField(isPersistant = true, guiActive = true, guiName = "Ballast %", guiFormat = "F3")]
        public float specificVolumeFractionBallast = 0.00f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Buoyancy %", guiFormat = "F3")]
        public float specificVolumeFractionBuoyant = 0.00f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Depth Control")]
        private bool toggleDepthControl = false;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Target Vertical Velocity", guiFormat = "F3")]
        public float targetVerticalVelocity = 0f;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Make Stationary")]
        private bool togglePersistenceControl = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Pitch %", guiFormat = "F3")]
        public float targetPitchBallast = 0;

        // Can this be a controllable ballast? 0 no, 1 yes
        [KSPField(isPersistant = false, guiActive = false)]
        public int canControl = 1;

        // Is this the lead Ballast?
        [KSPField(isPersistant = true, guiActive = false, guiName = "Lead Ballast")]
        public bool isLeadBallast = false;

        // 0 for do not remove buoyancy from other vessel parts, 1 for yes
        [KSPField(isPersistant = false, guiActive = false)]
        public int clearStockBuoyancy = 1;

        // 0 for do not add buoyancy to crewed parts, 1 for yes
        [KSPField(isPersistant = false, guiActive = false)]
        public int addCrewedBuoyancy = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public float fixedUpdateAltitude = 600;

        // EVA!
        [KSPField]
        public int convertEVA = 1;

        // Scales force of buoyancy
        [KSPField]
        public float buoyancyTop = 0.5f;
        [KSPField]
        public float buoyancyBottom = -0.5f;

        // From Snjo
        [KSPField]
        public float partAltitude;
        [KSPField]
        public float previousPartAltitude;
        [KSPField]
        public float buoyancyVerticalOffset = 0.0f; // how high the part rides on the water in meters. Not a position offset inside the part. This will be applied in the global axis regardless of part rotation. Think iceberg/styrofoam.
        [KSPField]
        public bool splashFXEnabled = true;
        [KSPField]
        public float dragInWater = 0.5f; // when in water, apply drag to slow the craft down. Stock drag is 3.
        [KSPField]
        public float waterDragMultiplier = 1f;
        [KSPField]
        public float waterImpactMultiplier = 10f;

        private List<PartBuoyancy> toBeDestroyed = new List<PartBuoyancy>();

        public bool splashed;
        private float splashTimer = 0f;
        public float splashCooldown = 0.5f;

        private bool cameraWasManual = false;

        #endregion

        # region GUI

        // GUI
        private Rect windowPos;
        private int SubmarineWindowID;
        public bool activeGUI = false;
        private float windowWidth;

        private bool resetGUIsize = false;
        private bool willReset1 = false;

        private List<HLBallastPartModule> Ballasts = new List<HLBallastPartModule>();
        public HLBallastPartModule leadBallast = null;

        // Debug
        // private int activeVessel = 0;

        #endregion

        #region KSPEvents

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast ++")]
        public void BallastPP_Event()
        {
            targetPitchBallast += 0.01f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast +")]
        public void BallastP_Event()
        {
            targetPitchBallast += 0.001f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast -")]
        public void BallastN_Event()
        {
            targetPitchBallast -= 0.001f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast --")]
        public void BallastNN_Event()
        {
            targetPitchBallast -= 0.01f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast Max")]
        public void BallastM_Event()
        {
            targetPitchBallast = 1f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast Min")]
        public void BallastZ_Event()
        {
            targetPitchBallast = -1f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 2000, guiName = "Ballast Clear Adjustments")]
        public void BallastC_Event()
        {
            targetPitchBallast = 0f;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Toggle GUI")]
        public void guiToggle_Event()
        {
            part.SendEvent("guiToggle");
        }

        [KSPEvent(guiActive = false, guiActiveUnfocused = false)]
        public void guiToggle()
        {
            if (this == leadBallast && guiOn)
            {
                guiOn = false;
                removeGUI();
            }
            else if (this == leadBallast)
            {
                guiOn = true;
                addGUI();
            }
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Toggle Underwater Camera")]
        public void cameraToggle_Event()
        {
            // Seem to have issues with EVAs toggling
            if (this.vessel.isEVA) return;
            UnderwaterCamera.ManualControl = !UnderwaterCamera.ManualControl;
        }

        #endregion

        #region KSPActions

        [KSPAction("GUI Toggle")]
        public void guiToggle_Action(KSPActionParam param)
        {
            guiToggle_Event();
        }

        [KSPAction("Ballast ++")]
        public void BallastP_Action(KSPActionParam param)
        {
            BallastPP_Event();
        }

        [KSPAction("Ballast +")]
        public void BallastPP_Action(KSPActionParam param)
        {
            BallastP_Event();
        }

        [KSPAction("Ballast -")]
        public void BallastN_Action(KSPActionParam param)
        {
            BallastN_Event();
        }

        [KSPAction("Ballast --")]
        public void BallastNN_Action(KSPActionParam param)
        {
            BallastNN_Event();
        }

        [KSPAction("Ballast Max")]
        public void BallastM_Action(KSPActionParam param)
        {
            BallastM_Event();
        }

        [KSPAction("Ballast Zero")]
        public void BallastZ_Action(KSPActionParam param)
        {
            BallastZ_Event();
        }

        [KSPAction("Clear Adjustments")]
        public void BallastC_Action(KSPActionParam param)
        {
            BallastC_Event();
        }

        #endregion

        public override void OnAwake()
        {
            /// Constructor style setup. 
            /// Called in the Part\'s Awake method.  
            /// The model may not be built by this point
        }

        public override void OnUpdate()
        {
            /// Per-frame update 
            /// Called ONLY when Part is ACTIVE! 

            // Finds all 
            try { findBallasts(); }
            catch (Exception ex) { print("findBallasts Exception!"); print(ex.Message); }

            // Freeze in place if requested
            if (togglePersistenceControl) try { persistentParts(); }
                catch (Exception ex) { print("persistentParts Exception!"); print(ex.Message); }

            if (this == leadBallast && guiOn) try { addGUI(); }
                catch (Exception ex) { print("addGUI Exception!"); print(ex.Message); }

            else if (!guiOn || !leadBallast) try { removeGUI(); }
                catch (Exception ex) { print("removeGUI Exception!"); print(ex.Message); }
        }

        public override void OnStart(StartState state)
        {
            // OnFlightStart seems to have been removed
            /// Called during the Part startup. 
            /// StartState gives flag values of initial state
            Debug.Log("HL Submarine Plugin Start");
            specificVolumeFractionBallast = Mathf.Clamp01(specificVolumeFractionBallast);
            specificVolumeFractionBuoyant = Mathf.Clamp01(specificVolumeFractionBuoyant);

            this.enabled = true;

            // activeVessel = FlightGlobals.ActiveVessel.GetInstanceID();
            // Debug.Log("Set lead Ballast");
            // if (this == leadBallast) { targetBallastVessel = Mathf.Clamp01(targetBallastVessel); }
        }

        public void OnDestroy()
        {
            if (this == leadBallast)
            {
                removeGUI();
                try { UnderwaterCamera.RestoreCameraParent(); }
                catch (Exception ex) { print("RestoreCameraParent Exception!"); print(ex.Message); }
            }
        }

        public void onDisconnect()
        {
            if (this == leadBallast)
            {
                removeGUI();
                try { UnderwaterCamera.RestoreCameraParent(); }
                catch (Exception ex) { print("RestoreCameraParent Exception!"); print(ex.Message); }
            }
        }

        public override void OnFixedUpdate()
        {
            /// Per-physx-frame update 
            /// Called ONLY when Part is ACTIVE!

            // Only perform when in a reasonable area of the game
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // ... And low enough
            if (this.vessel.altitude > fixedUpdateAltitude)
                return;

            // KSP's basic buoyancy is no good for us!  It must be... DESTROYED and REPLACED with ours
            if (vessel.mainBody.ocean)
            {
                toBeDestroyed.Clear();

                // For attached parts to this vessel
                try { foreach (Part stockParts in this.vessel.parts) replaceStockBuoyancy(stockParts); }
                catch (Exception ex) { print("replaceStockBuoyancy Exception for vessel parts!"); print(ex.Message); }

                // For nearby kerbals
                if (convertEVA == 1) try { makeKerbalsSink(); }
                    catch (Exception ex) { print("replaceStockBuoyancy Exception for EVA Kerbals!"); print(ex.Message); }
            }

            // Sets one Ballast to run the GUI and control logic
            try { determineLeadBallast(); }
            catch (Exception ex) { print("determineLeadBallast Exception!"); print(ex.Message); partAltitude = 0f; }

            // Find height.. now that we cannot determine splashed normally
            try { findPartAltitude(this.part, buoyancyVerticalOffset); }
            catch (Exception ex) { print("findPartAltitude Exception!"); print(ex.Message); partAltitude = 0f; }

            if (leadBallast == this) leadBallastUpdate();

            try { dragUpdate(); }
            catch (Exception ex) { print("dragUpdate Exception!"); print(ex.Message); }

            if (splashFXEnabled) try { splashyTime(); }
                catch (Exception ex) { print("splashyTime Exception!"); print(ex.Message); }

            // Update Ballast properties
            BallastUpdate();
        }

        private void makeKerbalsSink()
        {
            // Find loaded EVAs
            foreach (Vessel isItEVA in FlightGlobals.Vessels)
            {
                if (isItEVA.isEVA)
                {
                    foreach (Part evaPart in isItEVA.parts) replaceStockBuoyancy(evaPart);
                }

                // Flags too!
                if (isItEVA.vesselType == VesselType.Flag)
                {
                    foreach (Part evaPart in isItEVA.parts) replaceStockBuoyancy(evaPart);
                }
            }
        }

        private void replaceStockBuoyancy(Part stocky)
        {
            // Debug.Log(this.part.partName + " " + this.gameObject.GetInstanceID() + " is removing stock partBuoyancy.");
            // If this part has physics and stock buoyancy, remove it
            if (clearStockBuoyancy == 1 && stocky.Rigidbody != null && stocky.partBuoyancy != null && !toBeDestroyed.Contains(stocky.partBuoyancy))
            {
                toBeDestroyed.Add(stocky.partBuoyancy);
                // Debug.Log(this.part.partName + " " + this.gameObject.GetInstanceID() + " is removing stock partBuoyancy from " + stocky.name + " " + stocky.gameObject.GetInstanceID());
                Destroy(stocky.partBuoyancy);
                // Debug.Log("partBuoyancy successfully removed");
            }

            // If the part contains crew we add a basic ballast to it.
            if (stocky.Rigidbody != null && stocky.Modules.OfType<HLBallastPartModule>().FirstOrDefault() == null && stocky.GetComponentInChildren<WheelCollider>() == null)
            {
                // Debug.Log("Creating new HLBallastPartModule for " + stocky.gameObject.GetInstanceID());
                Debug.Log(this.part.name + " " + this.gameObject.GetInstanceID() + " is adding HLBuoyancyPartModule to " + stocky.name + " " + stocky.gameObject.GetInstanceID());
                stocky.AddModule("HLBallastPartModule");
                // Debug.Log("HLBallastPartModule successfully added to " + stocky.gameObject.GetInstanceID());

                // Find the ballast plugin we just created
                HLBallastPartModule newBallast = stocky.Modules.OfType<HLBallastPartModule>().FirstOrDefault();
                Debug.Log("HLBallastPartModule for " + stocky.gameObject.GetInstanceID() + " successfully added.");

                // Cannot be a lead envelope
                newBallast.canControl = 0;

                // It cannot change its buoyancy
                newBallast.airRate = 0; newBallast.ballastRate = 0;

                // No events
                // Debug.Log("Deactivating events");
                newBallast.Events["BallastC_Event"].active = false;
                newBallast.Events["BallastM_Event"].active = false;
                newBallast.Events["BallastN_Event"].active = false;
                newBallast.Events["BallastNN_Event"].active = false;
                newBallast.Events["BallastP_Event"].active = false;
                newBallast.Events["BallastPP_Event"].active = false;
                newBallast.Events["BallastZ_Event"].active = false;
                newBallast.Events["guiToggle_Event"].active = false;
                Debug.Log("HLBallastPartModule for " + stocky.gameObject.GetInstanceID() + " has events deactivated.");

                /* This doesn't work
                // Less data
                foreach (KSPField field in newBallast.Fields)
                {
                    field.guiActive = false;
                    Debug.Log(field.guiName + " has been set to false.");
                }
                Debug.Log("HLBallastPartModule for " + stocky.gameObject.GetInstanceID() + " has most data deactivated.");
                 * */

                // Assume 1 m3 of uncompressed air per max crew
                // Debug.Log("Adjusting buoyancy");
                if (stocky.vessel.isEVA) newBallast.BallastVolume = 0.09f; // Should provide approximately the right amount of lift for neutral buoyancy
                else if (this.addCrewedBuoyancy == 1)
                {
                    newBallast.BallastVolume = (float)stocky.CrewCapacity;
                }
                else
                {
                    newBallast.BallastVolume = 0f;
                }
                Debug.Log("HLBallastPartModule for " + stocky.gameObject.GetInstanceID() + " " + stocky.partName + " successfully modified.  Next!");
            }
        }

        private void determineLeadBallast()
        {
            if (Ballasts.Count == 0)
                return;

            // Check to see if this Ballast should be promoted to lead Ballast
            foreach (HLBallastPartModule checkBallast in Ballasts)
            {
                if (checkBallast.leadBallast == null || canControl == 0)
                    continue;
                if (checkBallast.leadBallast != this)
                    this.leadBallast = checkBallast.leadBallast;
            }

            if (!Ballasts.Contains(leadBallast) && canControl == 1)
            {
                leadBallast = this;
            }

            // Update all ballasts with the lead
            foreach (HLBallastPartModule ballast in Ballasts)
            {
                newLeadBallast(this.leadBallast, ballast);
            }
        }

        public void leadBallastUpdate()
        {
            // Finds all parts within the vessel (specifically mass and buoyancy)
            try { findParts(); }
            catch (Exception ex) { print("findParts Exception!"); print(ex.Message); }

            // Keeps the Submarine at a steady Depth
            try { DepthControl(); }
            catch (Exception ex) { print("DepthControl Exception!"); print(ex.Message); }

            // Tells each Ballast what its Ballast should be
            try { setBallast(); }
            catch (Exception ex) { print("setBallast Exception!"); print(ex.Message); }

            // No Try here, its contents have its own exceptions
            cameraFix();
        }

        private void findPartAltitude(Part floatyPart, float vertical)
        {
            partAltitude = Vector3.Distance(floatyPart.WCoM, vessel.mainBody.position) - (float)(vessel.mainBody.Radius - vertical);

            if (partAltitude < -buoyancyBottom)
            {
                this.splashed = true;

                if (!vessel.isEVA)
                {
                    vessel.Splashed = true;
                    part.WaterContact = true;
                }
                else
                {
                    vessel.Splashed = false;
                    part.WaterContact = false;
                }
            }
            else if (splashed)
            {
                splashed = false;

                part.WaterContact = false;
                part.vessel.checkSplashed();
            }
        }

        private void dragUpdate()
        {
            // Apply extra drag if underwater
            // Based on Snjo's FSbuoyancy
            if (this.part.Rigidbody != null)
                if (partAltitude - buoyancyBottom < 0) this.part.Rigidbody.drag = part.maximum_drag * waterDragMultiplier;
                else this.part.Rigidbody.drag = 0;
        }

        private void findLiquidDensity()
        {
            // First, have we slashed down?
            // From Snjo!
            if (partAltitude < buoyancyBottom)
                // Where have we splashed down?
                // Eve might have a hydrazine ocean.  Everywhere else would be water...?
                if (vessel.mainBody.name == "Eve") liquidDensity = liquidDensityEve;
                else liquidDensity = liquidDensityKerbin;
            else liquidDensity = 0;

            // There isn't a need to adjust for depth - on Earth, the water at the bottom of the ocean is only 1.8% denser than at the top
        }

        private void DepthControl()
        {
            // The goal is to have gravity and buoyancy cancel each other out
            if (toggleDepthControl)
            {
                // Are we in the water?
                if (!vessel.Splashed) return;

                // First to determine the fixed amount of float
                float fixedWeight = totalGravityForce - vesselNetForceUnchanging; // Remove the fixed lift from weight

                // This is the amount we can change
                float varyingWeight = -1 * (vesselNetForceMax - vesselNetForceUnchanging); // Reduce the net force by fixed

                // Not working yet...
                // targetBallastVessel = Mathf.Clamp01(varyingWeight / fixedWeight - (float)((targetVerticalVelocity - this.vessel.verticalSpeed) * Time.deltaTime * 2));

                // Go with PID instead
                targetBallastVessel += (float)((this.vessel.verticalSpeed - targetVerticalVelocity) * Time.deltaTime / 100);
            }
            else
                togglePersistenceControl = false;
        }

        // Gathers information on the Ballasts
        public void findBallasts()
        {
            Ballasts.Clear();

            // More info: http://www.grc.nasa.gov/WWW/k-12/WindTunnel/Activities/balance_of_forces.html

            foreach (Part thingy in this.vessel.parts)
            {
                if (thingy.isAttached)
                {
                    HLBallastPartModule thingyBallast = null;
                    thingyBallast = thingy.Modules.OfType<HLBallastPartModule>().FirstOrDefault();
                    // Skip if not an Ballast
                    if (thingyBallast == null)
                    {
                        // Debug.Log("Did not find an Ballast on " + part.name);
                        continue;
                    }

                    Ballasts.Add(thingyBallast);

                    // Ballasts may have to be active to work... but they must not do anything else, so only dedicated ballasts
                    if (thingyBallast.canControl == 1 && thingyBallast.part.State != PartStates.ACTIVE) thingyBallast.part.force_activate();
                }
            }

            // Debug.Log("Total Ballasts Found by " + part.gameObject.GetInstanceID() + " = " + Ballasts.Count);
        }

        public void newLeadBallast(HLBallastPartModule newLeadBallast, HLBallastPartModule ballast)
        {
            ballast.leadBallast = newLeadBallast;
            if (ballast == leadBallast)
                ballast.isLeadBallast = true;
            else ballast.isLeadBallast = false;
        }

        private void crashInWater()
        {
            if (this.part.Rigidbody.velocity.magnitude > this.part.crashTolerance * waterImpactMultiplier) // && partAltitude < -buoyancyBottom)
            {
                GameEvents.onCrashSplashdown.Fire(new EventReport(FlightEvents.SPLASHDOWN_CRASH, this.part, this.part.partInfo.title, "ocean", 0, "HLBallastPartModule: Hit the water too fast"));
                this.part.explode();
                return;
            }
        }

        private void splashyTime()
        {
            if (splashTimer > 0f) splashTimer -= Time.deltaTime;
            else
            {
                if (part.Rigidbody.velocity.magnitude > 6f && partAltitude < 0 && partAltitude < previousPartAltitude && previousPartAltitude > 0) // don't splash if you are deep in the water or going slow
                {
                    if (Vector3.Distance(part.WCoM, FlightGlobals.camera_position) < 500f)
                    {
                        FXMonger.Splash(part.WCoM, part.Rigidbody.velocity.magnitude / 50f);
                    }
                    splashTimer = splashCooldown;

                    try { crashInWater(); }
                    catch (Exception ex) { print("crashInWater Exception!"); print(ex.Message); }
                }
            }
            previousPartAltitude = partAltitude;
        }

        private void cameraFix()
        {
            if (!this.vessel.isActiveVessel) return;
            
            try
            {
                if (UnderwaterCamera.ManualControl && (!splashed || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)) // Exit when IVA too
                {
                    UnderwaterCamera.RestoreCameraParent();
                    return;
                }
            }
            catch (Exception ex) { print("RestoreCameraParent Exception!"); print(ex.Message); }

            try
            {
                if (splashed && !UnderwaterCamera.ManualControl && this.vessel.GetTransform() != null && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA && CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Internal)
                {
                    UnderwaterCamera.SetCameraParent();
                }
            }
            catch (Exception ex) { print("SetCameraParent Exception!"); print(ex.Message); }

            try
            {
                if (UnderwaterCamera.ManualControl != cameraWasManual) //  && KtCamera.ActiveFlightCamera == null
                {
                    Debug.Log("Underwater Camera Control is " + UnderwaterCamera.ManualControl);
                    cameraWasManual = UnderwaterCamera.ManualControl;
                }
            }
            catch (Exception ex) { print("cameraWasManual Exception!"); print(ex.Message); }
        }

        private void findParts()
        {
            this.vesselNetForceTotal = 0;
            this.vesselNetForceUnchanging = 0;
            this.vesselNetForceMax = 0;

            // Hey, a method for finding mass!
            this.totalMass = this.vessel.GetTotalMass();

            // Downward force...
            this.totalGravityForce = (float)(this.totalMass * FlightGlobals.getGeeForceAtPosition(vessel.transform.position).magnitude);

            foreach (Part isItBuoyant in this.vessel.parts)
            {
                /*
                // Testing buoyancy
                if (Planetarium.GetUniversalTime() - Math.Floor(Planetarium.GetUniversalTime()) < 0.1)
                {
                    Debug.Log(isItBuoyant.name + " buoyancy is " + isItBuoyant.buoyancy);
                    Debug.Log(isItBuoyant.name + " buoyant force is " + isItBuoyant.partBuoyancy.buoyancyForce);
                    Debug.Log(isItBuoyant.name + " crew capactiy is " + isItBuoyant.CrewCapacity);
                }
                 * */

                // Do not affect ballast tanks
                if (isItBuoyant.Modules.OfType<HLBallastPartModule>().FirstOrDefault() == null)
                    return;

                HLBallastPartModule thisBuoyant = isItBuoyant.Modules.OfType<HLBallastPartModule>().FirstOrDefault();

                thisBuoyant.totalMass = this.totalMass;
                thisBuoyant.totalGravityForce = this.totalGravityForce;

                this.vesselNetForceTotal += thisBuoyant.netForceMagnitude;
                this.vesselNetForceMax += thisBuoyant.netForceMagnitudeMax;
                if (canControl == 0) this.vesselNetForceUnchanging += thisBuoyant.netForceMagnitude;

                /*
                if (thisBuoyant.netForceMagnitude > 0)
                {
                    Debug.Log("Found net force of " + netForceMagnitude);
                    Debug.Log("this.vesselNetForceTotal is now " + this.vesselNetForceTotal);
                }
                */
            }

            /*
            if (this.vesselNetForceTotal > 0)
            {
                Debug.Log("this.vesselNetForceTotal final total is " + this.vesselNetForceTotal);
            }
             * */
        }

        public void setBallast()
        {
            float sumPitch = 0;

            // Find out the sum of all the pitches
            foreach (HLBallastPartModule Ballast in Ballasts)
            {
                sumPitch += Ballast.targetPitchBallast;
            }

            // The lead Ballast sets the Ballast for every Ballast, including itself
            foreach (HLBallastPartModule Ballast in Ballasts)
            {
                Ballast.targetBallastVessel = this.targetBallastVessel;

                Mathf.Clamp01(Ballast.targetPitchBallast);

                // Use total
                Ballast.updateTargetSpecificVolumeFraction(Mathf.Clamp01(Ballast.targetPitchBallast + targetBallastVessel), Ballast);
            }
        }

        private void persistentParts()
        {
            // this.vessel.SetWorldVelocity(new Vector3d(0,0,0));

            if (this.vessel.srf_velocity.magnitude > makeStationarySpeedClamp)
            {
                this.vessel.SetWorldVelocity(vessel.srf_velocity.normalized * makeStationarySpeedClamp * 0.9);
            }
        }

        // GUI
        private void WindowGUI(int windowID)
        {
            float targetBoyantForceFractionCompressorTemp;
            targetBoyantForceFractionCompressorTemp = targetBallastVessel;

            float targetVerticalVelocityTemp;
            targetVerticalVelocityTemp = targetVerticalVelocity;

            #region General GUI
            // General GUI window information
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.name = "Ballast GUI";
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(2, 2, 2, 2);

            // Ballast -, current %, and + buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("-", mySty))
            {
                targetBallastVessel -= 0.002f;
                toggleDepthControl = false;
            }
            targetBallastVessel = Mathf.Clamp01(targetBallastVessel);

            GUILayout.Label("" + Mathf.RoundToInt(targetBallastVessel * 100) + "%");

            if (GUILayout.RepeatButton("+", mySty))
            {
                targetBallastVessel += 0.002f;
                toggleDepthControl = false;
            }
            GUILayout.EndHorizontal();

            // Slider control.  Also is set by the other controls.
            GUILayout.BeginHorizontal();
            float temp = targetBallastVessel;
            targetBallastVessel = GUILayout.HorizontalSlider(targetBallastVessel, 0f, 1f);
            if (temp != targetBallastVessel)
            {
                toggleDepthControl = false;
            }
            GUILayout.EndHorizontal();

            targetBallastVessel = Mathf.Clamp01(targetBallastVessel);
            #endregion

            #region Toggle Depth
            // Depth control.  Should be deactivated when pressing any other unrelated control.
            GUILayout.BeginHorizontal();
            string toggleDepthControlString = "Depth Control Off";
            if (toggleDepthControl) toggleDepthControlString = "Depth Control On";
            toggleDepthControl = GUILayout.Toggle(toggleDepthControl, toggleDepthControlString);
            GUILayout.EndHorizontal();
            #endregion

            if (toggleDepthControl)
            {
                #region Depth Control
                willReset1 = true;

                // Vertical Velocity -, target velocity, and + buttons
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target Vertical Velocity");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.RepeatButton("--", mySty)) targetVerticalVelocity -= 0.1f;
                if (GUILayout.Button("-", mySty)) targetVerticalVelocity -= 0.1f;
                if (GUILayout.Button(targetVerticalVelocity.ToString("00.0") + " m/s", mySty)) targetVerticalVelocity = 0;
                if (GUILayout.Button("+", mySty)) targetVerticalVelocity += 0.1f;
                if (GUILayout.RepeatButton("++", mySty)) targetVerticalVelocity += 0.1f;
                GUILayout.EndHorizontal();
                #endregion

                #region Persistence
                // Allows "landing" (persistence) by slowing the vehicle, as long as the this.vessel is "splashed"
                if (partAltitude < -buoyancyBottom)
                {
                    if (Mathf.Abs((int)vessel.horizontalSrfSpeed) < makeStationarySpeedMax && Mathf.Abs((int)vessel.verticalSpeed) < makeStationarySpeedMax && (vessel.Landed || vessel.Splashed))
                    {
                        GUILayout.BeginHorizontal();
                        togglePersistenceControl = GUILayout.Toggle(togglePersistenceControl, "Make Slow to Save", mySty);
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        if (this.vessel.Landed || this.vessel.Splashed)
                        {
                            GUILayout.Label("Reduce Speed to Save");
                        }
                        /*
                    else if (maxBallast.magnitude > 0)
                    {
                        GUILayout.Label("Touch Ground to Save");
                        togglePersistenceControl = false;
                    }
                         */
                        GUILayout.EndHorizontal();
                    }
                }
                #endregion
            }
            else
            {
                targetVerticalVelocity = 0;
                if (willReset1)
                {
                    resetGUIsize = true;
                    willReset1 = false;
                }
            }

            if (resetGUIsize)
            {
                // Reset window size
                windowPos.Set(windowPos.x, windowPos.y, 10, 10);
                resetGUIsize = false;
            }

            #region Debug
            // Debug info

            // if (Mathf.Abs(targetBoyantForceFractionCompressorTemp - targetBoyantForceFractionCompressor) > 0.002)
            // debugHLSubmarine("targetBoyantForceFractionCompressor", targetBoyantForceFractionCompressor.ToString("0.000"));

            // if (targetVerticalVelocityTemp != targetVerticalVelocity)
            // debugHLSubmarine("targetVerticalVelocityTemp", targetVerticalVelocity.ToString("00.0"));

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Ballast: " + totalBallast.ToString("0.0"));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Gravity: " + totalGravityForce.ToString("0.0"));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Buoyancy - Weight: " + (vesselNetForceTotal - totalGravityForce).ToString("0.00"));
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Angle from Up: " + (ContAngle(heading, up, up)).ToString("0.0"));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Front Torque: " + (totalTorqueP).ToString("0.0"));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Rear Torque: " + (totalTorqueN).ToString("0.0"));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Front B: " + (targetBallastP).ToString("0.00"));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Rear B: " + (targetBallastN).ToString("0.00"));
            //GUILayout.EndHorizontal();

            //int x = 0;
            //foreach (HLBallastPart Ballast in Ballasts)
            //{
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Env" + x + " Location: " + (Ballast.eDistanceFromCoM).ToString("0.00"));
            //    GUILayout.EndHorizontal();
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Env" + x + " Ballast: " + (Ballast.buoyantForce.magnitude).ToString("0.00"));
            //    GUILayout.EndHorizontal();
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Env" + x + " Specific Volume: " + (Ballast.specificVolumeFractionBallast).ToString("0.00"));
            //    GUILayout.EndHorizontal();
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Env" + x + " targetPitchBallast: " + (Ballast.targetPitchBallast).ToString("0.00"));
            //    GUILayout.EndHorizontal();
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label("Env" + x + " targetBoyantForceFractionCompressor: " + (Ballast.targetBoyantForceFractionCompressor).ToString("0.00"));
            //    GUILayout.EndHorizontal();

            //    x += 1;
            //}

            GUI.DragWindow(new Rect(0, 0, 500, 20));
            #endregion
        }

        // Changes Ballast targets
        public void updateTargetSpecificVolumeFraction(float fraction, HLBallastPartModule ballast)
        {
            // Difference between specific volume and target specific volume
            float delta = fraction - specificVolumeFractionBallast;

            // Clamp rates.  If the ballast is not in water then it cannot gain ballast.
            if (ballast.splashed) delta = Mathf.Clamp(delta, -airRate * Time.deltaTime, ballastRate * Time.deltaTime);
            else delta = Mathf.Clamp(delta, -airRate * Time.deltaTime, 0);

            // Add or remove that much ballast
            ballast.specificVolumeFractionBallast += delta;
            ballast.specificVolumeFractionBuoyant = 1 - ballast.specificVolumeFractionBallast;

            // Also, update net force
            ballast.vesselNetForceTotal = leadBallast.vesselNetForceTotal;
        }

        public void BallastUpdate()
        {
            // Density of exterior liquid
            try { findLiquidDensity(); }
            catch (Exception ex) { print("liquidDensity Exception!"); print(ex.Message); }

            // Resulting force is net between upward and downward force
            // Note that the "GeeForce" is gravity, which points downward.  A large buoyant area will cause lift

            // For depth control, maximum possible lift
            netForceMagnitudeMax = (BallastVolume * liquidDensity * (float)FlightGlobals.getGeeForceAtPosition(part.WCoM).magnitude);

            Vector3 netForce = (specificVolumeFractionBallast - specificVolumeFractionBuoyant) * BallastVolume * liquidDensity * FlightGlobals.getGeeForceAtPosition(part.WCoM);

            // Scale the amount of force based on how submerged
            if (partAltitude > buoyancyVerticalOffset && partAltitude < buoyancyTop)
                netForce = netForce * 0.5f * ((buoyancyTop + buoyancyVerticalOffset - partAltitude) / (buoyancyTop - buoyancyVerticalOffset));
            if (partAltitude > buoyancyBottom && partAltitude < buoyancyVerticalOffset)
                netForce = netForce * 0.5f * (1 + ((partAltitude) / (buoyancyBottom - buoyancyVerticalOffset)));
            part.Rigidbody.AddForceAtPosition(netForce, part.WCoM);

            // How much force, and in what direction? Say positive up
            netForceMagnitude = Vector3.Dot(netForce, -1 * FlightGlobals.getGeeForceAtPosition(part.WCoM).normalized);

        }

        //More GUI stuff
        private void drawGUI()
        {
            if (this.vessel != FlightGlobals.ActiveVessel)
                return;

            GUI.skin = HighLogic.Skin;
            SubmarineWindowID = (int)part.GetInstanceID();
            windowPos = GUILayout.Window(SubmarineWindowID, windowPos, WindowGUI, "Ballast Control", GUILayout.MinWidth(200));
        }

        protected void initGUI()
        {
            windowWidth = 300;

            if ((windowPos.x == 0) && (windowPos.y == 0))
            {
                windowPos = new Rect(Screen.width - windowWidth, Screen.height * 0.35f, 10, 10);
            }
        }

        private void addGUI()
        {
            if (!activeGUI && guiOn)
            {
                initGUI();
                RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI)); //start the GUI

                foreach (HLBallastPartModule Ballast in Ballasts)
                    Ballast.activeGUI = true;
            }

        }

        protected void removeGUI()
        {
            if (activeGUI)
            {
                RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI
                foreach (HLBallastPartModule Ballast in Ballasts)
                    Ballast.activeGUI = false;
            }
        }

        // This was recommended by Kreuzung
        public void OnCenterOfLiftQuery(CenterOfLiftQuery q)
        {
            q.dir = Vector3.up;
            q.lift = (float)BallastVolume;
            q.pos = transform.position;
        }
    }
}