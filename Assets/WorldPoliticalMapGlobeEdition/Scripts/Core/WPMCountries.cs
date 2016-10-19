// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define PAINT_MODE
//#define TRACE_CTL

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Poly2Tri;

namespace WPM {

	public partial class WorldMapGlobe : MonoBehaviour {

		#region Internal variables

		// resources
		Material frontiersMat, inlandFrontiersMat;
		Material hudMatCountry;

		// gameObjects
		GameObject countryRegionHighlightedObj;
		GameObject frontiersLayer, inlandFrontiersLayer;

		class FrontierSegment { 
			public Vector3 p0, p1;
			public int repetitions=0, countryIndex; 
			public Region region;
			public FrontierSegment(Vector3 p0, Vector3 p1, int countryIndex, Region region) {
				this.p0 = p0; this.p1 = p1; this.countryIndex = countryIndex; this.region = region;
			}
		}

		// caché and gameObject lifetime control
		Vector3[][] frontiers;
		int[][] frontiersIndices;
		List<Vector3> frontiersPoints;
		Dictionary<int,FrontierSegment> frontiersCacheHit;
		struct CountryRegionRef { public int countryIndex; public int regionIndex; }
		CountryRegionRef[] sortedRegions;

		Vector3[][] inlandFrontiers;
		int[][] inlandFrontiersIndices;
		List<Vector3> inlandFrontiersPoints;

		/// <summary>
		/// Country look up dictionary. Used internally for fast searching of country names.
		/// </summary>
		Dictionary<string, int>_countryLookup;
		int lastCountryLookupCount = -1;
		
		Dictionary<string, int>countryLookup {
			get {
				if (_countryLookup != null && countries.Length == lastCountryLookupCount)
					return _countryLookup;
				if (_countryLookup == null) {
					_countryLookup = new Dictionary<string,int> ();
				} else {
					_countryLookup.Clear ();
				}
				for (int k=0; k<countries.Length; k++)
					_countryLookup.Add (countries [k].name, k);
				lastCountryLookupCount = _countryLookup.Count;
				return _countryLookup;
			}
		}

		#endregion



	#region System initialization

