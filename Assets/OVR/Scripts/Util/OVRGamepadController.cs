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

/// <summary>
/// OVRGamepadController is an interface class to a gamepad controller.
/// </summary>
public class OVRGamepadController : MonoBehaviour
{
	/// <summary> An axis on the gamepad. </summary>
	public enum Axis
	{
		None = -1,
		LeftXAxis = 0,
	   	LeftYAxis,
	   	RightXAxis,
	   	RightYAxis,
	   	LeftTrigger,
	   	RightTrigger,
        DPad_X_Axis,
        DPad_Y_Axis,
		Max,
	};

	/// <summary> A button on the gamepad. </summary>
	public enum Button
	{
		None = -1,
		A = 0,
	   	B,
	   	X,
	   	Y,
	   	Up,
	   	Down,
		Left,
		Right,
	   	Start,
	   	Back,
	   	LStick,
		RStick,
		LeftShoulder,
		RightShoulder,
		Max
	};

	/// <summary>
	/// The default Android Unity input name for each gamepad Axis.
	/// </summary>
	public static string[] AndroidAxisNames = new string[(int)Axis.Max]
	{
		"Left_X_Axis",
		"Left_Y_Axis",
		"Right_X_Axis",
		"Right_Y_Axis",
		"LeftTrigger",
		"RightTrigger",
        "DPad_X_Axis",
        "DPad_Y_Axis",
	};

	/// <summary>
	/// The default Android Unity input name for each gamepad Button.
	/// </summary>
    public static string[] AndroidButtonNames = new string[(int)Button.Max]
	{
		"Button A",
		"Button B",
		"Button X",
		"Button Y",
		"Up",
		"Down",
		"Left",
		"Right",
		"Start",
		"Back",
		"LStick",
		"RStick",
		"LeftShoulder",
		"RightShoulder",
	};

	/// <summary>
	/// The default Unity input name for each gamepad Axis.
	/// </summary>
	public static string[] DesktopAxisNames = new string[(int)Axis.Max]
	{
		"Desktop_Left_X_Axis",
		"Desktop_Left_Y_Axis",
		"Desktop_Right_X_Axis",
		"Desktop_Right_Y_Axis",
		"Desktop_LeftTrigger",
		"Desktop_RightTrigger",
        "Desktop_DPad_X_Axis",
        "Desktop_DPad_Y_Axis",
	};
	
	/// <summary>
	/// The default Unity input name for each gamepad Button.
	/// </summary>
	public static string[] DesktopButtonNames = new string[(int)Button.Max]
	{
		"Desktop_Button A",
		"Desktop_Button B",
		"Desktop_Button X",
		"Desktop_Button Y",
		"Desktop_Up",
		"Desktop_Down",
		"Desktop_Left",
		"Desktop_Right",
		"Desktop_Start",
		"Desktop_Back",
		"Desktop_LStick",
		"Desktop_RStick",
		"Desktop_LeftShoulder",
		"Desktop_RightShoulder",
	};

	public static int[] DefaultButtonIds = new int[(int)Button.Max]
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
	};

    private static bool[] ButtonDownLastFrame = new bool[(int)Button.Max]; 

	/// <summary>
	/// The current Unity input names for all gamepad axes.
	/// </summary>
    public static string[] AxisNames = null;

	/// <summary>
	/// The current Unity input names for all gamepad buttons.
	/// </summary>
    public static string[] ButtonNames = null;

#if !UNITY_ANDROID || UNITY_EDITOR
    private static int lastGPCRefresh = 0;
    private static bool isMapped = true;
#endif

    static OVRGamepadController()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SetAxisNames(AndroidAxisNames);
        SetButtonNames(AndroidButtonNames);
#else
		SetAxisNames(DesktopAxisNames);
		SetButtonNames(DesktopButtonNames);
#endif
    }

	/// <summary>
	/// Sets the current names for all gamepad axes.
	/// </summary>
	public static void SetAxisNames(string[] axisNames)
	{
		AxisNames = axisNames;
	}

	/// <summary>
	/// Sets the current Unity input names for all gamepad buttons.
	/// </summary>
	/// <param name="buttonNames">Button names.</param>
	public static void SetButtonNames(string[] buttonNames)
	{
		ButtonNames = buttonNames;
	}

	/// <summary> Handles an axis read event. </summary>
	public delegate float ReadAxisDelegate(Axis axis);

	/// <summary> Handles an button read event. </summary>
	public delegate bool ReadButtonDelegate(Button button);

	/// <summary> Occurs when an axis has been read. </summary>
	public static ReadAxisDelegate ReadAxis = DefaultReadAxis;

	/// <summary> Occurs when a button has been read. </summary>
	public static ReadButtonDelegate ReadButton = DefaultReadButton;

#if (!UNITY_ANDROID || UNITY_EDITOR)
	private static bool GPC_Available = false;
	
	//-------------------------
	// Public access to plugin functions
	
	/// <summary>
	/// GPC_Initialize.
	/// </summary>
	/// <returns><c>true</c>, if c_ initialize was GPed, <c>false</c> otherwise.</returns>
	public static bool GPC_Initialize()
    {
        if (!OVRManager.instance.isSupportedPlatform)
            return false;
		return OVR_GamepadController_Initialize();
	}

	/// <summary>
	/// GPC_Destroy
	/// </summary>
	/// <returns><c>true</c>, if c_ destroy was GPed, <c>false</c> otherwise.</returns>
	public static bool GPC_Destroy()
	{
        if (!OVRManager.instance.isSupportedPlatform)
            return false;
		return OVR_GamepadController_Destroy();
	}

	/// <summary>
	/// GPC_Update
	/// </summary>
	/// <returns><c>true</c>, if c_ update was GPed, <c>false</c> otherwise.</returns>
	public static bool GPC_Update()
    {
        lastGPCRefresh = Time.frameCount;
        if (!OVRManager.instance.isSupportedPlatform)
            return false;
		return OVR_GamepadController_Update();
	}
