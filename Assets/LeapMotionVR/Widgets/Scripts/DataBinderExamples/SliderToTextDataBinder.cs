using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LMWidgets;

public class SliderToTextDataBinder : DataBinderSlider {
  [SerializeField] 
  Text uiText;

  override protected void setDataModel(float value) {
    uiText.text = value.ToString();
  }
  
  override public float GetCurrentData() {
    return float.Parse(uiText.text);
  }
}
