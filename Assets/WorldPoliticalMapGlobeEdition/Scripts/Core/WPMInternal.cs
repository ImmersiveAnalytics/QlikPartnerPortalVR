// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define VR_EYE_RAY_CAST_SUPPORT  // Uncomment this line to support VREyeRayCast script - note that you must have already imported this script from Unity VR Samples

//#define TRACE_CTL				   // Used by us to debug/trace some events

using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Poly2Tri;
#if VR_EYE_RAY_CAST_SUPPORT
using VRStandardAssets.Utils;
#endif

namespace WPM {

	[Serializable]
	[ExecuteInEditMode]
	public partial class WorldMapGlobe : MonoBehaviour {

		public const float MAP_PRECISION = 5000000f;
		public const string WPM_OVERLAY_NAME = "WPMOverlay";

		const float MIN_FIELD_OF_VIEW = 10.0f;
		const float MAX_FIELD_OF_VIEW = 85.0f;
		const float MIN_ZOOM_DISTANCE = 0.58f;
		const float EARTH_RADIUS_KM = 6371f;
		const int SMOOTH_STRAIGHTEN_ON_POLES = -1;
		const string SPHERE_OVERLAY_LAYER_NAME = "SphereOverlayLayer";

		enum OVERLAP_CLASS {
			OUTSIDE = -1,
			PARTLY_OVERLAP = 0,
			INSIDE = 1
		}

		[NonSerialized]
		public bool
			isDirty; // internal variable used to confirm changes in custom inspector - don't change its value


		#region Internal variables

		// resources
		Material coloredMat, texturizedMat;
		Material outlineMat, cursorMat, gridMatOverlay, gridMatMasked;
		Material markerMat;

		// gameObjects
		GameObject _surfacesLayer;
		GameObject surfacesLayer { get { if (_surfacesLayer==null) CreateSurfacesLayer(); return _surfacesLayer; } }
		GameObject cursorLayer, latitudeLayer, longitudeLayer;
		GameObject markersLayer, overlayMarkersLayer;

		// caché and gameObject lifetime control
		Dictionary<int, GameObject>surfaces;
		int countryProvincesDrawnIndex;

		Dictionary<Color, Material>coloredMatCache;
		Dictionary<Color, Material>markerMatCache;

		// FlyTo functionality
		Quaternion flyToStartQuaternion, flyToEndQuaternion;
		bool flyToActive;
		float flyToStartTime, flyToDuration;
		Vector3 flyToCameraStartPosition, flyToCameraEndPosition;
		Vector3 flyToGlobeStartPosition, flyToEndDestination;
		NAVIGATION_MODE flyToMode;

		// UI interaction variables
		int mapUnityLayer;
		Vector3 mouseDragStart, dragDirection, mouseDragStartCursorLocation;
		bool mouseStartedDragging;
		int dragDamping;
		float wheelAccel;
		Vector3 lastGlobeScaleCheck;
		bool mouseIsOverUIElement;
		int simulatedMouseButtonClick, simulatedMouseButtonPressed;
		bool leftMouseButtonClick, rightMouseButtonClick, leftMouseButtonRelease, rightMouseButtonRelease;
		bool leftMouseButtonPressed, rightMouseButtonPressed;
		float lastCameraDistanceSqr;
		Vector3 lastCameraRotationDiff;

		// Overlay (Labels, tickers, ...)
		public const int overlayWidth = 200;	 // don't change these values or 
		public const int overlayHeight = 100;	 // overlay wont' work
		RenderTexture overlayRT;
		GameObject overlayLayer, sphereOverlayLayer;
		Font labelsFont;
		Material labelsShadowMaterial;
		bool isScenic;

#if VR_EYE_RAY_CAST_SUPPORT
								VREyeRaycaster _VREyeRayCaster;
								VREyeRaycaster VRCameraEyeRayCaster {
								get {
				if (_VREyeRayCaster==null) {
				_VREyeRayCaster = transform.GetComponent<VREyeRaycaster>();
			}
			return _VREyeRayCaster;
		}
		}
#endif

		#endregion



	#region System initialization

		public void Init () {
			// Load materials
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": init");
			#endif

			// Setup references & layers

			mapUnityLayer = gameObject.layer;
			// Updates layer in children

			foreach (Transform t in transform) {
				t.gameObject.layer = mapUnityLayer;
			}
			Transform tsky = transform.FindChild ("WorldMapGlobeAtmosphere");
			if (tsky!=null) skyRenderer = tsky.GetComponent<MeshRenderer>() ?? tsky.gameObject.AddComponent<MeshRenderer>();

			Transform tearth = transform.FindChild ("WorldMapGlobeEarth");
			if (tearth!=null) earthRenderer = tearth.GetComponent<MeshRenderer>() ?? tearth.gameObject.AddComponent<MeshRenderer>();

			// Labels materials
			labelsFont = GameObject.Instantiate (Resources.Load <Font> ("Font/Lato"));
			labelsFont.hideFlags = HideFlags.DontSave;
			Material fontMaterial = Instantiate (labelsFont.material);
			fontMaterial.hideFlags = HideFlags.DontSave;
			labelsFont.material = fontMaterial;
			labelsShadowMaterial = GameObject.Instantiate (fontMaterial);
			labelsShadowMaterial.hideFlags = HideFlags.DontSave;
			labelsShadowMaterial.renderQueue--;

			// Map materials
			frontiersMat = Instantiate (Resources.Load <Material> ("Materials/Frontiers"));
			frontiersMat.hideFlags = HideFlags.DontSave;
			inlandFrontiersMat = Instantiate (Resources.Load <Material> ("Materials/InlandFrontiers"));
			inlandFrontiersMat.hideFlags = HideFlags.DontSave;
			hudMatCountry = Instantiate (Resources.Load <Material> ("Materials/HudCountry"));
			hudMatCountry.hideFlags = HideFlags.DontSave;
			hudMatProvince = Instantiate (Resources.Load <Material> ("Materials/HudProvince"));
			hudMatProvince.hideFlags = HideFlags.DontSave;
			hudMatProvince.renderQueue++;
			citySpot = Resources.Load <GameObject> ("Prefabs/CitySpot");
			citySpotCapitalRegion = Resources.Load <GameObject> ("Prefabs/CityCapitalRegionSpot");
			citySpotCapitalCountry = Resources.Load <GameObject> ("Prefabs/CityCapitalCountrySpot");
			citiesNormalMat = Instantiate (Resources.Load <Material> ("Materials/Cities"));
			citiesNormalMat.name = "Cities";
			citiesNormalMat.hideFlags = HideFlags.DontSave;
			citiesRegionCapitalMat = Instantiate (Resources.Load <Material> ("Materials/CitiesCapitalRegion"));
			citiesRegionCapitalMat.name = "CitiesCapitalRegion";
			citiesRegionCapitalMat.hideFlags = HideFlags.DontSave;
			citiesCountryCapitalMat = Instantiate (Resources.Load <Material> ("Materials/CitiesCapitalCountry"));
			citiesCountryCapitalMat.name = "CitiesCapitalCountry";
			citiesCountryCapitalMat.hideFlags = HideFlags.DontSave;
			provincesMat = Instantiate (Resources.Load <Material> ("Materials/Provinces"));
			provincesMat.hideFlags = HideFlags.DontSave;
			outlineMat = Instantiate (Resources.Load <Material> ("Materials/Outline"));
			outlineMat.hideFlags = HideFlags.DontSave;
			outlineMat.renderQueue++;
			coloredMat = Instantiate (Resources.Load <Material> ("Materials/ColorizedRegion"));
			coloredMat.hideFlags = HideFlags.DontSave;
			texturizedMat = Instantiate (Resources.Load <Material> ("Materials/TexturizedRegion"));
			texturizedMat.hideFlags = HideFlags.DontSave;
			cursorMat = Instantiate (Resources.Load <Material> ("Materials/Cursor"));
			cursorMat.hideFlags = HideFlags.DontSave;
			gridMatOverlay = Instantiate (Resources.Load <Material> ("Materials/GridOverlay"));
			gridMatOverlay.hideFlags = HideFlags.DontSave;
			gridMatMasked = Instantiate (Resources.Load <Material> ("Materials/GridMasked"));
			gridMatMasked.hideFlags = HideFlags.DontSave;
			markerMat = Instantiate (Resources.Load <Material> ("Materials/Marker"));
			markerMat.hideFlags = HideFlags.DontSave;
			mountPointSpot = Resources.Load <GameObject> ("Prefabs/MountPointSpot");
			mountPointsMat = Instantiate (Resources.Load <Material> ("Materials/Mount Points"));
			mountPointsMat.hideFlags = HideFlags.DontSave;
			earthGlowMat = Instantiate (Resources.Load <Material> ("Materials/EarthGlow"));
			earthGlowMat.hideFlags = HideFlags.DontSave;
			earthGlowScatterMat = Instantiate (Resources.Load <Material> ("Materials/EarthGlow2"));
			earthGlowScatterMat.hideFlags = HideFlags.DontSave;

