using UnityEngine;
using System;
using System.Collections.Generic;

namespace LMWidgets {

  // Interface to define an object that can be a data provider to a widget.
  public abstract class DataBinder<WidgetType, PayloadType> : MonoBehaviour where WidgetType : IDataBoundWidget<WidgetType, PayloadType> {
    [SerializeField]
    private List<WidgetType> m_widgets;

    private PayloadType m_lastDataValue;

    // Fires when the data is updated with the most recent data as the payload
    public event EventHandler<EventArg<PayloadType>> DataChangedHandler;
    
    // Returns the current system value of the data.
    // In the default implementation of the data-binder this is called every frame (in Update) so it's best to keep
    // this implementation light weight.
    abstract public PayloadType GetCurrentData();
    
    // Set the current system value of the data.
    abstract protected void setDataModel(PayloadType value);

    public void SetCurrentData(PayloadType value) {
      setDataModel (value);
      updateLinkedWidgets ();
      fireDataChangedEvent (GetCurrentData ());
      m_lastDataValue = GetCurrentData ();
    }

    private void updateLinkedWidgets() {
      foreach(WidgetType widget in m_widgets) {
        widget.SetWidgetValue(GetCurrentData());
      }
    }

    virtual protected void Awake() {
      foreach (WidgetType widget in m_widgets) {
        widget.RegisterDataBinder(this);
      }
    }

    // Grab the inital value of GetCurrentData
    virtual protected void Start() {
      m_lastDataValue = GetCurrentData();
    }

    // Checks for change in data.
    void Update() {
      PayloadType currentData = GetCurrentData();
      if (!compare (m_lastDataValue, currentData)) {
        updateLinkedWidgets ();
        fireDataChangedEvent (currentData);
      }
      m_lastDataValue = currentData;
    }

    protected void fireDataChangedEvent(PayloadType currentData) {
      EventHandler<EventArg<PayloadType>> handler = DataChangedHandler;
      if ( handler != null ) { handler(this, new EventArg<PayloadType>(currentData)); }
    }

    // Handles proper comparison of generic types.
    private bool compare(PayloadType x, PayloadType y)
    {
      return EqualityComparer<PayloadType>.Default.Equals(x, y);
    }
  }

  public abstract class DataBinderSlider : DataBinder<SliderBase, float> {};
  public abstract class DataBinderToggle : DataBinder<ButtonToggleBase, bool> {};
}