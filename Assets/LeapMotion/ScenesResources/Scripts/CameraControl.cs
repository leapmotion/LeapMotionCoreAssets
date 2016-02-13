using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {

  public GameObject scene = null;

  private Vector3 m_originPosition;
  private Vector3 m_originDirection;
  private Vector3 m_originRotation;

  private Vector3 m_rotation;

  void Reset()
  {
    transform.position = m_originPosition;
    transform.forward = m_originDirection;
    m_rotation = m_originRotation;
  }

	// Use this for initialization
	void Start ()
  {
    transform.LookAt(new Vector3(0f, 0.2f, 0f));
    m_originPosition = transform.position;
    m_originDirection = transform.forward;
    m_originRotation = transform.localEulerAngles;
    Reset();
  }
	
	// Update is called once per frame
	void Update () {
	  if (Input.GetMouseButton(0))
    {
      float speed = 200f;
      m_rotation.y += Input.GetAxis("Mouse X") * Time.deltaTime * speed;
      m_rotation.x -= Input.GetAxis("Mouse Y") * Time.deltaTime * speed;
      m_rotation.z = 0f;
      transform.localEulerAngles = m_rotation;
    }

    if (Input.GetMouseButton(1))
    {
      float speed = 1 / 40f;
      float horizontalDisplacement = Input.GetAxis("Mouse X") * speed;
      float verticalDisplacement = Input.GetAxis("Mouse Y") * speed;
      // Remove all y-components of right and forward and remain normalized
      transform.position += -transform.right * horizontalDisplacement - transform.up * verticalDisplacement;
    }

    float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
    if (mouseScroll != 0f) {
      float speed = 1 / 2f;
      transform.position += transform.forward * mouseScroll;
    }

    if (Input.GetKeyDown(KeyCode.R))
    {
      Reset();
    }
	}
}