		void ReadCountriesPackedString () {
			lastCountryLookupCount = -1;

			string frontiersFileName = _frontiersDetail == FRONTIERS_DETAIL.Low ? "Geodata/countries110" : "Geodata/countries10";
			TextAsset ta = Resources.Load<TextAsset> (frontiersFileName);
			string s = ta.text;
		
			string[] countryList = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int countryCount = countryList.Length;
//			countries = new Country[countryCount]; 
			List<Country> newCountries = new List<Country>(countryCount);
			List<Vector3> regionPoints = new List<Vector3>(10000);
			List<PolygonPoint> latlons = new List<PolygonPoint>(10000);
			for (int k=0; k<countryCount; k++) {
				string[] countryInfo = countryList [k].Split (new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
				string name = countryInfo [0];
				string continent = countryInfo [1];
				Country country = new Country (name, continent);
				string[] regions;
				if (countryInfo.Length<3) {
					regions = new string[0];
				} else {
					regions = countryInfo [2].Split (new char[] {'*'}, StringSplitOptions.RemoveEmptyEntries);
				}
				int regionCount = regions.Length;
				country.regions = new List<Region> (regionCount);

				float maxArea = 0;
				for (int r=0; r<regionCount; r++) {
					string[] coordinates = regions [r].Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					int coorCount = coordinates.Length;
					Vector2 minMaxLat = new Vector2(float.MaxValue, float.MinValue);
					Vector2 minMaxLon = new Vector2(float.MaxValue, float.MinValue);
					Region countryRegion = new Region (country, country.regions.Count);
					regionPoints.Clear();
					latlons.Clear();
					int regionPointsIndex = -1;
					for (int c=0; c<coorCount; c++) {
						float lat, lon;
						GetLatLonFromPackedString(coordinates[c], out lat, out lon);

						// Convert to sphere coordinates
						Vector3 point = GetSpherePointFromLatLon(lat, lon);
						if (regionPointsIndex>-1 && regionPoints[regionPointsIndex]== point) {
							continue; // In fact, points sometimes repeat so this check is neccesary
						}

						PolygonPoint latlon = new PolygonPoint(lat, lon);
						latlons.Add (latlon);

						if (lat<minMaxLat.x) minMaxLat.x = lat;
						if (lat>minMaxLat.y) minMaxLat.y = lat;
						if (lon<minMaxLon.x) minMaxLon.x = lon;
						if (lon>minMaxLon.y) minMaxLon.y = lon;

						regionPointsIndex++;
						regionPoints.Add (point);
					}
					countryRegion.latlon = latlons.ToArray();
					countryRegion.points = regionPoints.ToArray();
					countryRegion.minMaxLat = minMaxLat;
					countryRegion.minMaxLon = minMaxLon;
					countryRegion.rect2D = GetRect2DFromMinMaxLatLon(minMaxLat, minMaxLon);
					Vector2 midLatLon = new Vector2( (minMaxLat.x + minMaxLat.y)/2, (minMaxLon.x + minMaxLon.y)/2);
					Vector3 normRegionCenter = GetSpherePointFromLatLon(midLatLon.x, midLatLon.y);
					countryRegion.center = normRegionCenter;

					float area = countryRegion.rect2D.width * countryRegion.rect2D.height;
					if (area > maxArea) {
						maxArea = area;
						country.mainRegionIndex = country.regions.Count;
						country.mainRegionArea = countryRegion.rect2D.width * countryRegion.rect2D.height;
						country.center = countryRegion.center;
					}
					country.regions.Add (countryRegion);
				}
				// hidden
				if (countryInfo.Length>=4) {
					int hidden = 0;
					if (int.TryParse(countryInfo[3], out hidden)) {
						country.hidden = hidden>0;
					}
				}
				newCountries.Add(country);
			}

			// Sort by surface area (required to allow small countries inside bigger countries to be detected on mouse over)
			newCountries.Sort( (Country c1, Country c2) => { return c1.mainRegionArea.CompareTo(c2.mainRegionArea); } );
			countries = newCountries.ToArray();

			OptimizeFrontiers ();
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of countries array
		/// </summary>
		public void RefreshCountryDefinition (int countryIndex, List<Region>filterRegions)
		{
			lastCountryLookupCount = -1;
			if (countryIndex >= 0 && countryIndex < countries.Length) {
				float maxArea = 0;
				Country country = countries [countryIndex];
				int regionCount = country.regions.Count;
				for (int r=0; r<regionCount; r++) {
					Vector2 minMaxLat = new Vector2(float.MaxValue, float.MinValue);
					Vector2 minMaxLon = new Vector2(float.MaxValue, float.MinValue);
					Region countryRegion = country.regions[r];
					int coorCount = countryRegion.latlon.Length;
					for (int c=0; c<coorCount; c++) {
						PolygonPoint latlon = countryRegion.latlon[c];
						latlon.Reset();
						float lat = latlon.Xf;
						float lon = latlon.Yf;
						if (lat<minMaxLat.x) minMaxLat.x = lat;
						if (lat>minMaxLat.y) minMaxLat.y = lat;
						if (lon<minMaxLon.x) minMaxLon.x = lon;
						if (lon>minMaxLon.y) minMaxLon.y = lon;
					}
					countryRegion.minMaxLat = minMaxLat;
					countryRegion.minMaxLon = minMaxLon;
					countryRegion.rect2D = GetRect2DFromMinMaxLatLon(minMaxLat, minMaxLon);
					Vector2 midLatLon = new Vector2( (minMaxLat.x + minMaxLat.y)/2, (minMaxLon.x + minMaxLon.y)/2);
					Vector3 normRegionCenter = GetSpherePointFromLatLon(midLatLon.x, midLatLon.y);
					countryRegion.center = normRegionCenter;

					float area = countryRegion.rect2D.width * countryRegion.rect2D.height;
					if (area > maxArea) {
						maxArea = area;
						country.mainRegionIndex = r;
						country.center = countryRegion.center;
					}
				}
			}
			// Refresh latlongs
			if (filterRegions!=null) {
				for (int k=0;k<filterRegions.Count;k++) {
					Region region = filterRegions[k];
					for (int p=0;p<region.latlon.Length;p++)
						region.latlon[p].Reset();
				}
			}
			OptimizeFrontiers (filterRegions);
			DrawFrontiers ();

			if (_showInlandFrontiers) {
				DrawInlandFrontiers();
			}
		}

		/// <summary>
		/// Regenerates frontiers mesh for all countries
		/// </summary>
		public void OptimizeFrontiers () {
			OptimizeFrontiers(null);
		}

		/// <summary>
		/// Generates frontiers mesh for specific regions.
		/// </summary>
		void OptimizeFrontiers (List<Region>filterRegions)
		{
			if (frontiersPoints == null) {
				frontiersPoints = new List<Vector3> (200000);
			} else {
				frontiersPoints.Clear ();
			}
			if (frontiersCacheHit == null) {
				frontiersCacheHit = new Dictionary<int, FrontierSegment> (200000);
			} else {
				frontiersCacheHit.Clear ();
			}
			if (inlandFrontiersPoints == null) {
				inlandFrontiersPoints = new List<Vector3> (200000);
			} else {
				inlandFrontiersPoints.Clear ();
			}
			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				for (int r=0; r<country.regions.Count; r++) {
					Region region = country.regions [r];
					if (filterRegions == null || filterRegions.Contains (region)) {
						region.entity = country;
						region.regionIndex = r;
						region.neighbours.Clear ();
					}
				}
			}

			double t1 = 500;
			int[] roff = new int[] { -1000001, -1000000, -999999, -1, 0, 1, 999999, 1000000, 1000001 };

			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				if (country.hidden) continue;
				int lastRegion = country.regions.Count;
				for (int r=0; r<lastRegion; r++) {
					Region region = country.regions [r];
					if (filterRegions == null || filterRegions.Contains (region)) {
						int max = region.points.Length - 1;
						for (int i = 0; i<=max; i++) {
							PolygonPoint p0, p1;
							if (i<max) {
								p0 = region.latlon [i];
								p1 = region.latlon [i + 1];
							} else {
								p0 = region.latlon [max];
								p1 = region.latlon [0];
							}
							bool isNew = true;

							int hc = (int) (p0.X*t1) + (int)(p1.X*t1) + 1000000*(int)(p0.Y*t1) + 1000000*(int)(p1.Y*t1);
							for (int h=0;h<roff.Length;h++) {
								int hc1 = hc + roff[h];
								if (frontiersCacheHit.ContainsKey (hc1)) {
									if (frontiersCacheHit[hc1].countryIndex!=k) {
										frontiersCacheHit[hc1].repetitions++;
										isNew = false;
										Region neighbour = frontiersCacheHit [hc1].region;
										if (neighbour != region) {
											if (!region.neighbours.Contains (neighbour)) {
												region.neighbours.Add (neighbour);
												neighbour.neighbours.Add (region);
											}
										}
									}
									break;
								}
							}
							if (isNew) {
								// Add frontier segment
								Vector3 v0, v1;
								if (i<max) {
									v0 = region.points [i];
									v1 = region.points [i + 1];
								} else {
									v0 = region.points [max];
									v1 = region.points [0];
								}
								FrontierSegment ifs = new FrontierSegment(v0, v1, k, region);
								frontiersCacheHit.Add (hc, ifs);
								frontiersPoints.Add (v0);
								frontiersPoints.Add (v1);
							}
						}
					}
				}
			}
			
			// Prepare frontiers mesh data
			int meshGroups = (frontiersPoints.Count / 65000) + 1;
			int meshIndex = -1;
			frontiersIndices = new int[meshGroups][];
			frontiers = new Vector3[meshGroups][];
			for (int k=0; k<frontiersPoints.Count; k+=65000) {
				int max = Mathf.Min (frontiersPoints.Count - k, 65000); 
				frontiers [++meshIndex] = new Vector3[max];
				frontiersIndices [meshIndex] = new int[max];
				for (int j=k; j<k+max; j++) {
					frontiers [meshIndex] [j - k] = frontiersPoints [j];
					frontiersIndices [meshIndex] [j - k] = j - k;
				}
			}

			// Prepare inland frontiers mesh data
			List<FrontierSegment> fs = new List<FrontierSegment>(frontiersCacheHit.Values);
			for (int k=0;k<fs.Count;k++) {
				if (fs[k].repetitions==0) {
					if (fs[k].countryIndex!=95 || _frontiersDetail != FRONTIERS_DETAIL.Low) { // Avoid Lesotho - special case
						inlandFrontiersPoints.Add (fs[k].p0);
						inlandFrontiersPoints.Add (fs[k].p1); 
					}
				}
			}
			
			meshGroups = (inlandFrontiersPoints.Count / 65000) + 1;
			meshIndex = -1;
			inlandFrontiersIndices = new int[meshGroups][];
			inlandFrontiers = new Vector3[meshGroups][];
			for (int k=0; k<inlandFrontiersPoints.Count; k+=65000) {
				int max = Mathf.Min (inlandFrontiersPoints.Count - k, 65000); 
				inlandFrontiers [++meshIndex] = new Vector3[max];
				inlandFrontiersIndices [meshIndex] = new int[max];
				for (int j=k; j<k+max; j++) {
					inlandFrontiers [meshIndex] [j - k] = inlandFrontiersPoints [j];
					inlandFrontiersIndices [meshIndex] [j - k] = j - k;
				}
			}
		}

	
	#endregion

