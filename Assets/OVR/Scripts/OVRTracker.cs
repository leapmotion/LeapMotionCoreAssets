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
using System.Runtime.InteropServices;
using UnityEngine;
using VR = UnityEngine.VR;

/// <summary>
/// An infrared camera that tracks the position of a head-mounted display.
/// </summary>
public class OVRTracker
{
	/// <summary>
	/// The (symmetric) visible area in front of the tracker.
	/// </summary>
	public struct Frustum
	{
		/// <summary>
		/// The tracker cannot track the HMD unless it is at least this far away.
		/// </summary>
		public float nearZ;
		/// <summary>
		/// The tracker cannot track the HMD unless it is at least this close.
		/// </summary>
		public float farZ;
		/// <summary>
		/// The tracker's horizontal and vertical fields of view in degrees.
		/// </summary>
		public Vector2 fov;
	}

	/// <summary>
	/// If true, a tracker is attached to the system.
	/// </summary>
	public bool isPresent
	{
		get {
			if (!VR.VRDevice.isPresent)
				return false;

			return OVRPlugin.positionSupported;
		}
	}

	/// <summary>
	/// If true, the tracker can see and track the HMD. Otherwise the HMD may be occluded or the system may be malfunctioning.
	/// </summary>
	public bool isPositionTracked
	{
		get {
			return OVRPlugin.positionTracked;
		}
	}

	/// <summary>
	/// If this is true and a tracker is available, the system will use position tracking when isPositionTracked is also true.
	/// </summary>
	public bool isEnabled
	{
		get {
			if (!VR.VRDevice.isPresent)
				return false;

			return OVRPlugin.position;
        }

		set {
			if (!VR.VRDevice.isPresent)
				return;

			OVRPlugin.position = value;
		}
	}

	/// <summary>
	/// Gets the tracker's viewing frustum.
	/// </summary>
	public Frustum frustum
	{
		get {
			if (!VR.VRDevice.isPresent)
				return new Frustum();

            return OVRPlugin.GetTrackerFrustum(OVRPlugin.Tracker.Default).ToFrustum();
		}
	}

	/// <summary>
	/// Gets the tracker's pose, relative to the head's pose at the time of the last pose recentering.
	/// </summary>
	public OVRPose GetPose(double predictionTime)
	{
		if (!VR.VRDevice.isPresent)
			return OVRPose.identity;

		var p = OVRPlugin.GetTrackerPose(OVRPlugin.Tracker.Default).ToOVRPose();
		
		return new OVRPose()
		{
			position = p.position,
			orientation = p.orientation * Quaternion.Euler(0, 180, 0)
		};
	}
}
