using System;

using UnityEngine;

using OpenBodyCams.Compatibility;

namespace OpenBodyCams
{
    public static class ShipObjects
    {
        public static Material blackScreenMaterial;

        public static ManualCameraRenderer ShipCameraRenderer;

        public static Terminal TerminalScript;
        public static bool TwoRadarCamsPresent = false;

        public static void EarlyInitialization()
        {
            blackScreenMaterial = StartOfRound.Instance.mapScreen.offScreenMat;

            GetAndMaybeDisableShipCamera();
        }

        static void GetAndMaybeDisableShipCamera()
        {
            var shipCameraObject = GameObject.Find("Environment/HangarShip/Cameras/ShipCamera");
            if (shipCameraObject == null)
            {
                Plugin.Instance.Logger.LogError("Could not find the internal ship camera object.");
                return;
            }

            ShipCameraRenderer = shipCameraObject.GetComponent<ManualCameraRenderer>();
            if (ShipCameraRenderer?.mesh == null)
            {
                Plugin.Instance.Logger.LogError("Internal ship camera does not have a camera renderer.");
                return;
            }

            if (!Plugin.DisableInternalShipCamera.Value)
                return;

            var shipScreenMaterialIndex = Array.FindIndex(ShipCameraRenderer.mesh.sharedMaterials, material => material.name.StartsWith("ShipScreen1Mat"));

            if (blackScreenMaterial == null || shipScreenMaterialIndex == -1)
            {
                Plugin.Instance.Logger.LogError("Internal ship camera monitor does not have the expected materials.");
                return;
            }

            shipCameraObject.SetActive(false);

            var newMaterials = ShipCameraRenderer.mesh.sharedMaterials;
            newMaterials[shipScreenMaterialIndex] = blackScreenMaterial;
            ShipCameraRenderer.mesh.sharedMaterials = newMaterials;
        }

        public static void LateInitialization()
        {
            TerminalScript = UnityEngine.Object.FindObjectOfType<Terminal>();
            TwoRadarCamsPresent = TerminalScript.GetComponent<ManualCameraRenderer>() != null;

            InitializeBodyCam();

            // Prevent the radar from targeting a nonexistent player at the start of a round:
            if (!TwoRadarCamsPresent && StartOfRound.Instance.IsServer)
            {
                var mainMap = StartOfRound.Instance.mapScreen;
                mainMap.SwitchRadarTargetAndSync(Math.Min(mainMap.targetTransformIndex, mainMap.radarTargets.Count - 1));
            }
        }

        static void InitializeBodyCam()
        {
            var bottomMonitors = GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube.001");
            if (bottomMonitors == null)
            {
                Plugin.Instance.Logger.LogError("Could not find the bottom monitors' game object.");
                return;
            }

            if (bottomMonitors.GetComponent<BodyCamComponent>() == null)
            {
                var bodyCam = bottomMonitors.AddComponent<BodyCamComponent>();

                var renderer = bottomMonitors.GetComponent<MeshRenderer>();
                var materialIndex = 2;

                // GeneralImprovements BetterMonitors enabled:
                int monitorID = Plugin.GeneralImprovementsBetterMonitorIndex.Value - 1;
                if (monitorID < 0)
                    monitorID = 13;
                if (GeneralImprovementsCompatibility.GetMonitorForID(monitorID) is MeshRenderer giMonitorRenderer)
                {
                    renderer = giMonitorRenderer;
                    materialIndex = 0;
                }

                if (renderer == null)
                {
                    Plugin.Instance.Logger.LogError("Failed to find the monitor renderer.");
                    return;
                }

                bodyCam.monitorRenderer = renderer;
                bodyCam.monitorMaterialIndex = materialIndex; ;
                bodyCam.enabled = true;
            }
        }
    }
}