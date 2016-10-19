using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Text;
using WPM;

namespace WPM_Editor {
	[CustomEditor(typeof(WorldMapCalculator))]
	public class WorldMapCalculatorInspector : Editor {

		// attrezzo
		Texture2D _blackTexture;
		GUIStyle blackStyle;

		// units
		string[] unitNames;
		UNIT_TYPE fromUnit = UNIT_TYPE.Degrees;
		UNIT_TYPE toUnit = UNIT_TYPE.DecimalDegrees;

		// UI variables (need to be string)
		string fromLatDegree = "";
		string fromLatMinute = "";
		string fromLatSeconds = "";
		// From: longitude (degree)
		string fromLonDegree = "";
		string fromLonMinute = "";
		string fromLonSeconds = "";
		// From: decimal degrees
		string fromLatDec = "", fromLonDec = "";
		// From: spherical coordinates
		string fromX = "", fromY = "", fromZ = "";
		// To: latitude (degree)
		string toLatDegree = "";
		string toLatMinute = "";
		string toLatSeconds = "";
		// To: longitude (degree)
		string toLonDegree = "";
		string toLonMinute = "";
		string toLonSeconds = "";
		// To: decimal degrees
		string toLatDec = "", toLonDec = "";
		// To: spherical coordinates
		string toX = "", toY = "", toZ = "";

		// Other utility variables
		string errorMsg;
		WorldMapCalculator _calc;
		int lastCityCountFrom=-1, lastCityCountTo=-1;
		string[] _cityNamesFrom;
		string[] cityNamesFrom {
			get {
				if (_calc.map!=null && lastCityCountFrom!=_calc.map.cities.Count) {
					lastCityCountFrom = _calc.map.cities.Count;
					_cityNamesFrom = _calc.map.GetCityNames(_calc.countryDistanceFrom, true);
				}
				return _cityNamesFrom;
			}
		}

		string[] _cityNamesTo;
		string[] cityNamesTo {
			get {
				if (_calc.map!=null && lastCityCountTo!=_calc.map.cities.Count) {
					lastCityCountTo = _calc.map.cities.Count;
					_cityNamesTo = _calc.map.GetCityNames(_calc.countryDistanceTo, true);
				}
				return _cityNamesTo;
			}
		}


		int lastCountryCount=-1;
		string[] _countryNames;
		string[] countryNames {
			get {
				if (_calc.map!=null && lastCountryCount!=_calc.map.countries.Length) {
					_countryNames = _calc.map.GetCountryNames(true);
					lastCountryCount = _countryNames.Length;
				}
				return _countryNames;
			}
		}

		void OnEnable () {
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f): new Color(0.7f,0.7f,0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;
			unitNames = new string[] {
				"Degrees",
				"Decimal Degrees",
				"Sphere Coordinates"
			};
			errorMsg = "";
			_calc = (WorldMapCalculator)target;
		}

