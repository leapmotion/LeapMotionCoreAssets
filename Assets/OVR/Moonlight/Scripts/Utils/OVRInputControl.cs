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
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Cross-platform wrapper for Unity Input.
/// See OVRGamepadController for a list of the base axis and button names.  
/// Depending on joystick number and platform
/// the base names will be pre-pended with "Platform:Joy #:" to look them up in the
/// Unity Input table.  For instance: using an axis name of "Left_X_Axis" with GetJoystickAxis()
/// will result in looking up the axis named "Win: Joy 1: Left_X_Axis" when running on
/// Windows and "Android: Joy 1: Left_X_Axis" when running on Android.
///
/// In addition to wrapping joystick input, this class allows the assignment of up, held 
/// and down events for any key, mouse button, or joystick button via the AddInputHandler()
/// method.
///
/// Currently this class relies on enumerations defined in OVRGamepadController
/// so that it remains compatible with existing Unity OVR projects.  When this class
/// is included it overloads the default GPC_GetAxis() and GPC_GetButton() calls to
/// to ReadAxis() and ReadButton() in this class.  
/// Ideally this class would completely replace the OVRGamepadController class.  This 
/// would involve changing the GPC_GetAxis() and GPC_GetButton() calls in a project 
/// and removing references to OVRGamepadController in this file (and moving some of 
/// the tables to InputControl).
/// </summary>
public static class OVRInputControl
{
	[SerializeField]
	// FIXME: this was originally settable on the behavior before this was a static class... maybe remove it.
	/// <summary>
	/// Set 'true' to allow keyboard input (will be set 'false' on some platforms).
	/// </summary>
	private	static bool	allowKeyControls = true;

	/// <summary>
	/// If true, prints information about each input event to the log.
	/// </summary>
	public static bool verbose = false;

	public delegate void OnKeyUp(MonoBehaviour comp);
	public delegate void OnKeyDown(MonoBehaviour comp);
	public delegate void OnKeyHeld(MonoBehaviour comp);

	/// <summary>
	/// Types of devices we can handle input for.
	/// </summary>
	public enum DeviceType
	{
		None = -1,
		Keyboard = 0, // a key
		Mouse,        // a mouse button
		Gamepad,      // a gamepad button
		Axis,         // a joystick axis (or trigger)
	};

	/// <summary>
	/// Mouse button definitions.
	/// </summary>
	public enum MouseButton
	{
		None = -1,
		Left = 0,
		Right = 1,
		Middle = 2,
		Fourth = 4,
		Fifth = 5,
	};

	/// <summary>
	/// Holds information about a single key event.
	/// </summary>
	class KeyInfo
	{
		public DeviceType deviceType = DeviceType.None;
		public string keyName = "";
		public MouseButton mouseButton = MouseButton.None;
		public OVRGamepadController.Button joystickButton = OVRGamepadController.Button.None;
		public OVRGamepadController.Axis joystickAxis = OVRGamepadController.Axis.None;
		public float threshold = 1000.0f; // threshold for triggers
		public bool wasDown = false;
		public OnKeyDown downHandler;
		public OnKeyHeld heldHandler;
		public OnKeyUp upHandler;

		/// <summary>
		/// Key constructor.
		/// </summary>
		public KeyInfo(
				DeviceType inDeviceType,
				string inKeyName,
				OnKeyDown inDownHandler,
				OnKeyHeld inHeldHandler,
				OnKeyUp inUpHandler)
		{
			deviceType = inDeviceType;
			keyName = inKeyName;
			mouseButton = MouseButton.None;
			joystickButton = OVRGamepadController.Button.None;
			joystickAxis = OVRGamepadController.Axis.None;
			threshold = 1000.0f;
			wasDown = false;
			downHandler = inDownHandler;
			heldHandler = inHeldHandler;
			upHandler = inUpHandler;
		}

