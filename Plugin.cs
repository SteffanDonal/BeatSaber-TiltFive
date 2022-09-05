using HarmonyLib;
using IPA;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using TiltFive.Logging;
using UnityEngine;
using UnityEngine.XR;
using IPALogger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

[assembly: AssemblyTitle("TiltFive")]
[assembly: AssemblyFileVersion("0.0.1")]
[assembly: AssemblyCopyright("MIT License - Copyright © 2022 Steffan Donal")]

[assembly: Guid("d2103150-3ad9-472b-a4d4-a356fffd664e")]

namespace TiltFive
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static readonly string Name = Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        public static readonly string Version = Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        public static IPALogger Log { get; internal set; }


        static bool _isInitialized;

        [Init]
        public void Init(object _, IPALogger log)
        {
            Log = log;

            // Disable Beat Saber's VR support.
            XRSettings.enabled = false;
            XRSettings.LoadDeviceByName("");
        }

        [OnStart]
        public void OnStart()
        {
            if (_isInitialized)
                throw new InvalidOperationException($"Plugin had {nameof(OnStart)} called more than once! Critical failure.");

            _isInitialized = true;

            try
            {
                var harmony = new Harmony("com.github.steffandonal.beatsaber-tiltfive");
                harmony.PatchAll(Assembly);
            }
            catch (Exception e)
            {
                Log.Error("This plugin requires Harmony. Make sure you installed the plugin properly, as the Harmony DLL should have been installed with it.");
                Log.Error(e.ToString());

                return;
            }

            Log.Info($"v{Version} ready!");

            // The main work for Tilt Five setup is triggered by an internal Beat Saber method that's called when the main camera is
            // ready for use. See "InitOnMainAvailable.cs" for details!
        }

        #region TiltFive-Specific Gubbins

        /// <summary>
        /// Hardcoded version string so that the SDK connection works without needing to load a version asset.
        /// </summary>
        public static readonly string TiltFiveVersion = @"1.2.1 Huggable Hellhound";

        /// <summary>
        /// Contains all shaders needed for Tilt Five to work correctly.
        /// Indexed by the shader's full name.
        /// </summary>
        public static readonly Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();

        public static GameObject TiltFiveWandOne;
        public static GameObject TiltFiveHead;
        public static GameObject TiltFiveBoard;

        public static void SetupEverything()
        {
            Log.Info("Loading Tilt Five resources...");
            LoadTiltFive();

            Log.Info("Beginning setup.");
            CreateTiltFive();
        }

        static void LoadTiltFive()
        {
            using (var stream = Assembly.GetManifestResourceStream("TiltFive.TiltFiveResources"))
            {
                var shaders = AssetBundle.LoadFromStream(stream).LoadAllAssets<Shader>();

                foreach (var shader in shaders)
                {
                    Shaders[shader.name] = shader;
                    Log.Info($"Loaded shader: {shader.name}");
                }
            }
        }

        static void CreateTiltFive()
        {
            var managerObject = new GameObject("TiltFiveManager");
            var boardObject = new GameObject("TiltFiveBoard");
            var wandOneObject = new GameObject("TiltFiveWandOne");

            boardObject.transform.parent = managerObject.transform;
            wandOneObject.transform.parent = managerObject.transform;

            managerObject.SetActive(false);

            Log.Debug("Cloning main camera, then cleaning it up...");

            var cameraClone = Object.Instantiate(FindMainCamera(), Vector3.zero, Quaternion.identity, managerObject.transform);
            cameraClone.name = "TiltFiveCamera";

            var camera = cameraClone.GetComponent<Camera>();
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.tag = "Untagged";
            camera.depthTextureMode = DepthTextureMode.Depth;

            Log.Debug("Cleaning up children...");

            foreach (Transform child in cameraClone.transform)
                Object.Destroy(child.gameObject);

            Log.Debug("Cleaning up components...");

            foreach (var component in cameraClone.GetComponents<Behaviour>())
                if (MainCameraBehavioursToDestroy.Contains(component.GetType().Name)) Object.DestroyImmediate(component);

            var board = boardObject.AddComponent<GameBoard>();
            var manager = managerObject.AddComponent<TiltFiveManager>();

            Log.Debug("Setting Tilt Five SDK configuration...");

            manager.logSettings = new LogSettings();
            manager.secondaryWandSettings = new WandSettings();

            manager.glassesSettings = new GlassesSettings
            {
                headPoseCamera = camera,
                nearClipPlane = 0.3f,
                farClipPlane = 5000f,
                glassesMirrorMode = GlassesMirrorMode.None
            };

            manager.gameBoardSettings = new GameBoardSettings
            {
                currentGameBoard = board
            };

            manager.scaleSettings = new ScaleSettings
            {
                contentScaleRatio = 0.075f,
                contentScaleUnit = LengthUnit.Meters
            };

            manager.primaryWandSettings = new WandSettings
            {
                FailureMode = TrackableSettings.TrackingFailureMode.FreezePosition,
                RejectUntrackedPositionData = true,
                FingertipPoint = wandOneObject // Use the fingertip point for a more natural saber pivot.
            };

            // Set Beat Saber to be viewed from a reasonable distance away from the gameplay area,
            // tilted down so incoming objects can be seen well.
            // Also, the viewing position is rotated 45 degrees clockwise to enable a wider view of the game.
            boardObject.transform.SetLocalPositionAndRotation(
                new Vector3(0, 2.5f, -0.5f),
                Quaternion.AngleAxis(-35, Vector3.right) * Quaternion.AngleAxis(-45, Vector3.up)
            );

            Log.Debug("Ensuring Tilt Five will remain active throughout the play session...");

            Object.DontDestroyOnLoad(managerObject);

            Log.Info("Enabling Tilt Five!");

            managerObject.SetActive(true);

            TiltFiveWandOne = wandOneObject;
            TiltFiveHead = cameraClone;
            TiltFiveBoard = boardObject;
        }

        #endregion

        #region Beat Saber Hackery

        /// <summary>
        /// Dictionary of component names that need to be removed from Beat Saber's camera before it
        /// can be used with Tilt Five.
        /// </summary>
        static readonly HashSet<string> MainCameraBehavioursToDestroy = new HashSet<string> { "AudioListener", "LIV", "MainCamera", "MeshCollider" };

        /// <summary>
        /// Find's Beat Saber's main camera.
        /// </summary>
        /// <returns>The GameObject containing Beat Saber's main camera.</returns>
        public static GameObject FindMainCamera()
        {
            var mainCamera = Camera.main;

            return mainCamera != null
                ? mainCamera.gameObject
                : GameObject.FindGameObjectsWithTag("MainCamera")[0];
        }

        #endregion

    }
}
