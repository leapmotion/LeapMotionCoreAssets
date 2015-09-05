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

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Miscellaneous extension methods that any script can use.
/// </summary>
public static class OVRExtensions
{
	internal static OVRPose ToOVRPose(this OVRPlugin.Posef p)
	{
		return new OVRPose()
		{
			position = new Vector3(p.Position.x, p.Position.y, -p.Position.z),
			orientation = new Quaternion(-p.Orientation.x, -p.Orientation.y, p.Orientation.z, p.Orientation.w)
		};
	}
	
	internal static OVRTracker.Frustum ToFrustum(this OVRPlugin.Frustumf f)
	{
		return new OVRTracker.Frustum()
		{
			nearZ = f.zNear,
			farZ = f.zFar,
			
			fov = new Vector2()
			{
				x = Mathf.Rad2Deg * f.fovX,
				y = Mathf.Rad2Deg * f.fovY
			}
		};
	}

	internal static OVRPlugin.Vector3f ToVector3f(this Vector3 v)
	{
		return new OVRPlugin.Vector3f() { x = v.x, y = v.y, z = v.z };
	}

	internal static OVRPlugin.Quatf ToQuatf(this Quaternion q)
	{
		return new OVRPlugin.Quatf() { x = q.x, y = q.y, z = q.z, w = q.w };
	}

	internal static OVRPlugin.Bool ToBool(this bool b)
	{
		return (b) ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
	}
}

/// <summary>
/// An affine transformation built from a Unity position and orientation.
/// </summary>
public struct OVRPose
{
	/// <summary>
	/// A pose with no translation or rotation.
	/// </summary>
	public static OVRPose identity
	{
		get {
			return new OVRPose()
			{
				position = Vector3.zero,
				orientation = Quaternion.identity
			};
		}
	}

	/// <summary>
	/// The position.
	/// </summary>
	public Vector3 position;

	/// <summary>
	/// The orientation.
	/// </summary>
	public Quaternion orientation;

	internal OVRPlugin.Posef ToPosef()
	{
		return new OVRPlugin.Posef()
		{
			Position = position.ToVector3f(),
			Orientation = orientation.ToQuatf()
		};
	}
}
