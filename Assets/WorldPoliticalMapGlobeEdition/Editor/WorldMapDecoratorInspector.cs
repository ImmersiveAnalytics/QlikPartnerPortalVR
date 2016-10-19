using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM;

namespace WPM_Editor {
	[CustomEditor(typeof(WorldMapDecorator))]
	public class WorldMapDecoratorInspector : Editor {
	
		WorldMapDecorator _decorator;
		Texture2D _blackTexture;
		GUIStyle blackStyle;
		string[] groupNames, countryNames;
		CountryDecorator decorator;
		CountryDecoratorGroupInfo decoratorGroup;
		int lastCountryCount;
		Vector3 oldCameraPos;
		bool zoomState;

		WorldMapGlobe _map { get { return _decorator.map; } }

		void OnEnable () {
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f) : new Color (0.7f, 0.7f, 0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;
			_decorator = (WorldMapDecorator)target;
//			if (!Application.isPlaying && _decorator.decoratorLayer == null)
//				_decorator.Init ();
			groupNames = new string[WorldMapDecorator.NUM_GROUPS];
			ReloadGroupNames ();
			ReloadCountryNames ();
		}

		void OnDisable () {
			if (zoomState)
				ToggleZoomState ();
		}

		public override void OnInspectorGUI () {
			if (_decorator == null)
				return;

			bool requestChanges = false;

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginVertical (blackStyle);

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Group", GUILayout.Width (120));
			int oldGroup = _decorator.GUIGroupIndex;
			_decorator.GUIGroupIndex = EditorGUILayout.Popup (_decorator.GUIGroupIndex, groupNames, GUILayout.MaxWidth (200));
			if (_decorator.GUIGroupIndex != oldGroup || decoratorGroup == null) {
				decoratorGroup = _decorator.GetDecoratorGroup (_decorator.GUIGroupIndex, true);
			}

			if (GUILayout.Button ("Clear")) {
				_decorator.ClearDecoratorGroup (_decorator.GUIGroupIndex);
				ReloadGroupNames ();
				ReloadCountryNames ();
			}

			EditorGUILayout.EndHorizontal ();
		
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("   Enabled", GUILayout.Width (120));
			decoratorGroup.active = EditorGUILayout.Toggle (decoratorGroup.active, GUILayout.Width (20));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();

			// country selector
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Country", GUILayout.Width (120));
			if (lastCountryCount != _map.countries.Length) {
				ReloadCountryNames ();
			}
			int selection = EditorGUILayout.Popup (_decorator.GUICountryIndex, countryNames);
			if (selection != _decorator.GUICountryIndex) {
				_decorator.GUICountryName = "";
				_decorator.GUICountryIndex = selection;
				FlyToCountry ();
			}

			bool prevc = _decorator.groupByContinent;
			GUILayout.Label ("Grouped");
			_decorator.groupByContinent = EditorGUILayout.Toggle (_decorator.groupByContinent, GUILayout.Width (20));
			if (_decorator.groupByContinent != prevc) {
				ReloadCountryNames ();
			}

			EditorGUILayout.EndHorizontal ();

			// type of decoration
			if (_decorator.GUICountryName.Length > 0) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("", GUILayout.Width (120));
				if (GUILayout.Button ("Toggle Zoom")) {
					ToggleZoomState ();
				}
				if (GUILayout.Button ("Fly To")) {
					FlyToCountry ();
				}
				EditorGUILayout.EndHorizontal ();

				CountryDecorator existingDecorator = _decorator.GetCountryDecorator (_decorator.GUIGroupIndex, _decorator.GUICountryName);
				if (existingDecorator != null) {
					decorator = existingDecorator;
				} else if (decorator == null || !decorator.countryName.Equals (_decorator.GUICountryName)) {
					decorator = new CountryDecorator (_decorator.GUICountryName);
				}

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Hidden", GUILayout.Width (120));
				bool prevHidden = decorator.hidden;
				decorator.hidden = EditorGUILayout.Toggle (decorator.hidden);
				if (prevHidden != decorator.hidden)
					requestChanges = true;
				EditorGUILayout.EndHorizontal ();
				
				if (!decorator.hidden) {

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Label Visible", GUILayout.Width (120));
				bool prevLabelVisible = decorator.labelVisible;
				decorator.labelVisible = EditorGUILayout.Toggle (decorator.labelVisible);
				if (prevLabelVisible != decorator.labelVisible)
					requestChanges = true;
				EditorGUILayout.EndHorizontal ();

				if (decorator.labelVisible) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Text", GUILayout.Width (120));
					string prevLabel = decorator.customLabel;
					decorator.customLabel = EditorGUILayout.TextField (decorator.customLabel);
					if (!prevLabel.Equals (decorator.customLabel))
						requestChanges = true;
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Font", GUILayout.Width (120));
					Font prevFont = decorator.labelFontOverride;
					decorator.labelFontOverride = (Font)EditorGUILayout.ObjectField (decorator.labelFontOverride, typeof(Font), false);
					if (decorator.labelFontOverride != prevFont)
						requestChanges = true;
					EditorGUILayout.EndHorizontal ();


					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Custom Color", GUILayout.Width (120));
					decorator.labelOverridesColor = EditorGUILayout.Toggle (decorator.labelOverridesColor);
					if (decorator.labelOverridesColor) {
						GUILayout.Label ("Color", GUILayout.Width (120));
						Color prevColor = decorator.labelColor;
						decorator.labelColor = EditorGUILayout.ColorField (decorator.labelColor, GUILayout.Width (50));
						if (prevColor != decorator.labelColor)
							requestChanges = true;
					}
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Offset", GUILayout.Width (120));
					Vector2 prevLabelOffset = decorator.labelOffset;
					decorator.labelOffset = EditorGUILayout.Vector2Field ("", decorator.labelOffset);
					if (prevLabelOffset != decorator.labelOffset)
						requestChanges = true;
					EditorGUILayout.EndHorizontal ();
				
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Rotation", GUILayout.Width (120));
					float prevLabelRotation = decorator.labelRotation;
					decorator.labelRotation = EditorGUILayout.Slider (decorator.labelRotation, 0, 359);
					if (prevLabelRotation != decorator.labelRotation)
						requestChanges = true;
					EditorGUILayout.EndHorizontal ();
				}

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Colorized", GUILayout.Width (120));
				bool prevColorized = decorator.isColorized;
				decorator.isColorized = EditorGUILayout.Toggle (decorator.isColorized);
				if (decorator.isColorized != prevColorized) {
					requestChanges = true;
				}
				if (decorator.isColorized) {
					GUILayout.Label ("Fill Color", GUILayout.Width (120));
					Color prevColor = decorator.fillColor;
					decorator.fillColor = EditorGUILayout.ColorField (decorator.fillColor, GUILayout.Width (50));
					if (prevColor != decorator.fillColor)
						requestChanges = true;
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("Texture", GUILayout.Width (120));
					Texture2D prevTexture = decorator.texture;
					decorator.texture = (Texture2D)EditorGUILayout.ObjectField (decorator.texture, typeof(Texture2D), false);
					if (decorator.texture != prevTexture)
						requestChanges = true;

					if (decorator.texture != null) {
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						Vector2 prevVector = decorator.textureScale;
						decorator.textureScale = EditorGUILayout.Vector2Field ("   Scale", decorator.textureScale);
						if (prevVector != decorator.textureScale)
							requestChanges = true;
						EditorGUILayout.EndHorizontal ();

						EditorGUILayout.BeginHorizontal ();
						prevVector = decorator.textureOffset;
						decorator.textureOffset = EditorGUILayout.Vector2Field ("   Offset", decorator.textureOffset);
						if (prevVector != decorator.textureOffset)
							requestChanges = true;
						EditorGUILayout.EndHorizontal ();

						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Rotation", GUILayout.Width (120));
						float prevFloat = decorator.textureRotation;
						decorator.textureRotation = EditorGUILayout.Slider (decorator.textureRotation, 0, 360);
						if (prevFloat != decorator.textureRotation)
							requestChanges = true;
					}
				}
				EditorGUILayout.EndHorizontal ();

				}
				EditorGUILayout.BeginHorizontal ();
				if (decorator.isNew) {
					if (GUILayout.Button ("Assign")) {
						_decorator.SetCountryDecorator (_decorator.GUIGroupIndex, _decorator.GUICountryName, decorator);
						ReloadGroupNames ();
						ReloadCountryNames ();
					}
				} else if (GUILayout.Button ("Remove")) {
					decorator = null;
					_decorator.RemoveCountryDecorator (_decorator.GUIGroupIndex, _decorator.GUICountryName);
					ReloadGroupNames ();
					ReloadCountryNames ();
				}
				EditorGUILayout.EndHorizontal ();

