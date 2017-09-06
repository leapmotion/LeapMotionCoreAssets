using UnityEngine;
using System.Collections;

public class MovementManager : MonoBehaviour {
  public GameObject leapMotionOVRController = null;

  [Range(1.0f, 100.0f)]
  public float _mouseLookSensitivity;
  [Range(0.01f, 5.0f)]
  public float _moveSpeed;

  private KeyCode _forwardKey = KeyCode.W;
  private KeyCode _backwardKey = KeyCode.S;
  private KeyCode _leftKey = KeyCode.A;
  private KeyCode _rightKey = KeyCode.D;

  private Vector2 _lastMousePosition = Vector2.zero;
  private bool _mouseInitialized = false;

  private float startingHeight;
	// Use this for initialization
	void Start () {
    startingHeight = leapMotionOVRController.transform.position.y;
	}

  void Update() {
    handleKeyboardInput();
    handleMouseMove();
  }
	
	// Update is called once per frame
	void LateUpdate () {
    if (leapMotionOVRController == null || HandController.Main == null)
      return;

    // Move forward if both palms are facing outwards! Whoot!
    HandModel[] hands = HandController.Main.GetAllGraphicsHands();
    if (hands.Length > 1)
    {
      Vector3 direction0 = (hands[0].GetPalmPosition() - HandController.Main.transform.position).normalized;
      Vector3 normal0 = hands[0].GetPalmNormal().normalized;

      Vector3 direction1 = (hands[1].GetPalmPosition() - HandController.Main.transform.position).normalized;
      Vector3 normal1 = hands[1].GetPalmNormal().normalized;

      if (Vector3.Dot(direction0, normal0) > direction0.sqrMagnitude * 0.5f && Vector3.Dot(direction1, normal1) > direction1.sqrMagnitude * 0.5f)
      {
        Vector3 target = (hands[0].GetPalmPosition() + hands[1].GetPalmPosition()) / 2.0f;
        target.y = startingHeight;
        leapMotionOVRController.transform.position = Vector3.Lerp(leapMotionOVRController.transform.position, target, 0.1f);
      }
    }
	}

  private void handleKeyboardInput() {
    if (Input.GetKey(_forwardKey)) {
      Debug.Log("Move me forward: " + (_moveSpeed * Time.deltaTime));
      leapMotionOVRController.transform.localPosition += leapMotionOVRController.transform.forward * _moveSpeed * Time.deltaTime;
    }

    if (Input.GetKey(_backwardKey)) {
      leapMotionOVRController.transform.localPosition += leapMotionOVRController.transform.forward * -1 * _moveSpeed * Time.deltaTime;
    }

    if (Input.GetKey(_leftKey)) {
      leapMotionOVRController.transform.localPosition += leapMotionOVRController.transform.right * -1 * _moveSpeed * Time.deltaTime;
    }

    if (Input.GetKey(_rightKey)) {
      leapMotionOVRController.transform.localPosition += leapMotionOVRController.transform.right * _moveSpeed * Time.deltaTime;
    }
  }

  private void handleMouseMove() {
    if (!_mouseInitialized) {
      _lastMousePosition = Input.mousePosition;
      _mouseInitialized = true;
      return;
    }

    Vector2 mousePosition = Input.mousePosition;
    Vector2 mouseVelocity = (mousePosition - _lastMousePosition) * Time.deltaTime;
    Quaternion playerRotation = Quaternion.Euler(0.0f, mouseVelocity.x * _mouseLookSensitivity, 0.0f);
    Debug.Log("rotate me: " + (mouseVelocity.x * _mouseLookSensitivity));
    leapMotionOVRController.transform.localRotation *= playerRotation;
    _lastMousePosition = mousePosition;
  }
}
