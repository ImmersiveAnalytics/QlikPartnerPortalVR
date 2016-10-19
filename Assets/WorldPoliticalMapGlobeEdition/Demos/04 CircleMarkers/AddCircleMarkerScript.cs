using UnityEngine;
using System.Collections;
using WPM;

public class AddCircleMarkerScript : MonoBehaviour {

	WorldMapGlobe map;
	GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;

	float kmRadius = 200;
	float ringWidthStart = 0;
	float ringWidthEnd = 1.0f;

	void Start () {

		// Listen to map clicks
		map = WorldMapGlobe.instance;
		map.OnLeftClick += (Vector3 sphereLocation) => map.AddMarker(MARKER_TYPE.CIRCLE_PROJECTED, sphereLocation, kmRadius, ringWidthStart, ringWidthEnd, Color.green);

		// Straight the globe preserving current center -- totally optional and has nothing to do with markers
		map.StraightenGlobe();

		// UI Setup - non-important, only for this demo
		GUIResizer.Init (800, 500); 

		labelStyle = new GUIStyle ();
		labelStyle.normal.textColor = Color.white;
		buttonStyle = new GUIStyle (labelStyle);
		buttonStyle.alignment = TextAnchor.MiddleLeft;
		buttonStyle.normal.background = Texture2D.whiteTexture;
		buttonStyle.normal.textColor = Color.white;
		sliderStyle = new GUIStyle ();
		sliderStyle.normal.background = Texture2D.whiteTexture;
		sliderStyle.fixedHeight = 4.0f;
		sliderThumbStyle = new GUIStyle ();
		sliderThumbStyle.normal.background = Resources.Load<Texture2D> ("thumb");
		sliderThumbStyle.overflow = new RectOffset (0, 0, 8, 0);
		sliderThumbStyle.fixedWidth = 20.0f;
		sliderThumbStyle.fixedHeight = 12.0f;
	}


	void OnGUI () {

		// Do autoresizing of GUI layer
		GUIResizer.AutoResize ();

		GUI.Label (new Rect (10, 15, 430, 30), "Click on the globe to add a circle marker.", labelStyle);

		GUI.Button (new Rect (10, 45, 100, 30), "Circle radius", labelStyle);
		kmRadius = GUI.HorizontalSlider (new Rect (120, 50, 80, 20), kmRadius, 50, 1000, sliderStyle, sliderThumbStyle);

		GUI.Button (new Rect (10, 75, 100, 30), "Ring Width Start", labelStyle);
		ringWidthStart = GUI.HorizontalSlider (new Rect (120, 80, 80, 20), ringWidthStart, 0, 1f, sliderStyle, sliderThumbStyle);

		GUI.Button (new Rect (10, 105, 100, 30), "Ring Width End", labelStyle);
		ringWidthEnd = GUI.HorizontalSlider (new Rect (120, 110, 80, 20), ringWidthEnd, 0, 1f, sliderStyle, sliderThumbStyle);

	}


}
