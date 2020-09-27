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
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

[InitializeOnLoad]
class OVRMoonlightLoader
{
    static OVRMoonlightLoader()
	{
		var mgrs = GameObject.FindObjectsOfType<OVRManager>().Where(m => m.isActiveAndEnabled);

		EditorApplication.update += EnforceBundleId;

		if (mgrs.Count() != 0 && !PlayerSettings.virtualRealitySupported)
		{
			Debug.Log("Enabling Unity VR support");
			PlayerSettings.virtualRealitySupported = true;
		}

		if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
			return;

		if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft)
		{
			Debug.Log("MoonlightLoader: Setting orientation to Landscape Left");
			// Default screen orientation must be set to landscape left.
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
		}

		if (!PlayerSettings.virtualRealitySupported)
		{
			// NOTE: This value should not affect the main window surface
			// when Built-in VR support is enabled.

			// NOTE: On Adreno Lollipop, it is an error to have antiAliasing set on the
			// main window surface with front buffer rendering enabled. The view will
			// render black.
			// On Adreno KitKat, some tiling control modes will cause the view to render
			// black.
			if (QualitySettings.antiAliasing != 0 && QualitySettings.antiAliasing != 1)
			{
				Debug.Log("MoonlightLoader: Disabling antiAliasing");
				QualitySettings.antiAliasing = 1;
			}
		}

		if (QualitySettings.vSyncCount != 0)
		{
			Debug.Log("MoonlightLoader: Setting vsyncCount to 0");
			// We sync in the TimeWarp, so we don't want unity syncing elsewhere.
			QualitySettings.vSyncCount = 0;
		}
	}

	private static void EnforceBundleId()
	{
		if (!PlayerSettings.virtualRealitySupported)
			return;

		if (PlayerSettings.applicationIdentifier == "" || PlayerSettings.applicationIdentifier == "com.Company.ProductName")
		{
			string defaultBundleId = "com.oculus.UnitySample";
			Debug.LogWarning("\"" + PlayerSettings.applicationIdentifier + "\" is not a valid bundle identifier. Defaulting to \"" + defaultBundleId + "\".");
			PlayerSettings.applicationIdentifier = defaultBundleId;
		}
	}
}
