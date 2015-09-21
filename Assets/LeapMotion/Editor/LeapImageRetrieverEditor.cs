using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(LeapImageRetriever))]
public class LeapImageRetrieverEditor : Editor {

  private List<string> BasicModePropertyNames = new List<string>() {
      "m_Script",
      "handController",
    };

  public override void OnInspectorGUI() {
    SerializedProperty properties = serializedObject.GetIterator();

    bool useEnterChildren = true;
    while (properties.NextVisible(useEnterChildren) == true) {
      useEnterChildren = false;
      if (AdvancedMode._advancedModeEnabled || BasicModePropertyNames.Contains(properties.name)) {
        EditorGUILayout.PropertyField(properties, true);
      }
    }

    SerializedProperty eyeProperty = serializedObject.FindProperty("retrievedEye");
    if (eyeProperty.enumValueIndex == -1) {
      LeapImageRetriever retrieverScript = target as LeapImageRetriever;
      bool containsLeft = retrieverScript.gameObject.name.ToLower().Contains("left");
      eyeProperty.enumValueIndex = containsLeft ? (int)LeapImageRetriever.EYE.LEFT : (int)LeapImageRetriever.EYE.RIGHT;
    }

    serializedObject.ApplyModifiedProperties();
  }

}