				if (!decoratorGroup.active) {
					DrawWarningLabel ("Enable the decoration group to activate changes");
				}
			}


			EditorGUILayout.EndVertical ();

			if (requestChanges) {
				_decorator.ForceUpdateDecorators ();
				EditorUtility.SetDirty(target);
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

		void ReloadGroupNames () {
			for (int k=0; k<groupNames.Length; k++) {
				int dc = _decorator.GetCountryDecoratorCount (k);
				if (dc > 0) {
					groupNames [k] = k.ToString () + " (" + dc + " decorators)";
				} else {
					groupNames [k] = k.ToString ();
				}
			}
		}

		void ReloadCountryNames () {
			if (_map == null || _map.countries == null)
				lastCountryCount = -1;
			else
				lastCountryCount = _map.countries.Length;
			_decorator.GUICountryIndex = -1;
			List<string> all = new List<string> ();
			all.AddRange (_decorator.GetDecoratedCountries (_decorator.GUIGroupIndex, true));
			// recover GUI country index selection
			if (_decorator.GUICountryName.Length > 0) {
				for (int k=0; k<all.Count; k++) {
					if (all [k].StartsWith (_decorator.GUICountryName)) {
						_decorator.GUICountryIndex = k;
						break; 
					}
				}
			}
			if (all.Count > 0) 
				all.Add ("---");
			all.AddRange (_map.GetCountryNames (_decorator.groupByContinent));
			countryNames = all.ToArray ();
		}

		void DrawWarningLabel (string s) {
			GUIStyle warningLabelStyle = new GUIStyle (GUI.skin.label);
			warningLabelStyle.normal.textColor = new Color (0.31f, 0.38f, 0.56f);
			GUILayout.Label (s, warningLabelStyle);
		}

		void ToggleZoomState () {
			zoomState = !zoomState;
			if (zoomState) {
				oldCameraPos = Camera.main.transform.position;
				Camera.main.transform.position = _map.transform.position + (Camera.main.transform.position - _map.transform.position) * _map.transform.localScale.z * 0.6f;
			} else {
				Camera.main.transform.position = oldCameraPos;
			}
		}

		void FlyToCountry () {
			string[] s = countryNames [_decorator.GUICountryIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				_decorator.GUICountryName = s [0].Trim ();
				int countryIndex = int.Parse (s [1]);
				if (countryIndex >= 0) {
					if (Application.isPlaying) {
						_map.FlyToCountry (countryIndex, 2.0f);
						_map.BlinkCountry (countryIndex, Color.black, Color.green, 2.2f, 0.2f);
					} else {
						_map.FlyToCountry (countryIndex, 0);
					}
				}
			}
		}
	}

}