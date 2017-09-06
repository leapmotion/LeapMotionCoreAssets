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

// Internal C# wrapper for OVRPlugin.

internal static class OVRPlugin
{
	public const string WrapperVersion = "0.1.0";

	public enum Bool
	{
		False = 0,
		True
	}

	public enum Eye
	{
		None = -1,
		Left = 0,
		Right = 1,
		Count = 2
	}

	public enum Tracker
	{
		Default = 0,
		Count,
	}

	public enum BatteryStatus
	{
		Charging = 0,
		Discharging,
		Full,
		NotCharging,
		Unknown,
	}

	public enum PlatformUI
	{
		GlobalMenu = 0,
		ConfirmQuit,
	}

	private enum Key
	{
		Version = 0,
		ProductName,
		Latency,
		EyeDepth,
		EyeHeight,
		BatteryLevel,
		BatteryTemperature,
		CpuLevel,
		GpuLevel,
		SystemVolume,
		QueueAheadFraction,
		IPD,
#if OVR_LEGACY
		NativeTextureScale,
		VirtualTextureScale,
        Frequency,
#endif
    }

	private enum Caps
	{
		SRGB = 0,
		Chromatic,
		FlipInput,
		Rotation,
		HeadModel,
		Position,
		CollectPerf,
		DebugDisplay,
		Monoscopic,
#if OVR_LEGACY
		ShareTexture,
#endif
	}

