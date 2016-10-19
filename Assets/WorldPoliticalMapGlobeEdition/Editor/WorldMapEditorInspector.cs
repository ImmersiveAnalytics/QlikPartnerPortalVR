using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM;

namespace WPM_Editor {
	[CustomEditor(typeof(WorldMapEditor))]
	public class WorldMapEditorInspector : Editor {
	
		static Vector3 pointSnap = MiscVector.Vector3one * 0.1f;
		const float HANDLE_SIZE = 0.05f;
		const float HIT_PRECISION = 0.001f;
		const string EDITORPREF_SCALE_WARNED = "ScaleWarned";

		WorldMapEditor _editor;
		Texture2D _blackTexture, _areaTexture;
		GUIStyle blackStyle, labelsStyle, areaStyle;
		GUIContent[] mainToolbarIcons;
		GUIContent[] reshapeRegionToolbarIcons, reshapeCityToolbarIcons, reshapeMountPointToolbarIcons, createToolbarIcons;
		int[] controlIds;
		bool startedReshapeRegion, startedReshapeCity, startedReshapeMountPoint, undoPushStarted;
		long tickStart;
		string[] reshapeRegionModeExplanation, reshapeCityModeExplanation, reshapeMountPointModeExplanation, editingModeOptions, editingCountryFileOptions, createModeExplanation, cityClassOptions;
		int[] cityClassValues;
		string[] emptyStringArray;
		bool zoomed = false;
		Vector3 zoomOldValue;
		Vector3 zoomOldPosition;
		Quaternion frontFaceQuaternion;
		LABELS_QUALITY labelsOldQuality;


		WorldMapGlobe _map { get { return _editor.map; } }

		const string INFO_MSG_CHANGES_SAVED = "Changes saved. Original geodata files in /Backup folder.";
		const string INFO_MSG_REGION_DELETED = "Region deleted!";
		const string INFO_MSG_BACKUP_NOT_FOUND = "Backup folder not found!";
		const string INFO_MSG_BACKUP_RESTORED = "Backup restored.";
		const string INFO_MSG_GEODATA_LOW_QUALITY_CREATED = "Low quality geodata file created.";
		const string INFO_MSG_CITY_DELETED = "City deleted!";
		const string INFO_MSG_NO_CHANGES_TO_SAVE = "Nothing changed to save!";
		const string INFO_MSG_CHOOSE_COUNTRY = "Choose a country first.";
		const string INFO_MSG_CHOOSE_PROVINCE = "Choose a province first.";
		const string INFO_MSG_CONTINENT_DELETED = "Continent deleted!";
		const string INFO_MSG_COUNTRY_DELETED = "Country deleted!";
		const string INFO_MSG_PROVINCE_DELETED = "Province deleted!";
		const string INFO_MSG_MOUNT_POINT_DELETED = "Mount point deleted!";

		#region Inspector lifecycle

