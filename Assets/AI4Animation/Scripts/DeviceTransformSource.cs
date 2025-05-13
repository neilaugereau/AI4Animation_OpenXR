using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using XRController = UnityEngine.InputSystem.XR.XRController;

public class DeviceTransformSource : MonoBehaviour
{
    [Header("OpenXR elements (Tracked Pose Driver Transforms)")]
    public Transform head;
    public Transform leftController;
    public Transform rightController;
}