	#region Drawing stuff

		
		int GetCacheIndexForCountryRegion (int countryIndex, int regionIndex) {
			return countryIndex * 1000 + regionIndex;
		}
	
		void DrawFrontiers () {
			if (!gameObject.activeInHierarchy || frontiers==null)
				return;
			
			// Create frontiers layer
			Transform t = transform.FindChild ("Frontiers");
			if (t != null)
				DestroyImmediate (t.gameObject);
			frontiersLayer = new GameObject ("Frontiers");
			frontiersLayer.transform.SetParent (transform, false);
			frontiersLayer.transform.localPosition = MiscVector.Vector3zero;
			frontiersLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			frontiersLayer.transform.localScale = _earthInvertedMode ? MiscVector.Vector3one * 0.995f: MiscVector.Vector3one * 1.0001f;

			for (int k=0; k<frontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
			flayer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				flayer.transform.SetParent (frontiersLayer.transform, false);
				flayer.transform.localPosition = MiscVector.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);

				Mesh mesh = new Mesh ();
				mesh.vertices = frontiers [k];
				mesh.SetIndices (frontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;

				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;

				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.useLightProbes = false;
				mr.sharedMaterial = frontiersMat;
			}

			// Toggle frontiers visibility layer according to settings
			frontiersLayer.SetActive (_showFrontiers);
		}


