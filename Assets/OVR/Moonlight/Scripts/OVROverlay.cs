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
using System;
using System.Collections;
using System.Runtime.InteropServices;
using VR = UnityEngine.VR;

/// <summary>
/// Add OVROverlay script to an object with a Quad mesh filter to have the quad
/// rendered as a TimeWarp overlay instead by drawing it into the eye buffer.
/// This will take full advantage of the display resolution and avoid double
/// resampling of the texture.
/// 
/// If the texture is dynamically generated, as for an interactive GUI or
/// animation, it must be explicitly triple buffered to avoid flickering
/// when it is referenced asynchronously by TimeWarp.
/// </summary>
public class OVROverlay : MonoBehaviour
{
	public enum OverlayType
	{
		None,			// Disabled the overlay
		Underlay,		// Eye buffers blend on top
		Overlay,		// Blends on top of the eye buffer
		OverlayShowLod	// Blends on top and colorizes texture level of detail
	};

	OverlayType		currentOverlayType = OverlayType.Overlay;
#if !UNITY_ANDROID || UNITY_EDITOR
    Texture         texture;
#endif
	IntPtr 			texId = IntPtr.Zero;

	void Awake()
	{
		Debug.Log ("Overlay Awake");

		// Getting the NativeTextureID/PTR synchronizes with the multithreaded renderer, which
		// causes a problem on the first frame if this gets called after the OVRDisplay initialization,
		// so do it in Awake() instead of Start().
		texId = this.GetComponent<Renderer>().material.mainTexture.GetNativeTexturePtr();
#if !UNITY_ANDROID || UNITY_EDITOR
        texture = this.GetComponent<Renderer>().material.mainTexture;
#endif
    }

	void Update()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        if (this.GetComponent<Renderer>().material.mainTexture != texture)
        {
            texId = this.GetComponent<Renderer>().material.mainTexture.GetNativeTexturePtr();
            texture = this.GetComponent<Renderer>().material.mainTexture;
        }
#endif

		if (Input.GetKey (KeyCode.Joystick1Button0))
		{
			currentOverlayType = OverlayType.None;
		}
		else if (Input.GetKey (KeyCode.Joystick1Button1))
		{
			currentOverlayType = OverlayType.OverlayShowLod;
		}
		else
		{
			currentOverlayType = OverlayType.Overlay;
		}
	}

	void OnRenderObject ()
	{
		// The overlay must be specified every eye frame, because it is positioned relative to the
		// current head location.  If frames are dropped, it will be time warped appropriately,
		// just like the eye buffers.

		if (currentOverlayType == OverlayType.None)
		{
			GetComponent<Renderer>().enabled = true;	// use normal renderer
			return;
		}

		bool overlay = (currentOverlayType == OverlayType.Overlay);

		Transform camPose = Camera.current.transform;
		Matrix4x4 modelToCamera = camPose.worldToLocalMatrix * transform.localToWorldMatrix;

		Vector3 headPos = VR.InputTracking.GetLocalPosition(VR.VRNode.Head);
		Quaternion headOrt = VR.InputTracking.GetLocalRotation(VR.VRNode.Head);
		Matrix4x4 cameraToStart = Matrix4x4.TRS(headPos, headOrt, Vector3.one);

		Matrix4x4 modelToStart = cameraToStart * modelToCamera;

		OVRPose pose;
		pose.position = modelToStart.GetColumn(3);
		pose.orientation = Quaternion.LookRotation(modelToStart.GetColumn(2), modelToStart.GetColumn(1));

        // Convert left-handed to right-handed.
        pose.position.z = -pose.position.z;
        pose.orientation.w = -pose.orientation.w;

		Vector3 scale = transform.lossyScale;
        for (int i = 0; i < 3; ++i)
            scale[i] /= Camera.current.transform.lossyScale[i];

		OVRPlugin.Bool result = OVRPlugin.SetOverlayQuad(overlay.ToBool(), texId, IntPtr.Zero, pose.ToPosef(), scale.ToVector3f());

		GetComponent<Renderer>().enabled = (result == OVRPlugin.Bool.False);		// render with the overlay plane instead of the normal renderer
	}
	
}
