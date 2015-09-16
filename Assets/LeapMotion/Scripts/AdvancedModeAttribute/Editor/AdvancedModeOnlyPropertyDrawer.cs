/*
   * Enables advanced mode.
   * By default a lot of tuning values that are not useful to most developers
   * are hidden in the editor. This include things like managing temporal and spacial 
   * alignments, and the underlying structure of how images are sent to the image passthrough
   * quads for the Head Mounted Rig. 
   * 
   * Enabling advanced mode will make these values visible in the editor.
   */

#define ADVANCED_MODE_ENABLED

#if !ADVANCED_MODE_ENABLED
using UnityEngine;
using UnityEditor;
using System.Collections;

/*
 * Known Issue: 
 * - Doesn't work properly with other custom property drawers, such as list views
 * - Doesn't block out headers
 */
[CustomPropertyDrawer(typeof(AdvancedModeOnlyAttribute))]
public class AdvancedModeOnlyPropertyDrawer : PropertyDrawer {

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return 0;
  }

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }

}
#endif