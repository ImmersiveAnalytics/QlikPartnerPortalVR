// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
// ***************************************************************************
// This is the public API file - every property or public method belongs here
// ***************************************************************************

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM_Editor;

namespace WPM {

	public enum MARKER_TYPE {
		CIRCLE = 0,
		CIRCLE_PROJECTED = 1
	}

	public enum GRID_MODE {
		OVERLAY = 0,
		MASKED = 1
	}


	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		[SerializeField]
		bool
			_showCursor = true;
		
		/// <summary>
		/// Toggle cursor lines visibility.
		/// </summary>
		public bool showCursor { 
			get {
				return _showCursor; 
			}
			set {
				if (value != _showCursor) {
					_showCursor = value;
					isDirty = true;
					
					if (cursorLayer != null) {
						cursorLayer.SetActive (_showCursor);
					}
				}
			}
		}
		
		/// <summary>
		/// Cursor lines color.
		/// </summary>
		[SerializeField]
		Color
			_cursorColor = new Color (0.56f, 0.47f, 0.68f);
		
		public Color cursorColor {
			get {
				if (cursorMat != null) {
					return cursorMat.color;
				} else {
					return _cursorColor;
				}
			}
			set {
				if (value != _cursorColor) {
					_cursorColor = value;
					isDirty = true;
					
					if (cursorMat != null && _cursorColor != cursorMat.color) {
						cursorMat.color = _cursorColor;
					}
				}
			}
		}
		
		[SerializeField]
		bool
			_cursorFollowMouse = true;
		
		/// <summary>
		/// Makes the cursor follow the mouse when it's over the World.
		/// </summary>
		public bool cursorFollowMouse { 
			get {
				return _cursorFollowMouse; 
			}
			set {
				if (value != _cursorFollowMouse) {
					_cursorFollowMouse = value;
					isDirty = true;
				}
			}
		}
		
		[NonSerialized]
		Vector3
			_cursorLocation;
		
		public Vector3
		cursorLocation {
			get {
				return _cursorLocation;
			}
			set {
				if (_cursorLocation != value) {
					_cursorLocation = value;
					if (cursorLayer != null) {
						cursorLayer.transform.localRotation = Quaternion.LookRotation (cursorLocation);
					}
				}
			}
		}

		/// <summary>
		/// If set to false, cursor will be hidden when mouse if not over the globe.
		/// </summary>
		[SerializeField]
		bool
			_cursorAllwaysVisible = true;
		
		public bool cursorAlwaysVisible {
			get {
				return _cursorAllwaysVisible;
			}
			set {
				if (value != _cursorAllwaysVisible) {
					_cursorAllwaysVisible = value;
					isDirty = true;
					CheckCursorVisibility();
				}
			}
		}
		
		[SerializeField]
		bool
			_showLatitudeLines = true;
		
		/// <summary>
		/// Toggle latitude lines visibility.
		/// </summary>
		public bool showLatitudeLines { 
			get {
				return _showLatitudeLines; 
			}
			set {
				if (value != _showLatitudeLines) {
					_showLatitudeLines = value;
					isDirty = true;
					
					if (latitudeLayer != null) {
						latitudeLayer.SetActive (_showLatitudeLines);
					} else {
						DrawLatitudeLines();
					}
				}
			}
		}
		
		[SerializeField]
		[Range(5.0f, 45.0f)]
		int
			_latitudeStepping = 15;
		/// <summary>
		/// Specify latitude lines separation.
		/// </summary>
		public int latitudeStepping { 
			get {
				return _latitudeStepping; 
			}
			set {
				if (value != _latitudeStepping) {
					_latitudeStepping = value;
					isDirty = true;
					
					DrawLatitudeLines ();
				}
			}
		}
		
		[SerializeField]
		bool
			_showLongitudeLines = true;
		
		/// <summary>
		/// Toggle longitude lines visibility.
		/// </summary>
		public bool showLongitudeLines { 
			get {
				return _showLongitudeLines; 
			}
			set {
				if (value != _showLongitudeLines) {
					_showLongitudeLines = value;
					isDirty = true;
					
					if (longitudeLayer != null) {
						longitudeLayer.SetActive (_showLongitudeLines);
					} else {
						DrawLongitudeLines();
					}
				}
			}
		}
		
		[SerializeField]
		[Range(5.0f, 45.0f)]
		int
			_longitudeStepping = 15;
		/// <summary>
		/// Specify longitude lines separation.
		/// </summary>
		public int longitudeStepping { 
			get {
				return _longitudeStepping; 
			}
			set {
				if (value != _longitudeStepping) {
					_longitudeStepping = value;
					isDirty = true;
					
					DrawLongitudeLines ();
				}
			}
		}
		
