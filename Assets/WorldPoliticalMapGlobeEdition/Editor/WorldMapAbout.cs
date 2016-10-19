using UnityEngine;
using UnityEditor;
using System.Collections;

namespace WPM {

	public class WorldMapAbout : EditorWindow {
		Texture2D _headerTexture, _blackTexture;
		GUIStyle richLabelStyle;
		Vector2 readmeScroll = Vector2.zero;
		string readmeText;
		GUIStyle blackStyle;

		public static void ShowAboutWindow() {
			float height = 550.0f;
			float width = 600.0f;

			Rect rect = new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - height * 0.5f, width, height);
			GetWindowWithRect<WorldMapAbout>(rect, true, "About World Political Map Globe Edition", true);
		}


		void OnEnable() {
			Color backColor = EditorGUIUtility.isProSkin ? new Color (0.18f, 0.18f, 0.18f) : new Color (0.7f, 0.7f, 0.7f);
			_blackTexture = MakeTex (4, 4, backColor);
			_blackTexture.hideFlags = HideFlags.DontSave;
			_headerTexture = Resources.Load<Texture2D> ("EditorHeader");
			blackStyle = new GUIStyle ();
			blackStyle.normal.background = _blackTexture;

			// load readme.txt
			readmeText = System.IO.File.ReadAllText(GetAssetPath() + "/README.txt");
		}

		void OnGUI(){
			if (richLabelStyle==null) {
				richLabelStyle = new GUIStyle(GUI.skin.label);
				richLabelStyle.richText = true;
				richLabelStyle.wordWrap = true;
			}

			EditorGUILayout.Separator ();
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;  
			GUILayout.Label (_headerTexture, GUILayout.ExpandWidth (true));
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;  
			EditorGUILayout.Separator ();

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("<b>World Political Map Globe Edition</b>\nCopyright (C) by Kronnect", richLabelStyle);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator ();
			GUILayout.Label("Thanks for purchasing!");
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			readmeScroll = GUILayout.BeginScrollView(readmeScroll, blackStyle, GUILayout.Width(Screen.width * 0.95f));
			GUILayout.Label(readmeText, richLabelStyle);
			GUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator ();
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Support Forum and more assets!", GUILayout.Height(40))){
				Application.OpenURL("http://kronnect.me");
			}
			if(GUILayout.Button("Rate this Asset", GUILayout.Height(40))){
				Application.OpenURL("com.unity3d.kharma:content/41890");
			}
			if(GUILayout.Button("Close Window", GUILayout.Height(40))){
				Close();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator ();

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

		
		string GetAssetPath() {
			// Proceed and restore
			string[] paths = AssetDatabase.GetAllAssetPaths ();
			for (int k=0; k<paths.Length; k++) {
				if (paths [k].EndsWith ("WorldPoliticalMapGlobeEdition")) {
					return paths[k];
				}
			}
			return "";
		}
	}

}