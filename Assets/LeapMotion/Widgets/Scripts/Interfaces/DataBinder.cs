using UnityEngine;
using System;
using System.Collections.Generic;

namespace LMWidgets {
  // Interface to define an object that can be a data provider to a widget.
  public abstract class DataBinder<T> : MonoBehaviour {
    private T m_lastDataValue;

    // Fires when the data is updated with the most recent data as the payload
    public event EventHandler<EventArg<T>> DataChangedHandler;
    
    // Returns the current system value of the data.
    // In the default implementation of the data-binder this is called every frame (in Update) so it's best to keep
    // this implementation light weight.
    abstract public T GetCurrentData();
    
    // Set the current system value of the data.
    abstract public void SetCurrentData(T value);

    // Grab the inital value of GetCurrentData
    void Start() {
      m_lastDataValue = GetCurrentData();
    }

    // Checks for change in data.
    void Update() {
      T currentData = GetCurrentData();

      if ( !compare (m_lastDataValue, currentData ) ) {
        EventHandler<EventArg<T>> handler = DataChangedHandler;
        if ( handler != null ) { handler(this, new EventArg<T>(currentData)); }
      }

      m_lastDataValue = currentData;
    }

    // Handles proper comparison of generic types.
    private bool compare(T x, T y)
    {
      return EqualityComparer<T>.Default.Equals(x, y);
    }
  }

  // Non generic hacks so we can show these serialized in the Unity Editor
  public abstract class DataBinderBool : DataBinder<bool> {};
  public abstract class DataBinderFloat : DataBinder<float> {};
  public abstract class DataBinderVector3 : DataBinder<Vector3> {};
  public abstract class DataBinderDateTime : DataBinder<DateTime> {};
  public abstract class DataBinderInt : DataBinder<int> {};
}