		/// <summary>
		/// Mouse button constructor.
		/// </summary>
		public KeyInfo(
				DeviceType inDeviceType,
				MouseButton inMouseButton,
				OnKeyDown inDownHandler,
				OnKeyHeld inHeldHandler,
				OnKeyUp inUpHandler)
		{
			deviceType = inDeviceType;
			keyName = "Mouse Button " + (int)inMouseButton;
			mouseButton = inMouseButton;
			joystickButton = OVRGamepadController.Button.None;
			joystickAxis = OVRGamepadController.Axis.None;
			threshold = 1000.0f;
			wasDown = false;
			downHandler = inDownHandler;
			heldHandler = inHeldHandler;
			upHandler = inUpHandler;
		}

		/// <summary>
		/// Joystick button constructor.
		/// </summary>
		public KeyInfo(
				DeviceType inDeviceType,
				OVRGamepadController.Button inJoystickButton,
				OnKeyDown inDownHandler,
				OnKeyHeld inHeldHandler,
				OnKeyUp inUpHandler)
		{
			deviceType = inDeviceType;
			keyName = OVRGamepadController.ButtonNames[(int)inJoystickButton];
			mouseButton = MouseButton.None;
			joystickButton = inJoystickButton;
			joystickAxis = OVRGamepadController.Axis.None;
			threshold = 1000.0f;
			wasDown = false;
			downHandler = inDownHandler;
			heldHandler = inHeldHandler;
			upHandler = inUpHandler;
		}

		/// <summary>
		/// Joystick axis constructor.
		/// </summary>
		public KeyInfo(
				DeviceType inDeviceType,
				OVRGamepadController.Axis inJoystickAxis,
				OnKeyDown inDownHandler,
				OnKeyHeld inHeldHandler,
				OnKeyUp inUpHandler)
		{
			deviceType = inDeviceType;
			keyName = OVRGamepadController.AxisNames[(int)inJoystickAxis];
			mouseButton = MouseButton.None;
			joystickButton = OVRGamepadController.Button.None;
			joystickAxis = inJoystickAxis;
			threshold = 0.5f;
			wasDown = false;
			downHandler = inDownHandler;
			heldHandler = inHeldHandler;
			upHandler = inUpHandler;
		}
	};
	
	private static List<KeyInfo> keyInfos = new List<KeyInfo>();
	
	private static string platformPrefix = "";

	/// <summary>
	/// Maps joystick input to a component.
	/// </summary>
	public class InputMapping
	{
		public InputMapping(MonoBehaviour comp, int inJoystickNumber)
		{
			component = comp;
			joystickNumber = inJoystickNumber;
		}

		public MonoBehaviour component; // the component input goes to
		public int joystickNumber; 		// the joystick that controls the object
	};

	/// <summary>
	/// List of mappings from joystick to component.
	/// </summary>
	private static List<InputMapping> inputMap = new List<InputMapping>();

	/// <summary>
	/// Initializes the input system for OSX.
	/// </summary>
	private static void Init_Windows()
	{
		if (verbose)
			Debug.Log("Initializing input for Windows.");
		allowKeyControls = false;
		platformPrefix = "Win:";
	}

	/// <summary>
	/// Initializes the input system for Windows when running from the Unity editor.
	/// </summary>
	private static void Init_Windows_Editor()
	{
		if (verbose)
			Debug.Log("Initializing input for Windows Editor.");
		allowKeyControls = true;
		platformPrefix = "Win:";
	}

	/// <summary>
	/// Initializes the input system for Android.
	/// </summary>
	private static void Init_Android()
	{
		if (verbose)
			Debug.Log("Initializing input for Android.");
		allowKeyControls = true;
		platformPrefix = "Android:";
	}

	/// <summary>
	/// Initializes the input system for OSX.
	/// </summary>
	private static void Init_OSX()
	{
		if (verbose)
			Debug.Log("Initializing input for OSX.");
		allowKeyControls = false;
		platformPrefix = "OSX:";
	}

