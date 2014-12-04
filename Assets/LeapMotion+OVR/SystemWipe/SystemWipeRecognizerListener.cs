using UnityEngine;
using System;
using Leap.Util;

using System.Runtime.InteropServices;

public class SystemWipeArgs : EventArgs {
	private SystemWipeInfo m_wipeInfo;

	public SystemWipeInfo WipeInfo { get { return m_wipeInfo; } }

	public SystemWipeArgs(SystemWipeInfo wipeInfo) {
		m_wipeInfo = wipeInfo;
	}
}

public class SystemWipeRecognizerListener : MonoBehaviour {

	public event EventHandler<SystemWipeArgs> SystemWipeUpdate;

	public static SystemWipeRecognizerListener Instance;

	private SystemWipeInfo m_latestWipeInfo;

	private bool m_wipeInfoDirty = false;

	private object wipeInfoLock = new object();

	void Awake() {
		if ( Instance == null ) { 
			Instance = this;
		}
		else {
			throw new Exception("Attempting to create multiple SystemWipeRecognizerListeners. Only the first recognizer will be listed.");
		}
	}

	void Update() {
		lock(wipeInfoLock) {
			if( m_wipeInfoDirty) {
				EventHandler<SystemWipeArgs> handler = SystemWipeUpdate;
				
				if ( handler != null ) {
					handler(this, new SystemWipeArgs(m_latestWipeInfo));
				}
				m_wipeInfoDirty = false;
			}
		}
	}

  void SystemWipeInfoCallback(Leap.Util.SystemWipeInfo info)
  {
    //Debug.Log("Swipe " + info.Status + " " + info.Direction + " " + info.Progress);
		lock(wipeInfoLock) {
			m_wipeInfoDirty = true;
			m_latestWipeInfo = info;
		}
  }

  // Called before the body's first Update() and, if you Disable the body it's called again before the first following Update().
  void OnEnable()
  {
    systemWipeInfoDelegate = new Leap.Util.SystemWipeRecognizerNative.CallbackSystemWipeInfoDelegate(SystemWipeInfoCallback);
    Leap.Util.SystemWipeRecognizerNative.SetSystemWipeRecognizerCallback(Marshal.GetFunctionPointerForDelegate(systemWipeInfoDelegate));

    Leap.Util.SystemWipeRecognizerNative.EnableSystemWipeRecognizer();
  }

  // Called when the body is disabled. Also called upon body destruction.
  void OnDisable()
  {
    Leap.Util.SystemWipeRecognizerNative.DisableSystemWipeRecognizer(); 
  }

  Leap.Util.SystemWipeRecognizerNative.CallbackSystemWipeInfoDelegate systemWipeInfoDelegate;
}