		void DrawInlandFrontiers () {
			if (!gameObject.activeInHierarchy) return;

			Transform t = transform.FindChild ("InlandFrontiers");
			if (t != null)
				DestroyImmediate (t.gameObject);

			if (!_showInlandFrontiers)
				return;

			// Create frontiers layer
			inlandFrontiersLayer = new GameObject ("InlandFrontiers");
			inlandFrontiersLayer.transform.SetParent (transform, false);
			inlandFrontiersLayer.transform.localPosition = MiscVector.Vector3zero;
			inlandFrontiersLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			inlandFrontiersLayer.transform.localScale = _earthInvertedMode ? MiscVector.Vector3one * 0.995f: MiscVector.Vector3one;
			
			for (int k=0; k<inlandFrontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.transform.SetParent (inlandFrontiersLayer.transform, false);
				flayer.transform.localPosition = MiscVector.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
				
				Mesh mesh = new Mesh ();
				mesh.vertices = inlandFrontiers [k];
				mesh.SetIndices (inlandFrontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;
				
				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;
				
				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.useLightProbes = false;
				mr.sharedMaterial = inlandFrontiersMat;
			}
			
			// Toggle frontiers visibility layer according to settings
			inlandFrontiersLayer.SetActive (_showInlandFrontiers);
		}

	#endregion

		
		#region Map Labels
		
		/// <summary>
		/// Forces redraw of all labels.
		/// </summary>
		public void RedrawMapLabels() {
			DestroyMapLabels();
			DrawMapLabels();
		}
		
		/// <summary>
		/// Draws the map labels. Note that it will update cached textmesh objects if labels are already drawn.
		/// </summary>
		void DrawMapLabels () { 
			
			GameObject textRoot = null;
			
			// Set colors
			labelsFont.material.color = _countryLabelsColor;
			labelsShadowMaterial.color = _countryLabelsShadowColor;
			
			// Create texts
			GameObject overlay = GetOverlayLayer (true);
			Transform t = overlay.transform.FindChild ("TextRoot");
			if (t == null) {
				textRoot = new GameObject ("TextRoot");
				textRoot.layer = overlay.layer;
			} else {
				textRoot = t.gameObject;
				textRoot.transform.SetParent(null);
			}
			textRoot.transform.localPosition = new Vector3 (0, 0, -0.001f);
			textRoot.transform.rotation = Quaternion.Euler(MiscVector.Vector3zero);		  // needs rotation to be 0,0,0 for getting correct bounds sizes - it's fixed at the end of the method
			textRoot.transform.localScale = MiscVector.Vector3one;
			
			List<MeshRect> meshRects = new List<MeshRect> ();
			for (int countryIndex=0; countryIndex<countries.Length; countryIndex++) {
				Country country = countries [countryIndex];
				if (country.hidden ||  !country.labelVisible) continue;
				
				Vector2 center = Drawing.SphereToBillboardCoordinates (country.center) + country.labelOffset;
				Region region = country.regions [country.mainRegionIndex];
				
				// Special countries adjustements
				if (_frontiersDetail == FRONTIERS_DETAIL.Low) {
					switch (countryIndex) {
					case 135: // Russia
						center.y ++;
						center.x -= 8;
						break;
					case 6: // Antartica
						center.y += 9f;
						break;
					case 65: // Greenland
						center.y -= 2f;
						break;
					case 22: // Brazil
						center.y += 4f;
						center.x += 1.0f;
						break;
					case 73: // India
						center.x -= 2f;
						break;
					case 168: // USA
						center.x -= 1f;
						break;
					case 27: // Canada
						center.x -= 3f;
						break;
					case 30: // China
						center.x -= 1f;
						center.y -= 2f;
						break;
					}
				} else {
					switch (countryIndex) {
					case 122: // Russia
						center.y -= 4f;
						break;
					case 22: // Antartica
						center.y += 9f;
						break;
					case 72: // Brazil
						center.y += 4f;
						break;
					}
				}
				
				// Adjusts country name length
				string countryName = country.customLabel != null ? country.customLabel : country.name.ToUpper();
				bool introducedCarriageReturn = false;
				if (countryName.Length > 15) {
					int spaceIndex = countryName.IndexOf (' ', countryName.Length / 2);
					if (spaceIndex >= 0) {
						countryName = countryName.Substring (0, spaceIndex) + "\n" + countryName.Substring (spaceIndex + 1);
						introducedCarriageReturn = true;
					}
				}
				
				// add caption
				GameObject textObj;
				TextMesh tm;
				Renderer textRenderer;
				if (country.labelGameObject == null) {
					Color labelColor = country.labelColorOverride ? country.labelColor : _countryLabelsColor;
					Font customFont = country.labelFontOverride ?? labelsFont;
					Material customLabelShadowMaterial = country.labelFontShadowMaterial ?? labelsShadowMaterial;
					tm = Drawing.CreateText (countryName, null, overlay.layer, center, customFont, labelColor, _showLabelsShadow, customLabelShadowMaterial, _countryLabelsShadowColor);
					textObj = tm.gameObject;
					country.labelGameObject = tm;
					textRenderer = textObj.GetComponent<Renderer>();
					Bounds bounds = textRenderer.bounds;
					country.labelMeshWidth = bounds.size.x;
					country.labelMeshHeight = bounds.size.y;
					country.labelMeshCenter = center;
					textObj.transform.SetParent(textRoot.transform, false);
					textObj.transform.localPosition = center;
					textObj.layer = textRoot.gameObject.layer;
					if (_showLabelsShadow) {
						country.labelShadowGameObject = textObj.transform.FindChild("shadow").GetComponent<TextMesh>();
						country.labelShadowGameObject.gameObject.layer = textObj.layer;
					}
				} else {
					tm = country.labelGameObject;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
					textRenderer = textObj.GetComponent<Renderer>();
				}
				
				float meshWidth = country.labelMeshWidth;
				float meshHeight = country.labelMeshHeight;
				
				// adjusts caption
				Rect rect = region.rect2D;
				float absoluteHeight;
				if (country.labelRotation>0) {
					textObj.transform.localRotation = Quaternion.Euler (0, 0, country.labelRotation);
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				} else if (rect.height > rect.width * 1.45f) {
					float angle;
					if (rect.height > rect.width * 1.5f) {
						angle = 90;
					} else {
						angle = Mathf.Atan2 (rect.height, rect.width) * Mathf.Rad2Deg;
					}
					textObj.transform.localRotation = Quaternion.Euler (0, 0, angle);
					absoluteHeight = Mathf.Min (rect.width * _countryLabelsSize, rect.height);
				} else {
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				}
				
				// adjusts scale to fit width in rect
				float adjustedMeshHeight = introducedCarriageReturn ? meshHeight * 0.5f : meshHeight;
				float scale = absoluteHeight / adjustedMeshHeight;
				float desiredWidth = meshWidth * scale;
				if (desiredWidth > rect.width) {
					scale = rect.width / meshWidth;
				}
				if (adjustedMeshHeight * scale < _countryLabelsAbsoluteMinimumSize) {
					scale = _countryLabelsAbsoluteMinimumSize / adjustedMeshHeight;
				}
				
				// stretchs out the caption
				float displayedMeshWidth = meshWidth * scale;
				float displayedMeshHeight = meshHeight * scale;
				string wideName;
				int times = Mathf.FloorToInt (rect.width * 0.45f / (meshWidth * scale));
				if (times > 10)
					times = 10;
				if (times > 0) {
					StringBuilder sb = new StringBuilder ();
					string spaces = new string (' ', times * 2);
					for (int c=0; c<countryName.Length; c++) {
						sb.Append (countryName [c]);
						if (c < countryName.Length - 1) {
							sb.Append (spaces);
						}
					}
					wideName = sb.ToString ();
				} else {
					wideName = countryName;
				}
				
				if (tm.text.Length != wideName.Length) {
					tm.text = wideName;
					// bounds has changed
					country.labelMeshWidth = textRenderer.bounds.size.x; 
					country.labelMeshHeight = textRenderer.bounds.size.y;
					displayedMeshWidth = country.labelMeshWidth * scale;	
					displayedMeshHeight = country.labelMeshHeight * scale;
					if (_showLabelsShadow) {
						textObj.transform.FindChild ("shadow").GetComponent<TextMesh> ().text = wideName;
					}
				}
				
				// apply scale
				textObj.transform.localScale = new Vector3 (scale, scale, 1);

				// Cache position of the label rect in the sphere, used for fader.
				Vector2 minMaxPlaneY = new Vector2(textObj.transform.localPosition.y - displayedMeshHeight * 0.5f, textObj.transform.localPosition.y + displayedMeshHeight * 0.5f);   // country.regions[country.mainRegionIndex].minMaxLat;
				Vector2 labelLatLonTop = GetLatLonFromBillboard(new Vector2(textObj.transform.localPosition.x, minMaxPlaneY.y));
				Vector2 labelLatLonBottom = GetLatLonFromBillboard(new Vector2(textObj.transform.localPosition.x, minMaxPlaneY.x));
				country.labelSphereEdgeTop = GetSpherePointFromLatLon(labelLatLonTop.x, labelLatLonTop.y);
				country.labelSphereEdgeBottom = GetSpherePointFromLatLon(labelLatLonBottom.x, labelLatLonBottom.y);

				// Save mesh rect for overlapping checking
				if (country.labelOffset == MiscVector.Vector2zero) {
					MeshRect mr = new MeshRect (countryIndex, new Rect (center.x - displayedMeshWidth * 0.5f, center.y - displayedMeshHeight * 0.5f, displayedMeshWidth, displayedMeshHeight));
					meshRects.Add (mr);
				}
			}
			
			// Simple-fast overlapping checking
			int cont = 0;
			bool needsResort = true;
			
			while (needsResort && ++cont<10) {
				meshRects.Sort (overlapComparer);
				
				for (int c=1; c<meshRects.Count; c++) {
					Rect thisMeshRect = meshRects [c].rect;
					for (int prevc=c-1; prevc>=0; prevc--) {
						Rect otherMeshRect = meshRects [prevc].rect;
						if (thisMeshRect.Overlaps (otherMeshRect)) {
							needsResort = true;
							int thisCountryIndex = meshRects [c].countryIndex;
							Country country = countries [thisCountryIndex];
							GameObject thisLabel = country.labelGameObject.gameObject;
							
							// displaces this label
							float offsety = (thisMeshRect.yMax - otherMeshRect.yMin);
							offsety = Mathf.Min (country.regions[country.mainRegionIndex].rect2D.height * 0.35f, offsety);
							thisLabel.transform.localPosition = new Vector3 (country.labelMeshCenter.x, country.labelMeshCenter.y - offsety, thisLabel.transform.localPosition.z);
							thisMeshRect = new Rect (thisLabel.transform.localPosition.x - thisMeshRect.width * 0.5f,
							                         thisLabel.transform.localPosition.y - thisMeshRect.height * 0.5f,
							                         thisMeshRect.width, thisMeshRect.height);
							meshRects [c].rect = thisMeshRect;
						}
					}
				}
			}
			
			textRoot.transform.SetParent (overlay.transform, false);
			textRoot.transform.localPosition = MiscVector.Vector3zero;
			textRoot.transform.localRotation = Quaternion.Euler(MiscVector.Vector3zero);
		}
		
		int overlapComparer (MeshRect r1, MeshRect r2) {
			return (r2.rect.center.y).CompareTo (r1.rect.center.y);
		}
		
		class MeshRect {
			public int countryIndex;
			public Rect rect;
			
			public MeshRect (int countryIndex, Rect rect) {
				this.countryIndex = countryIndex;
				this.rect = rect;
			}
		}
		
		void DestroyMapLabels () {
			#if TRACE_CTL			
			Debug.Log ("CTL " + DateTime.Now + ": destroy labels");
			#endif
			if (countries != null) {
				for (int k=0; k<countries.Length; k++) {
					if (countries [k].labelGameObject != null) {
						DestroyImmediate (countries [k].labelGameObject);
						countries [k].labelGameObject = null;
					}
				}
			}
			// Security check: if there're still gameObjects under TextRoot, also delete it
			if (overlayLayer != null) {
				Transform t = overlayLayer.transform.FindChild ("TextRoot");
				if (t != null && t.childCount > 0) {
					DestroyImmediate (t.gameObject);
				}
			}
		}

		void FadeCountryLabels() {
			
			// Automatically fades in/out country labels based on their screen size
			if (!_showCountryNames) return;

			float maxAlpha = _countryLabelsColor.a;
			float maxAlphaShadow = _countryLabelsShadowColor.a;
			for (int k=0;k<countries.Length;k++) {
				Country country = countries[k];
				TextMesh tm = country.labelGameObject;
				if (tm!=null) {
					// Fade label
					float ad = 1f;
					if (_countryLabelsEnableAutomaticFade && !_earthInvertedMode)  {
						Vector2 lc0 = Camera.main.WorldToScreenPoint(transform.TransformPoint(country.labelSphereEdgeBottom));
						Vector2 lc1 = Camera.main.WorldToScreenPoint(transform.TransformPoint(country.labelSphereEdgeTop));
						float screenHeight = Vector2.Distance(lc0, lc1) / Camera.main.pixelHeight;
						if (screenHeight<_countryLabelsAutoFadeMinHeight) {
							ad = Mathf.Lerp(1.0f, 0, (_countryLabelsAutoFadeMinHeight - screenHeight) / _countryLabelsAutoFadeMinHeightFallOff);
						} else if (screenHeight>_countryLabelsAutoFadeMaxHeight) {
							ad = Mathf.Lerp (1.0f, 0, (screenHeight - _countryLabelsAutoFadeMaxHeight) / _countryLabelsAutoFadeMaxHeightFallOff);
						}
					}
					bool enableRendering = ad > 0;
					TextMesh tmShadow = country.labelShadowGameObject;
					Renderer mr = tm.transform.GetComponent<Renderer>();
					if (mr.enabled != enableRendering) {
						mr.enabled = enableRendering;
						if (tmShadow!=null) {
							tmShadow.GetComponent<Renderer>().enabled = enableRendering;
						}
					}
					if (enableRendering) {
						float newAlpha = ad * maxAlpha;
						if (tm.color.a != newAlpha) {
							tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, newAlpha);
						}
						// Fade label shadow
						if (tmShadow!=null) {
							newAlpha = ad * maxAlphaShadow;
							if ( tmShadow.color.a != newAlpha) {
								tmShadow.color = new Color(tmShadow.color.r, tmShadow.color.g, tmShadow.color.b, newAlpha);
							}
						}
					}
				}
			}
		}

