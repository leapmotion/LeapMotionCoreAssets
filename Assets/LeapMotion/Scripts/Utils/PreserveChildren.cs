using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PreserveChildren : MonoBehaviour
{
		[HideInInspector]
		public List<GameObject> preserved;

		void Awake ()
		{
				preserved = new List<GameObject> ();
		}

		void OnDestroy ()
		{
				foreach (GameObject child in preserved) {
						child.transform.parent = null;
				}
		}
}
