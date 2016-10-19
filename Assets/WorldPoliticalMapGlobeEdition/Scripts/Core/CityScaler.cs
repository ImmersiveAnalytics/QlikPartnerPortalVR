using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{
	/// <summary>
	/// City scaler. Checks the city icons' size is always appropiate
	/// </summary>
	public class CityScaler : MonoBehaviour
	{

		const int CITY_SIZE_ON_SCREEN = 10;
		Vector3 lastCamPos, lastPos;
		float lastIconSize;
		float lastCustomSize;

		[NonSerialized]
		public WorldMapGlobe map;

		void Start ()
		{
			ScaleCities ();
		}
	
		// Update is called once per frame
		void Update ()
		{
			if (lastPos == transform.position && lastCamPos == Camera.main.transform.position && lastIconSize == map.cityIconSize)
				return;
			ScaleCities ();
		}

		public void ScaleCities ()
		{
			lastPos = transform.position;
			lastCamPos = Camera.main.transform.position;
			lastIconSize = map.cityIconSize;
			float oldFV = Camera.main.fieldOfView;
			Camera.main.fieldOfView = 60.0f;
			Vector3 refPos = transform.position;
			if (map.earthInvertedMode) refPos += Vector3.forward; // otherwise, transform.position = 0 in inverted mode
			Vector3 a = Camera.main.WorldToScreenPoint(refPos);
			Vector3 b = new Vector3(a.x, a.y + CITY_SIZE_ON_SCREEN, a.z);
			if ( Camera.main.pixelWidth==0) return; // Camera pending setup
			Vector3 aa =  Camera.main.ScreenToWorldPoint(a);
			Vector3 bb =  Camera.main.ScreenToWorldPoint(b);
			Camera.main.fieldOfView = oldFV;
			float scale = (aa - bb).magnitude * map.cityIconSize;
			scale = Mathf.Clamp(scale, 0.00001f, 0.005f);
			Vector3 newScale = new Vector3 (scale, scale, 1.0f);
			foreach (Transform t in transform.FindChild("Normal Cities"))
				t.localScale = newScale;
			foreach (Transform t in transform.FindChild("Region Capitals"))
				t.localScale = newScale * 1.75f;
			foreach (Transform t in transform.FindChild("Country Capitals"))
				t.localScale = newScale * 2.0f;

		}

		public void ScaleCities(float customSize) {
			customSize = Mathf.Clamp(customSize, 0, 0.005f);
			if (customSize==lastCustomSize) return;
			lastCustomSize = customSize;
			Vector3 newScale = new Vector3(customSize, customSize, 1);
			foreach (Transform t in transform.FindChild("Normal Cities"))
				t.localScale = newScale;
			foreach (Transform t in transform.FindChild("Region Capitals"))
				t.localScale = newScale * 1.75f;
			foreach (Transform t in transform.FindChild("Country Capitals"))
				t.localScale = newScale * 2.0f;
		}
	}

}