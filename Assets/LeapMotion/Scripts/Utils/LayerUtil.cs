using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class LayerUtil {

  private static Dictionary<string, int> _nameToLayerIndex = new Dictionary<string, int>();

  /// <summary>
  /// This method functions much like the built in LayerMask.NameToLayer method.  If
  /// allowAssignEmpty is set to true, when a layerName is passed in that does not 
  /// correspond to a defined layer, that layerName will be assigned to an empty 
  /// layer slot.  Future calls to GetLayerIndex with the same layerName will continue
  /// to report the same layer name until the game ends.  
  /// 
  /// If the layer could not be found, or if there was no free layers to assign a
  /// layername to, this method will return -1
  /// </summary>
  /// <param name="layerName"></param>
  /// <param name="allowAssignEmpty"></param>
  /// <returns></returns>
  public static int GetLayerIndex(string layerName, bool allowAssignEmpty = true) {
    int index;
    if(_nameToLayerIndex.TryGetValue(layerName, out index)){
      return index;
    }

    index = LayerMask.NameToLayer(layerName);
    if (index == -1 && allowAssignEmpty) {
      index = assignLayerNewIndex(layerName);
    }

    return index;
  }

  /// <summary>
  /// This method functions much like the built in LayerMask.GetMask method, and follows
  /// the same assignment logic as GetLayerIndex.  If the layer could not be found, 
  /// or could not be assigned due to no free layers, this method will return 0.
  /// </summary>
  /// <param name="layer"></param>
  /// <param name="allowAssignEmpty"></param>
  /// <returns></returns>
  public static int GetLayerMask(string layer, bool allowAssignEmpty = true) {
    int index = GetLayerIndex(layer, allowAssignEmpty);
    if (index == -1) {
      return 0;
    }
    return 1 << index;
  }

  private static int assignLayerNewIndex(string layerName) {
    for (int i = 8; i < 32; i++) {
      if (LayerMask.LayerToName(i) == "" && !_nameToLayerIndex.ContainsValue(i)) {
        _nameToLayerIndex[layerName] = i;
        return i;
      }
    }
    return -1;
  }
}