		[SerializeField]
		Color
			_gridColor = new Color (0.16f, 0.33f, 0.498f);
		
		/// <summary>
		/// Color for imaginary lines (longitude and latitude).
		/// </summary>
		public Color gridLinesColor {
			get {
				return _gridColor;
			}
			set {
				if (value != _gridColor) {
					_gridColor = value;
					isDirty = true;
					
					if (gridMatOverlay != null && _gridColor != gridMatOverlay.color) {
						gridMatOverlay.color = _gridColor;
					}
					if (gridMatMasked != null && _gridColor != gridMatMasked.color) {
						gridMatMasked.color = _gridColor;
					}
				}
			}
		}

		[SerializeField]
		GRID_MODE _gridMode = GRID_MODE.OVERLAY;

		public GRID_MODE gridMode {
			get {
				return _gridMode;
			}
			set {
				if (value != _gridMode) {
					_gridMode = value;
					isDirty = true;
					DrawGrid();
				}
			}
		}


	#region Public API area

		/// <summary>
		/// Adds a custom marker (gameobject) to the globe on specified location and with custom scale.
		/// </summary>
		public void AddMarker(GameObject marker, Vector3 sphereLocation, float markerScale) {
			mAddMarker(marker, sphereLocation, markerScale, false, 0f);
			
		}
		
		/// <summary>
		/// Adds a custom marker (gameobject) to the globe on specified location and with custom scale.
		/// </summary>
		/// <param name="isBillboard">If set to <c>true</c> game object will be oriented to position normal facing outside.</param>
		/// <param name="surfaceSeparation">Makes the marker a little bit off the surface to prevent clipping with other elements like city spots</param>
		public void AddMarker(GameObject marker, Vector3 sphereLocation, float markerScale, bool isBillboard, float surfaceSeparation) {
			mAddMarker(marker, sphereLocation, markerScale, isBillboard, surfaceSeparation);
		}

		/// <summary>
		/// Adds a custom marker (polygon) to the globe on specified location and with custom size in km.
		/// </summary>
		/// <param name="type">Polygon type.</param>
		/// <param name="sphereLocation">Sphere location.</param>
		/// <param name="kmRadius">Radius in KM.</param>
		/// <param name="ringWidthStart">Ring inner limit (0..1). Pass 0 to draw a full circle.</param>
		/// <param name="ringWidthEnd">Ring outer limit (0..1). Pass 1 to draw a full circle.</param>
		/// <param name="color">Color</param>
		public GameObject AddMarker(MARKER_TYPE type, Vector3 sphereLocation, float kmRadius, float ringWidthStart, float ringWidthEnd, Color color) {
			return mAddMarker(type, sphereLocation, kmRadius, ringWidthStart, ringWidthEnd, color);
		}


		/// <summary>
		/// Adds a line to the globe with options (returns the line gameobject).
		/// </summary>
		/// <param name="start">starting location on the sphere</param>
		/// <param name="end">end location on the sphere</param>
		/// <param name="Color">line color</param>
		/// <param name="arcElevation">arc elevation relative to the sphere size.</param>
		/// <param name="duration">drawing speed (0 for instant drawing)</param>
		/// <param name="fadeOutAfter">duration of the line once drawn after which it fades out (set this to 0 to make the line stay forever)</param>
		public GameObject AddLine(Vector3 start, Vector3 end, Color color, float arcElevation, float duration, float lineWidth, float fadeOutAfter) {
			CheckMarkersLayer();
			GameObject newLine = new GameObject("MarkerLine");
			newLine.transform.SetParent(markersLayer.transform, false);
			LineMarkerAnimator lma =  newLine.AddComponent<LineMarkerAnimator>();
			lma.start = start;
			lma.end = end;
			lma.color = color;
			lma.arcElevation = arcElevation;
			lma.duration = duration;
			lma.lineWidth = lineWidth;
			lma.lineMaterial = markerMat;
			lma.autoFadeAfter = fadeOutAfter;
			lma.earthInvertedMode = _earthInvertedMode;
			return newLine;
		}

		/// <summary>
		/// Deletes all custom markers and lines
		/// </summary>
		public void ClearMarkers() {
			if (markersLayer==null) return;
			Destroy (markersLayer);
		}


		/// <summary>
		/// Removes all marker lines.
		/// </summary>
		public void ClearLineMarkers() {
			if (markersLayer==null) return;
			LineRenderer[] t = markersLayer.transform.GetComponentsInChildren<LineRenderer>();
			for (int k=0;k<t.Length;k++)
				Destroy (t[k].gameObject);
		}

		#endregion


	}

}