using UnityEngine;
using System.Collections;

public class AdvancedModeOnlyAttribute : PropertyAttribute {
  public AdvancedModeOnlyAttribute() {
    order = int.MaxValue;
  }
}
