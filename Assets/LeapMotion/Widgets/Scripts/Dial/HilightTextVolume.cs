using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using VRWidgets;

public class HilightTextVolume : MonoBehaviour {

	private Color textColor;
	public string CurrentHilightValue;
	
	public DialGraphics dialGraphics;

	// Use this for initialization
	void Awake () {
		dialGraphics = GetComponentInParent<DialGraphics>();
		textColor = dialGraphics.TextColor;
	}
	
	void OnTriggerEnter(Collider other) {
		Text text = other.GetComponentInChildren<Text>();
		text.color = Color.white;
//		CurrentHilightValue = text.text;
		
		
	}
	
	void OnTriggerStay(Collider other){
		Text text = other.GetComponentInChildren<Text>();
		text.color = Color.white;
		CurrentHilightValue = text.text;
	}
	
	void OnTriggerExit(Collider other) {
		other.GetComponentInChildren<Text>().color = textColor;
	}
	
	
	// Update is called once per frame
	void Update () {
	
	}
}
