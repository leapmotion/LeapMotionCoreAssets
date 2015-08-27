using UnityEngine;
using System.Collections;

public class RewindGameObject : MonoBehaviour {
  public enum CameraType {
    left = -1,
    center = 0,
    right = 1
  };
  public CameraType cameraType;
  public LeapCameraAlignment cameraAlignment;
	
	// this.LateUpdate must be called after cameraAlignment.lateUpdate
  void LateUpdate () {
    cameraAlignment.RelativeRewind (transform, (int)cameraType);
	}
}
