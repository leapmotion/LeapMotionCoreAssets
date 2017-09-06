using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LMWidgets;

public class DialToTextBinder : DataBinderDial {

  [SerializeField] 
  Text uiText;
  
  override protected void setDataModel(string value) {
    uiText.text = value;
  }
  
  override public string GetCurrentData() {
    return uiText.text;
  }
}
