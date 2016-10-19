using UnityEngine;
using System;
using System.Collections;
using Poly2Tri;

namespace WPM {

	public enum UNIT_TYPE {
		Degrees,
		DecimalDegrees,
		SphereCoordinates
	}

	[Serializable]
	[RequireComponent(typeof(WorldMapGlobe))]
	public class WorldMapCalculator : MonoBehaviour {

		public UNIT_TYPE fromUnit = UNIT_TYPE.Degrees;

		// From: latitude (degree)
		public float fromLatDegrees;
		public int fromLatMinutes;
		public float fromLatSeconds;
		// From: longitude (degree)
		public float fromLonDegrees;
		public int fromLonMinutes;
		public float fromLonSeconds;
		// From: decimal degrees
		public float fromLatDec, fromLonDec;
		// From: spherical coordinates
		public float fromX, fromY, fromZ;
		// To: latitude (degree)
		public float toLatDegree;
		public int toLatMinute;
		public float toLatSeconds;
		// To: longitude (degree)
		public float toLonDegree;
		public int toLonMinute;
		public float toLonSecond;
		// To: decimal degrees
		public float toLatDec, toLonDec;
		// To: spherical coordinates
		public float toX, toY, toZ;
		public bool captureCursor;
		public bool isDirty;

		public int GUICityDistanceFrom=-1, GUICityDistanceTo=-1;
		public int GUICountryDistanceFrom=-1, GUICountryDistanceTo=-1;
		public int countryDistanceFrom=-1, countryDistanceTo=-1;
		public string cityDistanceResult = "";


		WorldMapGlobe _map;
		/// <summary>
		/// Accesor to the World Map Globe core API
		/// </summary>
		public WorldMapGlobe map { get { 
				if (_map==null) _map = GetComponent<WorldMapGlobe> ();
				return _map;
			}
		}

		/// <summary>
		/// Returns a Vector3 value with the converted position in spherical coordinates
		/// </summary>
		public Vector3 toSphereLocation { get { return new Vector3(toX, toY, toZ)*0.5f; } }

		/// <summary>
		/// Returns or set the vector3 value with the source position in spherical coordinates
		/// </summary>
		public Vector3 fromSphereLocation { get { return new Vector3(fromX, fromY, fromZ); } set { fromX = value.x; fromY = value.y; fromZ = value.z; } }

		/// <summary>
		/// Returns either "N" or "S" depending on the converted latitude
		/// </summary>
		public string toLatCardinal {
			get {
				return toLatDec >=0 ? "N": "S";
			}
		}

		/// <summary>
		/// Returns either "W" or "E" depending on the converted longitude
		/// </summary>
		public string toLonCardinal {
			get {
				return toLonDec >=0 ? "E": "W";
			}
		}

		Vector3 lastCursorPos;
		public string errorMsg;

		void Update () {
			if (captureCursor) {
				if (map != null && map.cursorLocation != lastCursorPos) {
					if (map.transform.localScale.z!=1.0f) {
						lastCursorPos = map.cursorLocation.normalized;
					} else {
						lastCursorPos = map.cursorLocation;
					}
			
					fromX = map.cursorLocation.x;
					fromY = map.cursorLocation.y;
					fromZ = map.cursorLocation.z;
					Convert ();
				}
				if (Input.GetKeyDown(KeyCode.C)) {
					captureCursor = false;
				}
			}
		}

