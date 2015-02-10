using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace LMWidgets
{
	public class DialGraphics : MonoBehaviour, AnalogInteractionHandler<int>, IDataBoundWidget<DialGraphics, string>
  {
    public event EventHandler<EventArg<int>> ChangeHandler;
    public event EventHandler<EventArg<int>> StartHandler;
    public event EventHandler<EventArg<int>> EndHandler;

    protected DataBinderDial m_dataBinder;

		private string m_currentDialValue; 
		public string CurrentDialValue   
		{
			get 
			{
				return m_currentDialValue; 
			}
			set
			{
        int dialIndex = 0;

        try { 
          dialIndex = parseDialString(value); 
        }
        catch (ArgumentException e) { // Thrown if 'value' isn't a valid label;
          Debug.LogException(e);
          return;
        }

        m_currentDialValue = value;
        CurrentDialInt = dialIndex;
				EditorDisplayString = value;
			}
		}

    private int m_currentDialInt;
    public int CurrentDialInt
    {
      get 
      {
        return m_currentDialInt; 
      }
			set
			{
				if(m_currentDialInt != value){
					SetPhysicsStep(value);
				}
				m_currentDialInt = value;
				EditorDisplayInt = value;

        if ( !m_dialLabelsInitilized ) {
          initializeDialLabels ();
        }

        try { 
          m_currentDialValue = DialLabels[m_currentDialInt];
        }
        catch (System.ArgumentOutOfRangeException e ){
          Debug.LogException(e);
        }
			}
		}
		
		public string EditorDisplayString;
		public int EditorDisplayInt;
		
		public List<string> GenericLabels;
		public List<string> DialLabels;
		public List<string> YearLabels;
		public List<string> MonthLabels;
		public List<string> DayLabels;
		public List<string> HourLabels;
		public float DialRadius = .07f;
		public float LabelAngleRangeStart = 0f;
		public float LabelAngleRangeEnd = 360f;
		public Transform LabelPrefab;
		public Transform DialPhysicsOffset;
		public Transform DialPhysics;
		private DialModeBase m_dialModeBase;
		public Transform DialCenter;
		public List<float> LabelAngles;
		public Dictionary<string, float> DialLabelAngles = new Dictionary<string, float>();

    public bool IsEngaged = false;
		
		public Color PickerColorInActive;
		public Color PickerColorActive;
		public Image PickerBoxImage;
		
		public HilightTextVolume hilightTextVolume;
		
		public enum PickerType {Generic, Year, Month, Day, Hour};
		public PickerType thisPickerType;
		
		public Color TextColor;

    private bool m_dialLabelsInitilized = false;

    private int parseDialString (string valueString){
      int index = -1;

      if(thisPickerType == PickerType.Generic){
				index = GenericLabels.IndexOf( valueString);
			}
			if(thisPickerType == PickerType.Year){
        index = Convert.ToInt32( valueString);
			}
			if(thisPickerType == PickerType.Month){
        index = MonthLabels.IndexOf(valueString) + 1;
			}
			if(thisPickerType == PickerType.Day){
        index = Convert.ToInt32( valueString);
			}
			if(thisPickerType == PickerType.Hour){
        index = HourLabels.IndexOf(valueString);
			} 

      if (index == -1) {
        throw new System.ArgumentException("valueString \"" + valueString + "\" is not a valid label.");
      }

      return index;
		}

    public void SetWidgetValue(string value) {
      CurrentDialValue = value;
    }

		//covert the integer from WidgetController.GetCurrentData() to index integer
		private int parseDialInt (int valueInt){
			if(thisPickerType == PickerType.Generic){
				return valueInt;
			}
			if(thisPickerType == PickerType.Year){
				return Convert.ToInt32( YearLabels[valueInt]);
				
			}
			if(thisPickerType == PickerType.Month){
				return valueInt + 1;
			}
			if(thisPickerType == PickerType.Day){
				return valueInt + 1;
			}
			if(thisPickerType == PickerType.Hour){
				return valueInt;
			}
			return 0;
		}

    // Stop listening to any previous data binder and start listening to the new one.
    public void RegisterDataBinder(DataBinder<DialGraphics, string> dataBinder) {
      if (dataBinder == null) {
        return;
      }
      
      UnregisterDataBinder ();
      m_dataBinder = dataBinder as DataBinderDial;
      CurrentDialValue = m_dataBinder.GetCurrentData ();
    }
    
    // Stop listening to any previous data binder.
    public void UnregisterDataBinder() {
      m_dataBinder = null;
    }

    void Awake() {
      if (m_dialModeBase == null) {
        m_dialModeBase = DialPhysics.GetComponent<DialModeBase> ();
      }

      if (m_dialModeBase == null) {
        throw new System.NullReferenceException("Could not find DialModeBase on DialPhysics Object.");
      }

      initializeDialLabels ();
    }

    private void initializeDialLabels() {
      if (m_dialLabelsInitilized) {
        return;
      }

      if(thisPickerType == PickerType.Generic){
        DialLabels = GenericLabels;
      }
      else if(thisPickerType == PickerType.Year){
        DialLabels = YearLabels;
      }
      else if(thisPickerType == PickerType.Month){
        DialLabels = MonthLabels;
        LabelAngleRangeEnd = 180f;
      }
      else if(thisPickerType == PickerType.Day){
        DialLabels = DayLabels;
      }
      else if(thisPickerType == PickerType.Hour){
        DialLabels = HourLabels;
      }

      m_dialLabelsInitilized = true;
    }
    		
		void Start () {
			DialCenter.localPosition = new Vector3(0f, 0f, DialRadius);
			DialPhysicsOffset.localPosition = new Vector3(-DialRadius * 10f, 0f, 0f);
			
			
			float currentLayoutXAngle = LabelAngleRangeStart;

      for( int i=1; i<=DialLabels.Count; i++ ) {
				Transform labelPrefab = Instantiate(LabelPrefab, DialCenter.transform.position, transform.rotation) as Transform;
				labelPrefab.Rotate(currentLayoutXAngle, 0f, 0f);
				LabelAngles.Add (-currentLayoutXAngle);			
				labelPrefab.parent = DialCenter;
				labelPrefab.localScale = new Vector3(1f, 1f, 1f);
				Text labelText = labelPrefab.GetComponentInChildren<Text>();
				labelText.text = DialLabels[i - 1];
				DialLabelAngles.Add(DialLabels[i - 1], -currentLayoutXAngle);
				labelText.transform.localPosition = new Vector3(0f, 0f, -DialRadius);
				currentLayoutXAngle = ((Mathf.Abs(LabelAngleRangeStart) + Mathf.Abs(LabelAngleRangeEnd))/(DialLabels.Count)) * -i;
			}

			LabelPrefab.gameObject.SetActive(false);

			if( m_dataBinder != null ) {
				//Set the Dial value based on a string
        CurrentDialValue = m_dataBinder.GetCurrentData();
				SetPhysicsStep(CurrentDialInt);
			}
		}
		
		void Update () {

			Vector3 physicsRotation = new Vector3 (DialPhysics.localRotation.eulerAngles.y, 0f, 0f);
			DialCenter.localEulerAngles = physicsRotation;
			CurrentDialInt = parseDialInt (m_dialModeBase.CurrentStep);

			if(IsEngaged == true){
				if(m_dataBinder != null){
          m_dataBinder.SetCurrentData(CurrentDialValue); //Set the Dial value based on an int
				}

				if(ChangeHandler != null){
					ChangeHandler(this, new EventArg<int>( CurrentDialInt));
				}
			}
		}

		public void HilightDial () {
			IsEngaged = true;
			
      if( StartHandler != null )  {	
				StartHandler(this, new EventArg<int>(CurrentDialInt));
			}

			PickerBoxImage.color = PickerColorActive;
		}
		
		public void UpdateDial (){
			CurrentDialInt = parseDialInt (m_dialModeBase.CurrentStep);
			
      if(m_dataBinder != null){
				//Set the Dial value based on a string
				//make sure we are what the program thinks we are
        m_dataBinder.SetCurrentData(CurrentDialValue);
			}

			if(EndHandler != null){
				EndHandler(this, new EventArg<int>(CurrentDialInt));
      }

			IsEngaged = false;
			PickerBoxImage.color = PickerColorInActive;
    }
		
		public void SetPhysicsStep(int newInt){
      if (m_dialModeBase == null) {
        m_dialModeBase = DialPhysics.GetComponent<DialModeBase>();
      }

			if(thisPickerType == PickerType.Month || thisPickerType == PickerType.Day){
				newInt = newInt - 1;
			}

			m_dialModeBase.CurrentStep = newInt;
			
		}
	}
}
