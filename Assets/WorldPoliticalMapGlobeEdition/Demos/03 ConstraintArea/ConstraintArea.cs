using UnityEngine;
using System.Collections;
using WPM;

public class ConstraintArea : MonoBehaviour {

	// Use this for initialization
	void Start () {
		WorldMapGlobe map = WorldMapGlobe.instance;

		// Gets France center in sphere coordinates
		int countryIndex = map.GetCountryIndex("France");
		Vector3 countryCenter = map.countries[countryIndex].center;

		// Center on France and set constraint around country center
		map.FlyToLocation(countryCenter, 0);
		map.constraintPosition = countryCenter;
		map.constraintAngle = 5f;
		map.constraintPositionEnabled = true;

		// Set zoom level and stop rotation
		map.SetZoomLevel(0.1f);
		map.autoRotationSpeed = 0f;

	}

}