		public bool Convert () {
			errorMsg = "";
			try {
				if (fromUnit == UNIT_TYPE.Degrees) {
					toLatDegree = fromLatDegrees;
					toLatMinute = fromLatMinutes;
					toLatSeconds = fromLatSeconds;
					toLonDegree = fromLonDegrees;
					toLonMinute = fromLonMinutes;
					toLonSecond = fromLonSeconds;
					toLatDec = fromLatDegrees + fromLatMinutes / 60.0f + fromLatSeconds / 3600.0f;
					toLonDec = fromLonDegrees + fromLonMinutes / 60.0f + fromLonSeconds / 3600.0f;
					float phi = toLatDec * Mathf.Deg2Rad;
					float theta = (toLonDec + 90.0f) * Mathf.Deg2Rad;
					toX = Mathf.Cos (phi) * Mathf.Cos (theta);
					toY = Mathf.Sin (phi);
					toZ = Mathf.Cos (phi) * Mathf.Sin (theta);
				} else if (fromUnit == UNIT_TYPE.DecimalDegrees) {
					toLatDec = fromLatDec;
					toLonDec = fromLonDec;
					toLatDegree = (int)fromLatDec;
					toLatMinute = (int)(Mathf.Abs (fromLatDec) * 60) % 60;
					toLatSeconds = (Mathf.Abs (fromLatDec) * 3600) % 60;
					toLonDegree = (int)fromLonDec;
					toLonMinute = (int)(Mathf.Abs (fromLonDec) * 60) % 60;
					toLonSecond = (Mathf.Abs (fromLonDec) * 3600) % 60;
					float phi = fromLatDec * Mathf.Deg2Rad;
					float theta = (fromLonDec + 90.0f) * Mathf.Deg2Rad;
					toX = Mathf.Cos (phi) * Mathf.Cos (theta);
					toY = Mathf.Sin (phi);
					toZ = Mathf.Cos (phi) * Mathf.Sin (theta);
				} else if (fromUnit == UNIT_TYPE.SphereCoordinates) {
					float phi = Mathf.Asin (fromY*2.0f);
					float theta = Mathf.Atan2(fromX, fromZ);
					toLatDec = phi * Mathf.Rad2Deg;
					toLonDec = -theta * Mathf.Rad2Deg;
					toLatDegree = (int)toLatDec;
					toLatMinute = (int)(Mathf.Abs (toLatDec) * 60) % 60;
					toLatSeconds = (Mathf.Abs (toLatDec) * 3600) % 60;
					toLonDegree = (int)toLonDec;
					toLonMinute = (int)(Mathf.Abs (toLonDec) * 60) % 60;
					toLonSecond = (Mathf.Abs (toLonDec) * 3600) % 60;
					toX = fromX;
					toY = fromY;
					toZ = fromZ;
				}
			} catch (ApplicationException ex) {
				errorMsg = ex.Message;
			}
			isDirty = true;
			return errorMsg.Length == 0;
		}

		public void FlyTo () {
			if (map != null) {
				map.FlyToLocation (toSphereLocation);
			} else {
				Debug.LogWarning ("FlyTo requires WorldMapGlobe component.");
			}
		}

		/// <summary>
		/// Returns a formatted lat/lon coordinates string based on the current cursor position
		/// </summary>
		/// <value>The pretty current lat lon.</value>
		public string prettyCurrentLatLon {
			get {
				fromUnit = UNIT_TYPE.SphereCoordinates;
				fromSphereLocation = map.cursorLocation;
				Convert ();
//				return toLatDec.ToString("F5") + " " + toLonDec.ToString("F5");
				return string.Format("{0}°{1}'{2:F2}\"{3} {4}°{5}'{6:F2}\"{7}", Mathf.Abs (toLatDegree), toLatMinute, toLatSeconds, toLatCardinal, Mathf.Abs (toLonDegree), toLonMinute, toLonSecond, toLonCardinal);
			}
		}

		/// <summary>
		/// Returns distance in meters between two lat/lon coordinates
		/// </summary>
		public float Distance(float latDec1, float lonDec1, float latDec2, float lonDec2) {
			float R = 6371000; // metres
			float phi1 = latDec1 * Mathf.Deg2Rad;
			float phi2 = latDec2 * Mathf.Deg2Rad;
			float deltaPhi = (latDec2-latDec1)* Mathf.Deg2Rad;
			float deltaLambda = (lonDec2-lonDec1)* Mathf.Deg2Rad;
			
			float a = Mathf.Sin(deltaPhi/2) * Mathf.Sin(deltaPhi/2) +
				Mathf.Cos(phi1) * Mathf.Cos(phi2) *
					Mathf.Sin(deltaLambda/2) * Mathf.Sin(deltaLambda/2);
			float c = 2.0f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1.0f-a));
			return R * c;
		}
		
		/// <summary>
		/// Returns distance in meters between two cities
		/// </summary>
		public float Distance(City city1, City city2) {
			PolygonPoint latlon1 = _map.GetLatLonFromSpherePoint(city1.unitySphereLocation);
			PolygonPoint latlon2 = _map.GetLatLonFromSpherePoint(city2.unitySphereLocation);
			return Distance (latlon1.Xf, latlon1.Yf, latlon2.Xf, latlon2.Yf);
		}

		/// <summary>
		/// Returns distance in meters from two sphere positions
		/// </summary>
		public float Distance(Vector3 position1, Vector3 position2) {
			float latDec1 = 180.0f * position1.y;
			float lonDec1 = 360.0f * (position1.x + 0.5f) - 180.0f;
			float latDec2 = 180.0f * position2.y;
			float lonDec2 = 360.0f * (position2.x + 0.5f) - 180.0f;
			return Distance (latDec1, lonDec1, latDec2, lonDec2);
			
		}


	}



}