#endif
	static void GPC_LateUpdate()
    {
        if (!OVRManager.instance.isSupportedPlatform)
            return;
        for (int i = 0; i < (int)Button.Max; i++)
        {
            ButtonDownLastFrame[i] = GPC_GetButton((Button)i);
        }
	}

	/// <summary>
	/// GPC_GetAxis
	/// The default delegate for retrieving axis info.
	/// </summary>
	/// <returns>The current value of the axis.</returns>
	/// <param name="axis">Axis.</param>
	public static float DefaultReadAxis(Axis axis)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return Input.GetAxis(AxisNames[(int)axis]);
#else
        float xinputValue = OVR_GamepadController_GetAxis((int)axis);

        float unityValue = 0f;
        if (isMapped)
        {
            try { unityValue = Input.GetAxis(AxisNames[(int)axis]); }
            catch { isMapped = false; }
        }

        return Mathf.Abs(xinputValue) > Mathf.Abs(unityValue) ? xinputValue : unityValue;
#endif
	}
	
	/// <summary>
	/// Returns the current value of the given Axis.
	/// </summary>
	public static float GPC_GetAxis(Axis axis)
	{
		if (ReadAxis == null)
			return 0f;
		return ReadAxis(axis);
	}

	public static void SetReadAxisDelegate(ReadAxisDelegate del)
	{
		ReadAxis = del;
	}

	/// <summary>
	/// Uses XInput to check if the given Button is down.
	/// </summary>
	public static bool DefaultReadButton(Button button)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		return Input.GetButton(ButtonNames[(int)button]);
#else
        if (Time.frameCount != lastGPCRefresh)
        {
            GPC_Update();
        }

        bool unityValue = false;
        if (isMapped)
        {
            try { unityValue = Input.GetButton(ButtonNames[(int)button]); }
            catch { isMapped = false; }
        }

		return OVR_GamepadController_GetButton((int)button) || unityValue;
#endif
	}

	/// <summary>
	/// Returns true if the given Button is down.
	/// </summary>
	public static bool GPC_GetButton(Button button)
	{
		if (ReadButton == null)
			return false;
		return ReadButton(button);
	}
    /// <summary>
	/// Returns true if the given Button was pressed this frame.
	/// </summary>
    public static bool GPC_GetButtonDown(Button button)
    {
        if (ReadButton == null)
            return false;
        return ReadButton(button) && !ButtonDownLastFrame[(int)button];
    }
    /// <summary>
	/// Returns true if the given Button was released this frame.
	/// </summary>
    public static bool GPC_GetButtonUp(Button button)
    {
        if (ReadButton == null)
            return false;
        return !ReadButton(button) && ButtonDownLastFrame[(int)button];
    }

	public static void SetReadButtonDelegate(ReadButtonDelegate del)
	{
		ReadButton = del;
	}

	/// <summary>
	/// Returns true if the gamepad controller is available.
	/// </summary>
	public static bool GPC_IsAvailable()
	{
#if !UNITY_ANDROID || UNITY_EDITOR
		return GPC_Available;
#else
		return true;
#endif
	}

	void GPC_Test()
	{
		// Axis test
		Debug.Log(string.Format("LT:{0:F3} RT:{1:F3} LX:{2:F3} LY:{3:F3} RX:{4:F3} RY:{5:F3}",
		GPC_GetAxis(Axis.LeftTrigger), GPC_GetAxis(Axis.RightTrigger),
		GPC_GetAxis(Axis.LeftXAxis), GPC_GetAxis(Axis.LeftYAxis),
		GPC_GetAxis(Axis.RightXAxis), GPC_GetAxis(Axis.RightYAxis)));

		// Button test
		Debug.Log(string.Format("A:{0} B:{1} X:{2} Y:{3} U:{4} D:{5} L:{6} R:{7} SRT:{8} BK:{9} LS:{10} RS:{11} L1:{12} R1:{13}",
		GPC_GetButton(Button.A), GPC_GetButton(Button.B),
		GPC_GetButton(Button.X), GPC_GetButton(Button.Y),
		GPC_GetButton(Button.Up), GPC_GetButton(Button.Down),
		GPC_GetButton(Button.Left), GPC_GetButton(Button.Right),
		GPC_GetButton(Button.Start), GPC_GetButton(Button.Back),
		GPC_GetButton(Button.LStick), GPC_GetButton(Button.RStick),
		GPC_GetButton(Button.LeftShoulder), GPC_GetButton(Button.RightShoulder)));
	}

#if !UNITY_ANDROID || UNITY_EDITOR
	void Start()
    {
		GPC_Available = GPC_Initialize();
    }

    void Update()
    {
        if (lastGPCRefresh < Time.frameCount)
        {
            GPC_Available = GPC_Update();
        }
    }

	void OnDestroy()
	{
		GPC_Destroy();
		GPC_Available = false;
	}

	public const string DllName = "OVRGamepad";
	
	[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool OVR_GamepadController_Initialize();
	[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool OVR_GamepadController_Destroy();
	[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool OVR_GamepadController_Update();
	[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
	public static extern float OVR_GamepadController_GetAxis(int axis);
	[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool OVR_GamepadController_GetButton(int button);
#endif
    void LateUpdate()
    {
        GPC_LateUpdate();
    }
}
