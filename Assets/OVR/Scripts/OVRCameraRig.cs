/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VR = UnityEngine.VR;

/// <summary>
/// A head-tracked stereoscopic virtual reality camera rig.
/// </summary>
[ExecuteInEditMode]
public class OVRCameraRig : MonoBehaviour
{
	/// <summary>
	/// The left eye camera.
	/// </summary>
	public Camera leftEyeCamera { get; private set; }
	/// <summary>
	/// The right eye camera.
	/// </summary>
	public Camera rightEyeCamera { get; private set; }
	/// <summary>
	/// Provides a root transform for all anchors in tracking space.
	/// </summary>
	public Transform trackingSpace { get; private set; }
	/// <summary>
	/// Always coincides with the pose of the left eye.
	/// </summary>
	public Transform leftEyeAnchor { get; private set; }
	/// <summary>
	/// Always coincides with average of the left and right eye poses.
	/// </summary>
	public Transform centerEyeAnchor { get; private set; }
	/// <summary>
	/// Always coincides with the pose of the right eye.
	/// </summary>
	public Transform rightEyeAnchor { get; private set; }
	/// <summary>
	/// Always coincides with the pose of the tracker.
	/// </summary>
	public Transform trackerAnchor { get; private set; }
	/// <summary>
	/// Occurs when the eye pose anchors have been set.
	/// </summary>
	public event System.Action<OVRCameraRig> UpdatedAnchors;

	private readonly string trackingSpaceName = "TrackingSpace";
	private readonly string trackerAnchorName = "TrackerAnchor";
	private readonly string eyeAnchorName = "EyeAnchor";
	private readonly string legacyEyeAnchorName = "Camera";

#if UNITY_ANDROID && !UNITY_EDITOR
    bool correctedTrackingSpace = false;
#endif

#region Unity Messages
	private void Awake()
	{
		EnsureGameObjectIntegrity();
	}

	private void Start()
	{
		EnsureGameObjectIntegrity();

		if (!Application.isPlaying)
			return;

		UpdateAnchors();
	}

	private void Update()
	{
		EnsureGameObjectIntegrity();
		
		if (!Application.isPlaying)
			return;

		UpdateAnchors();

#if UNITY_ANDROID && !UNITY_EDITOR

        if (!correctedTrackingSpace)
        {
            //HACK: Unity 5.1.1p3 double-counts the head model on Android. Subtract it off in the reference frame.

            var headModel = new Vector3(0f, OVRManager.profile.eyeHeight - OVRManager.profile.neckHeight, OVRManager.profile.eyeDepth);
            var eyePos = -headModel + centerEyeAnchor.localRotation * headModel;

            if ((eyePos - centerEyeAnchor.localPosition).magnitude > 0.01f)
            {
                trackingSpace.localPosition = trackingSpace.localPosition - 2f * (trackingSpace.localRotation * headModel);
                correctedTrackingSpace = true;
            }
        }
#endif
	}

#endregion

	private void UpdateAnchors()
	{
		bool monoscopic = OVRManager.instance.monoscopic;

		OVRPose tracker = OVRManager.tracker.GetPose(0d);

		trackerAnchor.localRotation = tracker.orientation;
		centerEyeAnchor.localRotation = VR.InputTracking.GetLocalRotation(VR.VRNode.CenterEye);
        leftEyeAnchor.localRotation = monoscopic ? centerEyeAnchor.localRotation : VR.InputTracking.GetLocalRotation(VR.VRNode.LeftEye);
		rightEyeAnchor.localRotation = monoscopic ? centerEyeAnchor.localRotation : VR.InputTracking.GetLocalRotation(VR.VRNode.RightEye);

		trackerAnchor.localPosition = tracker.position;
		centerEyeAnchor.localPosition = VR.InputTracking.GetLocalPosition(VR.VRNode.CenterEye);
		leftEyeAnchor.localPosition = monoscopic ? centerEyeAnchor.localPosition : VR.InputTracking.GetLocalPosition(VR.VRNode.LeftEye);
		rightEyeAnchor.localPosition = monoscopic ? centerEyeAnchor.localPosition : VR.InputTracking.GetLocalPosition(VR.VRNode.RightEye);

		if (UpdatedAnchors != null)
		{
			UpdatedAnchors(this);
		}
	}

