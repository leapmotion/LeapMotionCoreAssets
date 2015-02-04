using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LMWidgets;

public class ToggleToTextDataBinder : DataBinderBool {
  [SerializeField] 
  Text uiText;

  override public bool GetCurrentData() {
    if ( uiText.text.ToLower() == "true" ) {
      return true;
    }
    else {
      return false;
    }
  }
  
  // Set the current system value of the data.
  override public void SetCurrentData(bool value) {
    if ( value == true ) { 
      uiText.text = "True";
    }
    else {
      uiText.text = "False";
    }
  }
}
