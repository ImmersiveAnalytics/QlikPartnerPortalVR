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

	public delegate void OnLeftClick(Vector3 sphereLocation);
	public delegate void OnRightClick(Vector3 sphereLocation);

	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		static WorldMapGlobe _instance;
		public event OnLeftClick OnLeftClick;
		public event OnRightClick OnRightClick;

		/// <summary>
		/// Instance of the world map. Use this property to access World Map functionality.
		/// </summary>
		public static WorldMapGlobe instance {
			get {
				if (_instance == null) {
					GameObject obj = GameObject.Find ("WorldMapGlobe");
					if (obj == null) {
						Debug.LogWarning ("'WorldMapGlobe' GameObject could not be found in the scene. Make sure it's created with this name before using any map functionality.");
					} else {
						_instance = obj.GetComponent<WorldMapGlobe> ();
					}
				}
				return _instance;
			}
		}

		int _surfacesCount;
		/// <summary>
		/// Returns number of visible (active) colorized surfaces.
		/// </summary>
		public int surfacesCount { get { return _surfacesCount; } }


	#region Public API area

		/// <summary>
		/// Returns the overlay base layer (parent gameObject), useful to overlay stuff on the map that needs to be overlayed (ie. flat icons or labels). It will be created if it doesn't exist.
		/// </summary>
		public GameObject GetOverlayLayer (bool createIfNotExists) {
			if (overlayLayer != null && sphereOverlayLayer != null) {
//				overlayLayer.transform.localScale = MiscVector.Vector3one;
				overlayLayer.transform.localScale = new Vector3(1.0f/transform.localScale.x, 1.0f/transform.localScale.y, 1.0f/transform.localScale.z);
				overlayLayer.transform.position = new Vector3 (5000, 5000, 0);
				return overlayLayer;
			} else if (createIfNotExists) {
				DestroyOverlay ();
				return CreateOverlay ();
			} else {
				return null;
			}
		}

#if !UNITY_WEBPLAYER
		public Texture2D BakeTexture(string outputFile) {

			// Get all triangles and its colors
			Texture2D texture = Instantiate(gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture) as Texture2D;
			texture.hideFlags = HideFlags.DontSave;
			int width = texture.width;
			int height = texture.height;
			Color[] colors = texture.GetPixels();

			if (_surfacesLayer!=null) {
			Transform[] surfaces = _surfacesLayer.GetComponentsInChildren<Transform>();
			// Antartica k = 16
			for (int k=0;k<surfaces.Length;k++) {
				// Get the color
				Color color;
				Renderer rr = surfaces[k].GetComponent<Renderer>();
				if (rr!=null)
					color =rr.sharedMaterial.color;
				else
					continue; // not valid

				// Get triangles and paint over the texture
				MeshFilter mf = surfaces[k].GetComponent<MeshFilter>();
				if (mf==null || mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles) continue;
				Vector3[] vertex = mf.sharedMesh.vertices;
				int[] index = mf.sharedMesh.GetTriangles(0);

				float maxEdge = width * 0.8f;
				float minEdge = width * 0.2f;
				for (int i=0;i<index.Length;i+=3) {
					Vector3 p1 = ConvertToTextureCoordinates(vertex[index[i]], width, height);
					Vector3 p2 = ConvertToTextureCoordinates(vertex[index[i+1]], width, height);
					Vector3 p3 = ConvertToTextureCoordinates(vertex[index[i+2]], width, height);
					// Sort points
					if (p2.x>p3.x) {
						Vector3 p = p2;
						p2 = p3;
						p3 = p;
					}
					if (p1.x>p2.x) {
						Vector3 p = p1;
						p1 = p2;
						p2 = p;
						if (p2.x>p3.x) {
							p = p2;
							p2 = p3;
							p3 = p;
						}
					}
					if (p1.x<minEdge && p2.x<minEdge && p3.x>maxEdge) {
						if (p1.x<1 && p2.x<1) {
							p1.x = width - p1.x;
							p2.x = width - p2.x;
						} else 
							p3.x = width - p3.x;
					} else if (p1.x<minEdge && p2.x>maxEdge && p3.x>maxEdge) {
						p1.x = width + p1.x;
					} 
					Drawing.DrawTriangle(colors, width, height, p1, p2, p3, color);
				}
			}
			texture.SetPixels(colors);
			texture.Apply();
			}

			if (File.Exists(outputFile)) File.Delete(outputFile);
			File.WriteAllBytes(outputFile, texture.EncodeToPNG());
			return texture;
		}
#endif



		/// <summary>
		/// Enables Calculator component and returns a reference to its API.
		/// </summary>
		public WorldMapCalculator calc { get { return GetComponent<WorldMapCalculator> () ?? gameObject.AddComponent<WorldMapCalculator> (); } }

		/// <summary>
		/// Enables Ticker component and returns a reference to its API.
		/// </summary>
		public WorldMapTicker ticker { get { return GetComponent<WorldMapTicker> () ?? gameObject.AddComponent<WorldMapTicker> (); } }

		/// <summary>
		/// Enables Decorator component and returns a reference to its API.
		/// </summary>
		public WorldMapDecorator decorator { get { return GetComponent<WorldMapDecorator> () ?? gameObject.AddComponent<WorldMapDecorator> (); } }

		/// <summary>
		/// Enables Editor component and returns a reference to its API.
		/// </summary>
		public WorldMapEditor editor { get { return GetComponent<WorldMapEditor> () ?? gameObject.AddComponent<WorldMapEditor> (); } }

		#endregion


	}

}