	private enum Status
	{
		Debug = 0,
		HSWVisible,
		PositionSupported,
		PositionTracked,
		PowerSaving,
#if OVR_LEGACY
		Initialized,
#endif
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3f
	{
		public float x;
		public float y;
		public float z;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Quatf
	{
		public float x;
		public float y;
		public float z;
		public float w;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Posef
	{
		public Quatf Orientation;
		public Vector3f Position;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Sizei
	{
		public int w;
		public int h;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Frustumf
	{
		public float zNear;
		public float zFar;
		public float fovX;
		public float fovY;
	}

	public static bool srgb
	{
		get { return GetCap(Caps.SRGB); }
		set { SetCap(Caps.SRGB, value); }
	}

	public static bool chromatic
	{
		get { return GetCap(Caps.Chromatic); }
		set { SetCap(Caps.Chromatic, value); }
	}

	public static bool flipInput
	{
		get { return GetCap(Caps.FlipInput); }
		set { SetCap(Caps.FlipInput, value); }
	}

	public static bool rotation
	{
		get { return GetCap(Caps.Rotation); }
		set { SetCap(Caps.Rotation, value); }
	}

	public static bool headModel
	{
		get { return GetCap(Caps.HeadModel); }
		set { SetCap(Caps.HeadModel, value); }
	}

	public static bool position
	{
		get { return GetCap(Caps.Position); }
		set { SetCap(Caps.Position, value); }
	}

	public static bool collectPerf
	{
		get { return GetCap(Caps.CollectPerf); }
		set { SetCap(Caps.CollectPerf, value); }
	}

	public static bool debugDisplay
	{
		get { return GetCap(Caps.DebugDisplay); }
		set { SetCap(Caps.DebugDisplay, value); }
	}

	public static bool monoscopic
	{
		get { return GetCap(Caps.Monoscopic); }
		set { SetCap(Caps.Monoscopic, value); }
	}

	public static bool debug { get { return GetStatus(Status.Debug); } }

	public static bool hswVisible { get { return GetStatus(Status.HSWVisible); } }

	public static bool positionSupported { get { return GetStatus(Status.PositionSupported); } }

	public static bool positionTracked { get { return GetStatus(Status.PositionTracked); } }

	public static bool powerSaving { get { return GetStatus(Status.PowerSaving); } }

	public static string version { get { return GetString(Key.Version); } }

	public static string productName { get { return GetString(Key.ProductName); } }

	public static string latency { get { return GetString(Key.Latency); } }

	public static float eyeDepth
	{
		get { return ovrp_GetFloat(Key.EyeDepth); }
		set { ovrp_SetFloat(Key.EyeDepth, value); }
	}

	public static float eyeHeight
	{
		get { return ovrp_GetFloat(Key.EyeHeight); }
		set { ovrp_SetFloat(Key.EyeHeight, value); }
	}

	public static float batteryLevel
	{
		get { return ovrp_GetFloat(Key.BatteryLevel); }
		set { ovrp_SetFloat(Key.BatteryLevel, value); }
	}

	public static float batteryTemperature
	{
		get { return ovrp_GetFloat(Key.BatteryTemperature); }
		set { ovrp_SetFloat(Key.BatteryTemperature, value); }
	}

	public static int cpuLevel
	{
		get { return (int)ovrp_GetFloat(Key.CpuLevel); }
		set { ovrp_SetFloat(Key.CpuLevel, (float)value); }
	}

	public static int gpuLevel
	{
		get { return (int)ovrp_GetFloat(Key.GpuLevel); }
		set { ovrp_SetFloat(Key.GpuLevel, (float)value); }
	}

	public static float systemVolume
	{
		get { return ovrp_GetFloat(Key.SystemVolume); }
		set { ovrp_SetFloat(Key.SystemVolume, value); }
	}

	public static float queueAheadFraction
	{
		get { return ovrp_GetFloat(Key.QueueAheadFraction); }
		set { ovrp_SetFloat(Key.QueueAheadFraction, value); }
	}

	public static float ipd
	{
		get { return ovrp_GetFloat(Key.IPD); }
		set { ovrp_SetFloat(Key.IPD, value); }
	}

#if OVR_LEGACY
	public static bool initialized { get { return ovrp_GetStatus(Status.Initialized); } }

	public static bool shareTexture
	{
		get { return ovrp_GetCap(Caps.ShareTexture); }
		set { SetCap(Caps.ShareTexture, value); }
	}

	public static float nativeTextureScale
	{
		get { return ovrp_GetFloat(Key.NativeTextureScale); }
		set { ovrp_SetFloat(Key.NativeTextureScale, value); }
	}

	public static float virtualTextureScale
	{
		get { return ovrp_GetFloat(Key.VirtualTextureScale); }
		set { ovrp_SetFloat(Key.VirtualTextureScale, value); }
	}
#endif

	public static BatteryStatus batteryStatus
	{
		get { return ovrp_GetBatteryStatus(); }
	}

	private static bool GetStatus(Status bit)
	{
		return ((int)ovrp_GetStatus() & (1 << (int)bit)) != 0;
	}

	private static bool GetCap(Caps cap)
	{
		return ((int)ovrp_GetCaps() & (1 << (int)cap)) != 0;
	}

	private static void SetCap(Caps cap, bool value)
	{
		if (GetCap(cap) == value)
			return;

		int caps = (int)ovrp_GetCaps();
		if (value)
			caps |= (1 << (int)cap);
		else
			caps &= ~(1 << (int)cap);

		ovrp_SetCaps((Caps)caps);
	}

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetNativePointer")]
	public static extern IntPtr GetNativePointer();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetBufferCount")]
	public static extern int GetBufferCount();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetEyePose")]
	public static extern Posef GetEyePose(Eye eyeId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetEyeVelocity")]
	public static extern Posef GetEyeVelocity(Eye eyeId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetEyeAcceleration")]
	public static extern Posef GetEyeAcceleration(Eye eyeId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetEyeFrustum")]
	public static extern Frustumf GetEyeFrustum(Eye eyeId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetEyeTextureSize")]
	public static extern Sizei GetEyeTextureSize(Eye eyeId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetTrackerPose")]
	public static extern Posef GetTrackerPose(Tracker trackerId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetTrackerFrustum")]
	public static extern Frustumf GetTrackerFrustum(Tracker trackerId);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_DismissHSW")]
	public static extern Bool DismissHSW();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern Caps ovrp_GetCaps();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern Bool ovrp_SetCaps(Caps caps);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern Status ovrp_GetStatus();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern float ovrp_GetFloat(Key key);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern Bool ovrp_SetFloat(Key key, float value);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern BatteryStatus ovrp_GetBatteryStatus();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_SetOverlayQuad")]
	public static extern Bool SetOverlayQuad(Bool onTop, IntPtr texture, IntPtr device, Posef pose, Vector3f scale);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_ShowUI")]
	public static extern Bool ShowUI(PlatformUI ui);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr ovrp_GetString(Key key);
	private static string GetString(Key key) { return Marshal.PtrToStringAnsi(ovrp_GetString(key)); }

#if OVR_LEGACY
	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_PreInitialize")]
	//public static extern Bool PreInitialize();

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_Initialize")]
	//public static extern Bool Initialize(RenderAPIType apiType, IntPtr platformArgs);

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_Shutdown")]
	//public static extern Bool Shutdown();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_SetupDistortionWindow")]
	public static extern Bool SetupDistortionWindow();

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_DestroyDistortionWindow")]
	public static extern Bool DestroyDistortionWindow();

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_RecreateEyeTexture")]
	//public static extern Bool RecreateEyeTexture(Eye eyeId, int stage, void* device, int height, int width, int samples, Bool isSRGB, void* result);
	
	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_SetEyeTexture")]
	//public static extern Bool SetEyeTexture(Eye eyeId, IntPtr texture);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_Update")]
	public static extern Bool ovrp_Update(int frameIndex);

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_BeginFrame")]
	//public static extern Bool BeginFrame(int frameIndex);

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_EndEye")]
	//public static extern Bool EndEye(Eye eye);

	//[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_EndFrame")]
	//public static extern Bool EndFrame(int frameIndex);

	[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_RecenterPose")]
	public static extern Bool RecenterPose();
#endif
}
