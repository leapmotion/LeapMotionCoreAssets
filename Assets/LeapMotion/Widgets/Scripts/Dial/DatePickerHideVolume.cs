using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DatePickerHideVolume : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	void OnTriggerEnter(Collider other) {
		other.GetComponentInChildren<Text>().enabled = false;
	}
	
	void OnTriggerExit(Collider other) {
		other.GetComponentInChildren<Text>().enabled = true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
