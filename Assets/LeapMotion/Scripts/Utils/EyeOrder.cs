using UnityEngine;
using System;
using System.Collections;

[System.Serializable]
public class EyeType {

  public enum OrderType{
    LEFT = 0,
    RIGHT = 1,
    CENTER = 2
  }

  [SerializeField]
  private OrderType _orderType = OrderType.LEFT;

  private bool _isOnFirst = false;
  private bool _hasBegun = false;

  public OrderType Type {
    get {
      return _orderType;
    }
  }

  public bool IsLeftEye {
    get {
      if (!_hasBegun) {
        throw new Exception("Cannot call IsLeftEye or IsRightEye before BeginCamera has been called!");
      }

      switch (_orderType) {
        case OrderType.LEFT: return true;
        case OrderType.RIGHT: return false;
        case OrderType.CENTER: return _isOnFirst;
        default: throw new Exception("Unexpected order type " + _orderType);
      }
    }
  }

  public bool IsRightEye {
    get {
      return !IsLeftEye;
    }
  }

  public EyeType(string name) {
    string lower = name.ToLower();
    if (lower.Contains("left")) {
      _orderType = OrderType.LEFT;
    } else if (lower.Contains("right")) {
      _orderType = OrderType.RIGHT;
    } else {
      _orderType = OrderType.CENTER;
    }
  }

  public EyeType(OrderType type) {
    _orderType = type;
  }

  public void BeginCamera() {
    if (!_hasBegun) {
      _isOnFirst = true;
      _hasBegun = true;
    } else {
      _isOnFirst = !_isOnFirst;
    }
  }

  public void Reset() {
    _hasBegun = false;
  }

  
}
