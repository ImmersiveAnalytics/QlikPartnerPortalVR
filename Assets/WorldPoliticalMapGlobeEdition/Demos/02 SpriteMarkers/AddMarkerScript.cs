using UnityEngine;
using System.Collections;
using WPM;

public class AddMarkerScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		float selected_latitude = 40.71f;
		float selected_longitude = -74f;
		Sprite selected_sprite = Resources.Load<Sprite>("NewYork");

		WorldMapGlobe map = WorldMapGlobe.instance;
		map.calc.fromLatDec = selected_latitude;
		map.calc.fromLonDec = selected_longitude;
		map.calc.fromUnit = UNIT_TYPE.DecimalDegrees;
		map.calc.Convert ();
		Vector3 sphereLocation = map.calc.toSphereLocation;

		// Create sprite
		GameObject destinationSprite = new GameObject();                                 
		SpriteRenderer dest_sprite = destinationSprite.AddComponent<SpriteRenderer>();                      
		dest_sprite.sprite = selected_sprite;

		// Add sprite billboard to the map with custom scale, billboard mode and little bit elevated from surface (to prevent clipping with city spots)
		map.AddMarker(destinationSprite, sphereLocation, 0.02f, true, 0.1f);

		// Locate it on the map
		map.FlyToLocation (sphereLocation);
		map.autoRotationSpeed = 0f;
	}

}
