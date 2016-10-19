using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM_Editor
{
	[CustomEditor(typeof(WorldMapGlobe))]
	public class WorldMapGlobeInspector : Editor
	{
		const int CALCULATOR = 0;
		const int TICKERS = 1;
		const int DECORATOR = 2;
		const int EDITOR = 3;
		
		WorldMapGlobe _map;
		Texture2D _headerTexture, _blackTexture;
		string[] earthStyleOptions, frontiersDetailOptions, labelsQualityOptions, gridModeOptions, navigationModeOptions;
		int[] earthStyleValues, gridModeValues, navigationModeValues;
		GUIStyle blackStyle;
		bool[] extracomp = new bool[4];
		
		void OnEnable ()
		{
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f) : new Color (0.7f, 0.7f, 0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			_map = (WorldMapGlobe)target;
			_headerTexture = Resources.Load<Texture2D> ("EditorHeader");
			if (_map!=null && _map.countries == null) {
				_map.Init ();
			}
			earthStyleOptions = new string[] {
				"Natural", "Natural (Scenic)", "Natural (8K)", "Natural (8K Scenic)", "Natural (8K Scenic Scatter)", "Alternate Style 1", "Alternate Style 2", "Alternate Style 3", "Solid Color", "Custom"
			};
			earthStyleValues = new int[] {
				(int)EARTH_STYLE.Natural, (int)EARTH_STYLE.Scenic, (int)EARTH_STYLE.NaturalHighRes, (int)EARTH_STYLE.NaturalHighResScenic, (int)EARTH_STYLE.NaturalHighResScenicScatter, (int)EARTH_STYLE.Alternate1, (int)EARTH_STYLE.Alternate2, (int)EARTH_STYLE.Alternate3, (int)EARTH_STYLE.SolidColor, (int)EARTH_STYLE.Custom
			};
			
			frontiersDetailOptions = new string[] {
				"Low",
				"High"
			};
			labelsQualityOptions = new string[] {
				"Low",
				"Medium",
				"High"
			};
			gridModeOptions = new string[] {
				"Overlay",
				"Masked"
			};
			gridModeValues = new int[] {
				(int)GRID_MODE.OVERLAY, (int)GRID_MODE.MASKED
			};
			
			navigationModeOptions = new string[] {
				"Earth Rotates",
				"Camera Rotates"
			};
			navigationModeValues = new int[] {
				(int)NAVIGATION_MODE.EARTH_ROTATES, (int)NAVIGATION_MODE.CAMERA_ROTATES
			};
			
			
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;
			
			UpdateExtraComponentStatus();
		}
		
		void UpdateExtraComponentStatus() {
			extracomp[CALCULATOR] = _map.gameObject.GetComponent<WorldMapCalculator>() != null;
			extracomp[TICKERS] = _map.gameObject.GetComponent<WorldMapTicker>() != null;
			extracomp[DECORATOR] = _map.gameObject.GetComponent<WorldMapDecorator>() != null;
			extracomp[EDITOR] = _map.gameObject.GetComponent<WorldMapEditor>() != null;
		}
		
		
		public override void OnInspectorGUI ()
		{
			if (_map==null) return;
			_map.isDirty = false;
			EditorGUILayout.Separator ();
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;  
			GUILayout.Label (_headerTexture, GUILayout.ExpandWidth (true));
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;  
			EditorGUILayout.Separator ();
			
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Earth", GUILayout.Width (120));
			_map.showEarth = EditorGUILayout.Toggle (_map.showEarth);
			
			if (GUILayout.Button ("Straighten")) {
				_map.StraightenGlobe (1.0f);
			}
			
			if (GUILayout.Button ("Tilt")) {
				//				_map.TiltGlobe(new Vector3(19.7f, 198.5f, 349.4f), 1.0f);
				_map.TiltGlobe (new Vector3 (0, 0, 23.4f), 1.0f);
			}
			
			if (GUILayout.Button ("Redraw")) {
				_map.Redraw();
			}

			EditorGUILayout.EndHorizontal ();
			
			if (_map.showEarth) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Earth Style", GUILayout.Width (120));
				_map.earthStyle = (EARTH_STYLE)EditorGUILayout.IntPopup ((int)_map.earthStyle, earthStyleOptions, earthStyleValues);
				
				if (_map.earthStyle == EARTH_STYLE.SolidColor) {
					GUILayout.Label ("Color");
					_map.earthColor = EditorGUILayout.ColorField (_map.earthColor, GUILayout.Width (50));
				}
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Glow Intensity", GUILayout.Width (120));
				_map.earthScenicGlowIntensity = EditorGUILayout.Slider(_map.earthScenicGlowIntensity, 0, 5);
				GUILayout.Label ("Phys. Based Glow");
				_map.earthGlowScatter = EditorGUILayout.Toggle (_map.earthGlowScatter, GUILayout.Width(20));
				EditorGUILayout.EndHorizontal ();
				
//				if (_map.earthGlowScatter && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)) {
//					EditorGUILayout.BeginHorizontal ();
//					DrawWarningLabel("   (Warning: atmosphere scattering currently not compatible with mobile)");
//					EditorGUILayout.EndHorizontal ();
//				}

				if (_map.earthStyle.isScenic()) {
					if (_map.transform.localScale.x>1f && (_map.earthStyle == EARTH_STYLE.Scenic || _map.earthStyle == EARTH_STYLE.NaturalHighResScenic)) {
						EditorGUILayout.BeginHorizontal ();
						DrawWarningLabel("   Warning: this Scenic style requires globe scale = 1.");
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						DrawWarningLabel("   (Use Scenic Scattering instead)");
						EditorGUILayout.EndHorizontal ();
					}
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Light Direction", GUILayout.Width (120));
					_map.earthScenicLightDirection = EditorGUILayout.Vector3Field("", _map.earthScenicLightDirection);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Effect Intensity", GUILayout.Width (120));
					_map.earthScenicAtmosphereIntensity = EditorGUILayout.Slider(_map.earthScenicAtmosphereIntensity, 0, 1);
					EditorGUILayout.EndHorizontal ();
				} else {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Inverted Mode", GUILayout.Width (120));
					_map.earthInvertedMode = EditorGUILayout.Toggle (_map.earthInvertedMode);
					EditorGUILayout.EndHorizontal ();
				}
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   High Density Mesh", GUILayout.Width (120));
				_map.earthHighDensityMesh = EditorGUILayout.Toggle (_map.earthHighDensityMesh);
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Latitude Lines", GUILayout.Width (120));
			_map.showLatitudeLines = EditorGUILayout.Toggle (_map.showLatitudeLines, GUILayout.Width(40));
			GUILayout.Label ("Stepping");
			_map.latitudeStepping = EditorGUILayout.IntSlider (_map.latitudeStepping, 5, 45);
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Longitude Lines", GUILayout.Width (120));
			_map.showLongitudeLines = EditorGUILayout.Toggle (_map.showLongitudeLines, GUILayout.Width(40));
			GUILayout.Label ("Stepping");
			_map.longitudeStepping = EditorGUILayout.IntSlider (_map.longitudeStepping, 5, 45);
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Grid Color", GUILayout.Width (120));
			_map.gridLinesColor = EditorGUILayout.ColorField (_map.gridLinesColor, GUILayout.Width (50));
			GUILayout.FlexibleSpace();
			GUILayout.Label ("Mode", GUILayout.Width(50));
			_map.gridMode = (GRID_MODE)EditorGUILayout.IntPopup ((int)_map.gridMode, gridModeOptions, gridModeValues, GUILayout.Width (130));
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Cities", GUILayout.Width (120));
			_map.showCities = EditorGUILayout.Toggle (_map.showCities);
			EditorGUILayout.EndHorizontal ();
			
			if (_map.showCities && _map.cities!=null) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Cities Color", GUILayout.Width (120));
				_map.citiesColor = EditorGUILayout.ColorField (_map.citiesColor, GUILayout.Width (40));
				
				GUILayout.Label ("Region Cap.");
				_map.citiesRegionCapitalColor = EditorGUILayout.ColorField (_map.citiesRegionCapitalColor, GUILayout.Width (40));
				
				GUILayout.Label ("Capital");
				_map.citiesCountryCapitalColor = EditorGUILayout.ColorField (_map.citiesCountryCapitalColor, GUILayout.Width (40));
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Icon Size", GUILayout.Width (120));
				_map.cityIconSize = EditorGUILayout.Slider (_map.cityIconSize, 0.02f, 0.5f);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Min Population (K)", GUILayout.Width (120));
				_map.minPopulation = EditorGUILayout.IntSlider (_map.minPopulation, 0, 3000);
				GUILayout.Label (_map.numCitiesDrawn + "/" + _map.cities.Count);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Always Visible:", GUILayout.Width (120));
				int cityClassFilter = 0;
				bool cityBit;
				cityBit = EditorGUILayout.Toggle ((_map.cityClassAlwaysShow & WorldMapGlobe.CITY_CLASS_FILTER_REGION_CAPITAL_CITY)!=0, GUILayout.Width(20));
				GUILayout.Label("Region Capitals");
				if (cityBit) cityClassFilter += WorldMapGlobe.CITY_CLASS_FILTER_REGION_CAPITAL_CITY;
				cityBit = EditorGUILayout.Toggle ((_map.cityClassAlwaysShow & WorldMapGlobe.CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY)!=0, GUILayout.Width(20));
				GUILayout.Label("Country Capitals");
				if (cityBit) cityClassFilter += WorldMapGlobe.CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY;
				_map.cityClassAlwaysShow = cityClassFilter;
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.EndVertical (); 
			
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Frontiers Detail", GUILayout.Width (120));
			_map.frontiersDetail = (FRONTIERS_DETAIL)EditorGUILayout.Popup ((int)_map.frontiersDetail, frontiersDetailOptions);
			GUILayout.Label (_map.countries.Length.ToString ());
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Inland Frontiers", GUILayout.Width (120));
			_map.showInlandFrontiers = EditorGUILayout.Toggle (_map.showInlandFrontiers);
			if (_map.showInlandFrontiers) {
				GUILayout.Label ("Color", GUILayout.Width (120));
				_map.inlandFrontiersColor = EditorGUILayout.ColorField (_map.inlandFrontiersColor, GUILayout.Width (50));
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Countries", GUILayout.Width (120));
			_map.showFrontiers = EditorGUILayout.Toggle (_map.showFrontiers);
			if (_map.showFrontiers) {
				GUILayout.Label ("Frontiers Color", GUILayout.Width (120));
				_map.frontiersColor = EditorGUILayout.ColorField (_map.frontiersColor, GUILayout.Width (50));
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Country Highlight", GUILayout.Width (120));
			_map.enableCountryHighlight = EditorGUILayout.Toggle (_map.enableCountryHighlight);
			
			if (_map.enableCountryHighlight) {
				GUILayout.Label ("Highlight Color", GUILayout.Width (120));
				_map.fillColor = EditorGUILayout.ColorField (_map.fillColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Draw Outline", GUILayout.Width (120));
				_map.showOutline = EditorGUILayout.Toggle (_map.showOutline);
				if (_map.showOutline) {
					GUILayout.Label ("Outline Color", GUILayout.Width (120));
					_map.outlineColor = EditorGUILayout.ColorField (_map.outlineColor, GUILayout.Width (50));
					if (_map.surfacesCount > 75) {
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						GUIStyle warningLabelStyle = new GUIStyle (GUI.skin.label);
						warningLabelStyle.normal.textColor = new Color (0.31f, 0.38f, 0.56f);
						GUILayout.Label ("Consider disabling outline to improve performance", warningLabelStyle);
					}
				}
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.EndVertical (); 
			
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Country Names", GUILayout.Width (120));
			_map.showCountryNames = EditorGUILayout.Toggle (_map.showCountryNames);
			EditorGUILayout.EndHorizontal ();
			
			if (_map.showCountryNames) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Labels Quality", GUILayout.Width (120));
				_map.labelsQuality = (LABELS_QUALITY)EditorGUILayout.Popup ((int)_map.labelsQuality, labelsQualityOptions);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Relative Size", GUILayout.Width (120));
				_map.countryLabelsSize = EditorGUILayout.Slider (_map.countryLabelsSize, 0.1f, 0.9f);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Minimum Size", GUILayout.Width (120));
				_map.countryLabelsAbsoluteMinimumSize = EditorGUILayout.Slider (_map.countryLabelsAbsoluteMinimumSize, 0.29f, 2.5f);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Elevation", GUILayout.Width (120));
				_map.labelsElevation = EditorGUILayout.Slider (_map.labelsElevation, 0.0f, 1.0f);
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Labels Color", GUILayout.Width (120));
				_map.countryLabelsColor = EditorGUILayout.ColorField (_map.countryLabelsColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("  Draw Shadow", GUILayout.Width (120));
				_map.showLabelsShadow = EditorGUILayout.Toggle (_map.showLabelsShadow);
				if (_map.showLabelsShadow) {
					GUILayout.Label ("Shadow Color", GUILayout.Width (120));
					_map.countryLabelsShadowColor = EditorGUILayout.ColorField (_map.countryLabelsShadowColor, GUILayout.Width (50));
				}
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Auto Fade Labels", GUILayout.Width (120));
				_map.countryLabelsEnableAutomaticFade = EditorGUILayout.Toggle (_map.countryLabelsEnableAutomaticFade);
				EditorGUILayout.EndHorizontal ();

				if (_map.countryLabelsEnableAutomaticFade) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("  Min Height", GUILayout.Width (120));
					_map.countryLabelsAutoFadeMinHeight= EditorGUILayout.Slider (_map.countryLabelsAutoFadeMinHeight, 0.01f, 0.25f);
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("  Min Height Fall Off", GUILayout.Width (120));
					_map.countryLabelsAutoFadeMinHeightFallOff= EditorGUILayout.Slider (_map.countryLabelsAutoFadeMinHeightFallOff, 0.001f, _map.countryLabelsAutoFadeMinHeight);
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("  Max Height", GUILayout.Width (120));
					_map.countryLabelsAutoFadeMaxHeight = EditorGUILayout.Slider (_map.countryLabelsAutoFadeMaxHeight, 0.1f, 1.0f);
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("  Max Height Fall Off", GUILayout.Width (120));
					_map.countryLabelsAutoFadeMaxHeightFallOff= EditorGUILayout.Slider (_map.countryLabelsAutoFadeMaxHeightFallOff, 0.01f, 1f);
					EditorGUILayout.EndHorizontal ();

				}


			}
			
			EditorGUILayout.EndVertical (); 
			
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Provinces", GUILayout.Width (120));
			_map.showProvinces = EditorGUILayout.Toggle (_map.showProvinces);
			EditorGUILayout.EndHorizontal ();
			
			if (_map.showProvinces) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Provinces Color", GUILayout.Width (120));
				_map.provincesColor = EditorGUILayout.ColorField (_map.provincesColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Highlight Color", GUILayout.Width (120));
				_map.provincesFillColor = EditorGUILayout.ColorField (_map.provincesFillColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.EndVertical (); 
			
			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Autorotation Speed", GUILayout.Width (120));
			_map.autoRotationSpeed = EditorGUILayout.Slider (_map.autoRotationSpeed, -2f, 2f);
			if (GUILayout.Button ("Stop")) {
				_map.autoRotationSpeed = 0;
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Cursor", GUILayout.Width (120));
			_map.showCursor = EditorGUILayout.Toggle (_map.showCursor);
			
			if (_map.showCursor) {
				GUILayout.Label ("Cursor Color", GUILayout.Width (120));
				_map.cursorColor = EditorGUILayout.ColorField (_map.cursorColor, GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Follow Mouse", GUILayout.Width (120));
				_map.cursorFollowMouse = EditorGUILayout.Toggle (_map.cursorFollowMouse);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Always Visible", GUILayout.Width (120));
				_map.cursorAlwaysVisible = EditorGUILayout.Toggle (_map.cursorAlwaysVisible, GUILayout.Width(40));
				GUILayout.Label ("Respect Other UI", GUILayout.Width (120));
				_map.respectOtherUI = EditorGUILayout.Toggle (_map.respectOtherUI);
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Allow User Rotation", GUILayout.Width (120));
			_map.allowUserRotation = EditorGUILayout.Toggle (_map.allowUserRotation, GUILayout.Width(40));
			if (_map.allowUserRotation) {
				GUILayout.Label ("Speed");
				_map.mouseDragSensitivity = EditorGUILayout.Slider (_map.mouseDragSensitivity, 0.1f, 3);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Right Click Centers", GUILayout.Width (120));
				_map.centerOnRightClick = EditorGUILayout.Toggle (_map.centerOnRightClick, GUILayout.Width(40));
				GUILayout.Label ("Constant Drag Speed", GUILayout.Width (120));
				_map.dragConstantSpeed = EditorGUILayout.Toggle (_map.dragConstantSpeed,  GUILayout.Width (50));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Allow Keys (WASD)", GUILayout.Width (120));
				_map.allowUserKeys = EditorGUILayout.Toggle (_map.allowUserKeys, GUILayout.Width(40));
				GUILayout.Label ("Keep Straight", GUILayout.Width (120));
				_map.keepStraight = EditorGUILayout.Toggle (_map.keepStraight,  GUILayout.Width (50));
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Allow User Zoom", GUILayout.Width (120));
			_map.allowUserZoom = EditorGUILayout.Toggle (_map.allowUserZoom, GUILayout.Width(40));
			if (_map.allowUserZoom) {
				GUILayout.Label ("Speed");
				_map.mouseWheelSensitivity = EditorGUILayout.Slider (_map.mouseWheelSensitivity, 0.1f, 3);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Invert Direction", GUILayout.Width (120));
				_map.invertZoomDirection = EditorGUILayout.Toggle (_map.invertZoomDirection, GUILayout.Width(40));
				GUILayout.Label (new GUIContent("Distance Min", "0 = default min distance"));
				_map.zoomMinDistance = EditorGUILayout.FloatField(_map.zoomMinDistance, GUILayout.Width(50));
				GUILayout.Label (new GUIContent("Max", "10m = default max distance"));
				_map.zoomMaxDistance = EditorGUILayout.FloatField(_map.zoomMaxDistance, GUILayout.Width(50));
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Navigation Time", GUILayout.Width (120));
			_map.navigationTime = EditorGUILayout.Slider (_map.navigationTime, 0, 10);
			GUILayout.Label ("Mode");
			_map.navigationMode = (NAVIGATION_MODE)EditorGUILayout.IntPopup ((int)_map.navigationMode, navigationModeOptions, navigationModeValues);
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.EndVertical ();
			
			// Extra components opener
			EditorGUILayout.Separator ();
			float buttonWidth = Screen.width * 0.4f;
			
			if (_map.gameObject.activeInHierarchy) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				
				if (extracomp[CALCULATOR]) {
					if (GUILayout.Button ("Hide Calculator", GUILayout.Width (buttonWidth))) {
						WorldMapCalculator e = _map.gameObject.GetComponent<WorldMapCalculator> ();
						if (e!=null) DestroyImmediate (e);
						UpdateExtraComponentStatus();
						EditorGUIUtility.ExitGUI ();
					}
				} else {
					if (GUILayout.Button ("Open Calculator", GUILayout.Width (buttonWidth))) {
						WorldMapCalculator e = _map.gameObject.GetComponent<WorldMapCalculator> ();
						if (e==null) _map.gameObject.AddComponent<WorldMapCalculator> ();
						UpdateExtraComponentStatus();
					}
				}
				
				if (extracomp[TICKERS]) {
					if (GUILayout.Button ("Hide Ticker", GUILayout.Width (buttonWidth))) {
						WorldMapTicker e = _map.gameObject.GetComponent<WorldMapTicker> ();
						if (e!=null) DestroyImmediate (e);
						UpdateExtraComponentStatus();
						EditorGUIUtility.ExitGUI ();
					}
				} else {
					if (GUILayout.Button ("Open Ticker", GUILayout.Width (buttonWidth))) {
						WorldMapTicker e = _map.gameObject.GetComponent<WorldMapTicker> ();
						if (e==null) _map.gameObject.AddComponent<WorldMapTicker> ();
						UpdateExtraComponentStatus();
					}
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (extracomp[EDITOR]) {
					if (GUILayout.Button ("Hide Editor", GUILayout.Width (buttonWidth))) {
						_map.HideProvinces ();
						_map.HideCountrySurfaces ();
						_map.HideProvinceSurfaces ();
						_map.Redraw ();
						WorldMapEditor e = _map.gameObject.GetComponent<WorldMapEditor> ();
						if (e!=null) {
							WorldMapEditorInspector[] editors = (WorldMapEditorInspector[])Resources.FindObjectsOfTypeAll<WorldMapEditorInspector>();
							if (editors.Length>0) editors[0].OnCloseEditor();
							DestroyImmediate (e);
						}
						UpdateExtraComponentStatus();
						EditorGUIUtility.ExitGUI ();
					}
				} else {
					if (GUILayout.Button ("Open Editor", GUILayout.Width (buttonWidth))) {
						WorldMapEditor e = _map.gameObject.GetComponent<WorldMapEditor> ();
						if (e==null) _map.gameObject.AddComponent<WorldMapEditor> ();
						// cancel scenic shaders since they look awful in editor window
						if (_map.earthStyle == EARTH_STYLE.Scenic) _map.earthStyle = EARTH_STYLE.Natural;
						if (_map.earthStyle == EARTH_STYLE.NaturalHighResScenic || _map.earthStyle == EARTH_STYLE.NaturalHighResScenicScatter) _map.earthStyle = EARTH_STYLE.NaturalHighRes;
						UpdateExtraComponentStatus();
					}
				}
				
				if (extracomp[DECORATOR]) {
					if (GUILayout.Button ("Hide Decorator", GUILayout.Width (buttonWidth))) {
						WorldMapDecorator e = _map.gameObject.GetComponent<WorldMapDecorator> ();
						if (e!=null) DestroyImmediate (e);
						UpdateExtraComponentStatus();
						EditorGUIUtility.ExitGUI ();
					}
				} else {
					if (GUILayout.Button ("Open Decorator", GUILayout.Width (buttonWidth))) {
						WorldMapDecorator e = _map.gameObject.GetComponent<WorldMapDecorator> ();
						if (e==null) _map.gameObject.AddComponent<WorldMapDecorator> ();
						UpdateExtraComponentStatus();
					}
				}
				GUILayout.FlexibleSpace ();
				
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("About", GUILayout.Width(buttonWidth * 2.0f))) {
				WorldMapAbout.ShowAboutWindow();
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal ();
			
			if (_map.isDirty) {
				EditorUtility.SetDirty (target);
			}
		}
		
		#if !UNITY_WEBPLAYER
		// Add a menu item called "Double Mass" to a Rigidbody's context menu.
		[MenuItem ("CONTEXT/WorldMapGlobe/Bake Earth Texture")]
		static void RestoreBackup (MenuCommand command)
		{
			if (!EditorUtility.DisplayDialog ("Bake Earth Texture", "This command will render the colorized areas to the current texture and save it to EarthCustom.png file inside Textures folder (existing file from a previous bake texture operation will be replaced).\n\nThis command can take some time depending on the current texture resolution and CPU speed, from a few seconds to one minute for high-res (8K) texstures.\n\nProceed?", "Ok", "Cancel")) 
				return;
			
			// Proceed and restore
			string[] paths = AssetDatabase.GetAllAssetPaths ();
			string textureFolder = "";
			for (int k=0; k<paths.Length; k++) {
				if (paths [k].EndsWith ("WorldPoliticalMapGlobeEdition/Resources/Textures")) {
					textureFolder = paths [k];
					break;
				}
			}
			if (textureFolder.Length > 0) {
				string fileName = "EarthCustom.png";
				string outputFile = textureFolder + "/" + fileName;
				if (((WorldMapGlobe)command.context).BakeTexture (outputFile) != null) {
					AssetDatabase.Refresh ();
				}
				EditorUtility.DisplayDialog ("Operation successful!", "Texture saved as \"" + fileName + "\" in WorldPoliticalMapGlobeEdition/Resources/Textures folder.\n\nTo use this texture:\n1- Check the import settings of the texture file (ensure max resolution is appropiated; should be 2048 at least, 8192 for high-res 8K textures).\n2- Set Earth style to Custom.", "Ok");
			} else {
				EditorUtility.DisplayDialog ("Required folder not found", "Cannot find \".../WorldPoliticalMapGlobeEdition/Resources/Textures\" folder!", "Ok");
			}
			
		}
		#endif
		
		Texture2D MakeTex (int width, int height, Color col)
		{
			Color[] pix = new Color[width * height];
			
			for (int i = 0; i < pix.Length; i++)
				pix [i] = col;
			
			Texture2D result = new Texture2D (width, height);
			result.SetPixels (pix);
			result.Apply ();
			
			return result;
		}
		
		
		GUIStyle warningLabelStyle;
		void DrawWarningLabel (string s) {
			if (warningLabelStyle == null)  {
				warningLabelStyle = new GUIStyle (GUI.skin.label);
			}
			warningLabelStyle.normal.textColor = new Color (0.22f, 0.46f, 0.9f);
			GUILayout.Label (s, warningLabelStyle);
		}
		
		
		
	}
	
}