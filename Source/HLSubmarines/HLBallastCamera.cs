// HLBallastCamera
// Taken from KerbalTown by Razchek at his suggestion
// https://github.transform.position/HubsElectrical/KerbTown

using System;
using UnityEngine;

namespace HLBallastSpace
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class HLBallastCamera : MonoBehaviour
    {
        public float CameraDistance = 50.0f;
        private float _cameraX;
        private float _cameraY;

        public float cameraAltitude;
        public double crushDepth = -600; // = 60 atmospheres pressure underwater, and when KSP freaks out about depth
        public double warnDepth = -500;
        private double oldVesselDepth = 0;
        private float warnTimer = 0;
        
        private GameObject deepOcean;
        private GameObject deepOceanPivot;
        private double oceanDarkDepth = 100;

        // Rather than adding a script to the FlightCamera, we'll just control it natively from... the ballast
        private void LateUpdate()
        {
            try { cameraManualControl(); }
            catch (Exception ex) { print("cameraManualControl Exception!"); print(ex.Message); }

            try { crushingDepth(); }
            catch (Exception ex) { print("crushingDepth Exception!"); print(ex.Message); }

            // try { createDeepOceanPlane(); }
            // catch (Exception ex) { print("createDeepOceanPlane Exception!"); print(ex.Message); }

            // try { updateDeepOceanPlane(); }
            // catch (Exception ex) { print("updateDeepOceanPlane Exception!"); print(ex.Message); }

            // try { createDeepOceanSphere(); }
            // catch (Exception ex) { print("deepWaterSphere Exception!"); print(ex.Message); }

            // try { updateDeepOceanSphere(); }
            // catch (Exception ex) { print("deepWaterSphere Exception!"); print(ex.Message); }

            // try { testObject(); }
            // catch (Exception ex) { print("deepWaterSphere Exception!"); print(ex.Message); }

            // try { flipOcean(); }
            // catch (Exception ex) { print("flipOcean Exception!"); print(ex.Message); }
        }

        private void crushingDepth()
        {
            if (FlightGlobals.ActiveVessel == null) return;

            if (warnTimer > 0f) warnTimer -= Time.deltaTime;

            if (FlightGlobals.ActiveVessel.altitude < warnDepth && oldVesselDepth > warnDepth && warnTimer <= 0)
            {
                ScreenMessages.PostScreenMessage("Warning! Vessel will be crushed at " + (crushDepth * -1) + "m depth!", 3, ScreenMessageStyle.LOWER_CENTER);
                warnTimer = 5;
            }

            oldVesselDepth = FlightGlobals.ActiveVessel.altitude;

            foreach (Vessel crushableVessel in FlightGlobals.Vessels)
            {
                if (crushableVessel.loaded && crushableVessel.altitude < warnDepth)
                {
                    foreach (Part crushablePart in crushableVessel.parts)
                    {
                        double partAltitude = findAltitude(crushablePart.transform);
                        if (partAltitude < crushDepth)
                        {
                            GameEvents.onCrashSplashdown.Fire(new EventReport(FlightEvents.SPLASHDOWN_CRASH, crushablePart, crushablePart.partInfo.title, "ocean", 0, "HLBallastPartModule: Part was crushed under the weight of the ocean"));
                            crushablePart.explode();
                        }
                    }
                }
            }
        }

        private void cameraManualControl()
        {
            if (!UnderwaterCamera.ManualControl)
                return;

            _cameraX = 0;
            _cameraY = 0;

            if (Input.GetMouseButton(1))    // RMB
            {
                // Debug.Log("RMB Clicked");
                _cameraX = Input.GetAxis("Mouse X") * UnderwaterCamera.CameraSpeed;  // Horizontal
                _cameraY = Input.GetAxis("Mouse Y") * UnderwaterCamera.CameraSpeed;  // Vertical
            }

            if (GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)   // MMB
            {
                // Debug.Log("Middle Mouse Wheel Scrolled");
                CameraDistance =
                    Mathf.Clamp(
                        CameraDistance *
                        (1f - (GameSettings.AXIS_MOUSEWHEEL.GetAxis() * UnderwaterCamera.ActiveFlightCamera.zoomScaleFactor)),
                        UnderwaterCamera.ActiveFlightCamera.minDistance, UnderwaterCamera.ActiveFlightCamera.maxDistance);
            }

            UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveCameraPivot.transform.position).normalized, _cameraX);
            UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * UnderwaterCamera.ActiveFlightCamera.transform.right, _cameraY);

            UnderwaterCamera.ActiveCameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;
            
            UnderwaterCamera.ActiveFlightCamera.transform.LookAt(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);
        }

        private void createDeepOceanSphere()
        {
            // Don't want more than one deep ocean
            if (deepOcean != null) return;

            // Eve does not have a transparent ocean
            if (FlightGlobals.ActiveVessel.mainBody.bodyName == "Eve") return;

            // Create the sphere which will go under the world's ocean
            deepOcean = GameObject.CreatePrimitive(PrimitiveType.Sphere);

			// No collider
			if (deepOcean.GetComponent<Collider>() != null)
			{
				GameObject.Destroy(deepOcean.GetComponent<Collider>());
			}

            // Place the sphere under the ocean
            deepOcean.transform.position = FlightGlobals.ActiveVessel.mainBody.position;
            double deepOceanRadius = FlightGlobals.ActiveVessel.mainBody.Radius - oceanDarkDepth;
            deepOcean.transform.localScale = new Vector3d(deepOceanRadius, deepOceanRadius, deepOceanRadius);

            // Deep water is blue and not transparent at all, and does not receive or cast shadows
            Color deepWater = Color.blue;
			var renderObject = deepOcean.GetComponent<MeshRenderer>();
			if (renderObject != null)
			{
				renderObject.material.color = deepWater;
				renderObject.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				renderObject.receiveShadows = false;
			}
        }

        private void createDeepOceanPlane()
        {
            // Don't want more than one deep ocean
            if (deepOcean != null) return;

            // Eve does not have a transparent ocean
            if (FlightGlobals.ActiveVessel.mainBody.bodyName == "Eve") return;

            // Create the sphere which will go under the world's ocean
            deepOcean = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deepOceanPivot = new GameObject();

			// No collider
			if (deepOcean.GetComponent<Collider>() != null)
			{
				GameObject.Destroy(deepOcean.GetComponent<Collider>());
			}

			// Place the plane under the ocean
			deepOcean.transform.rotation.SetLookRotation(-1 * FlightGlobals.getGeeForceAtPosition(FlightGlobals.ActiveVessel.transform.position));
            deepOcean.transform.Rotate(Vector3.right, 90);
            deepOcean.transform.localScale = new Vector3(10000, 0, 10000);

            // Deep water is blue and not transparent at all, and does not receive or cast shadows
            Color deepWater = Color.blue;
			var renderObject = deepOcean.GetComponent<MeshRenderer>();
			if (renderObject != null)
			{
				renderObject.material.color = deepWater;
				renderObject.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				renderObject.receiveShadows = false;
			}
		}

		private static double findAltitude(Transform aLocation)
        {
            if (FlightGlobals.ActiveVessel == null) return 0;
            return Vector3.Distance(aLocation.position, FlightGlobals.ActiveVessel.mainBody.position) - (FlightGlobals.ActiveVessel.mainBody.Radius);
        }

        private void updateDeepOceanPlane()
        {
            double cameraAltitude = 0;
            if (UnderwaterCamera.ActiveFlightCamera != null) cameraAltitude = findAltitude(UnderwaterCamera.ActiveFlightCamera.transform);
            else cameraAltitude = findAltitude(FlightCamera.fetch.transform);
            
            if (deepOcean != null)
            {
                if (!FlightGlobals.ActiveVessel.mainBody.ocean)
                {
                    GameObject.Destroy(deepOcean);
                    return;
                }

                deepOcean.transform.position = FlightGlobals.ActiveVessel.transform.position - FlightGlobals.getGeeForceAtPosition(FlightGlobals.ActiveVessel.transform.position).normalized * ((float)FlightGlobals.ActiveVessel.altitude + oceanDarkDepth);
                deepOcean.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.position);
                deepOcean.transform.Rotate(deepOcean.transform.right,90);
            }
        }

        private void updateDeepOceanSphere()
        {
            if (deepOcean != null)
            {
                if (!FlightGlobals.ActiveVessel.mainBody.ocean) GameObject.Destroy(deepOcean);
                deepOcean.transform.position = FlightGlobals.ActiveVessel.mainBody.position;
            }
        }
    }

    public static class UnderwaterCamera
    {
        private static Transform _originalParentTransform;
        private static bool _manualControl;

        public static FlightCamera ActiveFlightCamera;
        public static GameObject ActiveCameraPivot;

        public static float CameraSpeed = 0f;
        public static float CameraSpeedMulti = 20f;

        public static bool ManualControl
        {
            set
            {
                if (value && ActiveFlightCamera == null)
                {
                    _manualControl = false;
                    Debug.Log("Tried to set manual camera control while FlightCamera.fetch was null.");
                    return;
                }
                _manualControl = value;
            }
            get { return _manualControl; }
        }

        public static void SetCameraParent()
        {
            // Assign FlightCamera instance to public var.
            ActiveFlightCamera = FlightCamera.fetch;

            // For replacing the camera when done editing.
            if (_originalParentTransform == null)
                _originalParentTransform = ActiveFlightCamera.transform.parent;

            // For translating the camera
            if (ActiveCameraPivot != null) GameObject.Destroy(ActiveCameraPivot);
            ActiveCameraPivot = new GameObject("KtCamPivot");
            // ActiveCameraPivot.transform.parent = parentTransform;
            // ActiveCameraPivot.transform.localPosition = Vector3.zero;
            // ActiveCameraPivot.transform.localRotation = Quaternion.identity;
            ActiveCameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;

            ActiveFlightCamera.transform.position = FlightCamera.fetch.transform.position;
            ActiveCameraPivot.transform.LookAt(ActiveFlightCamera.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);
            ActiveFlightCamera.transform.LookAt(ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);

            // Switch to active object.
            ActiveFlightCamera.transform.parent = ActiveCameraPivot.transform;

            // Use the FlightCamera sensitivity for the speed.
            CameraSpeed = ActiveFlightCamera.orbitSensitivity * CameraSpeedMulti;

            // Take control of the flight camera.
            // ActiveFlightCamera.DeactivateUpdate();

            // Is the camera pointing up?  Fix that
            /* // Unfortunately this code doesn't work
            if (Vector3.Dot(UnderwaterCamera.ActiveFlightCamera.transform.forward, (-1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveCameraPivot.transform.position).normalized)) > 0)
            {
                UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * UnderwaterCamera.ActiveFlightCamera.transform.right, 135);
            }
             */

            // Instruct LateUpdate that we're controlling the camera manually now.
            ManualControl = true;

            // Say something.
            Debug.Log("[HLBallast] FlightCamera switched to: " + FlightGlobals.ActiveVessel.name);
        }

        public static void RestoreCameraParent()
        {
            // Only execute if we're actually controlling the camera.
            if (!ManualControl) return;

            // Restore camera control to vessel.
            FlightCamera.fetch.transform.parent = _originalParentTransform;
            _originalParentTransform = null;

            ManualControl = false;

            // ActiveFlightCamera.ActivateUpdate();

            // Say something.
            Debug.Log("[HLBallast] FlightCamera restored to vessel.");
        }
    }
}