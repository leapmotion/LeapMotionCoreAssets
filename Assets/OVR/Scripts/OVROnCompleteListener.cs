using UnityEngine;
using System.Collections;

#if UNITY_ANDROID && !UNITY_EDITOR

public abstract class OVROnCompleteListener : AndroidJavaProxy
{
	public OVROnCompleteListener() : base("com.oculus.svclib.OnCompleteListener")
	{
	}
	
	public abstract void onSuccess();

	public abstract void onFailure();
}

#endif