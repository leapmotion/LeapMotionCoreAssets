using UnityEngine;
using System.Collections;

/// <summary>
/// Redefine the parent of an object at run time.
/// </summary>
/// <remarks>
/// This allows prefab parenting without prefab merging.
/// </remarks>
public class AdoptParent : MonoBehaviour
{
		public GameObject adoptiveParent;
		public string adoptiveParentName;
		public bool worldPositionStays = true;

		void Start ()
		{
				if (adoptiveParent == null) {
						adoptiveParent = GameObject.Find (adoptiveParentName);
				}
				if (adoptiveParent) {
						transform.SetParent(adoptiveParent.transform, worldPositionStays);
				}
		}
}