	public void EnsureGameObjectIntegrity()
	{
		if (trackingSpace == null)
			trackingSpace = ConfigureRootAnchor(trackingSpaceName);

		if (leftEyeAnchor == null)
            leftEyeAnchor = ConfigureEyeAnchor(trackingSpace, VR.VRNode.LeftEye);

		if (centerEyeAnchor == null)
            centerEyeAnchor = ConfigureEyeAnchor(trackingSpace, VR.VRNode.CenterEye);

		if (rightEyeAnchor == null)
            rightEyeAnchor = ConfigureEyeAnchor(trackingSpace, VR.VRNode.RightEye);

		if (trackerAnchor == null)
			trackerAnchor = ConfigureTrackerAnchor(trackingSpace);

        bool needsCamera = (leftEyeCamera == null || rightEyeCamera == null);

		if (needsCamera)
		{
            leftEyeCamera = centerEyeAnchor.GetComponent<Camera>();
			if (leftEyeCamera == null)
			{
				leftEyeCamera = centerEyeAnchor.gameObject.AddComponent<Camera>();
			}

            rightEyeCamera = leftEyeCamera;
		}
		
		// Only the center eye camera should now render.

        int cameraCount = 0;
        int mainCount = 0;
		
		foreach (var c in gameObject.GetComponentsInChildren<Camera>().Where(v => v != leftEyeCamera))
		{
			if (c && c.enabled)
			{
				Debug.LogWarning("Having a Camera on " + c.name + " is deprecated. Disabling the Camera. Please use the Camera on " + leftEyeCamera.name + " instead.");
				c.enabled = false;

				if (c.CompareTag("MainCamera"))
					mainCount++;
			}
        }

        // Use "MainCamera" unless there were previously cameras and they didn't use it.
        if (needsCamera && (cameraCount == 0 || mainCount != 0))
            leftEyeCamera.tag = "MainCamera";
	}

	private Transform ConfigureRootAnchor(string name)
	{
		Transform root = transform.Find(name);

		if (root == null)
		{
			root = new GameObject(name).transform;
		}

		root.parent = transform;
		root.localScale = Vector3.one;
		root.localPosition = Vector3.zero;
		root.localRotation = Quaternion.identity;

		return root;
	}

	private Transform ConfigureEyeAnchor(Transform root, VR.VRNode eye)
	{
		string eyeName = (eye == VR.VRNode.CenterEye) ? "Center" : (eye == VR.VRNode.LeftEye) ? "Left" : "Right";
		string name = eyeName + eyeAnchorName;
		Transform anchor = transform.Find(root.name + "/" + name);

		if (anchor == null)
		{
			anchor = transform.Find(name);
		}

		if (anchor == null)
		{
			string legacyName = legacyEyeAnchorName + eye.ToString();
			anchor = transform.Find(legacyName);
		}

		if (anchor == null)
		{
			anchor = new GameObject(name).transform;
		}

		anchor.name = name;
		anchor.parent = root;
		anchor.localScale = Vector3.one;
		anchor.localPosition = Vector3.zero;
		anchor.localRotation = Quaternion.identity;

		return anchor;
	}

	private Transform ConfigureTrackerAnchor(Transform root)
	{
		string name = trackerAnchorName;
		Transform anchor = transform.Find(root.name + "/" + name);

		if (anchor == null)
		{
			anchor = new GameObject(name).transform;
		}

		anchor.parent = root;
		anchor.localScale = Vector3.one;
		anchor.localPosition = Vector3.zero;
		anchor.localRotation = Quaternion.identity;

		return anchor;
	}
}
