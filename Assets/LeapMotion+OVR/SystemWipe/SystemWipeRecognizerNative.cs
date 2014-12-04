using System;
using System.Runtime.InteropServices;

namespace Leap.Util
{
	public enum Direction : int { Up, Down };
	public enum Status : int { ErrorCannotAccessImages, Idle, SwipeBegin, SwipeUpdate, SwipeComplete, SwipeAbort };

	[StructLayout(LayoutKind.Sequential)]
	public struct SystemWipeInfo
	{
		public Direction Direction;
		public Status Status;
		public float Progress; 
	}

  public class SystemWipeRecognizerNative
	{
    public delegate void CallbackSystemWipeInfoDelegate(SystemWipeInfo systemWipeInfo);
#if UNITY_STANDALONE_OSX
	[DllImport("SystemWipeRecognizerDll")]
	public static extern void SetSystemWipeRecognizerCallback(IntPtr property);
	
	[DllImport("SystemWipeRecognizerDll")]
	public static extern void EnableSystemWipeRecognizer();
	
	[DllImport("SystemWipeRecognizerDll")]
	public static extern void DisableSystemWipeRecognizer();
	
	[DllImport("SystemWipeRecognizerDll")]
	public static extern bool WasLastImageAccessOk();
	
	[DllImport("SystemWipeRecognizerDll")]
	public static extern int GetFrameCount();
#else
	[DllImport("SystemWipeRecognizerDll", CallingConvention = CallingConvention.StdCall)]
    public static extern void SetSystemWipeRecognizerCallback(IntPtr property);

    [DllImport("SystemWipeRecognizerDll", CallingConvention = CallingConvention.StdCall)]
    public static extern void EnableSystemWipeRecognizer();

    [DllImport("SystemWipeRecognizerDll", CallingConvention = CallingConvention.StdCall)]
    public static extern void DisableSystemWipeRecognizer();

    [DllImport("SystemWipeRecognizerDll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool WasLastImageAccessOk();

    [DllImport("SystemWipeRecognizerDll", CallingConvention = CallingConvention.StdCall)]
    public static extern int GetFrameCount();
#endif
  }
}
