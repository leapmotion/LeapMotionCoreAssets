using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using VRWidgets;
using LMWidgets;

namespace VRWidgets
{

	public class DialGraphics : MonoBehaviour, AnalogInteractionHandler<int>{
//		private string currentDialValue; 
//		public string CurrentDialValue   
//		{
//			get 
//			{
//				return currentDialValue; 
//			}
//			set
//			{
//				Debug.Log ("CurrentDialValue being Set");
//				currentDialValue = value;
//				CurrentDialInt = ParseDialString(value);
//				EditorDisplayString = value;
//			}
//		}
		private int currentDialInt; 
		public int CurrentDialInt
		{
			get 
			{
				return currentDialInt; 
			}
			set
			{
				//Debug.Log ("CurrentDialInt being Set to: " + value);
				currentDialInt = value;
				EditorDisplayInt = value;
				
			}
		}
		public DataBinderInt WidgetController;
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
		private DialModeBase dialModeBase;
		public Transform DialCenter;
		public List<float> LabelAngles;
		public Dictionary<string, float> DialLabelAngles = new Dictionary<string, float>();
		
		
		public Color PickerColorInActive;
		public Color PickerColorActive;
		public Image PickerBoxImage;
		
		public HilightTextVolume hilightTextVolume;
		
		public enum PickerType {Generic, Year, Month, Day, Hour};
		public PickerType thisPickerType;
		
		public Color TextColor;
		
		private int ParseDialString (string valueString){
			if(thisPickerType == PickerType.Generic){
				return Convert.ToInt32( valueString);
			}
			if(thisPickerType == PickerType.Year){
				return Convert.ToInt32( valueString);
			}
			if(thisPickerType == PickerType.Month){
				return MonthLabels.IndexOf(valueString);
			}
			if(thisPickerType == PickerType.Day){
				return Convert.ToInt32( valueString);
			}
			if(thisPickerType == PickerType.Hour){
				return HourLabels.IndexOf(valueString);
			} 
			return 0;
		}
		//covert the integer from WidgetController.GetCurrentData() to index integer
		private int ParseDialInt (int valueInt){
			if(thisPickerType == PickerType.Generic){
				return valueInt;
			}
			if(thisPickerType == PickerType.Year){
//				string valueString = Convert.ToString(valueInt);
//				return YearLabels.IndexOf(valueString);
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
		
		public event EventHandler<EventArg<int>> ChangeHandler;
		public event EventHandler<EventArg<int>> StartHandler;
		public event EventHandler<EventArg<int>> EndHandler;
		
		void Start () {
			dialModeBase = DialPhysics.GetComponent<DialModeBase>();
			if(thisPickerType == PickerType.Generic){
				DialLabels = GenericLabels;
			}
			if(thisPickerType == PickerType.Year){
				DialLabels = YearLabels;
			}
			if(thisPickerType == PickerType.Month){
				DialLabels = MonthLabels;
				LabelAngleRangeEnd = 180f;
			}
			if(thisPickerType == PickerType.Day){
				DialLabels = DayLabels;
			}
			if(thisPickerType == PickerType.Hour){
				DialLabels = HourLabels;
			}
				
			DialCenter.localPosition = new Vector3(0f, 0f, DialRadius);
			DialPhysicsOffset.localPosition = new Vector3(-DialRadius * 10f, 0f, 0f);
			
			
			float currentLayoutXAngle = LabelAngleRangeStart;
			int counter = 1;
			foreach(string label in DialLabels){
	//			Vector3 labelLocalRotation = new Vector3(0.0f, 0.0f, 0.0f);
				Transform labelPrefab = Instantiate(LabelPrefab, DialCenter.transform.position, transform.rotation) as Transform;
				labelPrefab.Rotate(currentLayoutXAngle, 0f, 0f);
				LabelAngles.Add (-currentLayoutXAngle);			
				labelPrefab.parent = DialCenter;
				labelPrefab.localScale = new Vector3(1f, 1f, 1f);
				Text labelText = labelPrefab.GetComponentInChildren<Text>();
				labelText.text = DialLabels[counter - 1];
				DialLabelAngles.Add(DialLabels[counter - 1], -currentLayoutXAngle);
				labelText.transform.localPosition = new Vector3(0f, 0f, -DialRadius);
				currentLayoutXAngle = ((Mathf.Abs(LabelAngleRangeStart) + Mathf.Abs(LabelAngleRangeEnd))/(DialLabels.Count)) * -counter;
				counter++; 	
			}
			LabelPrefab.gameObject.SetActive(false);
			if(WidgetController != null){
				//Set the Dial value based on an int
				CurrentDialInt = WidgetController.GetCurrentData();
				//Debug.Log (thisPickerType + ": widgetController.GetCurrentData() = " + CurrentDialInt);
				SetPhysicsStep(CurrentDialInt);
//				WidgetController.DataChangedHandler += OnDataChanged;
				
			}
			
		}
	
		public bool IsEngaged = false;
		void Update () {
			Vector3 physicsRotation = new Vector3 (DialPhysics.localRotation.eulerAngles.y, 0f, 0f);
			DialCenter.localEulerAngles = physicsRotation;
			CurrentDialInt = ParseDialInt (dialModeBase.CurrentStep);
//			CurrentDialValue = hilightTextVolume.CurrentHilightValue;
			if(IsEngaged == true){
				if(WidgetController != null){
					//Set the Dial value based on an int
			        WidgetController.SetCurrentData(CurrentDialInt);
				}
				if(ChangeHandler != null){
					//Debug.Log ("ChangeHandler event firing");
					ChangeHandler(this, new EventArg<int>( CurrentDialInt));
				}
			}
		}
		public void HiLightDial (){
			IsEngaged = true;
			if(StartHandler != null){	
				//Debug.Log ("HiLightDial() event firing");
				StartHandler(this, new EventArg<int>(CurrentDialInt));
			}
			PickerBoxImage.color = PickerColorActive;
		}
		
		public void UpdateDial (){
			CurrentDialInt = ParseDialInt (dialModeBase.CurrentStep);
			if(WidgetController != null){
				//Set the Dial value based on an int
				//make sure we are what the program thinks we are
				WidgetController.SetCurrentData(CurrentDialInt);
			}
			if(EndHandler != null){
				//Debug.Log ("UpdateDial() event firing");
				EndHandler(this, new EventArg<int>(CurrentDialInt));
	      }
			IsEngaged = false;
			PickerBoxImage.color = PickerColorInActive;
	    }
		
		public void SetPhysicsStep(int newInt){
			int newStep = 0;
			if(thisPickerType == PickerType.Month){
				newInt = newInt - 1;
				newStep = newInt;
			}
			else if(DialLabels.Contains (Convert.ToString(newInt))){
				newStep = DialLabels.IndexOf(Convert.ToString(newInt));
//				Debug.Log (thisPickerType + ": SetPhysicsStep found " + (newInt -1) + "in DialLabeAngles with index of " + newStep);
			}
//			Debug.Log (thisPickerType + ": newStep  = " + newStep);

			dialModeBase.CurrentStep = newStep;
		}
//		private void OnDataChanged (object sender, VRWidgets.EventArg<int> args){
//			Debug.Log("OnDateChange: " + args.CurrentValue);
//			if(IsEngaged) {return;}
//			CurrentDialInt = WidgetController.GetCurrentData();
//			SetDialByInt(CurrentDialInt);
//		}
	}
}