		public override void OnInspectorGUI () {
			if (_calc == null)
				return;

			bool runConversion = false;
			bool runCalcDistance = false;

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Convert From", GUILayout.Width (120));
			UNIT_TYPE oldUnit = fromUnit;
			fromUnit = (UNIT_TYPE)EditorGUILayout.Popup ((int)fromUnit, unitNames, GUILayout.MaxWidth (200));
			if (fromUnit != oldUnit)
				runConversion = true;
			EditorGUILayout.EndHorizontal ();

			switch (fromUnit) {
			case UNIT_TYPE.Degrees:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Latitude", GUILayout.Width (120));
				fromLatDegree = GUILayout.TextField (fromLatDegree, GUILayout.Width (40));
				GUILayout.Label ("°", GUILayout.Width (10));
				fromLatMinute = GUILayout.TextField (fromLatMinute, GUILayout.Width (40));
				GUILayout.Label ("'", GUILayout.Width (10));
				fromLatSeconds = GUILayout.TextField (fromLatSeconds, GUILayout.Width (80));
				GUILayout.Label ("''", GUILayout.Width (10));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Longitude", GUILayout.Width (120));
				fromLonDegree = GUILayout.TextField (fromLonDegree, GUILayout.Width (40));
				GUILayout.Label ("°", GUILayout.Width (10));
				fromLonMinute = GUILayout.TextField (fromLonMinute, GUILayout.Width (40));
				GUILayout.Label ("'", GUILayout.Width (10));
				fromLonSeconds = GUILayout.TextField (fromLonSeconds, GUILayout.Width (80));
				GUILayout.Label ("''", GUILayout.Width (10));
				EditorGUILayout.EndHorizontal ();
				break;
			case UNIT_TYPE.DecimalDegrees:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Latitude", GUILayout.Width (120));
				fromLatDec = GUILayout.TextField (fromLatDec, GUILayout.Width (80));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Longitude", GUILayout.Width (120));
				fromLonDec = GUILayout.TextField (fromLonDec, GUILayout.Width (80));
				EditorGUILayout.EndHorizontal ();
				break;
			case UNIT_TYPE.SphereCoordinates:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   X", GUILayout.Width (120));
				fromX = GUILayout.TextField (fromX, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Y", GUILayout.Width (120));
				fromY = GUILayout.TextField (fromY, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Z", GUILayout.Width (120));
				fromZ = GUILayout.TextField (fromZ, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Follow Cursor", GUILayout.Width (120));
				_calc.captureCursor = EditorGUILayout.Toggle (_calc.captureCursor, GUILayout.Width (20));
				GUIStyle warningLabelStyle = new GUIStyle (GUI.skin.label);
				warningLabelStyle.normal.textColor = new Color (0.31f, 0.38f, 0.56f);
				if (!Application.isPlaying) {
					GUILayout.Label ("(not available in Edit mode)", warningLabelStyle);
				} else if (_calc.captureCursor) {
					GUILayout.Label ("(press C to capture)", warningLabelStyle);
				}
				EditorGUILayout.EndHorizontal ();
				break;
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();
			if (GUILayout.Button ("Convert"))
				runConversion = true;
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Convert To", GUILayout.Width (120));
			oldUnit = toUnit;
			toUnit = (UNIT_TYPE)EditorGUILayout.Popup ((int)toUnit, unitNames, GUILayout.MaxWidth (200));
			if (oldUnit != toUnit)
				runConversion = true;
			EditorGUILayout.EndHorizontal ();

			switch (toUnit) {
			case UNIT_TYPE.Degrees:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Latitude", GUILayout.Width (120));
				toLatDegree = GUILayout.TextField (toLatDegree, GUILayout.Width (40));
				GUILayout.Label ("°", GUILayout.Width (10));
				toLatMinute = GUILayout.TextField (toLatMinute, GUILayout.Width (40));
				GUILayout.Label ("'", GUILayout.Width (10));
				toLatSeconds = GUILayout.TextField (toLatSeconds, GUILayout.Width (80));
				GUILayout.Label ("''", GUILayout.Width (10));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Longitude", GUILayout.Width (120));
				toLonDegree = GUILayout.TextField (toLonDegree, GUILayout.Width (40));
				GUILayout.Label ("°", GUILayout.Width (10));
				toLonMinute = GUILayout.TextField (toLonMinute, GUILayout.Width (40));
				GUILayout.Label ("'", GUILayout.Width (10));
				toLonSeconds = GUILayout.TextField (toLonSeconds, GUILayout.Width (80));
				GUILayout.Label ("''", GUILayout.Width (10));
				EditorGUILayout.EndHorizontal ();
				break;
			case UNIT_TYPE.DecimalDegrees:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Latitude", GUILayout.Width (120));
				toLatDec = GUILayout.TextField (toLatDec, GUILayout.Width (80));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Longitude", GUILayout.Width (120));
				toLonDec = GUILayout.TextField (toLonDec, GUILayout.Width (80));
				EditorGUILayout.EndHorizontal ();
				break;
			case UNIT_TYPE.SphereCoordinates:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   X", GUILayout.Width (120));
				toX = GUILayout.TextField (toX, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Y", GUILayout.Width (120));
				toY = GUILayout.TextField (toY, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Z", GUILayout.Width (120));
				toZ = GUILayout.TextField (toZ, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				break;
			}

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			if (errorMsg.Length > 0) {
				GUIStyle warningLabelStyle = new GUIStyle (GUI.skin.label);
				warningLabelStyle.normal.textColor = new Color (0.31f, 0.38f, 0.56f);
				GUILayout.Label ("Conversion error: ", errorMsg);
			}

			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Copy to ClipBoard")) {
				StringBuilder sb = new StringBuilder ();
				switch (toUnit) {
				case UNIT_TYPE.DecimalDegrees:
					sb.Append ("Latitude (decimal degrees): ");
					sb.AppendLine (toLatDec);
					sb.Append ("Longitude (decimal degrees): ");
					sb.AppendLine (toLonDec);
					break;
				case UNIT_TYPE.Degrees:
					sb.Append ("Latitude (degrees): ");
					sb.Append (toLatDegree);
					sb.Append ("°");
					sb.Append (toLatMinute);
					sb.Append ("'");
					sb.Append (toLatSeconds);
					sb.AppendLine ("''");
					sb.Append ("Longitude (degrees): ");
					sb.Append (toLonDegree);
					sb.Append ("°");
					sb.Append (toLonMinute);
					sb.Append ("'");
					sb.Append (toLonSeconds);
					sb.AppendLine ("''");
					break;
				case UNIT_TYPE.SphereCoordinates:
					sb.Append ("X: ");
					sb.AppendLine (toX);
					sb.Append ("Y: ");
					sb.AppendLine (toY);
					sb.Append ("Z: ");
					sb.AppendLine (toZ);
					break;
				}
				EditorGUIUtility.systemCopyBuffer = sb.ToString ();
			}
			if (GUILayout.Button ("Locate (Play mode only)")) {
				if (Application.isPlaying) {
					_calc.FlyTo ();
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();
			
			EditorGUILayout.BeginVertical (blackStyle);
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Distance From", GUILayout.Width (120));
			int prev = _calc.GUICountryDistanceFrom;
			_calc.GUICountryDistanceFrom = EditorGUILayout.Popup (_calc.GUICountryDistanceFrom, countryNames, GUILayout.MaxWidth (200));
			GUILayout.EndHorizontal();
			if (_calc.GUICountryDistanceFrom!=prev) {
				lastCityCountFrom = -1;
				_calc.countryDistanceFrom = GetIndex(countryNames[_calc.GUICountryDistanceFrom]);
				_calc.GUICityDistanceFrom = -1;
				GUIUtility.ExitGUI();
			}
			if (_calc.countryDistanceFrom>=0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("", GUILayout.Width (120));
				prev = _calc.GUICityDistanceFrom;
				_calc.GUICityDistanceFrom = EditorGUILayout.Popup (_calc.GUICityDistanceFrom, cityNamesFrom, GUILayout.MaxWidth (200));
				if (_calc.GUICityDistanceFrom!=prev) runCalcDistance = true;
				GUILayout.EndHorizontal();
			}
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Distance To", GUILayout.Width (120));
			prev = _calc.GUICountryDistanceTo;
			_calc.GUICountryDistanceTo = EditorGUILayout.Popup (_calc.GUICountryDistanceTo, countryNames, GUILayout.MaxWidth (200));
			GUILayout.EndHorizontal();
			if (_calc.GUICountryDistanceTo!=prev) {
				lastCityCountTo = -1;
				_calc.countryDistanceTo = GetIndex(countryNames[_calc.GUICountryDistanceTo]);
				_calc.GUICityDistanceTo = -1;
				GUIUtility.ExitGUI();
			}
			if (_calc.countryDistanceTo>=0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("", GUILayout.Width (120));
				prev = _calc.GUICityDistanceTo;
				_calc.GUICityDistanceTo = EditorGUILayout.Popup (_calc.GUICityDistanceTo, cityNamesTo, GUILayout.MaxWidth (200));
				if (_calc.GUICityDistanceTo!=prev) runCalcDistance = true;
				GUILayout.EndHorizontal();
			}
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (120));
			GUILayout.TextField (_calc.cityDistanceResult, GUILayout.Width (100));
			GUILayout.Label ("km", GUILayout.Width (20));
			GUILayout.EndHorizontal();
			
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			if (runConversion)
				DoConvert ();
			
			if (runCalcDistance)
				DoCalcDistance();

			if (_calc.isDirty) {
				GetResults ();
				_calc.isDirty = false;
				EditorUtility.SetDirty (target);
			}
		}

		void DoConvert () {
			// Setup "from" parameters
			_calc.fromUnit = fromUnit;
			_calc.fromLatDec = GetFloat (fromLatDec);
			_calc.fromLatDegrees = GetFloat (fromLatDegree);
			_calc.fromLatMinutes = GetInt (fromLatMinute);
			_calc.fromLatSeconds = GetFloat (fromLatSeconds);
			_calc.fromLonDec = GetFloat (fromLonDec);
			_calc.fromLonDegrees = GetFloat (fromLonDegree);
			_calc.fromLonMinutes = GetInt (fromLonMinute);
			_calc.fromLonSeconds = GetFloat (fromLonSeconds);
			_calc.fromX = GetFloat (fromX);
			_calc.fromY = GetFloat (fromY);
			_calc.fromZ = GetFloat (fromZ);
			// Do conversion
			_calc.Convert ();
			GetResults ();
		}

		void GetResults () {
			// Recover results
			errorMsg = _calc.errorMsg;
//			fromLatDec = _calc.fromLatDec;
//			fromLatDegree = _calc.fromLatDegrees;
//			fromLatMinute = _calc.fromLatMinutes;
//			fromLatSeconds = _calc.fromLatSeconds;
//			fromLonDec = _calc.fromLonDec;
//			fromLonDegree= _calc.fromLonDegrees;
//			fromLonMinute = _calc.fromLonMinutes;
//			fromLonSeconds = _calc.fromLonSeconds;
			fromX = _calc.fromX.ToString();
			fromY = _calc.fromY.ToString();
			fromZ = _calc.fromZ.ToString();
			toLatDec = _calc.toLatDec.ToString ();
			toLatDegree = _calc.toLatDegree.ToString ();
			toLatMinute = _calc.toLatMinute.ToString ();
			toLatSeconds = _calc.toLatSeconds.ToString ();
			toLonDec = _calc.toLonDec.ToString ();
			toLonDegree = _calc.toLonDegree.ToString ();
			toLonMinute = _calc.toLonMinute.ToString ();
			toLonSeconds = _calc.toLonSecond.ToString ();
			toX = _calc.toX.ToString ();
			toY = _calc.toY.ToString ();
			toZ = _calc.toZ.ToString ();
		}
		
		public int GetInt (string value) {
			int intValue = 0;
			int.TryParse (value, out intValue);
			return intValue;
		}
		
		public float GetFloat (string value) {
			float floatValue = 0;
			float.TryParse (value, out floatValue);
			return floatValue;
		}

		int GetIndex(string s) {
			int k = s.IndexOf("(");
			int j = s.LastIndexOf(")");
			int i = -1;
			if (k>0 && j>k) {
				int.TryParse(s.Substring(k+1, j-k-1), out i);
			}
			return i;
			
		}

		void DoCalcDistance () {
			_calc.cityDistanceResult = "";
			if (_calc.GUICityDistanceFrom>=0 && _calc.GUICityDistanceTo>=0) {
				int c1 = GetIndex(cityNamesFrom[_calc.GUICityDistanceFrom]);
				int c2 = GetIndex(cityNamesTo[_calc.GUICityDistanceTo]);
				if (c1>=0 && c2>=0) {
					City city1 = _calc.map.cities[c1];
					City city2 = _calc.map.cities[c2];
					float distance = _calc.Distance(city1, city2)/1000;
					_calc.cityDistanceResult = distance.ToString("F3");
				}
			}
		}



		Texture2D MakeTex (int width, int height, Color col) {
			Color[] pix = new Color[width * height];
			
			for (int i = 0; i < pix.Length; i++)
				pix [i] = col;
			
			Texture2D result = new Texture2D (width, height);
			result.SetPixels (pix);
			result.Apply ();
			
			return result;
		}

	}

}