	/// <summary>
	/// Initializes the input system for OSX when running from the Unity editor.
	/// </summary>
	private static void Init_OSX_Editor()
	{
		if (verbose)
			Debug.Log("Initializing input for OSX Editor.");
		allowKeyControls = true;
		platformPrefix = "OSX:";
	}

	/// <summary>
	/// Initializes the input system for iPhone.
	/// </summary>
	private static void Init_iPhone()
	{
		if (verbose)
			Debug.Log("Initializing input for iPhone.");
		allowKeyControls = false;
		platformPrefix = "iPhone:";
	}

	/// <summary>
	/// Static contructor for the OVRInputControl class.
	/// </summary>
	static OVRInputControl()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		OVRGamepadController.SetReadAxisDelegate(ReadJoystickAxis);
		OVRGamepadController.SetReadButtonDelegate(ReadJoystickButton);
#endif
		switch (Application.platform)
		{
			case RuntimePlatform.WindowsPlayer:
				Init_Windows();
				break;
			case RuntimePlatform.WindowsEditor:
				Init_Windows_Editor();
				break;
			case RuntimePlatform.Android:
				Init_Android();
				break;
			case RuntimePlatform.OSXPlayer:
				Init_OSX();
				break;
			case RuntimePlatform.OSXEditor:
				Init_OSX_Editor();
				break;
			case RuntimePlatform.IPhonePlayer:
				Init_iPhone();
				break;
		}