			coloredMatCache = new Dictionary<Color, Material>();
			markerMatCache = new Dictionary<Color, Material>();

			// Destroy obsolete labels layer -> now replaced with overlay feature
			GameObject o = GameObject.Find ("WPMLabels");
			if (o != null)
				DestroyImmediate (o);
			Transform tlabel = transform.FindChild ("LabelsLayer");
			if (tlabel != null)
				DestroyImmediate (tlabel.gameObject);
			// End destroy obsolete.

			ReloadData ();

			lastGlobeScaleCheck = transform.localScale;

		}

		/// <summary>
		/// Reloads the data of frontiers and cities from datafiles and redraws the map.
		/// </summary>
		public void ReloadData () {
			// read baked data
			ReadCountriesPackedString ();
			if (_showProvinces) ReadProvincesPackedString ();
			ReadCitiesPackedString ();
			ReadMountPointsPackedString();

			// Redraw frontiers and cities -- destroy layers if they already exists
			Redraw ();
		}

		void GetLatLonFromPackedString(string s, out float lat, out float lon) {
			int j = s.IndexOf(",");
			string slat = s.Substring(0, j);
			string slon = s.Substring(j+1);
			lat = float.Parse (slat)/ MAP_PRECISION;
			lon = float.Parse (slon)/ MAP_PRECISION;
		}
	
	#endregion


		#region Game loop events

		
		void OnEnable () {
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": enable wpm");
			#endif
			if (countries == null) {
				Init ();
			}
			
			// Check material
			if (earthRenderer.sharedMaterial == null) {
				RestyleEarth();
			}
			