		#endregion


	#region Country highlighting

	
		bool GetCountryUnderMouse (Vector3 spherePoint, out int countryIndex, out int regionIndex)
		{
			PolygonPoint latlonPos = GetLatLonFromSpherePoint(spherePoint);
			int currentCountry = countries.Length;
			// Check if current country is still under mouse
			if (_countryHighlightedIndex>=0 && _countryRegionHighlightedIndex>=0) {
				Region region = countries[_countryHighlightedIndex].regions[_countryRegionHighlightedIndex];
				if (region.ContainsPoint (latlonPos.X, latlonPos.Y)) {
//					if (ContainsPoint2D (region.latlon, latlonPos.X, latlonPos.Y)) {
					currentCountry = _countryHighlightedIndex+1; // don't check for bigger countries since this is at least still highlighted - just check if any other smaller country inside this one can be highlighted
				}
			}

			// Check other countries
			countryIndex = regionIndex = -1;

			// Get regions list sorted by distance from mouse point
			Vector2 mouseBillboardPos = Drawing.SphereToBillboardCoordinates(spherePoint);
			if (sortedRegions==null) sortedRegions = new CountryRegionRef[100];
			int regionsAdded = -1;
			for (int c=0; c<currentCountry; c++) {
				if (countries[c].hidden) continue;
				for (int cr=0; cr<countries[c].regions.Count; cr++) {
					Region region = countries [c].regions [cr];
					if (region.rect2D.Contains(mouseBillboardPos)) {
						++regionsAdded;
						sortedRegions[regionsAdded].countryIndex = c;
						sortedRegions[regionsAdded].regionIndex = cr;
					}
				}
			}
			if (regionsAdded<0) return false;

			if (regionsAdded==0 && sortedRegions[0].countryIndex == _countryHighlightedIndex && sortedRegions[0].regionIndex == _countryRegionHighlightedIndex) {
				Region region = countries[_countryHighlightedIndex].regions[_countryRegionHighlightedIndex];
				if (region.ContainsPoint(latlonPos.X, latlonPos.Y)) {
//					if (ContainsPoint2D(region.latlon, latlonPos.X, latlonPos.Y)) {
					// we're highlighting the same country - return it
					countryIndex = _countryHighlightedIndex;
					regionIndex = _countryRegionHighlightedIndex;
					return true;
				} else {
					return false;
				}
			}

			for (int t=0;t<=regionsAdded;t++) {
				// Check if this region is visible and the mouse is inside
				int c = sortedRegions[t].countryIndex;
				int cr = sortedRegions[t].regionIndex;
				Region region = countries[c].regions[cr];
				if (region.ContainsPoint (latlonPos.X, latlonPos.Y)) {
//					if (ContainsPoint2D (region.latlon, latlonPos.X, latlonPos.Y)) {
					countryIndex = c;
					regionIndex = cr;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Disables all country regions highlights. This doesn't remove custom materials.
		/// </summary>
		public void HideCountryRegionHighlights (bool destroyCachedSurfaces) {
			HideCountryRegionHighlight();
			if (countries==null) return;
			for (int c=0;c<countries.Length;c++) {
				Country country = countries[c];
				if (country==null || country.regions == null) continue;
				for (int cr=0;cr<country.regions.Count;cr++) {
					Region region = country.regions[cr];
					int cacheIndex = GetCacheIndexForCountryRegion(c, cr);
					if (surfaces.ContainsKey(cacheIndex)) {
						GameObject surf = surfaces[cacheIndex];
						if (surf==null) {
							surfaces.Remove(cacheIndex);
						} else {
							if (destroyCachedSurfaces) {
								surfaces.Remove(cacheIndex);
								DestroyImmediate(surf);
							} else {
								if (region.customMaterial==null) {
									surf.SetActive(false);
								} else {
									ApplyMaterialToSurface (surf, region.customMaterial);
								}
							}
						}
					}
				}
			}
		}
		
		void HideCountryRegionHighlight () {
			HideProvinces();
			HideCityHighlight();
			if (_countryRegionHighlighted!=null && countryRegionHighlightedObj != null) {
				if (_countryRegionHighlighted!=null && _countryRegionHighlighted.customMaterial!=null) {
					ApplyMaterialToSurface (countryRegionHighlightedObj, _countryRegionHighlighted.customMaterial);
				} else {
					countryRegionHighlightedObj.SetActive (false);
				}
				countryRegionHighlightedObj = null;
			}
			// Raise exit event
			if (OnCountryExit!=null && _countryHighlightedIndex>=0) OnCountryExit(_countryHighlightedIndex, _countryRegionHighlightedIndex);

			_countryHighlighted = null;
			_countryHighlightedIndex = -1;
			_countryRegionHighlighted = null;
			_countryRegionHighlightedIndex = -1;
		}


		public GameObject HighlightCountryRegion (int countryIndex, int regionIndex, bool refreshGeometry, bool drawOutline) {
#if PAINT_MODE
			ToggleCountrySurface(countryIndex, true, Color.white);
			return null; 
#else
			if (countryRegionHighlightedObj!=null) {
				if (countryIndex == _countryHighlightedIndex && regionIndex == _countryRegionHighlightedIndex && !refreshGeometry) return countryRegionHighlightedObj;
				HideCountryRegionHighlight();
			}
			if (countryIndex<0 || countryIndex>=countries.Length || regionIndex<0 || regionIndex>=countries[countryIndex].regions.Count) return null;
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = surfaces [cacheIndex];
				surfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			if (_enableCountryHighlight) {
			if (existsInCache) {
				countryRegionHighlightedObj = surfaces [cacheIndex];
				if (countryRegionHighlightedObj==null) {
					surfaces.Remove(cacheIndex);
				} else {
					if (!countryRegionHighlightedObj.activeSelf)
						countryRegionHighlightedObj.SetActive (true);
					Renderer rr = countryRegionHighlightedObj.GetComponent<Renderer> ();
					if (rr.sharedMaterial!=hudMatCountry)
						rr.sharedMaterial = hudMatCountry;
				}
			} else {
				countryRegionHighlightedObj = GenerateCountryRegionSurface (countryIndex, regionIndex, hudMatCountry, MiscVector.Vector2one, MiscVector.Vector2zero, 0, drawOutline);
			}
			}
			_countryHighlightedIndex = countryIndex;
			_countryRegionHighlighted = countries[countryIndex].regions[regionIndex];
			_countryRegionHighlightedIndex = regionIndex;
			_countryHighlighted = countries[countryIndex];

			return countryRegionHighlightedObj;
#endif

		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, bool drawOutline) {
			return GenerateCountryRegionSurface(countryIndex, regionIndex, material, MiscVector.Vector2one, MiscVector.Vector2zero, 0, drawOutline);
		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool drawOutline) {
			Country country = countries[countryIndex];
			Region region = country.regions [regionIndex];
		
			Polygon poly = new Polygon(region.latlon);

			double maxTriangleSize = 10.0f;
			if (_frontiersDetail == FRONTIERS_DETAIL.Low && countryIndex == 6) { // Antarctica
				maxTriangleSize = 9.0f;
			}
			if ( Mathf.Abs (region.minMaxLat.x - region.minMaxLat.y) > maxTriangleSize || 
			    Mathf.Abs (region.minMaxLon.x - region.minMaxLon.y) > maxTriangleSize) { // special case; needs steiner points to reduce the size of triangles
				double step = maxTriangleSize/2;
				List<TriangulationPoint> steinerPoints = new List<TriangulationPoint>();
				for (double x = region.minMaxLat.x + step/2; x<region.minMaxLat.y - step/2;x += step) {
					for (double y = region.minMaxLon.x + step /2;y<region.minMaxLon.y - step / 2;y += step) {
//						if (ContainsPoint2D(region.latlon, x, y)) {
							if (region.ContainsPoint(x, y)) {
							steinerPoints.Add(new TriangulationPoint(x, y));
						}
					}
				}
				poly.AddSteinerPoints(steinerPoints);
			}

			P2T.Triangulate(poly);

			int flip1, flip2;
			if (_earthInvertedMode) {
				flip1 = 2; flip2 = 1;
			} else {
				flip1 = 1; flip2 = 2;
			}
			Vector3[] revisedSurfPoints = new Vector3[poly.Triangles.Count*3];
			for (int k=0;k<poly.Triangles.Count;k++) {
				DelaunayTriangle dt = poly.Triangles[k];
				revisedSurfPoints[k*3] = GetSpherePointFromLatLon(dt.Points[0].X, dt.Points[0].Y);
				revisedSurfPoints[k*3+flip1] = GetSpherePointFromLatLon(dt.Points[1].X, dt.Points[1].Y);
				revisedSurfPoints[k*3+flip2] = GetSpherePointFromLatLon(dt.Points[2].X, dt.Points[2].Y);
			}
			int revIndex = revisedSurfPoints.Length-1;

			// Generate surface mesh
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			string cacheIndexSTR = cacheIndex.ToString();
			Transform t = surfacesLayer.transform.FindChild(cacheIndexSTR);
			if (t!=null) DestroyImmediate(t.gameObject);
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, revisedSurfPoints, revIndex, material, region.rect2D, textureScale, textureOffset, textureRotation);									
			surf.transform.SetParent (surfacesLayer.transform, false);
			surf.transform.localPosition = MiscVector.Vector3zero;
			surf.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
			if (_earthInvertedMode) {
				surf.transform.localScale = MiscVector.Vector3one * 0.998f;
			}
			if (surfaces.ContainsKey(cacheIndex)) surfaces.Remove(cacheIndex);
			surfaces.Add (cacheIndex, surf);

			// draw outline
			if (drawOutline) {
				DrawCountryRegionOutline(region, surf);
			}
			return surf;
		}

		/// <summary>
		/// Draws the country outline.
		/// </summary>
		GameObject DrawCountryRegionOutline (Region region, GameObject surf)
		{
			int[] indices = new int[region.points.Length + 1];
			Vector3[] outlinePoints = new Vector3[region.points.Length + 1];
			for (int k=0; k<region.points.Length; k++) {
				indices [k] = k;
				outlinePoints [k] = region.points [k]; // + (region.points [k] - region.center).normalized * 0.0001f;
			}
			indices [region.points.Length] = indices [0];
			outlinePoints [region.points.Length] = region.points [0]; // + (region.points [0] - region.center).normalized * 0.0001f;

			GameObject boldFrontiers = new GameObject ("boldFrontiers");
			boldFrontiers.transform.SetParent (surf.transform, false);
			boldFrontiers.transform.localPosition = MiscVector.Vector3zero;
			boldFrontiers.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
//				boldFrontiers.transform.localScale = _earthInvertedMode ?  MiscVector.Vector3one * (1.0f + 0.998f) : MiscVector.Vector3one * (1.0f + 0.001f);
			
			Mesh mesh = new Mesh ();
			mesh.vertices = outlinePoints; //region.points;

			mesh.SetIndices (indices, MeshTopology.LineStrip, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				
			MeshFilter mf = boldFrontiers.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
				
			MeshRenderer mr = boldFrontiers.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.useLightProbes = false;
			mr.sharedMaterial = outlineMat;

			return boldFrontiers;
		}

		#endregion

	
	}

}