		void OnEnable () {

			// Setup basic inspector stuff
			_editor = (WorldMapEditor)target;
			if (_map.countries==null) {
				_map.Init();
			}

			// Setup styles
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f) : new Color (0.7f, 0.7f, 0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;

			labelsStyle = new GUIStyle();
			labelsStyle.normal.textColor = Color.green;
			labelsStyle.alignment = TextAnchor.MiddleCenter;

			Color areaColor = EditorGUIUtility.isProSkin ? new Color (0.1f, 0.1f, 0.1f) : new Color (0.62f, 0.62f, 0.62f);
			_areaTexture = MakeTex (4, 4, areaColor);
			_areaTexture.hideFlags = HideFlags.DontSave;
			areaStyle = new GUIStyle ();
			areaStyle.normal.background = _areaTexture;

			// Load UI icons
			Texture2D[] icons = new Texture2D[20];
			icons [0] = Resources.Load<Texture2D> ("IconSelect");
			icons [1] = Resources.Load<Texture2D> ("IconPolygon");
			icons [2] = Resources.Load<Texture2D> ("IconUndo");
			icons [3] = Resources.Load<Texture2D> ("IconConfirm");
			icons [4] = Resources.Load<Texture2D> ("IconPoint");
			icons [5] = Resources.Load<Texture2D> ("IconCircle");
			icons [6] = Resources.Load<Texture2D> ("IconMagnet");
			icons [7] = Resources.Load<Texture2D> ("IconSplitVert");
			icons [8] = Resources.Load<Texture2D> ("IconSplitHoriz");
			icons [9] = Resources.Load<Texture2D> ("IconDelete");
			icons [10] = Resources.Load<Texture2D> ("IconEraser");
			icons [11] = Resources.Load<Texture2D> ("IconMorePoints");
			icons [12] = Resources.Load<Texture2D> ("IconCreate");
			icons [13] = Resources.Load<Texture2D> ("IconPenCountry");
			icons [14] = Resources.Load<Texture2D> ("IconTarget");
			icons [15] = Resources.Load<Texture2D> ("IconPenCountryRegion");
			icons [16] = Resources.Load<Texture2D> ("IconPenProvince");
			icons [17] = Resources.Load<Texture2D> ("IconPenProvinceRegion");
			icons [18] = Resources.Load<Texture2D> ("IconMove");
			icons [19] = Resources.Load<Texture2D> ("IconMountPoint");

			// Setup main toolbar
			mainToolbarIcons = new GUIContent[5];
			mainToolbarIcons [0] = new GUIContent ("Select", icons [0], "Selection mode");
			mainToolbarIcons [1] = new GUIContent ("Reshape", icons [1], "Change the shape of this entity");
			mainToolbarIcons [2] = new GUIContent ("Create", icons [12], "Add a new entity to this layer");
			mainToolbarIcons [3] = new GUIContent ("Revert", icons [2], "Restore shape information");
			mainToolbarIcons [4] = new GUIContent ("Save", icons [3], "Confirm changes and save to file");

			// Setup reshape region command toolbar
			int RESHAPE_REGION_TOOLS_COUNT = 8;
			reshapeRegionToolbarIcons = new GUIContent[RESHAPE_REGION_TOOLS_COUNT];
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.POINT] = new GUIContent ("Point", icons [4], "Single Point Tool");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.CIRCLE] = new GUIContent ("Circle", icons [5], "Group Move Tool");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.SPLITV] = new GUIContent ("SplitV", icons [7], "Split Vertically");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.SPLITH] = new GUIContent ("SplitH", icons [8], "Split Horizontally");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.MAGNET] = new GUIContent ("Magnet", icons [6], "Join frontiers between different regions");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.SMOOTH] = new GUIContent ("Smooth", icons [11], "Add Point Tool");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.ERASER] = new GUIContent ("Erase", icons [10], "Removes Point Tool");
			reshapeRegionToolbarIcons [(int)RESHAPE_REGION_TOOL.DELETE] = new GUIContent ("Delete", icons [9], "Delete Region or Country");
			reshapeRegionModeExplanation = new string[RESHAPE_REGION_TOOLS_COUNT];
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.POINT] = "Drag a SINGLE point of currently selected region (and its neighbour)";
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.CIRCLE] = "Drag a GROUP of points of currently selected region (and from its neighbour region if present)"; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.SPLITV] = "Splits VERTICALLY currently selected region. One of the two splitted parts will form a new country."; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.SPLITH] = "Splits HORIZONTALLY currently selected region. One of the two splitted parts will form a new country."; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.MAGNET] = "Click several times on a group of points next to a neighbour frontier to makes them JOIN. You may need to add additional points on both sides using the smooth tool."; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.SMOOTH] = "Click around currently selected region to ADD new points."; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.ERASER] = "Click on target points of currently selected region to ERASE them."; 
			reshapeRegionModeExplanation [(int)RESHAPE_REGION_TOOL.DELETE] = "DELETES currently selected region. If this is the last region of the country or province, then the country or province will be deleted completely."; 

			// Setup create command toolbar
			int CREATE_TOOLS_COUNT = 6;
			createToolbarIcons = new GUIContent[CREATE_TOOLS_COUNT];
			createToolbarIcons [(int)CREATE_TOOL.CITY] = new GUIContent ("City", icons [14], "Create a new city");
			createToolbarIcons [(int)CREATE_TOOL.COUNTRY] = new GUIContent ("Country", icons [13], "Draw a new country");
			createToolbarIcons [(int)CREATE_TOOL.COUNTRY_REGION] = new GUIContent ("Co. Region", icons [15], "Draw a new region for current selected country");
			createToolbarIcons [(int)CREATE_TOOL.PROVINCE] = new GUIContent ("Province", icons [16], "Draw a new province for current selected country");
			createToolbarIcons [(int)CREATE_TOOL.PROVINCE_REGION] = new GUIContent ("Prov. Region", icons [17], "Draw a new region for current selected province");
			createToolbarIcons [(int)CREATE_TOOL.MOUNT_POINT] = new GUIContent ("Mount Point", icons [19], "Create a new mount point");
			createModeExplanation = new string[CREATE_TOOLS_COUNT];
			createModeExplanation [(int)CREATE_TOOL.CITY] = "Click over the map to create a NEW CITY for currrent COUNTRY"; 
			createModeExplanation [(int)CREATE_TOOL.COUNTRY] = "Click over the map to create a polygon and add points for a NEW COUNTRY";
			createModeExplanation [(int)CREATE_TOOL.COUNTRY_REGION] = "Click over the map to create a polygon and add points for a NEW REGION of currently selected COUNTRY";
			createModeExplanation [(int)CREATE_TOOL.PROVINCE] = "Click over the map to create a polygon and add points for a NEW PROVINCE of currently selected country";
			createModeExplanation [(int)CREATE_TOOL.PROVINCE_REGION] = "Click over the map to create a polygon and add points for a NEW REGION of currently selected PROVINCE";
			createModeExplanation [(int)CREATE_TOOL.MOUNT_POINT] = "Click over the map to create a NEW MOUNT POINT for current COUNTRY and optional PROVINCE";

			// Setup reshape city tools
			int RESHAPE_CITY_TOOLS_COUNT = 2;
			reshapeCityToolbarIcons = new GUIContent[RESHAPE_CITY_TOOLS_COUNT];
			reshapeCityToolbarIcons [(int)RESHAPE_CITY_TOOL.MOVE] = new GUIContent ("Move", icons [18], "Move city");
			reshapeCityToolbarIcons [(int)RESHAPE_CITY_TOOL.DELETE] = new GUIContent ("Delete", icons [9], "Delete city");
			reshapeCityModeExplanation = new string[RESHAPE_CITY_TOOLS_COUNT];
			reshapeCityModeExplanation [(int)RESHAPE_CITY_TOOL.MOVE] = "Click and drag currently selected CITY to change its POSITION";
			reshapeCityModeExplanation [(int)RESHAPE_CITY_TOOL.DELETE] = "DELETES currently selected CITY."; 

			// Setup reshape mount point tools
			int RESHAPE_MOUNT_POINT_TOOLS_COUNT = 2;
			reshapeMountPointToolbarIcons = new GUIContent[RESHAPE_MOUNT_POINT_TOOLS_COUNT];
			reshapeMountPointToolbarIcons [(int)RESHAPE_MOUNT_POINT_TOOL.MOVE] = new GUIContent ("Move", icons [18], "Move mount point");
			reshapeMountPointToolbarIcons [(int)RESHAPE_MOUNT_POINT_TOOL.DELETE] = new GUIContent ("Delete", icons [9], "Delete mount point");
			reshapeMountPointModeExplanation = new string[RESHAPE_MOUNT_POINT_TOOLS_COUNT];
			reshapeMountPointModeExplanation [(int)RESHAPE_MOUNT_POINT_TOOL.MOVE] = "Click and drag currently selected MOUNT POINT to change its POSITION";
			reshapeMountPointModeExplanation [(int)RESHAPE_MOUNT_POINT_TOOL.DELETE] = "DELETES currently selected MOUNT POINT."; 


			editingModeOptions = new string[] {
				"Only Countries",
				"Countries + Provinces"
			};

			editingCountryFileOptions = new string[] {
				"High Definition Geodata File",
				"Low Definition Geodata File"
			};
			cityClassOptions = new string[] { "City", "Country Capital", "Region Capital" };
			cityClassValues = new int[] { (int)CITY_CLASS.CITY, (int)CITY_CLASS.COUNTRY_CAPITAL, (int)CITY_CLASS.REGION_CAPITAL };

			emptyStringArray = new string[0];
			_map.showCities = true;
			_map.minPopulation = 0; // make sure all cities are visible

			// Setup scene view
			_editor.shouldHideEditorMesh = true;
			zoomed = _map.transform.localScale.x>=1000.0f;
			if (zoomed) {
				zoomOldValue = MiscVector.Vector3one;
			} else {
				zoomOldValue = _map.transform.localScale;
			}
			zoomOldPosition = _map.transform.position;
			labelsOldQuality = _map.labelsQuality;

			// Select globe and focus it
			if (Selection.activeGameObject!=_map.gameObject) {
				Selection.activeGameObject = _map.gameObject;
				if (SceneView.lastActiveSceneView!=null) 
					SceneView.lastActiveSceneView.FrameSelected();
			}
			// Update icons scale
			AdjustCityIconsScale();
			AdjustMountPointIconsScale();
			// Hint about changing scales
			CheckScale();

		}

		public void OnCloseEditor() {
			// Disables zoom
			if (zoomed) {
				DisableZoom();
				_map.ScaleCities ();
				_map.ScaleMountPoints();
			}
		}

		public override void OnInspectorGUI () {
			if (_editor == null)
				return;
			if (_map.showProvinces) {
				_editor.editingMode = EDITING_MODE.PROVINCES;
			} else {
				_editor.editingMode = EDITING_MODE.COUNTRIES;
				if (_map.frontiersDetail == FRONTIERS_DETAIL.High) {
					_editor.editingCountryFile  = EDITING_COUNTRY_FILE.COUNTRY_HIGHDEF;
				} else {
					_editor.editingCountryFile  = EDITING_COUNTRY_FILE.COUNTRY_LOWDEF;
				}
			}

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			if (Application.isPlaying) {
				EditorGUILayout.BeginHorizontal ();
				DrawWarningLabel ("Map Editor not available at runtime");
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();
				return;
			}

			EditorGUILayout.BeginHorizontal ();
			DrawWarningLabel ("Map Editor");
			GUILayout.FlexibleSpace();
			if (_map.transform.localScale.x>=1000)
				DrawWarningLabel ("Zoom: ON");
			else
				DrawWarningLabel ("Zoom: OFF");
			if (GUILayout.Button("Toggle Zoom")) ToggleZoom();
			if (GUILayout.Button("Redraw")) {
				_editor.RedrawAll ();
				CheckHideEditorMesh();
			}
			if (GUILayout.Button("Help")) EditorUtility.DisplayDialog("World Map Editor", "This editor component allows you to modify the borders of the map, and also perform some operations with provinces, countries and cities, like creating and merging.\n\nRemember that the map editor works on the Scene View and not in the Game View. Please read the documentation included for general instructions about the editor and this asset. For questions and support, visit kronnect.com", "Ok");
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Show Layers", GUILayout.Width (90));
			EDITING_MODE prevEditingMode = _editor.editingMode;
			_editor.editingMode = (EDITING_MODE)EditorGUILayout.Popup ((int)_editor.editingMode, editingModeOptions);
			if (_editor.editingMode!=prevEditingMode) {
				ChangeEditingMode(_editor.editingMode);
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Country File", GUILayout.Width (90));
			EDITING_COUNTRY_FILE prevCountryFile = _editor.editingCountryFile;
			_editor.editingCountryFile = (EDITING_COUNTRY_FILE)EditorGUILayout.Popup ((int)_editor.editingCountryFile, editingCountryFileOptions);
			if (_editor.editingCountryFile!=prevCountryFile) {
				if (!EditorUtility.DisplayDialog("Switch Geodata File", "Choosing a different country file will reload definitions and any unsaved change to current file will be lost. Continue?", "Switch Geodata File", "Cancel")) {
					_editor.editingCountryFile = prevCountryFile;
					CheckScale();
					return;
				}
				SwitchEditingFrontiersFile();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();

			ShowEntitySelectors();

			EditorGUILayout.BeginVertical(blackStyle);

			// main toolbar
			GUIStyle toolbarStyle = new GUIStyle (GUI.skin.button);
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();

			OPERATION_MODE prevOp = _editor.operationMode;
			_editor.operationMode = (OPERATION_MODE)GUILayout.Toolbar ((int)_editor.operationMode, mainToolbarIcons, toolbarStyle, GUILayout.Height (24), GUILayout.MaxWidth (350));
			if (prevOp != _editor.operationMode) {
				NewShapeInit();
				ProcessOperationMode ();
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();

			if (_editor.infoMsg.Length > 0) {
				if (Event.current.type == EventType.Layout && (DateTime.Now - _editor.infoMsgStartTime).TotalSeconds > 5) {
					_editor.infoMsg = "";
				} else {
					GUIStyle explanationStyle = new GUIStyle (GUI.skin.box);
					explanationStyle.normal.textColor = new Color (0.52f, 0.9f, 0.66f);
					GUILayout.Box (_editor.infoMsg, explanationStyle, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
				}
			}
			EditorGUILayout.Separator();
			switch (_editor.operationMode) {
			case OPERATION_MODE.UNDO:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				DrawWarningLabel ("Discard current changes?");
				if (GUILayout.Button ("Discard", GUILayout.Width (80))) {
					_editor.DiscardChanges ();
					_editor.operationMode = OPERATION_MODE.SELECTION;
				}
				if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
					_editor.reshapeRegionMode = RESHAPE_REGION_TOOL.POINT;
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Separator ();
				EditorGUILayout.EndVertical ();
				EditorGUILayout.Separator ();
				EditorGUILayout.BeginVertical (blackStyle);
				break;
			case OPERATION_MODE.CONFIRM:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				DrawWarningLabel ("Save changes?");
				if (GUILayout.Button ("Save", GUILayout.Width (80))) {
					if (SaveChanges ()) {
						_editor.SetInfoMsg (INFO_MSG_CHANGES_SAVED);
					} else {
						_editor.SetInfoMsg(INFO_MSG_NO_CHANGES_TO_SAVE);
					}
					_editor.operationMode = OPERATION_MODE.SELECTION;
				}
				if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
					_editor.reshapeRegionMode = RESHAPE_REGION_TOOL.POINT;
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Separator ();
				EditorGUILayout.EndVertical ();
				EditorGUILayout.Separator ();
				EditorGUILayout.BeginVertical (blackStyle);
				break;
			case OPERATION_MODE.RESHAPE:
				if (_editor.countryIndex < 0 && _editor.cityIndex<0) {
					DrawWarningLabel ("No country, province nor city selected.");
					break;
				}

				if (_editor.countryIndex>=0) {
					ShowReshapingRegionTools();
				}
				if (_editor.cityIndex>=0) {
					ShowReshapingCityTools();
				}
				if (_editor.mountPointIndex>=0) {
					ShowReshapingMountPointTools();
				}
				break;
			case OPERATION_MODE.CREATE:
				ShowCreateTools();
				break;
			}

			EditorGUILayout.Separator ();

			EditorGUILayout.EndVertical ();

			CheckHideEditorMesh();
		}

		void OnSceneGUI () {
			if (Application.isPlaying) return;

			ProcessOperationMode ();
		}

		Camera GetSceneViewCamera() {
			SceneView sv = SceneView.lastActiveSceneView;
			if (sv!=null) return sv.camera;
			return null;
		}


		void ToggleZoom() {
			Camera cam = GetSceneViewCamera();
			if (cam==null) {
				EditorUtility.DisplayDialog("Ops!", "Could not get a reference to the camera in the scene view. You will need to modify the globe scale and adjust the camera distance manually.", "Ok");
				return;
			}
			if (zoomed) {
				DisableZoom();
			} else {
				zoomed = true;
				_map.transform.localScale = MiscVector.Vector3one * 1000.0f;
				_map.transform.position = cam.transform.position + cam.transform.forward * _map.transform.localScale.x;
				_map.labelsQuality = LABELS_QUALITY.High;
				_map.Redraw();
				_map.Redraw(); // needs to refresh again to draw labels correctly; TODO: I need to debug this
			}
			_editor.ClearSelection();
			_editor.RedrawFrontiers();
		}

		void DisableZoom() {
			_map.transform.position = zoomOldPosition;
			_map.transform.localScale =  zoomOldValue;
			_map.labelsQuality = labelsOldQuality;
			_map.Redraw();
			if (SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.FrameSelected();
			zoomed = false;
		}


		void ChangeEditingMode(EDITING_MODE newMode) {
			_editor.editingMode = newMode;
			// Ensure file is loaded by the map
			switch(_editor.editingMode) {
			case EDITING_MODE.COUNTRIES: _map.showFrontiers = true; _map.showProvinces = false; _map.HideProvinces(); break;
			case EDITING_MODE.PROVINCES: _map.showProvinces = true; break;
			}
		}

		void ShowEntitySelectors() {

			// preprocesssing logic first to not interfere with layout and repaint events
			string[] provinceNames, countryNames = _editor.countryNames, cityNames = _editor.cityNames, mountPointNames = _editor.mountPointNames;
			if (_editor.editingMode != EDITING_MODE.PROVINCES) {
				provinceNames = emptyStringArray;
			} else {
				provinceNames = _editor.provinceNames;
				if (provinceNames==null) provinceNames = emptyStringArray;
			}
			if (mountPointNames==null) mountPointNames = emptyStringArray;

			EditorGUILayout.BeginVertical (blackStyle);
			// country selector
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Country", GUILayout.Width (90));
			int selection = EditorGUILayout.Popup (_editor.GUICountryIndex, countryNames);
			if (selection != _editor.GUICountryIndex) {
				_editor.CountrySelectByCombo (selection);
				if (_editor.countryRegionIndex>=0 && _editor.countryRegionIndex>=0) FocusSpherePoint(_map.countries[_editor.countryIndex].regions[_editor.regionIndex].center);
			}
			bool prevc = _editor.groupByParentAdmin;
			GUILayout.Label ("Grouped");
			_editor.groupByParentAdmin = EditorGUILayout.Toggle (_editor.groupByParentAdmin, GUILayout.Width (20));
			if (_editor.groupByParentAdmin != prevc) {
				_editor.ReloadCountryNames ();
			}
			EditorGUILayout.EndHorizontal ();
			if (_editor.countryIndex >= 0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Name", GUILayout.Width (90));
				_editor.GUICountryNewName = EditorGUILayout.TextField (_editor.GUICountryNewName);
				if (GUILayout.Button ("Update")) {
					_editor.CountryRename ();
				}
				if (GUILayout.Button("Sanitize")) {
					if (EditorUtility.DisplayDialog("Sanitize Frontiers", "This option detects polygon issues (like self-crossing polygon) and fix them. Only use if you encounter some problem with the shape of this country.\n\nContinue?", "Ok", "Cancel")) {
						_editor.CountrySanitize();
						_editor.CountryRegionSelect();
					}
				}
				if (GUILayout.Button ("Delete")) {
					if (EditorUtility.DisplayDialog("Delete Country", "This option will completely delete current country and all its dependencies (cities, provinces, mount points, ...)\n\nContinue?", "Yes", "No")) {
						_editor.CountryDelete();
						_editor.SetInfoMsg (INFO_MSG_COUNTRY_DELETED);
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
				}
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Hidden", GUILayout.Width (90));
				_editor.GUICountryHidden = EditorGUILayout.Toggle (_editor.GUICountryHidden);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Sovereign", GUILayout.Width (90));
				_editor.GUICountryTransferToCountryIndex = EditorGUILayout.Popup (_editor.GUICountryTransferToCountryIndex, countryNames);
				if (GUILayout.Button ("Transfer")) {
					if (_editor.GUICountryIndex != _editor.GUICountryTransferToCountryIndex) {
						string sourceCountry = countryNames[_editor.GUICountryIndex].Trim();
						string targetCountry = countryNames[_editor.GUICountryTransferToCountryIndex].Trim ();
						if (EditorUtility.DisplayDialog("Change Country's Sovereignty", "Current country " + sourceCountry + " will join target country " + targetCountry + ".\n\nAre you sure (can take near a minute on big countries)?", "Ok", "Cancel")) {
							_editor.CountryTransferTo();
							_editor.operationMode = OPERATION_MODE.SELECTION;
						}
					} else
						EditorUtility.DisplayDialog("Invalid destination", "Can't transfer to itself.", "Ok");
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Continent", GUILayout.Width (90));
				_editor.GUICountryNewContinent = EditorGUILayout.TextField (_editor.GUICountryNewContinent);
				GUI.enabled = _editor.countryIndex>=0;
				if (GUILayout.Button ("Rename")) {
					if (EditorUtility.DisplayDialog("Continent Renaming", "This option will rename the continent affecting to all countries in same continent. Continue?", "Yes", "No")) {
						_editor.ContinentRename ();
					}
				}
				if (GUILayout.Button("Delete")) {
					if (EditorUtility.DisplayDialog("Delete all countries (in same continent)", "You're going to delete all countries and provinces in continent " + _map.countries[_editor.countryIndex].continent + ".\n\nAre you sure?", "Yes", "No")) {
						_editor.CountryDeleteSameContinent();
						_editor.SetInfoMsg (INFO_MSG_CONTINENT_DELETED);
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}
			
			if (_editor.editingMode == EDITING_MODE.PROVINCES && _editor.countryIndex>=0) {
				EditorGUILayout.Separator();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Province/State", GUILayout.Width (90));
				int provSelection = EditorGUILayout.Popup (_editor.GUIProvinceIndex, provinceNames);
				if (provSelection != _editor.GUIProvinceIndex) {
					_editor.ProvinceSelectByCombo (provSelection);
					if (_editor.provinceIndex>=0 && _editor.provinceRegionIndex>=0) FocusSpherePoint(_map.provinces[_editor.provinceIndex].regions[_editor.provinceRegionIndex].center);
				}
				EditorGUILayout.EndHorizontal ();
				if (_editor.provinceIndex >= 0) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Name", GUILayout.Width (90));
					_editor.GUIProvinceNewName = EditorGUILayout.TextField (_editor.GUIProvinceNewName);
					if (GUILayout.Button ("Update")) {
						_editor.ProvinceRename ();
					}
					if (GUILayout.Button("Sanitize")) {
						if (EditorUtility.DisplayDialog("Sanitize Borders", "This option detects polygon issues (like self-crossing polygon) and fix them. Only use if you encounter some problem with the shape of this province.\n\nContinue?", "Ok", "Cancel")) {
							_editor.ProvinceSanitize();
							_editor.ProvinceRegionSelect();
						}
					}
					if (GUILayout.Button ("Delete")) {
						if (EditorUtility.DisplayDialog("Delete Province", "This option will completely delete current province.\n\nContinue?", "Yes", "No")) {
							_editor.ProvinceDelete ();
							_editor.SetInfoMsg (INFO_MSG_PROVINCE_DELETED);
							_editor.operationMode = OPERATION_MODE.SELECTION;
						}
					}		
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Sovereign", GUILayout.Width (90));
				_editor.GUIProvinceTransferToCountryIndex = EditorGUILayout.Popup (_editor.GUIProvinceTransferToCountryIndex, countryNames);
				if (GUILayout.Button ("Transfer")) {
					if (_editor.GUIProvinceIndex!=_editor.GUIProvinceTransferToCountryIndex) {
					string sourceProvince = provinceNames[_editor.GUIProvinceIndex].Trim ();
					string targetCountry = countryNames[_editor.GUIProvinceTransferToCountryIndex].Trim ();
					if (_editor.editingCountryFile == EDITING_COUNTRY_FILE.COUNTRY_LOWDEF) {
						EditorUtility.DisplayDialog("Change Province's Sovereignty", "This command is only available with High-Definition Country File selected.", "Ok");
					} else if (EditorUtility.DisplayDialog("Change Province's Sovereignty", "Current province " + sourceProvince + " will join target country " + targetCountry + ".\n\nAre you sure?", "Ok", "Cancel")) {
						_editor.ProvinceTransferTo();
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
					} else
						EditorUtility.DisplayDialog("Invalid destination", "Can't transfer to itself.", "Ok");

				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (" ", GUILayout.Width (90));
				if (GUILayout.Button ("Delete All Country Provinces", GUILayout.Width(180))) {
					if (EditorUtility.DisplayDialog("Delete All Country Provinces", "This option will delete all provinces of current country.\n\nContinue?", "Yes", "No")) {
						_editor.DeleteCountryProvinces ();
						_editor.SetInfoMsg (INFO_MSG_PROVINCE_DELETED);
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
				}		
				EditorGUILayout.EndHorizontal ();
			}

			if (_editor.countryIndex>=0) {
				EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("City", GUILayout.Width (90));
			int citySelection = EditorGUILayout.Popup (_editor.GUICityIndex, cityNames);
			if (citySelection != _editor.GUICityIndex) {
				_editor.CitySelectByCombo (citySelection);
				if (_editor.cityIndex>=0) FocusSpherePoint(_map.cities[_editor.cityIndex].unitySphereLocation);
			}
			EditorGUILayout.EndHorizontal ();
			if (_editor.cityIndex >= 0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Name", GUILayout.Width (90));
				_editor.GUICityNewName = EditorGUILayout.TextField (_editor.GUICityNewName);
				if (GUILayout.Button ("Update")) {
					UndoPushCityStartOperation("Undo Rename City");
					_editor.CityRename ();
					UndoPushCityEndOperation();
				}
				EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Class", GUILayout.Width (90));
					_editor.GUICityClass = (CITY_CLASS)EditorGUILayout.IntPopup((int)_editor.GUICityClass, cityClassOptions, cityClassValues);
					if (GUILayout.Button ("Update")) {
						UndoPushCityStartOperation("Undo Change City Class");
						_editor.CityClassChange();
						UndoPushCityEndOperation();
					}
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Population", GUILayout.Width (90));
				_editor.GUICityPopulation = EditorGUILayout.TextField (_editor.GUICityPopulation);
				if (GUILayout.Button ("Update")) {
					int pop = 0;
					if (int.TryParse(_editor.GUICityPopulation, out pop)) {
						UndoPushCityStartOperation("Undo Change Population");
						_editor.CityChangePopulation (pop);
						UndoPushCityEndOperation();
					}
				}
				EditorGUILayout.EndHorizontal ();
			}
			}

			if (_editor.countryIndex>=0) {
				EditorGUILayout.Separator();
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Mount Point", GUILayout.Width (90));
				int mpSelection = EditorGUILayout.Popup (_editor.GUIMountPointIndex, mountPointNames);
				if (mpSelection != _editor.GUIMountPointIndex) {
					_editor.MountPointSelectByCombo (mpSelection);
					if (_editor.mountPointIndex>=0) FocusSpherePoint(_map.mountPoints[_editor.mountPointIndex].unitySphereLocation);
				}
				EditorGUILayout.EndHorizontal ();
				if (_editor.mountPointIndex >= 0) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Name", GUILayout.Width (90));
					_editor.GUIMountPointNewName = EditorGUILayout.TextField (_editor.GUIMountPointNewName);
					if (GUILayout.Button ("Update")) {
						UndoPushMountPointStartOperation("Undo Rename Mount Point");
						_editor.MountPointRename ();
						UndoPushMountPointEndOperation();
					}
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Type", GUILayout.Width (90));
					_editor.GUIMountPointNewType = EditorGUILayout.TextField (_editor.GUIMountPointNewType);
					if (GUILayout.Button ("Update")) {
						UndoPushMountPointStartOperation("Undo Change Mount Point Type");
						_editor.MountPointUpdateType ();
						UndoPushMountPointEndOperation();
					}
					EditorGUILayout.EndHorizontal ();
					MountPoint mp = _map.mountPoints[_editor.mountPointIndex];
					foreach(string key in mp.customTags.Keys) {
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Tag", GUILayout.Width (90));
						string newKey = EditorGUILayout.TextField (key);
						string currentValue = mp.customTags[key];
						if (!newKey.Equals(key)) {
							mp.customTags.Remove(key);
							mp.customTags.Add (key, currentValue);
							_editor.mountPointChanges = true;
							break;
						}
						GUILayout.Label ("Value");
						string newValue = EditorGUILayout.TextField (currentValue);
						if (!newValue.Equals(currentValue)) {
							mp.customTags[key] = newValue;
							_editor.mountPointChanges = true;
						}
						if (GUILayout.Button("Remove")) {
							mp.customTags.Remove(key);
							_editor.mountPointChanges = true;
							break;
						}
						EditorGUILayout.EndHorizontal ();
					}
					// new tag line
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Tag", GUILayout.Width (90));
					_editor.GUIMountPointNewTagKey = EditorGUILayout.TextField (_editor.GUIMountPointNewTagKey);
					GUILayout.Label ("Value");
					_editor.GUIMountPointNewTagValue  = EditorGUILayout.TextField (_editor.GUIMountPointNewTagValue);
					if (GUILayout.Button("Add")) {
						_editor.MountPointAddNewTag();
					}
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndVertical ();
		}


		void ShowReshapingRegionTools() {
			EditorGUILayout.BeginVertical(areaStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			DrawWarningLabel("REGION MODIFYING TOOLS");
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			RESHAPE_REGION_TOOL prevTool = _editor.reshapeRegionMode;
			int selectionGridRows = (reshapeRegionToolbarIcons.Length - 1) / 4 + 1;
			GUIStyle selectionGridStyle = new GUIStyle (GUI.skin.button);
			selectionGridStyle.margin = new RectOffset (2, 2, 2, 2);
			_editor.reshapeRegionMode = (RESHAPE_REGION_TOOL)GUILayout.SelectionGrid ((int)_editor.reshapeRegionMode, reshapeRegionToolbarIcons, 4, selectionGridStyle, GUILayout.Height (24 * selectionGridRows), GUILayout.MaxWidth (300));
			if (_editor.reshapeRegionMode != prevTool) {
				if (_editor.countryIndex >= 0) {
					tickStart = DateTime.Now.Ticks;
				}
				ProcessOperationMode ();
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUIStyle explanationStyle = new GUIStyle (GUI.skin.box);
			explanationStyle.normal.textColor = new Color (0.52f, 0.66f, 0.9f);
			GUILayout.Box (reshapeRegionModeExplanation [(int)_editor.reshapeRegionMode], explanationStyle, GUILayout.ExpandWidth (true));
			EditorGUILayout.EndHorizontal ();
			
			if (_editor.reshapeRegionMode.hasCircle()) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Circle Width", GUILayout.Width (90));
				_editor.reshapeCircleWidth = EditorGUILayout.Slider (_editor.reshapeCircleWidth, 0.001f, 0.1f);
				EditorGUILayout.EndHorizontal ();
			}

			if (_editor.reshapeRegionMode == RESHAPE_REGION_TOOL.POINT || _editor.reshapeRegionMode == RESHAPE_REGION_TOOL.CIRCLE || _editor.reshapeRegionMode == RESHAPE_REGION_TOOL.ERASER) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label("Selected Region Only", GUILayout.Width (90));
				_editor.circleCurrentRegionOnly = EditorGUILayout.Toggle(_editor.circleCurrentRegionOnly, GUILayout.Width(20));
				EditorGUILayout.EndHorizontal ();
			}
			
			switch (_editor.reshapeRegionMode) {
			case RESHAPE_REGION_TOOL.CIRCLE:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label("Constant Move", GUILayout.Width (90));
				_editor.circleMoveConstant = EditorGUILayout.Toggle(_editor.circleMoveConstant, GUILayout.Width(20));
				EditorGUILayout.EndHorizontal ();
				break;
			case RESHAPE_REGION_TOOL.MAGNET:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label("Agressive Mode", GUILayout.Width (90));
				_editor.magnetAgressiveMode = EditorGUILayout.Toggle(_editor.magnetAgressiveMode, GUILayout.Width(20));
				EditorGUILayout.EndHorizontal ();
				break;
			case RESHAPE_REGION_TOOL.SPLITV:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				DrawWarningLabel ("Confirm split vertically?");
				if (GUILayout.Button ("Split", GUILayout.Width (80))) {
					_editor.SplitVertically ();
					_editor.operationMode = OPERATION_MODE.SELECTION;
				}
				if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
					_editor.reshapeRegionMode = RESHAPE_REGION_TOOL.POINT;
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				break;
			case RESHAPE_REGION_TOOL.SPLITH:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				DrawWarningLabel ("Confirm split horizontally?");
				if (GUILayout.Button ("Split", GUILayout.Width (80))) {
					_editor.SplitHorizontally ();
					_editor.operationMode = OPERATION_MODE.SELECTION;
				}
				if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
					_editor.reshapeRegionMode = RESHAPE_REGION_TOOL.POINT;
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				break;
			case RESHAPE_REGION_TOOL.DELETE:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (_editor.entityIndex < 0) {
					DrawWarningLabel ("Select a region to delete.");
				} else {
					if (_editor.editingMode == EDITING_MODE.COUNTRIES) {
						bool deletingRegion = _map.countries [_editor.countryIndex].regions.Count > 1;
						if (deletingRegion) {
							DrawWarningLabel ("Confirm delete this region?");
						} else {
							DrawWarningLabel ("Confirm delete this country?");
						}
						if (GUILayout.Button ("Delete", GUILayout.Width (80))) {
							if (deletingRegion) {
								_editor.CountryRegionDelete ();
								_editor.SetInfoMsg (INFO_MSG_REGION_DELETED);
							} else {
								_editor.CountryDelete ();
								_editor.SetInfoMsg (INFO_MSG_COUNTRY_DELETED);
							}
							_editor.operationMode = OPERATION_MODE.SELECTION;
						}
					} else {
						if (_editor.provinceIndex>=0 && _editor.provinceIndex<_map.provinces.Length) {
							bool deletingRegion = _map.provinces[_editor.provinceIndex].regions!=null && _map.provinces[_editor.provinceIndex].regions.Count > 1;
							if (deletingRegion) {
							DrawWarningLabel ("Confirm delete this region?");
						} else {
							DrawWarningLabel ("Confirm delete this province/state?");
						}
						if (GUILayout.Button ("Delete", GUILayout.Width (80))) {
								if (deletingRegion) {
								_editor.ProvinceRegionDelete ();
								_editor.SetInfoMsg (INFO_MSG_REGION_DELETED);
								} else {
									_editor.ProvinceDelete ();
									_editor.SetInfoMsg (INFO_MSG_PROVINCE_DELETED);
								}
							_editor.operationMode = OPERATION_MODE.SELECTION;
						}		
						}
					}
					
					if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
						_editor.reshapeRegionMode = RESHAPE_REGION_TOOL.POINT;
					}
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				break;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();
		}


		void ShowReshapingCityTools() {
			GUILayout.BeginVertical(areaStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			DrawWarningLabel("CITY MODIFYING TOOLS");
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			RESHAPE_CITY_TOOL prevTool = _editor.reshapeCityMode;
			int selectionGridRows = (reshapeCityToolbarIcons.Length - 1) / 2 + 1;
			GUIStyle selectionGridStyle = new GUIStyle (GUI.skin.button);
			selectionGridStyle.margin = new RectOffset (2, 2, 2, 2);
			_editor.reshapeCityMode = (RESHAPE_CITY_TOOL)GUILayout.SelectionGrid ((int)_editor.reshapeCityMode, reshapeCityToolbarIcons, 2, selectionGridStyle, GUILayout.Height (24 * selectionGridRows), GUILayout.MaxWidth (150));
			if (_editor.reshapeCityMode != prevTool) {
				ProcessOperationMode ();
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUIStyle explanationStyle = new GUIStyle (GUI.skin.box);
			explanationStyle.normal.textColor = new Color (0.52f, 0.66f, 0.9f);
			GUILayout.Box (reshapeCityModeExplanation [(int)_editor.reshapeCityMode], explanationStyle, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
			EditorGUILayout.EndHorizontal ();

			switch (_editor.reshapeCityMode) {
			case RESHAPE_CITY_TOOL.DELETE:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (_editor.cityIndex < 0) {
					DrawWarningLabel ("Select a city to delete.");
				} else {
					DrawWarningLabel ("Confirm delete this city?");
					if (GUILayout.Button ("Delete", GUILayout.Width (80))) {
						UndoPushCityStartOperation ("Undo Delete City");
						_editor.DeleteCity ();
						UndoPushCityEndOperation();
						_editor.SetInfoMsg (INFO_MSG_CITY_DELETED);
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
					if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
						_editor.reshapeCityMode = RESHAPE_CITY_TOOL.MOVE;
					}
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				break;
			}

			GUILayout.EndVertical();
		}

		void ShowReshapingMountPointTools() {
			GUILayout.BeginVertical(areaStyle);
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			DrawWarningLabel("MOUNT POINT MODIFYING TOOLS");
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			RESHAPE_MOUNT_POINT_TOOL prevTool = _editor.reshapeMountPointMode;
			int selectionGridRows = (reshapeMountPointToolbarIcons.Length - 1) / 2 + 1;
			GUIStyle selectionGridStyle = new GUIStyle (GUI.skin.button);
			selectionGridStyle.margin = new RectOffset (2, 2, 2, 2);
			_editor.reshapeMountPointMode = (RESHAPE_MOUNT_POINT_TOOL)GUILayout.SelectionGrid ((int)_editor.reshapeMountPointMode, reshapeMountPointToolbarIcons, 2, selectionGridStyle, GUILayout.Height (24 * selectionGridRows), GUILayout.MaxWidth (150));
			if (_editor.reshapeMountPointMode != prevTool) {
				ProcessOperationMode ();
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUIStyle explanationStyle = new GUIStyle (GUI.skin.box);
			explanationStyle.normal.textColor = new Color (0.52f, 0.66f, 0.9f);
			GUILayout.Box (reshapeMountPointModeExplanation [(int)_editor.reshapeMountPointMode], explanationStyle, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
			EditorGUILayout.EndHorizontal ();
			
			switch (_editor.reshapeMountPointMode) {
			case RESHAPE_MOUNT_POINT_TOOL.DELETE:
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (_editor.mountPointIndex < 0) {
					DrawWarningLabel ("Select a mount point to delete.");
				} else {
					DrawWarningLabel ("Confirm delete this mount point?");
					if (GUILayout.Button ("Delete", GUILayout.Width (80))) {
						UndoPushMountPointStartOperation ("Undo Delete Mount Point");
						_editor.DeleteMountPoint ();
						UndoPushMountPointEndOperation();
						_editor.SetInfoMsg (INFO_MSG_MOUNT_POINT_DELETED);
						_editor.operationMode = OPERATION_MODE.SELECTION;
					}
					if (GUILayout.Button ("Cancel", GUILayout.Width (80))) {
						_editor.reshapeMountPointMode = RESHAPE_MOUNT_POINT_TOOL.MOVE;
					}
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();
				break;
			}
			
			GUILayout.EndVertical();
		}

		void ShowCreateTools() {
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			CREATE_TOOL prevCTool = _editor.createMode;
			GUIStyle selectionCGridStyle = new GUIStyle (GUI.skin.button);
			int selectionCGridRows = (createToolbarIcons.Length - 1) / 3 + 1;
			selectionCGridStyle.margin = new RectOffset (2, 2, 2, 2);
			_editor.createMode = (CREATE_TOOL)GUILayout.SelectionGrid ((int)_editor.createMode, createToolbarIcons, 3, selectionCGridStyle, GUILayout.Height (24 * selectionCGridRows), GUILayout.MaxWidth (310));
			if (_editor.createMode != prevCTool) {
				ProcessOperationMode ();
				NewShapeInit();
				if (_editor.editingMode == EDITING_MODE.COUNTRIES && (_editor.createMode == CREATE_TOOL.PROVINCE || _editor.createMode == CREATE_TOOL.PROVINCE_REGION)) {
					ChangeEditingMode(EDITING_MODE.PROVINCES);
				}
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUIStyle explanationCStyle = new GUIStyle (GUI.skin.box);
			explanationCStyle.normal.textColor = new Color (0.52f, 0.66f, 0.9f);
			GUILayout.Box (createModeExplanation [(int)_editor.createMode], explanationCStyle, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
			EditorGUILayout.EndHorizontal ();
		}

		#endregion

		#region Processing logic


		// Add a menu item called "Double Mass" to a Rigidbody's context menu.
		[MenuItem ("CONTEXT/WorldMapEditor/Restore Backup")]
		static void RestoreBackup (MenuCommand command) {
			if (!EditorUtility.DisplayDialog("Restore original geodata files?", "Current geodata files will be replaced by the original files from Backup folder. Any changes will be lost. This operation can't be undone.\n\nRestore files?", "Restore", "Cancel")) {
				return;
			}

			// Proceed and restore
			string[] paths = AssetDatabase.GetAllAssetPaths ();
			bool backupFolderExists = false;
			string geoDataFolder = "", backupFolder = "";
			for (int k=0; k<paths.Length; k++) {
				if (paths [k].EndsWith ("Resources/Geodata")) {
					geoDataFolder = paths [k]; 
				} else if (paths [k].EndsWith ("WorldPoliticalMapGlobeEdition/Backup")) {
					backupFolder = paths[k];
					backupFolderExists = true;
				}
			}

			WorldMapEditor editor = (WorldMapEditor)command.context;
			
			if (!backupFolderExists) {
				editor.SetInfoMsg(INFO_MSG_BACKUP_NOT_FOUND);
				return;
			}

			// Countries110
			AssetDatabase.DeleteAsset( geoDataFolder + "/countries110.txt");
			AssetDatabase.SaveAssets();
			AssetDatabase.CopyAsset (backupFolder + "/countries110.txt", geoDataFolder + "/countries110.txt");
			// Countries10
			AssetDatabase.DeleteAsset( geoDataFolder + "/countries10.txt");
			AssetDatabase.SaveAssets();
			AssetDatabase.CopyAsset (backupFolder + "/countries10.txt", geoDataFolder + "/countries10.txt");
			// Provinces10
			AssetDatabase.DeleteAsset( geoDataFolder + "/provinces10.txt");
			AssetDatabase.SaveAssets();
			AssetDatabase.CopyAsset (backupFolder + "/provinces10.txt", geoDataFolder + "/provinces10.txt");
			// Cities10
			AssetDatabase.DeleteAsset( geoDataFolder + "/cities10.txt");
			AssetDatabase.SaveAssets();
			AssetDatabase.CopyAsset (backupFolder + "/cities10.txt", geoDataFolder + "/cities10.txt");
			// Mount points
			AssetDatabase.DeleteAsset( geoDataFolder + "/mountPoints.txt");
			AssetDatabase.SaveAssets();
			AssetDatabase.CopyAsset (backupFolder + "/mountPoints.txt", geoDataFolder + "/mountPoints.txt");

			AssetDatabase.Refresh();

			// Save changes
			editor.SetInfoMsg(INFO_MSG_BACKUP_RESTORED);
			editor.DiscardChanges();
		}


		// Add a menu item called "Double Mass" to a Rigidbody's context menu.
		[MenuItem ("CONTEXT/WorldMapEditor/Create Low Definition Geodata File")]
		static void CreateLowDefinitionFile (MenuCommand command) {
			WorldMapEditor editor = (WorldMapEditor)command.context;
			if (editor.editingCountryFile != EDITING_COUNTRY_FILE.COUNTRY_HIGHDEF) {
				EditorUtility.DisplayDialog("Create Low Definition Geodata File", "Switch to the high definition country geodata file first.", "Ok");
				return;
			}
			if (!EditorUtility.DisplayDialog("Create Low Definition Geodata File", "The low definition geodata file will be replaced by a reduced quality version of the high definition geodata file.\n\nChanges to the low definition file will be lost. Continue?", "Proceed", "Cancel")) {
				return;
			}

			string geoDataFolder;
			CheckBackup(out geoDataFolder);

			// Save changes
			string dataFileName = "countries110.txt";
			string fullPathName = Application.dataPath;
			int pos = fullPathName.LastIndexOf("/Assets");
			if (pos>0) fullPathName = fullPathName.Substring(0,pos+1);
			fullPathName += geoDataFolder + "/" + dataFileName;
			string data = editor.GetCountryGeoDataLowQuality ();
			File.WriteAllText (fullPathName, data);
			AssetDatabase.Refresh ();

			editor.SetInfoMsg(INFO_MSG_GEODATA_LOW_QUALITY_CREATED);
			editor.ClearSelection();
			editor.map.frontiersDetail = FRONTIERS_DETAIL.Low; // switch to low quality to see results
			editor.DiscardChanges();
		}


		void SwitchEditingFrontiersFile() {
			if (_editor.editingCountryFile == EDITING_COUNTRY_FILE.COUNTRY_HIGHDEF) {
				_map.frontiersDetail = FRONTIERS_DETAIL.High;
			} else {
				_map.frontiersDetail = FRONTIERS_DETAIL.Low;
			}
			_editor.DiscardChanges(); 
		}

		void ProcessOperationMode () {

			AdjustCityIconsScale();
			AdjustMountPointIconsScale();
			if (SceneView.lastActiveSceneView==null || SceneView.lastActiveSceneView.camera==null) return;
			Ray forward = SceneView.lastActiveSceneView.camera.ViewportPointToRay(MiscVector.ViewportCenter);
			frontFaceQuaternion = Quaternion.LookRotation(-forward.direction);

			// Check mouse buttons state and react to possible undo/redo operations
			bool mouseDown = false;
			Event e = Event.current;
			var controlID = GUIUtility.GetControlID (FocusType.Passive);
			if (GUIUtility.hotControl == controlID) {	// release hot control to allow standard navigation
				GUIUtility.hotControl = 0;
			}
			// locks control on map
			var eventType = e.GetTypeForControl (controlID);
			if (eventType == EventType.MouseDown && Event.current.button == 0) {
				mouseDown = true;
				GUIUtility.hotControl = controlID;
				startedReshapeRegion = false;
				startedReshapeCity = false;
			} else if (eventType == EventType.mouseUp && e.button == 0) {
				if (undoPushStarted) {
					if (startedReshapeRegion) {
						UndoPushRegionEndOperation ();
					}
					if (startedReshapeCity) {
						UndoPushCityEndOperation ();
					}
				}
			}

			if (e.type == EventType.ValidateCommand && e.commandName.Equals ("UndoRedoPerformed")) {
				_editor.UndoHandle ();
				EditorUtility.SetDirty (target);
				return;
			}

			switch (_editor.operationMode) {
			case OPERATION_MODE.SELECTION:
				// do we click inside a country or province?
				if (Camera.current == null) // can't ray-trace
					return;
				if (mouseDown) {
					Ray ray = HandleUtility.GUIPointToWorldRay (e.mousePosition);
					bool selected = _editor.CountrySelectByScreenClick (ray);
					if (!selected) {
						_editor.ClearSelection();
					} else {
						if (_editor.editingMode == EDITING_MODE.PROVINCES) {
							_map.DrawProvinces(_editor.countryIndex, true, false);
							selected = _editor.ProvinceSelectByScreenClick (_editor.countryIndex, ray);
							if (!selected) _editor.ClearProvinceSelection();
						}
					}
					if (!_editor.CitySelectByScreenClick(ray)) {
						_editor.ClearCitySelection();
					}
					if (!_editor.MountPointSelectByScreenClick(ray)) {
						_editor.ClearMountPointSelection();
					}
					// Reset the cursor if entity selected
					if (selected) {
						if (_editor.editingMode == EDITING_MODE.PROVINCES) _map.DrawProvinces(_editor.countryIndex, true, false);
						if (_editor.entities!=null)
							_editor.cursor = _editor.entities[_editor.entityIndex].center;
					}
				}

				// Draw selection
				ShowShapePoints (false);
				ShowCitySelected();
				ShowMountPointSelected();
				break;
				
			case OPERATION_MODE.RESHAPE:
				// do we move any handle to change frontiers?
				switch (_editor.reshapeRegionMode) {
				case RESHAPE_REGION_TOOL.POINT: 
				case RESHAPE_REGION_TOOL.CIRCLE: 
					ExecuteMoveTool ();
					break;
				case RESHAPE_REGION_TOOL.MAGNET: 
				case RESHAPE_REGION_TOOL.ERASER:
				case RESHAPE_REGION_TOOL.SMOOTH:
					ExecuteClickTool (e.mousePosition, mouseDown);
					break;
				case RESHAPE_REGION_TOOL.SPLITH:
				case RESHAPE_REGION_TOOL.SPLITV:
				case RESHAPE_REGION_TOOL.DELETE:
					ShowShapePoints(false);
					break;
				}
				switch(_editor.reshapeCityMode) {
				case RESHAPE_CITY_TOOL.MOVE:
					ExecuteCityMoveTool();
					break;
				}
				switch(_editor.reshapeMountPointMode) {
				case RESHAPE_MOUNT_POINT_TOOL.MOVE:
					ExecuteMountPointMoveTool();
					break;
				}
				break;
			case OPERATION_MODE.CREATE:
				switch(_editor.createMode) {
				case CREATE_TOOL.CITY:
					ExecuteCityCreateTool(e.mousePosition, mouseDown);
					break;
				case CREATE_TOOL.COUNTRY:
					ExecuteShapeCreateTool(e.mousePosition, mouseDown);
					break;
				case CREATE_TOOL.COUNTRY_REGION:
				case CREATE_TOOL.PROVINCE:
					if (_editor.countryIndex>=0 && _editor.countryIndex<_map.countries.Length) {
						ExecuteShapeCreateTool(e.mousePosition, mouseDown);
					} else {
						_editor.SetInfoMsg(INFO_MSG_CHOOSE_COUNTRY);
					}
					break;
				case CREATE_TOOL.PROVINCE_REGION:
					if (_editor.countryIndex<=0 || _editor.countryIndex>=_map.countries.Length) {
						_editor.SetInfoMsg(INFO_MSG_CHOOSE_COUNTRY);
					} else if (_editor.provinceIndex<0 || _editor.provinceIndex>=_map.provinces.Length) {
						_editor.SetInfoMsg(INFO_MSG_CHOOSE_PROVINCE);
					} else {
						ExecuteShapeCreateTool(e.mousePosition, mouseDown);
					}
					break;
				case CREATE_TOOL.MOUNT_POINT:
					ExecuteMountPointCreateTool(e.mousePosition, mouseDown);
					break;
				}
				break;
			case OPERATION_MODE.CONFIRM:
			case OPERATION_MODE.UNDO:
				break;
			}

			if (_editor.editingMode == EDITING_MODE.PROVINCES) {
				DrawEditorProvinceNames();
			}
			CheckHideEditorMesh();
		}

		void ExecuteMoveTool () {
			if (_editor.entityIndex<0 || _editor.regionIndex<0) return;
			bool frontiersUnchanged = true;
			if (_editor.entities[_editor.entityIndex].regions == null) return;
			Vector3[] points = _editor.entities [_editor.entityIndex].regions [_editor.regionIndex].points;
			Vector3 oldPoint, newPoint, sourcePosition = MiscVector.Vector3zero, displacement = MiscVector.Vector3zero, newCoor = MiscVector.Vector3zero;
			Transform mapTransform = _map.transform;
			if (controlIds == null || controlIds.Length < points.Length)
				controlIds = new int[points.Length];
			
			bool onePointSelected = false;
			Vector3 selectedPoint = MiscVector.Vector3zero;
			for (int i = 0; i < points.Length; i++) {
				oldPoint = mapTransform.TransformPoint (points [i]);
				float handleSize = HandleUtility.GetHandleSize (oldPoint) * HANDLE_SIZE;
				newPoint = Handles.FreeMoveHandle (oldPoint, frontFaceQuaternion, handleSize, pointSnap,   
				                                   (handleControlID, position, rotation, size) =>
				{
					controlIds [i] = handleControlID;
					Handles.DotCap (handleControlID, position, rotation, size); // rotation, size);
				});
				if (!onePointSelected && GUIUtility.keyboardControl == controlIds [i] && GUIUtility.keyboardControl != 0) {
					onePointSelected = true;
					selectedPoint = oldPoint;
				}
				if (frontiersUnchanged && oldPoint != newPoint) {
					frontiersUnchanged = false;
					newCoor = mapTransform.InverseTransformPoint (newPoint);
					sourcePosition = points [i];
					displacement = new Vector3 (newCoor.x - points [i].x, newCoor.y - points [i].y, newCoor.z - points[i].z);
				}
			}
			if (_editor.reshapeRegionMode.hasCircle ()) {
				if (!onePointSelected) {
					selectedPoint = mapTransform.TransformPoint (points [0]);
				}
				float size = _editor.reshapeCircleWidth * mapTransform.localScale.y;
				Handles.CircleCap (0, selectedPoint, frontFaceQuaternion, size);
			}

			if (!frontiersUnchanged) {
				List<Region> affectedRegions = null;
				switch (_editor.reshapeRegionMode) {
				case RESHAPE_REGION_TOOL.POINT:
					if (!startedReshapeRegion)
						UndoPushRegionStartOperation ("Undo Point Move");
					affectedRegions = _editor.MovePoint (sourcePosition, displacement);
					break;
				case RESHAPE_REGION_TOOL.CIRCLE:
					if (!startedReshapeRegion)
						UndoPushRegionStartOperation ("Undo Group Move");
					affectedRegions =_editor.MoveCircle (sourcePosition, displacement, _editor.reshapeCircleWidth);
					break;
				}
				_editor.RedrawFrontiers (affectedRegions, false);
				HandleUtility.Repaint ();
			}
		}

		void ExecuteClickTool (Vector2 mousePosition, bool clicked) {
			if (_editor.entityIndex < 0 || _editor.entityIndex >= _editor.entities.Length)
				return;

			// Show the mouse cursor
			if (Camera.current == null)
				return;

			// Show the points
			ShowShapePoints (_editor.reshapeRegionMode != RESHAPE_REGION_TOOL.SMOOTH);
			Transform mapTransform = _map.transform;

			Ray ray = HandleUtility.GUIPointToWorldRay (mousePosition);
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, _map.layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == _map.gameObject) {
						Vector3 cursorPos = hits [k].point;
						_editor.cursor = mapTransform.InverseTransformPoint (cursorPos);
						if (_editor.reshapeRegionMode == RESHAPE_REGION_TOOL.SMOOTH) {
							ShowCandidatePoint ();
						} else {
							// Show circle cursor
							float seconds = (float)new TimeSpan (DateTime.Now.Ticks - tickStart).TotalSeconds;
							seconds *= 4.0f;
							float t = seconds % 2;
							if (t >= 1)
								t = 2 - t;
							float effect = Mathf.SmoothStep (0, 1, t) / 10.0f;
							float size = _editor.reshapeCircleWidth * mapTransform.localScale.y * (0.9f + effect);
							Handles.CircleCap (0, cursorPos, frontFaceQuaternion, size);
						}

						if (clicked) {
							switch (_editor.reshapeRegionMode) {
							case RESHAPE_REGION_TOOL.MAGNET:
								if (!startedReshapeRegion)
									UndoPushRegionStartOperation ("Undo Magnet");
								_editor.Magnet (_editor.cursor, _editor.reshapeCircleWidth);
								break;
							case RESHAPE_REGION_TOOL.ERASER:
								if (!startedReshapeRegion)
									UndoPushRegionStartOperation ("Undo Eraser");
								_editor.Erase (_editor.cursor, _editor.reshapeCircleWidth);
								break;
							case RESHAPE_REGION_TOOL.SMOOTH:
								if (!startedReshapeRegion)
									UndoPushRegionStartOperation ("Undo Smooth");
								_editor.AddPoint (_editor.cursor); // Addpoint manages the refresh
								break;
							}
						}
						HandleUtility.Repaint ();
					}
				}
			}
		}


		void ExecuteCityCreateTool (Vector2 mousePosition, bool clicked) {

			// Show the mouse cursor
			if (Camera.current == null)
				return;
			
			// Show the points
			Transform mapTransform = _map.transform;
			
			Ray ray = HandleUtility.GUIPointToWorldRay (mousePosition);
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, _map.layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == _map.gameObject) {
						Vector3 cursorPos = hits [k].point;
						_editor.cursor = mapTransform.InverseTransformPoint (cursorPos);

						Handles.color = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
						float handleSize = HandleUtility.GetHandleSize (cursorPos) * HANDLE_SIZE * 4.0f;
						Handles.SphereCap (0, cursorPos, frontFaceQuaternion, handleSize);
						Handles.color = Color.white;

						if (clicked) {
							if (_editor.countryIndex<0 || _editor.countryIndex>=_map.countries.Length) {
								EditorUtility.DisplayDialog("Add new city", "Please choose a country first.", "Ok");
								return;
							}
							UndoPushCityStartOperation  ("Undo Create City");
							_editor.CityCreate (_editor.cursor);
							UndoPushCityEndOperation();
						}
					}
					HandleUtility.Repaint ();
				}
			}
		}

		void AdjustCityIconsScale() {
			// Adjust city icons in scene view
			if (_map==null || _map.cities==null) return;

			Transform t = _map.transform.FindChild ("Cities");
			if (t!=null) {
				Camera cam = GetSceneViewCamera();
				float f = cam!=null ? ((cam.transform.position - _map.transform.position)/_map.transform.localScale.x).sqrMagnitude: 1.0f;
				CityScaler scaler = t.GetComponent<CityScaler>();
				scaler.ScaleCities(0.005f*f);
			} else {
				// This should not happen but maybe the user deleted the layer. Forces refresh.
				_map.showCities = true;
				_map.DrawCities();
			}
		}


		void ShowCitySelected() {
			if (_editor.cityIndex<0 || _editor.cityIndex>=_map.cities.Count) return;
			Vector3 cityPos = _map.cities[_editor.cityIndex].unitySphereLocation;
			Vector3 worldPos = _map.transform.TransformPoint(cityPos);
			float handleSize = HandleUtility.GetHandleSize (worldPos) * HANDLE_SIZE * 2.0f;
			Handles.RectangleCap(0, worldPos, frontFaceQuaternion, handleSize);
		}


		void ExecuteCityMoveTool () {
			if (_editor.cityIndex<0 || _editor.cityIndex>=_map.cities.Count) return;

			Transform mapTransform = _map.transform;
			Vector3 cityPos = _map.cities[_editor.cityIndex].unitySphereLocation;
			Vector3 oldPoint = mapTransform.TransformPoint (cityPos);
			float handleSize = HandleUtility.GetHandleSize (oldPoint) * HANDLE_SIZE * 2.0f;

			Vector3 newPoint = Handles.FreeMoveHandle (oldPoint, frontFaceQuaternion, handleSize, pointSnap,   
				                                   (handleControlID, position, rotation, size) =>
				                                   {
				Handles.RectangleCap (handleControlID, position, rotation, size);
				});
			if (newPoint!=oldPoint) {
				newPoint = mapTransform.InverseTransformPoint(newPoint);
				if (!startedReshapeCity)
					UndoPushCityStartOperation ("Undo City Move");
				_editor.CityMove (newPoint);
				HandleUtility.Repaint ();
			}
		}


		void AdjustMountPointIconsScale() {
			// Adjust city icons in scene view
			if (_map==null || _map.mountPoints==null) return;
			
			Transform t = _map.transform.FindChild ("Mount Points");
			if (t!=null) {
				Camera cam = GetSceneViewCamera();
				float f = cam!=null ? ((cam.transform.position - _map.transform.position)/_map.transform.localScale.x).sqrMagnitude: 1.0f;
				MountPointScaler scaler = t.GetComponent<MountPointScaler>();
				scaler.ScaleMountPoints(0.005f*f);
			} else {
				// This should not happen but maybe the user deleted the layer. Forces refresh.
				_map.DrawMountPoints();
			}
		}

		void ExecuteMountPointCreateTool (Vector2 mousePosition, bool clicked) {
			
			// Show the mouse cursor
			if (Camera.current == null)
				return;
			
			// Show the points
			Transform mapTransform = _map.transform;
			
			Ray ray = HandleUtility.GUIPointToWorldRay (mousePosition);
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, _map.layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == _map.gameObject) {
						Vector3 cursorPos = hits [k].point;
						_editor.cursor = mapTransform.InverseTransformPoint (cursorPos);
						
						Handles.color = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
						float handleSize = HandleUtility.GetHandleSize (cursorPos) * HANDLE_SIZE * 4.0f;
						Handles.SphereCap (0, cursorPos, frontFaceQuaternion, handleSize);
						Handles.color = Color.white;
						
						if (clicked) {
							if (_editor.countryIndex<0 || _editor.countryIndex>=_map.countries.Length) {
								EditorUtility.DisplayDialog("Add new city", "Please choose a country first.", "Ok");
								return;
							}
							UndoPushMountPointStartOperation  ("Undo Create Mount Point");
							_editor.MountPointCreate (_editor.cursor);
							UndoPushMountPointEndOperation();
						}
					}
					HandleUtility.Repaint ();
				}
			}
		}

		void ShowMountPointSelected() {
			if (_editor.mountPointIndex<0 || _editor.mountPointIndex>=_map.mountPoints.Count) return;
			Vector3 mountPointPos = _map.mountPoints[_editor.mountPointIndex].unitySphereLocation;
			Vector3 worldPos = _map.transform.TransformPoint(mountPointPos);
			float handleSize = HandleUtility.GetHandleSize (worldPos) * HANDLE_SIZE * 2.0f;
			Handles.RectangleCap(0, worldPos, frontFaceQuaternion, handleSize);
		}

		
		void ExecuteMountPointMoveTool () {
			if (_map.mountPoints==null || _editor.mountPointIndex<0 || _editor.mountPointIndex>=_map.mountPoints.Count) return;
			
			Transform mapTransform = _map.transform;
			Vector3 mountPointPos = _map.mountPoints[_editor.mountPointIndex].unitySphereLocation;
			Vector3 oldPoint = mapTransform.TransformPoint (mountPointPos);
			float handleSize = HandleUtility.GetHandleSize (oldPoint) * HANDLE_SIZE * 2.0f;
			
			Vector3 newPoint = Handles.FreeMoveHandle (oldPoint, frontFaceQuaternion, handleSize, pointSnap,   
			                                           (handleControlID, position, rotation, size) =>
			                                           {
				Handles.RectangleCap (handleControlID, position, rotation, size);
			});
			if (newPoint!=oldPoint) {
				newPoint = mapTransform.InverseTransformPoint(newPoint);
				if (!startedReshapeMountPoint)
					UndoPushMountPointStartOperation ("Undo Mount Point Move");
				_editor.MountPointMove (newPoint);
				HandleUtility.Repaint ();
			}
		}

		void UndoPushRegionStartOperation (string operationName) {
			startedReshapeRegion = !startedReshapeRegion;
			undoPushStarted = true;
			Undo.RecordObject (target, operationName);	// record changes to the undo dummy flag
			_editor.UndoRegionsPush (_editor.highlightedRegions);

		}

		void UndoPushRegionEndOperation () {
			undoPushStarted = false;
			_editor.UndoRegionsInsertAtCurrentPos (_editor.highlightedRegions);
			if (_editor.reshapeRegionMode != RESHAPE_REGION_TOOL.SMOOTH) { // Smooth operation doesn't need to refresh labels nor frontiers
				_map.RedrawMapLabels ();
				_editor.RedrawFrontiers(null, true); // draw all frontiers again
			}
		}

		void UndoPushCityStartOperation (string operationName) {
			startedReshapeCity = !startedReshapeCity;
			undoPushStarted = true;
			Undo.RecordObject (target, operationName);	// record changes to the undo dummy flag
			_editor.UndoCitiesPush ();
		}
		
		void UndoPushCityEndOperation () {
			undoPushStarted = false;
			_editor.UndoCitiesInsertAtCurrentPos ();
		}

		void UndoPushMountPointStartOperation (string operationName) {
			startedReshapeMountPoint = !startedReshapeMountPoint;
			undoPushStarted = true;
			Undo.RecordObject (target, operationName);	// record changes to the undo dummy flag
			_editor.UndoMountPointsPush ();
		}
		
		void UndoPushMountPointEndOperation () {
			undoPushStarted = false;
			_editor.UndoMountPointsInsertAtCurrentPos ();
		}

		static void CheckBackup(out string geoDataFolder) {
			string[] paths = AssetDatabase.GetAllAssetPaths ();
			bool backupFolderExists = false;
			string rootFolder = "";
			geoDataFolder = "";
			for (int k=0; k<paths.Length; k++) {
				if (paths [k].EndsWith ("Resources/Geodata")) {
					geoDataFolder = paths [k]; 
				} else if (paths [k].EndsWith ("WorldPoliticalMapGlobeEdition")) {
					rootFolder = paths [k];
				} else if (paths [k].EndsWith ("WorldPoliticalMapGlobeEdition/Backup")) {
					backupFolderExists = true;
				}
			}
			
			if (!backupFolderExists) {
				// Do the backup
				AssetDatabase.CreateFolder (rootFolder, "Backup");
				string backupFolder = rootFolder + "/Backup";
				AssetDatabase.CopyAsset (geoDataFolder + "/countries110.txt", backupFolder + "/countries110.txt");
				AssetDatabase.CopyAsset (geoDataFolder + "/countries10.txt", backupFolder + "/countries10.txt");
				AssetDatabase.CopyAsset (geoDataFolder + "/provinces10.txt", backupFolder + "/provinces10.txt");
				AssetDatabase.CopyAsset (geoDataFolder + "/cities10.txt", backupFolder + "/cities10.txt");	
				AssetDatabase.CopyAsset (geoDataFolder + "/mountPoints.txt", backupFolder + "/mountPoints.txt");	
			}
		}


		string GetAssetsFolder() {
			string fullPathName = Application.dataPath;
			int pos = fullPathName.LastIndexOf("/Assets");
			if (pos>0) fullPathName = fullPathName.Substring(0,pos+1);
			return fullPathName;
		}

		bool SaveChanges () {

			if (!_editor.countryChanges && !_editor.provinceChanges && !_editor.cityChanges && !_editor.mountPointChanges) 
				return false;

			// First we make a backup if it doesn't exist
			string geoDataFolder;
			CheckBackup(out geoDataFolder);

			string dataFileName, fullPathName;
			// Save changes to countries
			if (_editor.countryChanges) {
				dataFileName = _editor.GetCountryGeoDataFileName ();
				fullPathName =  GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				string data = _editor.GetCountryGeoData ();
				File.WriteAllText (fullPathName, data);
				_editor.countryChanges = false;
			}
			// Save changes to provinces
			if (_editor.provinceChanges) {
				dataFileName = _editor.GetProvinceGeoDataFileName ();
				fullPathName = GetAssetsFolder();
				string fullAssetPathName = fullPathName + geoDataFolder + "/" + dataFileName;
				string data = _editor.GetProvinceGeoData ();
				File.WriteAllText (fullAssetPathName, data);
				_editor.provinceChanges = false;
			}
			// Save changes to cities
			if (_editor.cityChanges) {
				dataFileName = _editor.GetCityGeoDataFileName ();
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				File.WriteAllText (fullPathName, _editor.GetCityGeoData ());
				_editor.cityChanges = false;
			}
			// Save changes to mount points
			if (_editor.mountPointChanges) {
				dataFileName = _editor.GetMountPointGeoDataFileName ();
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				File.WriteAllText (fullPathName, _editor.GetMountPointsGeoData ());
				_editor.mountPointChanges = false;
			}
			AssetDatabase.Refresh ();
			return true;
		}

		float SignedAngleBetween (Vector3 a, Vector3 b, Vector3 n) {
			// angle in [0,180]
			float angle = Vector3.Angle (a, b);
			float sign = Mathf.Sign (Vector3.Dot (n, Vector3.Cross (a, b)));
			
			// angle in [-179,180]
			float signed_angle = angle * sign;
			
			return signed_angle;
		}

		void FocusSpherePoint(Vector3 point) {
			if (SceneView.lastActiveSceneView==null) return;
			Camera cam = SceneView.lastActiveSceneView.camera;
			if (cam==null) return;

			Vector3 v1 = point;
			Vector3 v2 = cam.transform.position - _map.transform.position;
			float angle = Vector3.Angle (v1, v2);
			Vector3 axis = Vector3.Cross (v1, v2);
			_map.transform.localRotation = Quaternion.AngleAxis (angle, axis); 
			// straighten view
			float angle2 = SignedAngleBetween (Vector3.up, _map.transform.up, v2);
			_map.transform.Rotate (v2, -angle2, Space.World);
		}


		#endregion

		#region Editor UI handling

		void CheckHideEditorMesh () {
			if (!_editor.shouldHideEditorMesh) return;
			_editor.shouldHideEditorMesh = false;
			Transform s = _map.transform;
			Renderer[] rr = s.GetComponentsInChildren<Renderer> (true);
			for (int k=0; k<rr.Length; k++) {
				EditorUtility.SetSelectedWireframeHidden (rr [k], true);
			}
		}

		void ShowShapePoints (bool highlightInsideCircle) {
			if (_map.countries==null) return;
			if (_editor.entityIndex >= 0 && _editor.entities!=null && _editor.entityIndex < _editor.entities.Length && _editor.regionIndex>=0) {
				if (_editor.entities [_editor.entityIndex].regions==null) return;
				Region region = _editor.entities [_editor.entityIndex].regions [_editor.regionIndex];
				Transform mapTransform = _map.transform;
				float circleSizeSqr = _editor.reshapeCircleWidth * _editor.reshapeCircleWidth;
				for (int i = 0; i < region.points.Length; i++) {
					Vector3 rp = region.points [i];
					Vector3 p = mapTransform.TransformPoint (rp);
					float handleSize = HandleUtility.GetHandleSize (p) * HANDLE_SIZE;
					if (highlightInsideCircle) {
						float dist = (rp-_editor.cursor).sqrMagnitude; // (rp.x - _editor.cursor.x) * (rp.x - _editor.cursor.x) * 4.0f + (rp.y - _editor.cursor.y) * (rp.y - _editor.cursor.y);
						if (dist < circleSizeSqr) {
							Handles.color = Color.green;
							Handles.DotCap (0, p, frontFaceQuaternion, handleSize);
							continue;
						} else {
							Handles.color = Color.white;
						}
					}
					Handles.RectangleCap (0, p, frontFaceQuaternion, handleSize);
				}
			}
			Handles.color = Color.white;
		}

		/// <summary>
		/// Shows a potential new point near from cursor location (point parameter, which is in local coordinates)
		/// </summary>
		void ShowCandidatePoint () {
			if (_editor.entityIndex<0 || _editor.regionIndex<0 || _editor.entities[_editor.entityIndex].regions==null) return;
			Region region = _editor.entities [_editor.entityIndex].regions [_editor.regionIndex];
			int max = region.points.Length;
			float minDist = float.MaxValue;
			int nearest = -1, previous = 0;
			for (int p=0; p<max; p++) {
				int q = p == 0 ? max - 1 : p - 1;
				Vector3 rp = (region.points [p] + region.points [q]) * 0.5f;
				float dist = (rp-_editor.cursor).sqrMagnitude; // (rp.x - _editor.cursor.x) * (rp.x - _editor.cursor.x) * 4 + (rp.y - _editor.cursor.y) * (rp.y - _editor.cursor.y);
				if (dist < minDist) {
					// Get nearest point
					minDist = dist;
					nearest = p;
					previous = q;
				}
			}
			
			if (nearest >= 0) {
				Transform mapTransform = _map.transform;
				Vector3 pointToInsert = (region.points [nearest] + region.points [previous]) * 0.5f;
				Handles.color = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				Vector3 pt = mapTransform.TransformPoint (pointToInsert);
				float handleSize = HandleUtility.GetHandleSize (pt) * HANDLE_SIZE;
				Handles.DotCap (0, pt, frontFaceQuaternion, handleSize);
				Handles.color = Color.white;
			}
		}


		void NewShapeInit() {
			if (_editor.newShape == null) _editor.newShape = new List<Vector3>(); else _editor.newShape.Clear();
		}


		/// <summary>
		/// Returns any city near the point specified in local coordinates.
		/// </summary>
		int NewShapeGetIndexNearPoint(Vector3 localPoint) {
			for (int c=0;c<_editor.newShape.Count;c++) {
				Vector3 cityLoc = _editor.newShape[c];
				if ( (cityLoc - localPoint).magnitude<HIT_PRECISION) return c;
			}
			return -1;
		}

		/// <summary>
		/// Shows a potential point to be added to the new shape and draws current shape polygon
		/// </summary>
		void ExecuteShapeCreateTool (Vector3 mousePosition, bool mouseDown) {
			// Show the mouse cursor
			if (Camera.current == null)
				return;
			
			// Show the points
			Transform mapTransform = _map.transform;

			int numPoints = _editor.newShape.Count+1;
			Vector3[] shapePoints = new Vector3[numPoints];
			for (int k=0;k<numPoints-1;k++) {
				shapePoints[k] = mapTransform.TransformPoint(_editor.newShape[k]);
			}
			shapePoints[numPoints-1] = mapTransform.TransformPoint(_editor.cursor);

			// Draw shape polygon in same color as corresponding frontiers
			if (numPoints>=2) {
				if (_editor.createMode == CREATE_TOOL.COUNTRY || _editor.createMode == CREATE_TOOL.COUNTRY_REGION) {
					Handles.color = _map.frontiersColor;
				} else {
					Handles.color = _map.provincesColor;
				}
				Handles.DrawPolyLine(shapePoints);
				Handles.color = Color.white;
			}

			// Draw handles
			for (int i = 0; i < shapePoints.Length-1; i++) {
				float handleSize = HandleUtility.GetHandleSize (shapePoints[i]) * HANDLE_SIZE;
				Handles.RectangleCap (0, shapePoints[i], frontFaceQuaternion, handleSize);
			}

			// Draw cursor
			Ray ray = HandleUtility.GUIPointToWorldRay (mousePosition);
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, _map.layerMask);
			bool closingPolygon = false;
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == _map.gameObject) {
						Vector3 cursorPos = hits [k].point;
						Vector3 newPos = mapTransform.InverseTransformPoint (cursorPos);
						_editor.cursor = newPos;
						if (numPoints>3) { // Check if we're over the first point
							int i = NewShapeGetIndexNearPoint(newPos);
							if (i==0) {
								if (numPoints>5) {
									closingPolygon = true;
									Handles.Label(cursorPos + Vector3.up * 0.17f, "Click to close polygon");
								} else {
									Handles.Label(cursorPos + Vector3.up * 0.17f, "Add " + (6- numPoints) + " more point(s)");
								}
							}
						}
						Handles.color = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
						Vector3 pt = mapTransform.TransformPoint (_editor.cursor);
						float handleSize = HandleUtility.GetHandleSize (pt) * HANDLE_SIZE;
						Handles.DotCap (0, pt, frontFaceQuaternion, handleSize);
						Handles.color = Color.white;
						
						if (mouseDown) {
							if (closingPolygon) {
								switch(_editor.createMode) {
								case CREATE_TOOL.COUNTRY:
									_editor.CountryCreate();
									break;
								case CREATE_TOOL.COUNTRY_REGION:
									_editor.CountryRegionCreate();
									_editor.CountryRegionSelect();
									break;
								case CREATE_TOOL.PROVINCE:
									_editor.CountryRegionCreate();
									_editor.ProvinceCreate();
									break;
								case CREATE_TOOL.PROVINCE_REGION:
									_editor.CountryRegionCreate();
									_editor.ProvinceRegionCreate();
									break;
								}
								NewShapeInit();
							} else {
								_editor.newShape.Add (_editor.cursor);
							}
						}
					}
					HandleUtility.Repaint ();
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

		GUIStyle warningLabelStyle;
		void DrawWarningLabel (string s) {
			if (warningLabelStyle == null)  {
				warningLabelStyle = new GUIStyle (GUI.skin.label);
				warningLabelStyle.normal.textColor = new Color (0.52f, 0.66f, 0.9f);
			}
			GUILayout.Label (s, warningLabelStyle);
		}

		void DrawEditorProvinceNames() {
			if (_editor.highlightedRegions==null) return;
			Transform mapTransform = _map.transform;
			for (int p=0;p<_editor.highlightedRegions.Count;p++) {
				Region region = _editor.highlightedRegions[p];
				if (region.regionIndex == region.entity.mainRegionIndex) {
					Vector3 regionCenter = mapTransform.TransformPoint(region.center);
					Handles.Label(regionCenter, region.entity.name, labelsStyle);
				}
			}
		}

		void CheckScale() {
			if (EditorPrefs.HasKey(EDITORPREF_SCALE_WARNED)) return;
			EditorPrefs.SetBool(EDITORPREF_SCALE_WARNED, true);
			if (_editor.editingCountryFile == EDITING_COUNTRY_FILE.COUNTRY_HIGHDEF && _map.transform.localScale.x<1000) {
				EditorUtility.DisplayDialog("Tip", "You're now in editor mode. Please note that all editing occurs on the Scene View window while the editor component is active, NOT in the Game View. This means that some objects (like cities) will appear with incorrect sizes in the Game View. They will recover their correct sizes for the Game View window when you close the editor component.\n\nIt's important to increase the scale of the globe gameobject to something like (X=1000,Y=1000,Z=1000) or click on 'Toggle Zoom' button to change it automatically so navigating and making precise selections on the map is easier.\n\nWhen you finish editing the map, remember to set its scale back to the original values (default scale is X=1, Y=1, Z=1) or alternatively click on 'Toggle Zoom' again.", "Ok");
			}
		}

			#endregion

	}

}