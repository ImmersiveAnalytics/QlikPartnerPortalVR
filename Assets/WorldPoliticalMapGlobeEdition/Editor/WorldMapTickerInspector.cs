using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM;

namespace WPM_Editor {
	[CustomEditor(typeof(WorldMapTicker))]
	public class WorldMapTickerInspector : Editor {

		int tickerLine;

		Texture2D _blackTexture;
		GUIStyle blackStyle;
		WorldMapTicker _ticker;
		string[] tickerNames;
		TickerText sampleTicker;
		string duration;
		bool pendingChanges = false;
		TickerBand[] tickers { get { return _ticker.tickerBands; } }

		void OnEnable () {
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f): new Color(0.7f,0.7f,0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;
			_ticker = (WorldMapTicker)target;
			if (tickers==null || tickers.Length==0) _ticker.Init();
			tickerNames = new string[tickers.Length];
			for(int k=0;k<tickerNames.Length;k++) tickerNames[k] = k.ToString();
			tickerLine = Mathf.Clamp (tickerLine, 0, tickerNames.Length-1);
			// instance a sample ticker text for the inspector
			sampleTicker = new TickerText();
		}

		public override void OnInspectorGUI () {
			if (_ticker == null)
				return;
			if (tickerNames.Length<tickers.Length)
				return;

			bool refresh = false;

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Ticker Band", GUILayout.Width (120));
			tickerLine = EditorGUILayout.Popup (tickerLine, tickerNames, GUILayout.Width (80));
			if (GUILayout.Button("Reset Ticker", GUILayout.MaxWidth(120))) {
				_ticker.ResetTickerBand(tickerLine);
				refresh = true;
			}
			if (GUILayout.Button("Reset All", GUILayout.MaxWidth(120))) {
				_ticker.ResetTickerBands();
				refresh = true;
			}

			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Visible",  GUILayout.Width (120));
			bool oldb = tickers[tickerLine].visible;
			tickers[tickerLine].visible = EditorGUILayout.Toggle (tickers[tickerLine].visible, GUILayout.Width (20));
			if (oldb!=tickers[tickerLine].visible) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Scroll Speed", GUILayout.Width (120));
			tickers[tickerLine].scrollSpeed = EditorGUILayout.Slider (tickers[tickerLine].scrollSpeed, -0.5f, 0.5f);

			if (GUILayout.Button("Static")) {
				tickers[tickerLine].scrollSpeed = 0;
			}


			EditorGUILayout.EndHorizontal ();


			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Vertical Offset", GUILayout.Width (120));
			float oldf = tickers[tickerLine].verticalOffset;
			tickers[tickerLine].verticalOffset = EditorGUILayout.Slider (tickers[tickerLine].verticalOffset, -0.5f, 0.5f);
			if (oldf!=tickers[tickerLine].verticalOffset) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Size",  GUILayout.Width (120));
			oldf = tickers[tickerLine].verticalSize;
			tickers[tickerLine].verticalSize = EditorGUILayout.Slider (tickers[tickerLine].verticalSize, 0.01f, 1.0f);
			if (oldf!=tickers[tickerLine].verticalSize) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUIContent autoHideContent = new GUIContent("   Auto Hide", "Ticker will automatically hide if it's empty");
			oldb = tickers[tickerLine].autoHide;
			GUILayout.Label (autoHideContent,  GUILayout.Width (120));
			tickers[tickerLine].autoHide = EditorGUILayout.Toggle (tickers[tickerLine].autoHide, GUILayout.Width (20));
			if (oldb!=tickers[tickerLine].autoHide) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Background Color", GUILayout.Width (120));
			Color oldc = tickers[tickerLine].backgroundColor;
			tickers[tickerLine].backgroundColor = EditorGUILayout.ColorField (tickers[tickerLine].backgroundColor, GUILayout.Width (50));
			if (oldc!=tickers[tickerLine].backgroundColor) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Fade Speed",  GUILayout.Width (120));
			oldf = tickers[tickerLine].fadeSpeed;
			tickers[tickerLine].fadeSpeed = EditorGUILayout.Slider (tickers[tickerLine].fadeSpeed, 0, 5.0f);
			if (oldf!=tickers[tickerLine].fadeSpeed) refresh = true;
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginVertical (blackStyle);

			// allows the user to create a sample ticker and add it to the map
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Add Sample Text", GUILayout.Width (120));
			EditorStyles.textField.wordWrap = true;
			sampleTicker.text = EditorGUILayout.TextArea (sampleTicker.text, GUILayout.Height (60));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Ticker Line", GUILayout.Width (120));
			sampleTicker.tickerLine = EditorGUILayout.Popup (sampleTicker.tickerLine, tickerNames, GUILayout.Width (80));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Horizontal Offset", GUILayout.Width (120));
			oldf = sampleTicker.horizontalOffset;
			sampleTicker.horizontalOffset = EditorGUILayout.Slider (sampleTicker.horizontalOffset, -0.5f, 0.5f);
			if (oldf!=sampleTicker.horizontalOffset) refresh = true;
			if (tickers[tickerLine].scrollSpeed!=0) {
				DrawWarningLabel("(ignored because scroll speed)");

			}
			EditorGUILayout.EndHorizontal ();


			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Text Color", GUILayout.Width (120));
			sampleTicker.textColor = EditorGUILayout.ColorField (sampleTicker.textColor, GUILayout.Width (50));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Draw Shadow",  GUILayout.Width (120));
			sampleTicker.drawTextShadow = EditorGUILayout.Toggle (sampleTicker.drawTextShadow);
			if (sampleTicker.drawTextShadow) {
				GUILayout.Label ("Shadow Color", GUILayout.Width (120));
				sampleTicker.shadowColor = EditorGUILayout.ColorField (sampleTicker.shadowColor, GUILayout.Width (50));
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Fade Duration", GUILayout.Width (120));
			sampleTicker.fadeDuration = EditorGUILayout.Slider (sampleTicker.fadeDuration, 0, 10.0f);
			EditorGUILayout.EndHorizontal ();

			if (tickers[tickerLine].scrollSpeed == 0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("   Duration", GUILayout.Width (120));
				duration = sampleTicker.duration.ToString();
				duration = EditorGUILayout.TextField (duration, GUILayout.Width (80));
				sampleTicker.duration = ToFloat(duration);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Blink Interval", GUILayout.Width (120));
			sampleTicker.blinkInterval = EditorGUILayout.Slider (sampleTicker.blinkInterval, 0, 5.0f);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Blink Repetitions", GUILayout.Width (120));
			sampleTicker.blinkRepetitions = EditorGUILayout.IntSlider (sampleTicker.blinkRepetitions, 0, 50);
			EditorGUILayout.EndHorizontal ();

			if (GUILayout.Button("Launch Test Ticker") && Application.isPlaying) {
				_ticker.AddTickerText(sampleTicker);
				if (sampleTicker.duration==0) {
					sampleTicker = (TickerText)sampleTicker.Clone(); // maintain properties but allow to create a new/different ticker without affecting previous one
				}
			}
			if (!Application.isPlaying) {
				DrawWarningLabel("Not available in Edit mode");
			}
			EditorGUILayout.EndVertical (); 
			EditorGUILayout.Separator ();

			if (!Application.isPlaying && (refresh || pendingChanges)) {
				pendingChanges = _ticker.UpdateTickerBands();
				if (pendingChanges) EditorUtility.SetDirty (target);
			}
		}

		void DrawWarningLabel(string s) {
			GUIStyle warningLabelStyle = new GUIStyle (GUI.skin.label);
			warningLabelStyle.normal.textColor = new Color (0.31f, 0.38f, 0.56f);
			GUILayout.Label (s, warningLabelStyle);
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

		float ToFloat(string s) {
			float f = 0;
			float.TryParse(s, out f);
			return f;
		}



	}

}