			if (hudMatCountry != null && hudMatCountry.color != _fillColor) {
				hudMatCountry.color = _fillColor;
			}
			if (frontiersMat != null && frontiersMat.color != _frontiersColor) {
				frontiersMat.color = _frontiersColor;
			}
			if (inlandFrontiersMat != null && inlandFrontiersMat.color != _inlandFrontiersColor) {
				inlandFrontiersMat.color = _inlandFrontiersColor;
			}
			if (hudMatProvince != null && hudMatProvince.color != _provincesFillColor) {
				hudMatProvince.color = _provincesFillColor;
			}
			if (provincesMat != null && provincesMat.color != _provincesColor) {
				provincesMat.color = _provincesColor;
			}
			if (citiesNormalMat.color != _citiesColor) {
				citiesNormalMat.color = _citiesColor;
			}
			if (citiesRegionCapitalMat.color != _citiesRegionCapitalColor) {
				citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
			}
			if (citiesCountryCapitalMat.color != _citiesCountryCapitalColor) {
				citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
			}
			if (outlineMat.color != _outlineColor) {
				outlineMat.color = _outlineColor;
			}
			if (cursorMat.color != _cursorColor) {
				cursorMat.color = _cursorColor;
			}
			if (gridMatOverlay.color != _gridColor) {
				gridMatOverlay.color = _gridColor;
			}
			if (gridMatMasked.color != _gridColor) {
				gridMatMasked.color = _gridColor;
			}
		}
		
		void OnDestroy () {
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": destroy wpm");
			#endif
			DestroyOverlay ();
			DestroySurfacesLayer();
		}

		
		void OnMouseEnter () {
			mouseIsOver = true;
		}
		
		void OnMouseExit () {
			mouseIsOver = false;
			HideCountryRegionHighlight ();
		}
		
		void Reset () {
			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": reset");
			#endif
			Redraw ();
		}

		void Update () {
			if (!Application.isPlaying) {
				// when saving the scene from Editor, the material of the sphere label layer is cleared - here's a fix to recreate it
				if (_showCountryNames && sphereOverlayLayer != null && sphereOverlayLayer.GetComponent<Renderer> () == null) {
					CreateOverlay ();
				}
				return;
			}

			// Check mouse buttons state
			leftMouseButtonClick = Input.GetMouseButtonDown(0) || simulatedMouseButtonClick==1 || Input.GetButtonDown("Fire1");
			leftMouseButtonPressed = leftMouseButtonClick || Input.GetMouseButton(0) || simulatedMouseButtonPressed==1;
			leftMouseButtonRelease = Input.GetMouseButtonUp(0) || simulatedMouseButtonClick==1 || Input.GetButtonUp("Fire1");
			rightMouseButtonClick = Input.GetMouseButtonDown(1) || simulatedMouseButtonClick==2;
			rightMouseButtonPressed = rightMouseButtonClick || Input.GetMouseButton(1) || simulatedMouseButtonPressed==2;
			rightMouseButtonRelease = Input.GetMouseButtonUp(1) || simulatedMouseButtonClick==2;

			// Check if navigateTo... has been called and in this case rotate the globe until the country is centered
			if (flyToActive) {
				RotateToDestination ();
			} else {
				// subtle/slow continuous rotation
				if (autoRotationSpeed != 0) {
					gameObject.transform.Rotate (Vector3.up, -autoRotationSpeed);
				}
			}
			
			CheckCursorVisibility();
			
			// Check whether the points is on an UI element, then cancels
			if (_respectOtherUI) {
				#if VR_EYE_RAY_CAST_SUPPORT
				if (VRCameraEyeRayCaster!=null && VRCameraEyeRayCaster.CurrentInteractible != null) {
					if (!mouseIsOverUIElement) {
						mouseIsOverUIElement = true;
						HideCountryRegionHighlight();
					}
					return;
				}
				#endif
				if (UnityEngine.EventSystems.EventSystem.current!=null) {
				if ( (Input.touchSupported && Input.touchCount>0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) || // mobile
				    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1)) {	// non-mobile
					if (!mouseIsOverUIElement) {
						mouseIsOverUIElement = true;
						HideCountryRegionHighlight ();
					}
					return;
				}
			}
			}
			mouseIsOverUIElement = false;
			
			// Verify if mouse enter a country boundary - we only check if mouse is inside the sphere of world
			if (mouseIsOver) { 
				CheckMousePos ();

				// Remember the last element clicked
				if (leftMouseButtonClick || rightMouseButtonClick) {
					_countryLastClicked = _countryHighlightedIndex;
					_countryRegionLastClicked = _countryRegionHighlightedIndex;
					if (_countryLastClicked>=0 && OnCountryClick!=null) OnCountryClick(_countryHighlightedIndex, _countryRegionHighlightedIndex);
					_provinceLastClicked = _provinceHighlightedIndex;
					_provinceRegionLastClicked = _provinceRegionHighlightedIndex;
					if (_provinceLastClicked>=0 && OnProvinceClick!=null) OnProvinceClick(_provinceLastClicked, _provinceRegionLastClicked);
					_cityLastClicked = _cityHighlightedIndex;
					if (_cityLastClicked>=0 && OnCityClick!=null) OnCityClick(_cityLastClicked);
				} else {
					if (leftMouseButtonRelease && OnLeftClick!=null && mouseDragStart == Input.mousePosition) OnLeftClick(_cursorLocation);
					if (rightMouseButtonRelease && OnRightClick!=null && mouseDragStart == Input.mousePosition) OnRightClick(_cursorLocation);
				}
				

			}
			
			// Handle interaction mode
			Quaternion currentRotation = transform.rotation;
			if (_earthInvertedMode) {
				CheckUserInteractionInvertedMode();
			} else {
				CheckUserInteractionNormalMode();
			}
			if (currentRotation != transform.rotation && !PassConstraintCheck()) {
				transform.rotation = currentRotation;
				dragDamping = 0;
			}

			// Has moved? Fades country labels and updates borders
			if (_countryLabelsEnableAutomaticFade && _showCountryNames) {
				Vector3 cameraRotationDiff = (Camera.main.transform.eulerAngles - transform.eulerAngles);
				float cameraDistanceSqr = (Camera.main.transform.position - transform.position).sqrMagnitude;
				if (cameraRotationDiff!=lastCameraRotationDiff || cameraDistanceSqr != lastCameraDistanceSqr) {
					lastCameraDistanceSqr = cameraDistanceSqr;
					lastCameraRotationDiff = cameraRotationDiff;
					FadeCountryLabels();
				}
			}

			// Reset simulated click
			simulatedMouseButtonClick = 0;
			simulatedMouseButtonPressed = 0;
		}

		void LateUpdate() {
			// Updates atmosphere scatter shader params
			if (_earthGlowScatter) UpdateAtmosphereScatterMaterial();
			if (_earthStyle == EARTH_STYLE.NaturalHighResScenicScatter) UpdateEarthScatterMaterial();
		}
		#endregion



	#region Drawing stuff

		/// <summary>
		/// Used internally and by other components to redraw the layers in specific moments. You shouldn't call this method directly.
		/// </summary>
		public void Redraw () {
			if (!gameObject.activeInHierarchy)
				return;

			if (countries==null) Init ();

			#if TRACE_CTL
			Debug.Log ("CTL " + DateTime.Now + ": Redraw");
#endif

			if (provincesLayer!=null) DestroyImmediate(provincesLayer);

			DestroyMapLabels ();

			// Initialize surface cache
			if (surfaces != null) {
				List<GameObject> cached = new List<GameObject> (surfaces.Values);
				for (int k=0; k<cached.Count; k++)
					if (cached [k] != null)
						DestroyImmediate (cached [k]);
			}
			surfaces = new Dictionary<int, GameObject> ();
			_surfacesCount = 0;
			DestroySurfacesLayer();

			HideProvinces();

			RestyleEarth ();	// Apply texture to Earth

			DrawFrontiers ();	// Redraw country frontiers

			DrawInlandFrontiers(); // Redraw inland frontiers

			DrawCities (); 		// Redraw cities layer

			DrawMountPoints();	// Redraw mount points (only in Editor time)

			DrawCursor (); 		// Draw cursor lines

			DrawGrid ();    	// Draw longitude & latitude lines

			DrawAtmosphere();

			if (_showCountryNames) {
				DrawMapLabels ();
				FadeCountryLabels();
			}

		}

		void CreateSurfacesLayer() {
			Transform t = transform.FindChild ("Surfaces");
			if (t != null) {
				DestroyImmediate (t.gameObject);
				for (int k=0;k<countries.Length;k++) 
					for (int r=0;r<countries[k].regions.Count;r++)
						countries[k].regions[r].customMaterial = null;
			}
			_surfacesLayer = new GameObject ("Surfaces");
			_surfacesLayer.transform.SetParent (transform, false);
			_surfacesLayer.transform.localScale = _earthInvertedMode ? MiscVector.Vector3one * 0.995f: MiscVector.Vector3one;
		}

		void DestroySurfacesLayer() {
			if (_surfacesLayer!=null) GameObject.DestroyImmediate(_surfacesLayer);
		}

		Material GetColoredTexturedMaterial(Color color, Texture2D texture) {
			if (texture==null && coloredMatCache.ContainsKey(color)) {
				return coloredMatCache[color];
			} else {
				Material customMat;
				if (texture!=null) {
					customMat = Instantiate(texturizedMat);
					customMat.name = texturizedMat.name;
					customMat.mainTexture = texture;
				} else {
					customMat = Instantiate (coloredMat);
					customMat.name = coloredMat.name;
					coloredMatCache[color] = customMat;
				}
				customMat.color = color;
				customMat.hideFlags = HideFlags.DontSave;
				return customMat;
			}
		}
		
		
		Material GetColoredMarkerMaterial(Color color) {
			if (markerMatCache.ContainsKey(color)) {
				return markerMatCache[color];
			} else {
				Material customMat;
				customMat = Instantiate (markerMat);
				customMat.name = markerMat.name;
				markerMatCache[color] = customMat;
				customMat.color = color;
				customMat.hideFlags = HideFlags.DontSave;
				return customMat;
			}
		}

		void ApplyMaterialToSurface(GameObject obj, Material sharedMaterial) {
			if (obj!=null) {
				Renderer r = obj.GetComponent<Renderer>();
				if (r!=null) 
					r.sharedMaterial = sharedMaterial;
			}
		}


	#endregion

		#region Internal functions

		/// <summary>
		/// Used internally to rotate the globe or the camera during FlyTo operations. Use FlyTo method.
		/// </summary>
		void RotateToDestination () {
			float delta, t;

			if (transform.position!=flyToGlobeStartPosition) {
				flyToGlobeStartPosition = transform.position;
				flyToEndQuaternion = GetQuaternion (flyToEndDestination.normalized);
			}

			if (flyToDuration == 0) {
				delta = flyToDuration;
				t = 1;
			} else {
				delta = (Time.time - flyToStartTime);
				t = Mathf.SmoothStep (0, 1, delta / flyToDuration);
			}
			if (flyToMode == NAVIGATION_MODE.EARTH_ROTATES || _earthInvertedMode) {
				transform.rotation = Quaternion.Lerp (flyToStartQuaternion, flyToEndQuaternion, t);
			} else {
				float camDistance = (Camera.main.transform.position - transform.position).magnitude;
				Camera.main.transform.position = Vector3.Lerp (flyToCameraStartPosition, flyToCameraEndPosition, t).normalized * camDistance;
				Quaternion cameraLookRotation = GetCameraStraightLookRotation();
				if (t<0.5f) { // smooth change camera rotation towards center of planet
					Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, cameraLookRotation, 0.5f);
				} else {
					Camera.main.transform.rotation = cameraLookRotation;
				}
			}
			if (delta >= flyToDuration) {
				flyToActive = false;
			}
		}

		Quaternion GetCameraStraightLookRotation() {

			Vector3 camVec = transform.position - Camera.main.transform.position;
			if (Mathf.Abs(Vector3.Dot (transform.up, camVec.normalized))>0.96f) {	// avoid going crazy around poles
				return Camera.main.transform.rotation;
			}

			Quaternion old = Camera.main.transform.rotation;
			Camera.main.transform.LookAt(transform.position);
			Vector3 camUp = Vector3.ProjectOnPlane(transform.up, camVec);
			float angle = SignedAngleBetween (Camera.main.transform.up, camUp, camVec);
			Camera.main.transform.Rotate(camVec, angle, Space.World);
			Quaternion q = Camera.main.transform.rotation;
			Camera.main.transform.rotation = old;
			return q;
		}

		void CheckUserInteractionNormalMode ()
		{
			// if mouse/finger is over map, implement drag and rotation of the world
			if (mouseIsOver) {
				// Use left mouse button and drag to rotate the world
				if (_allowUserRotation) {
					if (leftMouseButtonClick) {
						mouseDragStart = Input.mousePosition;
						mouseDragStartCursorLocation = _cursorLocation;
						mouseStartedDragging = true;
					} else if (mouseStartedDragging && leftMouseButtonPressed && (!Input.touchSupported || Input.touchCount == 1)) {
						if (_dragConstantSpeed) {
							float angle = Vector3.Angle (mouseDragStartCursorLocation, _cursorLocation);
							if (_navigationMode == NAVIGATION_MODE.EARTH_ROTATES) {
								Vector3 axis = Vector3.Cross (mouseDragStartCursorLocation, _cursorLocation);
								Vector3 angles = Quaternion.AngleAxis (angle, axis).eulerAngles;
								transform.Rotate(angles); 
							} else {
								Vector3 axis = Vector3.Cross (transform.TransformVector(mouseDragStartCursorLocation), transform.TransformVector(_cursorLocation));
								RotateAround(Camera.main.transform, transform.position, axis, -angle);
							}
						} else {
							float distFactor = Vector3.Distance (Camera.main.transform.position, transform.position) / transform.localScale.y;
							dragDirection = (mouseDragStart - Input.mousePosition) * 0.01f * distFactor * _mouseDragSensitivity;
							if (_navigationMode == NAVIGATION_MODE.EARTH_ROTATES) {
								gameObject.transform.Rotate (Vector3.up, dragDirection.x, Space.World);
								Vector3 axisY = Vector3.Cross (transform.position - Camera.main.transform.position, Vector3.up);
								transform.Rotate (axisY, dragDirection.y, Space.World);
							} else {
								RotateAround(Camera.main.transform, transform.position, Camera.main.transform.up, -dragDirection.x);
								RotateAround(Camera.main.transform, transform.position, Camera.main.transform.right, dragDirection.y);
							}
							dragDamping = 1;
						}
						flyToActive = false;
					} else {
						if (mouseStartedDragging) {
							mouseStartedDragging = false;
						}
					}
					
					// Use right mouse button and drag to spin the world around z-axis
					if (rightMouseButtonPressed && !Input.touchSupported && !flyToActive) {
						if (_showProvinces && _provinceHighlightedIndex>=0 && _centerOnRightClick && rightMouseButtonClick ) {
							FlyToProvince (_provinceHighlightedIndex, 0.8f);
						} else if (_countryHighlightedIndex >= 0 && _centerOnRightClick && rightMouseButtonClick ) {
							FlyToCountry (_countryHighlightedIndex, 0.8f);
						} else {
							Vector3 axis = (transform.position - Camera.main.transform.position).normalized;
							if (_navigationMode == NAVIGATION_MODE.EARTH_ROTATES) {
								transform.Rotate (axis, 2, Space.World);
							} else {
								Camera.main.transform.Rotate(axis, 2, Space.World);
							}
						}
					}
				}
			}
			
			// Check special keys
			if (_allowUserKeys && _allowUserRotation) {
				bool pressed = false;
				dragDirection = MiscVector.Vector3zero;
				if (Input.GetKey (KeyCode.W)) {
					dragDirection += Vector3.down;
					pressed = true;
				} 
				if (Input.GetKey (KeyCode.S)) {
					dragDirection += Vector3.up;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.A)) {
					dragDirection += Vector3.right;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.D)) {
					dragDirection += Vector3.left;
					pressed = true;
				}
				if (pressed) {
					dragDirection *= Vector3.Distance (Camera.main.transform.position, transform.position) * _mouseDragSensitivity;
					gameObject.transform.Rotate (Vector3.up, dragDirection.x, Space.World);
					Vector3 axisY = Vector3.Cross (transform.position - Camera.main.transform.position, Vector3.up);
					gameObject.transform.Rotate (axisY, dragDirection.y, Space.World);
					dragDamping = 1;
				}
			}
			
			if (dragDamping > 0) {
				if (++dragDamping < 20) {
					if (_navigationMode == NAVIGATION_MODE.EARTH_ROTATES) {
						gameObject.transform.Rotate (Vector3.up, dragDirection.x / dragDamping, Space.World);
						Vector3 axisY = Vector3.Cross (gameObject.transform.position - Camera.main.transform.position, Vector3.up);
						gameObject.transform.Rotate (axisY, dragDirection.y / dragDamping, Space.World);
					} else {
						RotateAround(Camera.main.transform, transform.position, Camera.main.transform.up, -dragDirection.x / dragDamping);
						RotateAround(Camera.main.transform, transform.position, Camera.main.transform.right, dragDirection.y / dragDamping);
					}
				} else {
					dragDamping = 0;
				}
			}
			
			// Use mouse wheel to zoom in and out
			if (_allowUserZoom) {
				if (mouseIsOver || wheelAccel != 0) {
					float wheel = Input.GetAxis ("Mouse ScrollWheel");
					wheelAccel += wheel * (_invertZoomDirection ? -1: 1);
				}
				
				// Support for pinch on mobile
				if (Input.touchSupported && Input.touchCount == 2) {
					// Store both touches.
					Touch touchZero = Input.GetTouch (0);
					Touch touchOne = Input.GetTouch (1);
					
					// Find the position in the previous frame of each touch.
					Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
					Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
					
					// Find the magnitude of the vector (the distance) between the touches in each frame.
					float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
					float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
					
					// Find the difference in the distances between each frame.
					float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
					
					// Pass the delta to the wheel accel
					wheelAccel += deltaMagnitudeDiff;
				}
			}
			
			if (wheelAccel!=0) {
				wheelAccel = Mathf.Clamp (wheelAccel, -0.1f, 0.1f);
				if (wheelAccel >= 0.01f || wheelAccel <= -0.01f) {
					Vector3 camPos = Camera.main.transform.position - (transform.position - Camera.main.transform.position) * wheelAccel * _mouseWheelSensitivity;
					Camera.main.transform.position = camPos;
					float radiusSqr = _zoomMinDistance + transform.localScale.z * 0.5f + Camera.main.nearClipPlane; // _zoomMinDistance>0 ? _zoomMinDistance: transform.localScale.z * (MIN_ZOOM_DISTANCE + Camera.main.nearClipPlane);
					radiusSqr *= radiusSqr;
					float camDistSqr = (Camera.main.transform.position - transform.position).sqrMagnitude;
					if (camDistSqr < radiusSqr) {
						Camera.main.transform.position = transform.position + (Camera.main.transform.position - transform.position).normalized * Mathf.Sqrt (radiusSqr + 0.01f);
						wheelAccel = 0;
					} else {
						radiusSqr = _zoomMaxDistance + transform.localScale.z * 0.5f + Camera.main.nearClipPlane;
						radiusSqr *= radiusSqr;
						if (camDistSqr > radiusSqr) {
							Camera.main.transform.position = transform.position + (Camera.main.transform.position - transform.position).normalized * Mathf.Sqrt (radiusSqr - 0.01f);
							wheelAccel = 0;
						}
					}
					if (_dragConstantSpeed) {
						wheelAccel = 0;
					} else {
						wheelAccel /= 1.15f; // smooth dampening
					}
				} else {
					wheelAccel = 0;
				}
			}

			if (_keepStraight && !flyToActive) {
				if (_navigationMode == NAVIGATION_MODE.EARTH_ROTATES) {
					StraightenGlobe(SMOOTH_STRAIGHTEN_ON_POLES, true);
				} else {
					Camera.main.transform.rotation = Quaternion.Lerp (Camera.main.transform.rotation, GetCameraStraightLookRotation(), 0.3f);
				}
			}
			
		}
		
		
		void CheckUserInteractionInvertedMode ()
		{
			// if mouse/finger is over map, implement drag and rotation of the world
			if (mouseIsOver) {
				// Use left mouse button and drag to rotate the world
				if (_allowUserRotation) {
					if (leftMouseButtonClick) {
						mouseDragStart = Input.mousePosition;
						mouseDragStartCursorLocation = _cursorLocation;
						mouseStartedDragging = true;
					} else if (mouseStartedDragging && leftMouseButtonPressed && (!Input.touchSupported || Input.touchCount == 1)) {
						if (_dragConstantSpeed) {
							float angle = Vector3.Angle (mouseDragStartCursorLocation, _cursorLocation);
							Vector3 axis = Vector3.Cross (mouseDragStartCursorLocation, _cursorLocation);
							Vector3 angles = Quaternion.AngleAxis (-angle, axis).eulerAngles;
							angles.x *= -1;
							transform.Rotate(angles); 
						} else {
							Vector3 referencePos = transform.position + Camera.main.transform.forward * lastGlobeScaleCheck.z * 0.5f;
							float distFactor = Vector3.Distance (Camera.main.transform.position, referencePos);
							dragDirection = (Input.mousePosition - mouseDragStart) * 0.015f * distFactor * _mouseDragSensitivity;
							transform.Rotate (Vector3.up, dragDirection.x, Space.World);
							Vector3 axisY = Vector3.Cross (referencePos - Camera.main.transform.position, Vector3.up);
							transform.Rotate (axisY, dragDirection.y, Space.World);
							dragDamping = 1;
						}
						flyToActive = false;
					} else {
						if (mouseStartedDragging) {
							mouseStartedDragging = false;
						}
					}

					// Use right mouse button and drag to spin the world around z-axis
					if (rightMouseButtonPressed && !Input.touchSupported && !flyToActive) {
						if (_showProvinces && _provinceHighlightedIndex>=0 && _centerOnRightClick && rightMouseButtonClick ) {
							FlyToProvince (_provinceHighlightedIndex, 0.8f);
						} else if (_countryHighlightedIndex >= 0 && rightMouseButtonClick && _centerOnRightClick) {
							FlyToCountry (_countryHighlightedIndex, 0.8f);
						} else {
							Vector3 axis = (transform.position - Camera.main.transform.position).normalized;
							transform.Rotate (axis, 2, Space.World);
						}
					}
				}
			}
			
			// Check special keys
			if (_allowUserKeys && _allowUserRotation) {
				bool pressed = false;
				dragDirection = MiscVector.Vector3zero;
				if (Input.GetKey (KeyCode.W)) {
					dragDirection += Vector3.down;
					pressed = true;
				} 
				if (Input.GetKey (KeyCode.S)) {
					dragDirection += Vector3.up;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.A)) {
					dragDirection += Vector3.right;
					pressed = true;
				}
				if (Input.GetKey (KeyCode.D)) {
					dragDirection += Vector3.left;
					pressed = true;
				}
				if (pressed) {
					Vector3 referencePos = transform.position + Camera.main.transform.forward * lastGlobeScaleCheck.z * 0.5f;
					dragDirection *= Vector3.Distance (Camera.main.transform.position, referencePos) * _mouseDragSensitivity;
					transform.Rotate (Vector3.up, dragDirection.x, Space.World);
					Vector3 axisY = Vector3.Cross (referencePos - Camera.main.transform.position, Vector3.up);
					transform.Rotate (axisY, dragDirection.y, Space.World);
					dragDamping = 1;
				}
			}
			
			if (dragDamping > 0) {
				if (++dragDamping < 20) {
					gameObject.transform.Rotate (Vector3.up, dragDirection.x / dragDamping, Space.World);
					Vector3 axisY = Vector3.Cross (gameObject.transform.position - Camera.main.transform.position, Vector3.up);
					gameObject.transform.Rotate (axisY, dragDirection.y / dragDamping, Space.World);
				} else {
					dragDamping = 0;
				}
			}
			
			// Use mouse wheel to zoom in and out
			if (_allowUserZoom && (mouseIsOver || wheelAccel != 0)) {
				float wheel = Input.GetAxis ("Mouse ScrollWheel");
				wheelAccel += wheel * (_invertZoomDirection ? -1: 1);
			}
			
			// Support for pinch on mobile
			if (Input.touchSupported && Input.touchCount == 2) {
				// Store both touches.
				Touch touchZero = Input.GetTouch (0);
				Touch touchOne = Input.GetTouch (1);
				
				// Find the position in the previous frame of each touch.
				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
				
				// Find the magnitude of the vector (the distance) between the touches in each frame.
				float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
				float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
				
				// Find the difference in the distances between each frame.
				float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
				
				// Pass the delta to the wheel accel
				wheelAccel += deltaMagnitudeDiff;
			}
			
			if (wheelAccel!=0) {
				wheelAccel = Mathf.Clamp (wheelAccel, -0.1f, 0.1f);
				if (wheelAccel >= 0.01f || wheelAccel <= -0.01f) {
					Camera.main.fieldOfView = Mathf.Clamp( Camera.main.fieldOfView  + (90.0f * Camera.main.fieldOfView / MAX_FIELD_OF_VIEW) *  wheelAccel * _mouseWheelSensitivity, MIN_FIELD_OF_VIEW, MAX_FIELD_OF_VIEW);
					if (_dragConstantSpeed) {
						wheelAccel = 0;
					} else {
						wheelAccel /= 1.15f; // smooth dampening
					}
				} else {
					wheelAccel = 0;
				}
			}

			if (keepStraight && !flyToActive) {
				StraightenGlobe(SMOOTH_STRAIGHTEN_ON_POLES, true);
			}
		}

		void UpdateSurfaceCount() {
			if (_surfacesLayer!=null) 
				_surfacesCount = (_surfacesLayer.GetComponentsInChildren<Transform>().Length-1) / 2;
			else
				_surfacesCount = 0;
		}


		bool PassConstraintCheck() {
			if (!constraintPositionEnabled) return true;

			// Gets current center
			Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.rotation * Vector3.forward);
			RaycastHit[] hits;
			if (_earthInvertedMode) {
				hits = Physics.RaycastAll (ray.origin + ray.direction * transform.localScale.z * 1.5f, -ray.direction, transform.localScale.z * 2f, layerMask);
			} else {
				hits = Physics.RaycastAll (Camera.main.transform.position, ray.direction, Mathf.Min(lastCameraDistanceSqr, 1000), layerMask);
			}
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 startingPos = transform.InverseTransformPoint (hits [k].point);
						float angleDiff = Vector3.Angle(constraintPosition, startingPos);
						return angleDiff < constraintAngle;
					}
				}
			}
			return true;
		}
