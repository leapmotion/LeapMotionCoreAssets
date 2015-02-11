using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LMWidgets {
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
      if (text == null) { return; }
  		text.color = Color.white;
  	}
  	
  	void OnTriggerStay(Collider other){
  		Text text = other.GetComponentInChildren<Text>();
      if (text == null) { return; }
  		text.color = Color.white;
  		CurrentHilightValue = text.text;
  	}
  	
  	void OnTriggerExit(Collider other) {
      Text text = other.GetComponentInChildren<Text> ();
      if (text == null) { return; }
      text.color = textColor;
  	}
  	
  	
  	// Update is called once per frame
  	void Update () {
  	
  	}
  }
}