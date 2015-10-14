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
  void LateUpdate() {
    Vector3 rewoundPos;
    Quaternion rewoundRot;
    cameraAlignment.GetRewoundTransform((int)cameraType, out rewoundPos, out rewoundRot);
    transform.position = rewoundPos;
    transform.rotation = rewoundRot;
  }
}
