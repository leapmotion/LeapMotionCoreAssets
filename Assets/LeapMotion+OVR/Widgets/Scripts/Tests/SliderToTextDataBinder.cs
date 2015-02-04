using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LMWidgets;

public class SliderToTextDataBinder : DataBinderFloat {
  [SerializeField] 
  Text uiText;
  
  override public float GetCurrentData() {
    return float.Parse(uiText.text);
  }
  
  // Set the current system value of the data.
  override public void SetCurrentData(float value) {
    uiText.text = value.ToString();
  }
}