//
//						if (angleDiff > constraintAngle) {
//							Vector3 currentPos = startingPos;
//							Vector3 endingPos = constraintPosition;
//							int i = 0;
//							while (angleDiff>constraintAngle || i<8) {
//								if (angleDiff>constraintAngle) {
//									startingPos = currentPos;
//									currentPos = Vector3.Lerp(currentPos, endingPos, 0.5f);
//								} else {
//									endingPos = currentPos;
//									currentPos = Vector3.Lerp(currentPos, startingPos, 0.5f);
//								}
//								if (i++>32) break;
//								angleDiff = Vector3.Angle(constraintPosition, currentPos);
//							}
//							FlyToLocation(currentPos, 0);
//						}
//					}
//				}
//			}
//		}

		#endregion

	#region Highlighting

		public int layerMask { get {
				return 1<<mapUnityLayer;
			}
		}

		Ray GetRay() {
			Ray ray;
			if (VRSettings.enabled) {
				ray = new Ray(Camera.main.transform.position, Camera.main.transform.rotation * Vector3.forward);
			} else {
				Vector3 mousePos = Input.mousePosition;
				ray = Camera.main.ScreenPointToRay (mousePos);
			}
			return ray;
		}

		void CheckMousePos () {

			Ray ray = GetRay();
			RaycastHit[] hits;

			if (_earthInvertedMode) {
				hits = Physics.RaycastAll (ray.origin + ray.direction * transform.localScale.z * 1.5f, -ray.direction, transform.localScale.z * 2f, layerMask);
			} else {
				hits = Physics.RaycastAll (Camera.main.transform.position, ray.direction, Mathf.Min(lastCameraDistanceSqr, 1000), layerMask);
			}
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						// Cursor follow
						if (_enableCountryHighlight || _dragConstantSpeed || (_cursorFollowMouse && _showCursor) || OnLeftClick!=null || OnRightClick!=null) { // need the cursor location for highlighting test and constant drag speed
							cursorLocation = transform.InverseTransformPoint (hits [k].point);
						}
						// verify if hitPos is inside any country polygon
						int c, cr;
						if (GetCountryUnderMouse(cursorLocation, out c, out cr)) {
							if (c != _countryHighlightedIndex || (c == _countryHighlightedIndex && cr!= _countryRegionHighlightedIndex) ) {
								HighlightCountryRegion (c, cr, false, _showOutline);

								// Raise enter event
								if (OnCountryEnter!=null) OnCountryEnter(c, cr);
							}
							// if show provinces is enabled, then we draw provinces borders
							if (_showProvinces) {
								DrawProvinces (_countryHighlightedIndex, false, false); // draw provinces borders if not drawn
								int p, pr;
								// and now, we check if the mouse if inside a province, so highlight it
								if (GetProvinceUnderMouse(c, cursorLocation, out p, out pr)) {
									if (p != _provinceHighlightedIndex || (p == _provinceHighlightedIndex && pr != _provinceRegionHighlightedIndex) ) {
										HideProvinceHighlight ();

										// Raise enter event
										if (OnProvinceEnter!=null) OnProvinceEnter(p, pr);
									}
									HighlightProvinceRegion (p, pr, false);
								}
							}
							// if show cities is enabled, then check if mouse is over any city
							if (_showCities) {
								int ci;
								if (GetCityUnderMouse(c, cursorLocation, out ci)) {
									if (ci != _cityHighlightedIndex) {
										HideCityHighlight();

										// Raise enter event
										if (OnCityEnter!=null) OnCityEnter(ci);
									}
									HighlightCity(ci);
								} else if (_cityHighlightedIndex>=0) {
									HideCityHighlight();
								}
							}
							return;
						}
					}
				}
				HideCountryRegionHighlight ();
			} else {
				mouseIsOver = false;
			}
		}

	#endregion


		#region Geometric functions

		float SignedAngleBetween (Vector3 a, Vector3 b, Vector3 n) {
			// angle in [0,180]
			float angle = Vector3.Angle (a, b);
			float sign = Mathf.Sign (Vector3.Dot (n, Vector3.Cross (a, b)));
			
			// angle in [-179,180]
			float signed_angle = angle * sign;
			
			return signed_angle;
		}

		Quaternion GetQuaternion (Vector3 point)
		{
			Quaternion oldRotation = transform.localRotation;
			Quaternion q;
			// center destination
			if (_earthInvertedMode) {
				Camera.main.transform.LookAt(point);
				Vector3 angles = Camera.main.transform.localRotation.eulerAngles;
				Camera.main.transform.localRotation = Quaternion.Euler(new Vector3(angles.x, -angles.y, angles.z));
				q = Quaternion.Inverse(Camera.main.transform.localRotation);
				Camera.main.transform.localRotation = Quaternion.Euler(MiscVector.Vector3zero);
			} else {
				Vector3 v1 = point;
				Vector3 v2 = Camera.main.transform.position - transform.position;
				float angle = Vector3.Angle (v1, v2);
				Vector3 axis = Vector3.Cross (v1, v2);
				transform.localRotation = Quaternion.AngleAxis (angle, axis); 
				// straighten view
				Vector3 v3  = Vector3.ProjectOnPlane(transform.up, v2);
				float angle2 = SignedAngleBetween (Camera.main.transform.up, v3, v2);
				transform.Rotate (v2, -angle2, Space.World);
				q = transform.localRotation;
			}
			transform.localRotation = oldRotation;
			return q;
		}

		Vector2 GetLatLonFromBillboard(Vector2 position) {
			float mapWidth = 200.0f;
			float mapHeight = 100.0f;
			float lon = (position.x + mapWidth * 0.5f) * 360f / mapWidth - 180f;
			float lat = position.y * 180f / mapHeight;
			return new Vector2(lat, lon);
		}

		Vector2 GetBillboardPointFromLatLon(float lat, float lon) {
			Vector2 p;
			float mapWidth = 200.0f;
			float mapHeight = 100.0f;
			p.x = (lon+180)*(mapWidth/360f) - mapWidth * 0.5f;
			p.y = lat * (mapHeight/180f);
			return p;
		}

		Rect GetRect2DFromMinMaxLatLon(Vector2 minMaxLat, Vector2 minMaxLon) {
			Vector2 min = GetBillboardPointFromLatLon(minMaxLat.x, minMaxLon.x);
			Vector2 max = GetBillboardPointFromLatLon(minMaxLat.y, minMaxLon.y);
			return new Rect (min.x, min.y,  Math.Abs (max.x - min.x), Mathf.Abs (max.y - min.y));
		}

		public Vector3 GetSpherePointFromLatLon(PolygonPoint point) {
			return GetSpherePointFromLatLon(point.X, point.Y);
//			double phi = point.X * 0.0174532924; //Mathf.Deg2Rad;
//			double theta = (point.Y + 90.0) * 0.0174532924; //Mathf.Deg2Rad;
//			double x = Math.Cos (phi) * Math.Cos (theta) * 0.5;
//			double y = Math.Sin (phi) * 0.5;
//			double z = Math.Cos (phi) * Math.Sin (theta) * 0.5;
//			return new Vector3((float)x,(float)y,(float)z);
		}

		public Vector3 GetSpherePointFromLatLon(double lat, double lon) {
			double phi = lat * 0.0174532924; //Mathf.Deg2Rad;
			double theta = (lon + 90.0) * 0.0174532924; //Mathf.Deg2Rad;
			double x = Math.Cos (phi) * Math.Cos (theta) * 0.5;
			double y = Math.Sin (phi) * 0.5;
			double z = Math.Cos (phi) * Math.Sin (theta) * 0.5;
			return new Vector3((float)x,(float)y,(float)z);
		}

		public PolygonPoint GetLatLonFromSpherePoint(Vector3 p) {
			double phi = Mathf.Asin (p.y*2.0f);
			double theta = Mathf.Atan2(p.x, p.z);
			return new PolygonPoint(phi * Mathf.Rad2Deg, -theta * Mathf.Rad2Deg);
		}

		Vector3 ConvertToTextureCoordinates(Vector3 p, int width, int height) {
			float phi = Mathf.Asin (p.y*2.0f);
			float theta = Mathf.Atan2(p.x, p.z);
			float lonDec = -theta * Mathf.Rad2Deg;
			float latDec = phi * Mathf.Rad2Deg;
			p.x = (lonDec+180)*width/360.0f;
			p.y = latDec * (height/180.0f) + height/2.0f;
			return p;
		}

		/// <summary>
		/// Better than Transform.RotateAround
		/// </summary>
		void RotateAround(Transform transform, Vector3 center, Vector3 axis, float angle) {
			Vector3 pos = transform.position;
			Quaternion rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
			Vector3 dir = pos - center; 						// find current direction relative to center
			dir = rot * dir; 									// rotate the direction
			transform.position = center + dir; 					// define new position
			// rotate object to keep looking at the center:
			Quaternion myRot = transform.rotation;
			transform.rotation *= Quaternion.Inverse(myRot) * rot * myRot;
		}

		#endregion

		#region World Gizmos


		void CheckCursorVisibility() {
			if (cursorLayer!=null && _showCursor) {
				if ((mouseIsOverUIElement || !mouseIsOver) && cursorLayer.activeSelf && !cursorAlwaysVisible) {	// not over globe?
					cursorLayer.SetActive(false);
				}  else if (!mouseIsOverUIElement && mouseIsOver && !cursorLayer.activeSelf) {	// finally, should be visible?
					cursorLayer.SetActive(true);
				}
			}
		}


		void DrawCursor () {
			// Compute cursor dash lines
			float r = _earthInvertedMode ? 0.498f : 0.5f;
			Vector3 north = new Vector3 (0, r, 0);
			Vector3 south = new Vector3 (0, -r, 0);
			Vector3 west = new Vector3 (-r, 0, 0);
			Vector3 east = new Vector3 (r, 0, 0);
			Vector3 equatorFront = new Vector3 (0, 0, r);
			Vector3 equatorPost = new Vector3 (0, 0, -r);

			Vector3[] points = new Vector3[800];
			int[] indices = new int[800];

			// Generate circumference V
			for (int k=0; k<800; k++) {
				indices [k] = k;
			}
			for (int k=0; k<100; k++) {
				points [k] = Vector3.Lerp (north, equatorFront, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [100 + k] = Vector3.Lerp (equatorFront, south, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [200 + k] = Vector3.Lerp (south, equatorPost, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [300 + k] = Vector3.Lerp (equatorPost, north, k / 100.0f).normalized * r;
			}
			// Generate circumference H
			for (int k=0; k<100; k++) {
				points [400 + k] = Vector3.Lerp (west, equatorFront, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [500 + k] = Vector3.Lerp (equatorFront, east, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [600 + k] = Vector3.Lerp (east, equatorPost, k / 100.0f).normalized * r;
			}
			for (int k=0; k<100; k++) {
				points [700 + k] = Vector3.Lerp (equatorPost, west, k / 100.0f).normalized * r;
			}

			Transform t = transform.FindChild ("Cursor");
			if (t != null)
				DestroyImmediate (t.gameObject);
			cursorLayer = new GameObject ("Cursor");
			cursorLayer.transform.SetParent (transform, false);
			cursorLayer.transform.localPosition = MiscVector.Vector3zero;
			cursorLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			cursorLayer.SetActive (_showCursor);

			Mesh mesh = new Mesh ();
			mesh.vertices = points;
			mesh.SetIndices (indices, MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = cursorLayer.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = cursorLayer.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = cursorMat;

		}

		void DrawGrid () {
			DrawLatitudeLines ();
			DrawLongitudeLines ();
		}

		void DrawLatitudeLines () {
			// Generate latitude lines
			List<Vector3> points = new List<Vector3> ();
			List<int> indices = new List<int> ();
			float r = _earthInvertedMode ? 0.498f : 0.501f;
			int idx = 0;
			float m = _frontiersDetail == FRONTIERS_DETAIL.High ? 4.0f : 5.0f;

			for (float a =0; a<90; a += _latitudeStepping) {
				for (int h=1; h>=-1; h--) {
					if (h == 0)
						continue;

					float angle = a * Mathf.Deg2Rad;
					float y = h * Mathf.Sin (angle) * r;
					float r2 = Mathf.Cos (angle) * r;

					int step = Mathf.Min (1 + Mathf.FloorToInt (m * r / r2), 24);
					if ((100 / step) % 2 != 0)
						step++;

					for (int k=0; k<360 + step; k+=step) {
						float ax = k * Mathf.Deg2Rad;
						float x = Mathf.Cos (ax) * r2;
						float z = Mathf.Sin (ax) * r2;
						points.Add (new Vector3 (x, y, z));
						if (k > 0) {
							indices.Add (idx);
							indices.Add (++idx);
						}
					}
					idx++;
					if (a == 0)
						break;
				}
			}

			Transform t = transform.FindChild ("LatitudeLines");
			if (t != null)
				DestroyImmediate (t.gameObject);
			latitudeLayer = new GameObject ("LatitudeLines");
			latitudeLayer.transform.SetParent (transform, false);
			latitudeLayer.transform.localPosition = MiscVector.Vector3zero;
			latitudeLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			latitudeLayer.SetActive (_showLatitudeLines);
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points.ToArray ();
			mesh.SetIndices (indices.ToArray (), MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = latitudeLayer.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = latitudeLayer.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = _gridMode == GRID_MODE.OVERLAY ? gridMatOverlay: gridMatMasked;
			
		}

		void DrawLongitudeLines () {
			// Generate longitude lines
			List<Vector3> points = new List<Vector3> ();
			List<int> indices = new List<int> ();
			float r = _earthInvertedMode ? 0.498f : 0.501f;
			int idx = 0;
			int step = _frontiersDetail == FRONTIERS_DETAIL.High ? 4 : 5;

			for (float a =0; a<180; a += 180 / _longitudeStepping) {
				float angle = a * Mathf.Deg2Rad;
					
				for (int k=0; k<360 + step; k+=step) {
					float ax = k * Mathf.Deg2Rad;
					float x = Mathf.Cos (ax) * r * Mathf.Sin (angle); //Mathf.Cos (ax) * Mathf.Sin (angle) * r;
					float y = Mathf.Sin (ax) * r;
					float z = Mathf.Cos (ax) * r * Mathf.Cos (angle);
					points.Add (new Vector3 (x, y, z));
					if (k > 0) {
						indices.Add (idx);
						indices.Add (++idx);
					}
				}
				idx++;
			}
			
			Transform t = transform.FindChild ("LongitudeLines");
			if (t != null)
				DestroyImmediate (t.gameObject);
			longitudeLayer = new GameObject ("LongitudeLines");
			longitudeLayer.transform.SetParent (transform, false);
			longitudeLayer.transform.localPosition = MiscVector.Vector3zero;
			longitudeLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			longitudeLayer.SetActive (_showLongitudeLines);
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points.ToArray ();
			mesh.SetIndices (indices.ToArray (), MeshTopology.Lines, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave;
			
			MeshFilter mf = longitudeLayer.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			
			MeshRenderer mr = longitudeLayer.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = _gridMode == GRID_MODE.OVERLAY ? gridMatOverlay: gridMatMasked;

		}

		#endregion

		#region Overlay

		GameObject CreateOverlay () {

			if (!gameObject.activeInHierarchy) return null;

			// Prepare layer
			Transform t = transform.FindChild (WPM_OVERLAY_NAME);
			if (t == null) {
				overlayLayer = new GameObject (WPM_OVERLAY_NAME);
				overlayLayer.transform.SetParent (transform, false);
				overlayLayer.transform.position = new Vector3 (5000, 5000, 0);
				overlayLayer.layer = mapUnityLayer;
			} else {
				overlayLayer = t.gameObject;
			}
			overlayLayer.transform.localScale = new Vector3(1.0f/transform.localScale.x, 1.0f/transform.localScale.y, 1.0f/transform.localScale.z);

			// Sphere labels layer
			Material sphereOverlayMaterial = null;
			t = transform.FindChild (SPHERE_OVERLAY_LAYER_NAME);
			if (t != null) {
				Renderer r = t.gameObject.GetComponent<Renderer> ();
				if (r == null || r.sharedMaterial == null) {
					DestroyImmediate (t.gameObject);
					t = null;
				}
			}
			if (t == null) {
				sphereOverlayLayer = Instantiate (Resources.Load <GameObject> ("Prefabs/SphereOverlayLayer"));
				sphereOverlayLayer.hideFlags = HideFlags.DontSave;
				sphereOverlayLayer.name = SPHERE_OVERLAY_LAYER_NAME;
				sphereOverlayLayer.transform.SetParent (transform, false);
				sphereOverlayLayer.transform.localPosition = MiscVector.Vector3zero;
				sphereOverlayMaterial = Instantiate (sphereOverlayLayer.GetComponent<Renderer> ().sharedMaterial);
				sphereOverlayLayer.GetComponent<Renderer> ().sharedMaterial = sphereOverlayMaterial;
			} else {
				sphereOverlayLayer = t.gameObject;
				sphereOverlayLayer.SetActive (true);
				sphereOverlayMaterial = sphereOverlayLayer.GetComponent<Renderer> ().sharedMaterial;
			}
			sphereOverlayMaterial.hideFlags = HideFlags.DontSave;

			// Billboard
			GameObject billboard;
			t = overlayLayer.transform.FindChild ("Billboard");
			if (t == null) {
				billboard = Instantiate (Resources.Load <GameObject> ("Prefabs/Billboard"));
				billboard.name = "Billboard";
				billboard.transform.SetParent (overlayLayer.transform, false);
				billboard.transform.localPosition = MiscVector.Vector3zero;
				billboard.transform.localScale = new Vector3 (overlayWidth, overlayHeight, 1);
				billboard.layer = overlayLayer.layer;
			} else {
				billboard = t.gameObject;
			}
			
			// Render texture
			int imageWidth, imageHeight;
			switch (_labelsQuality) {
			case LABELS_QUALITY.Medium:
				imageWidth = 4096;
				imageHeight = 2048;
				break;
			case LABELS_QUALITY.High:
				imageWidth = 8192;
				imageHeight = 4096;
				break;
			default:
				imageWidth = 2048;
				imageHeight = 1024;
				break;
			}
			if (overlayRT != null && (overlayRT.width != imageWidth || overlayRT.height != imageHeight)) {
				overlayRT.Release ();
				DestroyImmediate (overlayRT);
				overlayRT = null;
			}
			
			Transform camTransform = overlayLayer.transform.FindChild ("MapperCam");
			if (overlayRT == null) {
				overlayRT = new RenderTexture (imageWidth, imageHeight, 0);
				overlayRT.hideFlags = HideFlags.DontSave;
				overlayRT.filterMode = FilterMode.Trilinear;
				if (camTransform != null) {
					camTransform.GetComponent<Camera> ().targetTexture = overlayRT;
				}
			}
			
			// Camera
			if (camTransform == null) {
				GameObject camObj = Instantiate (Resources.Load<GameObject> ("Prefabs/MapperCam"));
				camObj.name = "MapperCam";
				camObj.transform.SetParent (overlayLayer.transform, false);
				camObj.layer = overlayLayer.layer;
				Camera cam = camObj.GetComponent<Camera> ();
				cam.transform.localPosition = Vector3.back * 86.6f; // (10000.0f - 9999.13331f);
				cam.aspect = 2;
				cam.targetTexture = overlayRT;
				cam.cullingMask = 1 << camObj.layer;
			}
			
			// Assigns render texture to current material and recreates the camera
			sphereOverlayMaterial.mainTexture = overlayRT;

			// Reverse normals if inverted mode is enabled
			Drawing.ReverseSphereNormals(sphereOverlayLayer, _earthInvertedMode, _earthHighDensityMesh);
			AdjustSphereOverlayLayerScale();
			return overlayLayer;
		}

		void AdjustSphereOverlayLayerScale() {
			if (_earthInvertedMode) {
				sphereOverlayLayer.transform.localScale = MiscVector.Vector3one * (0.998f - _labelsElevation * 0.5f);
			} else {
				sphereOverlayLayer.transform.localScale = MiscVector.Vector3one * (1.01f + _labelsElevation * 0.05f);
			}
		}

		void DestroyOverlay () {

			if (sphereOverlayLayer != null) {
				sphereOverlayLayer.SetActive (false);
			}

			if (overlayLayer != null) {
				DestroyImmediate (overlayLayer);
				overlayLayer = null;
			}

			GameObject oldWPMOverlay = GameObject.Find (WPM_OVERLAY_NAME);
			if (oldWPMOverlay != null) {
				DestroyImmediate (oldWPMOverlay);
			}
			if (overlayRT != null) {
				overlayRT.Release ();
				DestroyImmediate (overlayRT);
				overlayRT = null;
			}
		}

		#endregion

		#region Markers support

		void CheckMarkersLayer() {
			if (markersLayer==null) { // try to capture an existing marker layer
				Transform t = transform.FindChild("Markers");
				if (t!=null) markersLayer = t.gameObject;
			}
			if (markersLayer==null) { // create it otherwise
				markersLayer = new GameObject("Markers");
				markersLayer.transform.SetParent(transform, false);
				markersLayer.transform.localPosition = MiscVector.Vector3zero;
			}
		}

		void CheckOverlayMarkersLayer() {
			GameObject overlayLayer = GetOverlayLayer(true);
			if (overlayMarkersLayer==null) { // try to capture an existing marker layer
				Transform t = overlayLayer.transform.FindChild("OverlayMarkers");
				if (t!=null) overlayMarkersLayer = t.gameObject;
			}
			if (overlayMarkersLayer==null) { // create it otherwise
				overlayMarkersLayer = new GameObject("OverlayMarkers");
				overlayMarkersLayer.transform.SetParent(overlayLayer.transform, false);
				overlayMarkersLayer.transform.localPosition = MiscVector.Vector3zero;
				overlayMarkersLayer.layer = overlayLayer.layer;
			}
		}

		/// <summary>
		/// Adds a custom marker (gameobject) to the globe on specified location and with custom scale.
		/// </summary>
		void mAddMarker(GameObject marker, Vector3 sphereLocation, float markerScale, bool isBillboard, float surfaceOffset) {
			// Try to get the height of the object
			float height = 0;
			if (marker.GetComponent<MeshFilter>()!=null)
				height = marker.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
			else if (marker.GetComponent<Collider>()!=null) 
				height = marker.GetComponent<Collider>().bounds.size.y;

			height += surfaceOffset;
			float h = height * markerScale / sphereLocation.magnitude; // lift the marker so it appears on the surface of the globe
			
			CheckMarkersLayer();

			// Assign marker parent, position, rotation
			marker.transform.SetParent(markersLayer.transform, false);
			marker.transform.localPosition = _earthInvertedMode ? sphereLocation * (1.0f - h) : sphereLocation * (1.0f + h * 0.5f);
			
			// apply custom scale
			marker.transform.localScale = MiscVector.Vector3one * markerScale; 

			if (_earthInvertedMode) {
				// flip localscale.x
				transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
				// once the marker is on the surface, rotate it so it looks to the surface
				marker.transform.LookAt(transform.position, transform.up);
				if (!isBillboard) marker.transform.Rotate(new Vector3(90,0,0), Space.Self);
				// flip back localscale.x
				transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
			} else {
				// once the marker is on the surface, rotate it so it looks to the surface
				marker.transform.LookAt(transform.position, transform.up); 
				if (!isBillboard) {
					marker.transform.Rotate(new Vector3(-90,0,0), Space.Self);
				}

			}

		}

		/// <summary>
		/// Adds a polygon over the sphere.
		/// </summary>
		GameObject mAddMarker(MARKER_TYPE type, Vector3 sphereLocation, float kmRadius, float ringWidthStart, float ringWidthEnd, Color color) {
			GameObject marker = null;
			CheckOverlayMarkersLayer();
			Vector2 position = Drawing.SphereToBillboardCoordinates(sphereLocation);
			switch(type) {
			case MARKER_TYPE.CIRCLE: {
				float rw = 2.0f * Mathf.PI * EARTH_RADIUS_KM;
				float w = kmRadius / rw;
				w *= 2.0f * overlayWidth;
				float h = w;
				marker = Drawing.DrawCircle("MarkerCircle", position, w,h, 0, Mathf.PI*2.0f, ringWidthStart,ringWidthEnd, 64, GetColoredMarkerMaterial(color), false);
				if (marker!=null) {
					marker.transform.SetParent(overlayMarkersLayer.transform, false);
					marker.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
					marker.layer = overlayMarkersLayer.layer;

					// Check seam
					Vector2 midPos = position;
					if (w + position.x > overlayWidth * 0.5f) {
						midPos.x -= overlayWidth;
					} else if (position.x - w < -overlayWidth * 0.5f) {
						midPos.x += overlayWidth;
					}
					if (midPos.x!=position.x) {
						GameObject midCircle = Drawing.DrawCircle("MarkerCircleMid", midPos, w,h, 0, Mathf.PI*2.0f, ringWidthStart,ringWidthEnd, 64, GetColoredMarkerMaterial(color), false);
						midCircle.transform.SetParent(overlayMarkersLayer.transform, false);
						midCircle.transform.localPosition = new Vector3(midPos.x, midPos.y, -0.01f);
						midCircle.transform.SetParent(marker.transform, true);
						midCircle.layer = overlayMarkersLayer.layer;
					}

				}
			}
			break;
			case MARKER_TYPE.CIRCLE_PROJECTED: {
				float rw = 2.0f * Mathf.PI * EARTH_RADIUS_KM;
				float w = kmRadius / rw;
				w *= 2.0f * overlayWidth;
				float h = w;
				marker = Drawing.DrawCircle("MarkerCircle", position, w,h, 0, Mathf.PI*2.0f, ringWidthStart,ringWidthEnd, 128, GetColoredMarkerMaterial(color), true);
				if (marker!=null) {
					marker.transform.SetParent(overlayMarkersLayer.transform, false);
					marker.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
					marker.layer = overlayMarkersLayer.layer;
					
					// Check seam
					Vector2 midPos = position;
					if (position.x > 0) {
						midPos.x -= overlayWidth;
					} else {
						midPos.x += overlayWidth;
					}
					GameObject midCircle = Drawing.DrawCircle("MarkerCircleMid", midPos, w,h, 0, Mathf.PI*2.0f, ringWidthStart,ringWidthEnd, 128, GetColoredMarkerMaterial(color), true);
					midCircle.transform.SetParent(overlayMarkersLayer.transform, false);
					midCircle.transform.localPosition = new Vector3(midPos.x, midPos.y, -0.01f);
					midCircle.transform.SetParent(marker.transform, true);
					midCircle.layer = overlayMarkersLayer.layer;
				}
			}
			break;
			}
			return marker;
		}

		#endregion

	}

}