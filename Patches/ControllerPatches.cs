using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace TiltFive.Patches
{
    [HarmonyPatch(typeof(DevicelessVRHelper))]
    [HarmonyPatch(nameof(IVRPlatformHelper.GetNodePose))]
    internal class ControllerGetNodePosePatch
    {
        static bool Prefix(
            DevicelessVRHelper __instance, ref bool __result,
            XRNode nodeType, int idx, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            // Don't do anything if Tilt Five isn't setup.
            if (Plugin.TiltFiveWandOne is null) return true;

            // We only support the right hand.
            if (nodeType != XRNode.RightHand) return true;

            // Position based on difference between the board and wand position.
            // Moved 2.25 meters upward to make it more comfortable.
            pos = Plugin.TiltFiveWandOne.transform.localPosition - Plugin.TiltFiveBoard.transform.localPosition + Vector3.up * 2.25f;

            // Shrink the required hand movement by 20%
            pos *= 1.2f;

            // Lock the saber's Z value so that hit timing is predictable / easier.
            pos.z = -0.4f;


            var wandRotation = Quaternion.Euler(35, 0, 0) * Plugin.TiltFiveWandOne.transform.localRotation;
            var rotatedForward = wandRotation * Vector3.forward;

            var leftRightDir = Vector3.ProjectOnPlane(rotatedForward, Vector3.up);
            var yawAngle = Vector3.SignedAngle(Vector3.forward, leftRightDir, Vector3.up);

            var upDownDir = Vector3.ProjectOnPlane(rotatedForward, Vector3.right);
            var pitchAngle = Vector3.SignedAngle(Vector3.forward, upDownDir, Vector3.right);

            // Apply power curves to the pitch/yaw rotations from the wand to make "swinging" easier.
            // Basically required if you want to have a chance of actually passing a map in single saber mode 😁
            // Has a nasty side-effect of making rotation wig out when it reaches the limits, but this is just a proof-of-concept
            var saberRotation = Quaternion.Euler(
                Mathf.Sign(pitchAngle) * Mathf.Pow(Mathf.Abs(pitchAngle), 1.35f),
                Mathf.Sign(yawAngle) * Mathf.Pow(Mathf.Abs(yawAngle), 1.35f),
                0);

            rot = saberRotation;

            // Beat Saber expects a "true" result if the controller is tracking correctly.
            __result = true;

            // Returning false will skip execution of Beat Saber's own "GetNodePose" method.
            return false;
        }
    }

    [HarmonyPatch(typeof(DevicelessVRHelper))]
    [HarmonyPatch(nameof(IVRPlatformHelper.AdjustControllerTransform))]
    internal class ControllerAdjustControllerTransformPatch
    {
        static bool Prefix(DevicelessVRHelper __instance, XRNode node, Transform transform, Vector3 position, Vector3 rotation)
        {
            // Don't do anything if Tilt Five isn't setup.
            if (Plugin.TiltFiveWandOne is null) return true;

            // We only support the right hand.
            if (node != XRNode.RightHand) return true;

            transform.Rotate(rotation);
            transform.Translate(position);

            return false;
        }
    }
}