		string[] joystickNames = Input.GetJoystickNames();
		for (int i = 0; i < joystickNames.Length; ++i)
		{
			if (verbose)
				Debug.Log("Found joystick '" + joystickNames[i] + "'...");
		}
	}

	/// <summary>
	///  Adds a handler for key input
	/// </summary>
	public static void AddInputHandler(
			DeviceType dt,
			string keyName,
			OnKeyDown onDown,
			OnKeyHeld onHeld,
		   	OnKeyUp onUp)
	{
		keyInfos.Add(new KeyInfo(dt, keyName, onDown, onHeld, onUp));
	}

	/// <summary>
	/// Adds a handler for mouse button input.
	/// </summary>
	public static void AddInputHandler(
			DeviceType dt,
		   	MouseButton mouseButton,
			OnKeyDown onDown,
			OnKeyHeld onHeld,
		   	OnKeyUp onUp)
	{
		keyInfos.Add(new KeyInfo(dt, mouseButton, onDown, onHeld, onUp));
	}

	/// <summary>
	/// Adds a handler for joystick button input.
	/// </summary>
	public static void AddInputHandler(
			DeviceType dt,
		   	OVRGamepadController.Button joystickButton,
			OnKeyDown onDown,
		   	OnKeyHeld onHeld,
		   	OnKeyUp onUp)
	{
		keyInfos.Add(new KeyInfo(dt, joystickButton, onDown, onHeld, onUp));
	}

	/// <summary>
	/// Adds a handler for joystick axis input.
	/// </summary>
	public static void AddInputHandler(
			DeviceType dt,
		   	OVRGamepadController.Axis axis,
			OnKeyDown onDown,
		   	OnKeyHeld onHeld,
		   	OnKeyUp onUp)
	{
		keyInfos.Add(new KeyInfo(dt, axis, onDown, onHeld, onUp));
	}

	/// <summary>
	/// Returns the current value of the joystick axis specified by the name parameter.
	/// The name should partially match the name of an axis specified in the Unity
	/// Edit -> Project Settings -> Input pane, minus the Platform: Joy #: qualifiers.
	/// For instance, specify "Left_X_Axis" to select the appropriate axis for the
	/// current platform.  This will be permuted into something like "Win:Joy 1:Left_X_Axis"
	/// before it is queried.
	/// </summary>
	public static float GetJoystickAxis(int joystickNumber, string name)
	{
		// TODO: except for the joystick prefix this could be a table lookup
		// with a table-per-joystick this could be a lookup.
#if UNITY_ANDROID && !UNITY_EDITOR
		// on the Samsung gamepad, the left and right triggers are actually buttons
		// so we map left and right triggers to the left and right shoulder buttons.
		if (name == "LeftTrigger")
		{
			return GetJoystickButton(joystickNumber, OVRGamepadController.Button.LeftShoulder) ? 1.0f : 0.0f;
		}
		else if (name == "RightTrigger")
		{
			return GetJoystickButton(joystickNumber, OVRGamepadController.Button.RightShoulder) ? 1.0f : 0.0f;
		}
#endif
		string platformName = platformPrefix + "Joy " + joystickNumber + ":" + name;
		return Input.GetAxis(platformName);
	}

	/// <summary>
	/// Delegate for OVRGamepadController.
	/// Returns the current value of the specified joystick axis.
	/// </summary>
	public static float GetJoystickAxis(int joystickNumber, OVRGamepadController.Axis axis)
	{
		string platformName = platformPrefix + "Joy " + joystickNumber + ":" + OVRGamepadController.AxisNames[(int)axis];
		return Input.GetAxis(platformName);
	}

	/// <summary>
	/// Delegate for OVRGamepadController.
	/// This only exists for legacy compatibility with OVRGamepadController.
	/// </summary>
	public static float ReadJoystickAxis(OVRGamepadController.Axis axis)
	{
		//if (verbose)
			//Debug.Log("OVRInputControl.ReadJoystickAxis");
		return GetJoystickAxis(1, axis);
	}

	/// <summary>
	/// Returns true if a joystick button is depressed.
	/// The name should partially match the name of an axis specified in the Unity
	/// Edit -> Project Settings -> Input pane, minus the Platform: Joy #: qualifiers.
	/// For instance, specify "Button A" to select the appropriate axis for the
	/// current platform.  This will be permuted into something like "Win:Joy 1:Button A"
	/// before it is queried.
	/// </summary>
	public static bool GetJoystickButton(int joystickNumber, string name)
	{
		// TODO: except for the joystick prefix this could be a table lookup
		// with a table-per-joystick this could be a lookup.
		string fullName = platformPrefix + "Joy " + joystickNumber + ":" + name;
		return Input.GetButton(fullName);
	}

	/// <summary>
	/// Delegate for OVRGamepadController.
	/// Returns true if the specified joystick button is pressed.
	/// </summary>
	public static bool GetJoystickButton(int joystickNumber, OVRGamepadController.Button button)
	{
		string fullName = platformPrefix + "Joy " + joystickNumber + ":" + OVRGamepadController.ButtonNames[(int)button];
		//if (verbose)
			//Debug.Log("Checking button " + fullName);
		return Input.GetButton(fullName);
	}

	/// <summary>
	/// Delegate for OVRGamepadController.
	/// This only exists for legacy compatibility with OVRGamepadController.
	/// </summary>
	public static bool ReadJoystickButton(OVRGamepadController.Button button)
	{
		//if (verbose)
			//Debug.Log("OVRInputControl.ReadJoystickButton");
		return GetJoystickButton(1, button);
	}

	//======================
	// GetMouseButton
	// Returns true if the specified mouse button is pressed.
	//======================
	public static bool GetMouseButton(MouseButton button)
	{
		return Input.GetMouseButton((int)button);
	}

	/// <summary>
	/// Outputs debug spam for any non-zero axis.
	/// This is only used for finding which axes are which with new controllers.
	/// </summary>
	private static void ShowAxisValues()
	{
		for (int i = 1; i <= 20; ++i)
		{
			string axisName = "Test Axis " + i;
			float v = Input.GetAxis(axisName);
			if (Mathf.Abs(v) > 0.2f)
			{
				if (verbose)
					Debug.Log("Test Axis " + i + ": v = " + v);
			}
		}
	}

	/// <summary>
	/// Outputs debug spam for any depressed button.
	/// This is only used for finding which buttons are which with new controllers.
	/// </summary>
	private static void ShowButtonValues()
	{
		for (int i = 0; i < 6; ++i)
		{
			string buttonName = "Test Button " + i;
			if (Input.GetButton(buttonName))
			{
				if (verbose)
					Debug.Log("Test Button " + i + " is down.");
			}
		}
	}

	/// <summary>
	/// Adds a mapping from a joystick to a behavior.
	/// </summary>
	public static void AddInputMapping(int joystickNumber, MonoBehaviour comp)
	{
		for (int i = 0; i < inputMap.Count; ++i)
		{
			InputMapping im = inputMap[i];
			if (im.component == comp && im.joystickNumber == joystickNumber)
			{
				OVRDebugUtils.Assert(false, "Input mapping already exists!");
				return;
			}
		}
		inputMap.Add(new InputMapping(comp, joystickNumber));
	}

	/// <summary>
	/// Removes a mapping from a joystick to a behavior.
	/// </summary>
	public static void RemoveInputMapping(int joystickNumber, MonoBehaviour comp)
	{
		for (int i = 0; i < inputMap.Count; ++i)
		{
			InputMapping im = inputMap[i];
			if (im.component == comp && im.joystickNumber == joystickNumber)
			{
				inputMap.RemoveAt(i);
				return;
			}
		}
	}

	/// <summary>
	/// Removes all control mappings.
	/// </summary>
	public static void ClearControlMappings()
	{
		inputMap.Clear();
	}

	/// <summary>
	/// Updates the state of all input mappings.  This must be called from 
	/// a single MonoBehaviour's Update() method for input to be read.
	/// </summary>
	public static void Update()
	{
		// Enable these two lines if you have a new controller that you need to
		// set up for which you do not know the axes.
		//ShowAxisValues();
		//ShowButtonValues();
		
		for (int i = 0; i < inputMap.Count; ++i)
		{
			UpdateInputMapping(inputMap[i].joystickNumber, inputMap[i].component);
		}
	}

	/// <summary>
	/// Updates a single input mapping.
	/// </summary>
	private static void UpdateInputMapping(int joystickNumber, MonoBehaviour comp)
	{
		for (int i = 0; i < keyInfos.Count; ++i)
		{
			bool keyDown = false;
			// query the correct device
			KeyInfo keyInfo = keyInfos[i];
			if (keyInfo.deviceType == DeviceType.Gamepad)
			{
				//if (verbose)
					//Debug.Log("Checking gamepad button " + keyInfo.KeyName);
				keyDown = GetJoystickButton(joystickNumber, keyInfo.joystickButton);
			}
			else if (keyInfo.deviceType == DeviceType.Axis)
			{
				float axisValue = GetJoystickAxis(joystickNumber, keyInfo.joystickAxis);
				keyDown = (axisValue >= keyInfo.threshold);
			}
			else if (allowKeyControls)
			{
				if (keyInfo.deviceType == DeviceType.Mouse)
				{
					keyDown = GetMouseButton(keyInfo.mouseButton);
				}
				else if (keyInfo.deviceType == DeviceType.Keyboard)
				{
					keyDown = Input.GetKey(keyInfo.keyName);
				}
			}

			// handle the event
			if (!keyDown)
			{
				if (keyInfo.wasDown)
				{
					// key was just released
					keyInfo.upHandler(comp);
				}
			}
			else
			{
				if (!keyInfo.wasDown)
				{
					// key was just pressed
					//if (verbose)
						//Debug.Log( "Key or Button down: " + keyInfo.KeyName );
					keyInfo.downHandler(comp);
				}
				else
				{
					// key is held
					keyInfo.heldHandler(comp);
				}
			}
			// update the key info
			keyInfo.wasDown = keyDown;
		}
	}
};
