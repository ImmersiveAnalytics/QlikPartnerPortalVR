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
		const string PROVINCES_BORDERS_LAYER = "Provinces";

		// resources
		Material provincesMat;
		Material hudMatProvince;

		// gameObjects
		GameObject provincesLayer;
		GameObject provinceRegionHighlightedObj;
		GameObject provinceCountryOutlineRef;	// maintains a reference to the country outline to hide it in provinces mode when mouse exits the country

		// caché and gameObject lifetime control
		public Vector3[][] provinceFrontiers;
		public int[][] provinceFrontiersIndices;
		public List<Vector3> provinceFrontiersPoints;
		public Dictionary<Vector3,Region> provinceFrontiersCacheHit;

		Dictionary<Province, int>_provinceLookup;
		int lastProvinceLookupCount = -1;

		Dictionary<Province, int>provinceLookup {
			get {
				if (_provinceLookup != null && provinces.Length == lastProvinceLookupCount)
					return _provinceLookup;
				if (_provinceLookup == null) {
					_provinceLookup = new Dictionary<Province,int> ();
				} else {
					_provinceLookup.Clear ();
				}
				for (int k=0; k<provinces.Length; k++) {
					_provinceLookup.Add (provinces[k], k);
				}
				lastProvinceLookupCount = provinces.Length;
				return _provinceLookup;
			}
		}

		#endregion



	#region System initialization

		void ReadProvincesPackedString () {

			if (GetComponent<WPM_Editor.WorldMapEditor>()==null && !_showProvinces)
				return;

			TextAsset ta = Resources.Load<TextAsset> ("Geodata/provinces10");
			string s = ta.text;

			string[] provincesPackedStringData = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int provinceCount = provincesPackedStringData.Length;
			List<Province>newProvinces = new List<Province>(provinceCount);
			List<Province>[] countryProvinces = new List<Province>[countries.Length];
			for (int k=0; k<provinceCount; k++) {
				string[] provinceInfo = provincesPackedStringData [k].Split (new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
				if (provinceInfo.Length <= 2)
					continue;
				string name = provinceInfo [0];
				string countryName = provinceInfo [1];
				int countryIndex = GetCountryIndex(countryName);
				if (countryIndex >= 0) {
					Province province = new Province (name, countryIndex);
					province.packedRegions = provinceInfo [2];
					newProvinces.Add (province);
					if (countryProvinces[countryIndex]==null)
						countryProvinces[countryIndex] = new List<Province>(50);
					countryProvinces[countryIndex].Add (province);
				}
			}
			provinces = newProvinces.ToArray ();
			lastProvinceLookupCount = -1;
			for (int k=0;k<countries.Length;k++) {
				if (countryProvinces[k]!=null) {
					countries[k].provinces = countryProvinces[k].ToArray();
				} 
			}
		}

		public void ReadProvincePackedString (Province province) {
			string[] regions = province.packedRegions.Split (new char[] {'*'}, StringSplitOptions.RemoveEmptyEntries);
			int regionCount = regions.Length;
			float maxArea = 0;
			province.regions = new List<Region> (regionCount);
			List<Vector3> regionPoints = new List<Vector3>(10000);
			List<PolygonPoint> latlons = new List<PolygonPoint>(10000);
			for (int r=0; r<regionCount; r++) {
				string[] coordinates = regions [r].Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				int coorCount = coordinates.Length;
				Vector2 minMaxLat = new Vector2(float.MaxValue, float.MinValue);
				Vector4 minMaxLon = new Vector2(float.MaxValue, float.MinValue);
				Region provinceRegion = new Region (province, province.regions.Count);
				provinceRegion.points = new Vector3[coorCount];
				provinceRegion.latlon = new PolygonPoint[coorCount];
				regionPoints.Clear();
				latlons.Clear();
				int regionPointsIndex = -1;
				for (int c=0; c<coorCount; c++) {
					float lat, lon;
					GetLatLonFromPackedString(coordinates[c], out lat, out lon);

					// Convert to sphere coordinates
					Vector3 point = GetSpherePointFromLatLon(lat, lon);
					if (regionPointsIndex>-1 && regionPoints[regionPointsIndex]== point) continue;

					regionPointsIndex++;
					regionPoints.Add (point);
					latlons.Add (new PolygonPoint(lat, lon));

					if (lat<minMaxLat.x) minMaxLat.x = lat;
					if (lat>minMaxLat.y) minMaxLat.y = lat;
					if (lon<minMaxLon.x) minMaxLon.x = lon;
					if (lon>minMaxLon.y) minMaxLon.y = lon;
				}
				provinceRegion.points = regionPoints.ToArray();
				provinceRegion.latlon = latlons.ToArray();
				provinceRegion.minMaxLat = minMaxLat;
				provinceRegion.minMaxLon = minMaxLon;
				provinceRegion.rect2D = GetRect2DFromMinMaxLatLon(minMaxLat, minMaxLon);
				Vector2 midLatLon = new Vector2( (minMaxLat.x + minMaxLat.y)/2, (minMaxLon.x + minMaxLon.y)/2);
				Vector3 normRegionCenter = GetSpherePointFromLatLon(midLatLon.x, midLatLon.y);
				provinceRegion.center = normRegionCenter; 

				float area = provinceRegion.rect2D.size.sqrMagnitude;
				if (area > maxArea) {
					maxArea = area;
					province.mainRegionIndex = r;
					province.mainRegionArea = provinceRegion.rect2D.width * provinceRegion.rect2D.height;
					province.center = provinceRegion.center;
				}
				province.regions.Add (provinceRegion);
			}
//			Debug.Log ("Total read: " + (DateTime.Now - start).TotalMilliseconds);
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of provinces array
		/// </summary>
		public void RefreshProvinceDefinition (int provinceIndex) {
			lastProvinceLookupCount = -1;
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return;
			float maxArea = 0;
			Province province = provinces [provinceIndex];
			if (province.regions==null) ReadProvincePackedString(province); // safe check for specific map editor situations
			int regionCount = province.regions.Count;
			for (int r=0; r<regionCount; r++) {
				Vector2 minMaxLat = new Vector2(float.MaxValue, float.MinValue);
				Vector4 minMaxLon = new Vector2(float.MaxValue, float.MinValue);
				Region provinceRegion = province.regions[r];
				provinceRegion.entity = province;
				provinceRegion.regionIndex = r;
				int coorCount = provinceRegion.latlon.Length;
				for (int c=0; c<coorCount; c++) {
					PolygonPoint latlon = provinceRegion.latlon[c];
					latlon.Reset();
					float lat = latlon.Xf;
					float lon = latlon.Yf;
					if (lat<minMaxLat.x) minMaxLat.x = lat;
					if (lat>minMaxLat.y) minMaxLat.y = lat;
					if (lon<minMaxLon.x) minMaxLon.x = lon;
					if (lon>minMaxLon.y) minMaxLon.y = lon;
				}
				provinceRegion.minMaxLat = minMaxLat;
				provinceRegion.minMaxLon = minMaxLon;
				provinceRegion.rect2D = GetRect2DFromMinMaxLatLon(minMaxLat, minMaxLon);
				Vector2 midLatLon = new Vector2( (minMaxLat.x + minMaxLat.y)/2, (minMaxLon.x + minMaxLon.y)/2);
				Vector3 normRegionCenter = GetSpherePointFromLatLon(midLatLon.x, midLatLon.y);
				provinceRegion.center = normRegionCenter; 
			
				float area = provinceRegion.rect2D.size.sqrMagnitude;
				if (area > maxArea) {
					maxArea = area;
					province.mainRegionIndex = r;
					province.center = provinceRegion.center;
				}
			}
			DrawProvinces (provinces [provinceIndex].countryIndex, true, true);
		}
		
	
	#endregion

	#region Drawing stuff

		/// <summary>
		/// Draws the provinces for specified country and optional also neighbours'
		/// </summary>
		/// <returns><c>true</c>, if provinces was drawn, <c>false</c> otherwise.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="includeNeighbours">If set to <c>true</c> include neighbours.</param>
		public bool DrawProvinces (int countryIndex, bool includeNeighbours, bool forceRefresh) {
			if (!gameObject.activeInHierarchy || provinces == null || (countryProvincesDrawnIndex == countryIndex && !forceRefresh))
				return false;
			countryProvincesDrawnIndex = countryIndex;

			// Create province layer if needed
			Transform t = transform.FindChild (PROVINCES_BORDERS_LAYER);
			if (forceRefresh && t!=null) {
				DestroyImmediate(t.gameObject);
				t = null;
			}
			if (t != null) {
				// Deactivate all children
				for (int tt=0;tt<t.childCount;tt++) {
					t.GetChild(tt).gameObject.SetActive(false);
				}
				provincesLayer = t.gameObject;
			} else {
				provincesLayer = new GameObject (PROVINCES_BORDERS_LAYER);
				provincesLayer.transform.SetParent (transform, false);
				provincesLayer.transform.localPosition = MiscVector.Vector3zero;
				provincesLayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
//				provincesLayer.transform.localScale = _earthInvertedMode ? MiscVector.Vector3one * 0.995f: MiscVector.Vector3one*1.001f;	// 1.001 multiplier not needed since Offset tag in shader
				provincesLayer.transform.localScale = _earthInvertedMode ? MiscVector.Vector3one * 0.995f: MiscVector.Vector3one;
			}
			provincesLayer.SetActive(true);

			// Check if mesh is created but inactive
			Transform pmesh = provincesLayer.transform.FindChild(countryIndex.ToString());
			if (pmesh!=null) {
				pmesh.gameObject.SetActive(true);
			} else {
				CreateProvincesMesh(countryIndex, includeNeighbours);
			}
			return true;
		}

		void CreateProvincesMesh(int countryIndex, bool includeNeighbours) {

			// prepare a list with the countries to be drawn
			countryProvincesDrawnIndex = countryIndex;
			List<Country> targetCountries = new List<Country> (20);
			// add selected country
			targetCountries.Add (countries [countryIndex]);
			// add neighbour countries?
			if (includeNeighbours) {
				for (int k=0; k<countries[countryIndex].regions.Count; k++) {
					List<Region> neighbours = countries [countryIndex].regions [k].neighbours;
					for (int n=0; n<neighbours.Count; n++) {
						Country c = (Country)neighbours [n].entity;
						if (!targetCountries.Contains (c))
							targetCountries.Add (c);
					}
				}
			}

			if (provinceFrontiersPoints == null) {
				provinceFrontiersPoints = new List<Vector3> (200000);
			} else {
				provinceFrontiersPoints.Clear ();
			}
			if (provinceFrontiersCacheHit == null) {
				provinceFrontiersCacheHit = new Dictionary<Vector3, Region> (200000);
			} else {
				provinceFrontiersCacheHit.Clear ();
			}

			for (int c=0;c<targetCountries.Count;c++) {
				Country targetCountry = targetCountries[c];
				if (targetCountry.provinces==null) continue;
				for (int p=0; p<targetCountry.provinces.Length; p++) {
					Province province = targetCountry.provinces [p];
					if (province.regions == null) { // read province data the first time we need it
						ReadProvincePackedString (province);
					}
					for (int r=0; r<province.regions.Count; r++) {
						Region region = province.regions [r];
						region.entity = province;
						region.regionIndex = r;
						region.neighbours.Clear ();
						int max = region.points.Length - 1;
						for (int i = 0; i<max; i++) {
							Vector3 p0 = region.points [i];
							Vector3 p1 = region.points [i + 1];
							Vector3 hc = p0 + p1;
							if (provinceFrontiersCacheHit.ContainsKey (hc)) {
								Region neighbour = provinceFrontiersCacheHit [hc];
								if (neighbour != region) {
									if (!region.neighbours.Contains (neighbour)) {
										region.neighbours.Add (neighbour);
										neighbour.neighbours.Add (region);
									}
								}
							} else {
								provinceFrontiersCacheHit.Add (hc, region);
								provinceFrontiersPoints.Add (p0);
								provinceFrontiersPoints.Add (p1);
							}
						}
						// Close the polygon
						provinceFrontiersPoints.Add (region.points [max]);
						provinceFrontiersPoints.Add (region.points [0]);
					}
				}
			}
			
			int meshGroups = (provinceFrontiersPoints.Count / 65000) + 1;
			int meshIndex = -1;
			provinceFrontiersIndices = new int[meshGroups][];
			provinceFrontiers = new Vector3[meshGroups][];
			for (int k=0; k<provinceFrontiersPoints.Count; k+=65000) {
				int max = Mathf.Min (provinceFrontiersPoints.Count - k, 65000); 
				provinceFrontiers [++meshIndex] = new Vector3[max];
				provinceFrontiersIndices [meshIndex] = new int[max];
				for (int j=k; j<k+max; j++) {
					provinceFrontiers [meshIndex] [j - k] = provinceFrontiersPoints [j];
					provinceFrontiersIndices [meshIndex] [j - k] = j - k;
				}
			}

			// Create province borders container
			GameObject provinceObj = new GameObject (countryIndex.ToString());
			provinceObj.transform.SetParent (provincesLayer.transform, false);
			provinceObj.transform.localPosition = MiscVector.Vector3zero;
			provinceObj.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);

			for (int k=0; k<provinceFrontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.transform.SetParent (provinceObj.transform, false);
				flayer.transform.localPosition = MiscVector.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (MiscVector.Vector3zero);
				
				Mesh mesh = new Mesh ();
				mesh.vertices = provinceFrontiers [k];
				mesh.SetIndices (provinceFrontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;
				
				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;
				
				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.useLightProbes = false;
				mr.sharedMaterial = provincesMat;
			}

			if (_showOutline) {
				Country country = countries[countryIndex];
				Region region = country.regions[country.mainRegionIndex];
				provinceCountryOutlineRef = DrawCountryRegionOutline(region, provinceObj);
			}
		}


	#endregion




	#region Province functions

		bool GetProvinceUnderMouse (int countryIndex, Vector3 spherePoint, out int provinceIndex, out int regionIndex)
		{
			float startingDistance = 0;
			provinceIndex = regionIndex = -1;
			Country country = countries[countryIndex];
			if (country.provinces == null) return false;
			int provincesCount = country.provinces.Length;
			if (provincesCount==0) return false;

			PolygonPoint latlonPos = GetLatLonFromSpherePoint(spherePoint);
			float maxArea = float.MaxValue;

			// Is this the same province currently selected?
			if (_provinceHighlightedIndex>=0 && _provinceRegionHighlightedIndex>=0 && provinces[_provinceHighlightedIndex].countryIndex == countryIndex) {
				Region region = provinces[_provinceHighlightedIndex].regions[_provinceRegionHighlightedIndex];
//				if (ContainsPoint2D (region.latlon, latlonPos.X, latlonPos.Y)) {
					if (region.ContainsPoint (latlonPos.X, latlonPos.Y)) {
						maxArea = region.entity.mainRegionArea;
					// cannot return yet - need to check if any other province (smaller than this) could be highlighted
//					provinceIndex = _provinceHighlightedIndex;
//					regionIndex = _provinceRegionHighlightedIndex;
//					return true;
				}
			}
			
			// Check other provinces
			for (int tries=0; tries<75; tries++) {
				float minDist = float.MaxValue;
				for (int p=0; p<provincesCount; p++) {
					Province province = country.provinces[p];
					if (province.regions==null || province.mainRegionArea>maxArea) continue;
					for (int pr=0; pr<province.regions.Count; pr++) {
						Vector3 regionCenter = province.regions [pr].center;
						float dist = (regionCenter - spherePoint).sqrMagnitude;
						if (dist > startingDistance && dist < minDist) {
							minDist = dist;
							provinceIndex = GetProvinceIndex (province);
							regionIndex = pr;
						}
					}
				}
				
				// Check if this region is visible and the mouse is inside
				if (provinceIndex>=0) {
					Region region = provinces[provinceIndex].regions[regionIndex];
					if (region.ContainsPoint (latlonPos.X, latlonPos.Y)) {
						return true;
					}
				}
				
				// Continue searching but farther centers
				startingDistance = minDist;
			}
			return false;
		}

		int GetCacheIndexForProvinceRegion (int provinceIndex, int regionIndex) {
			return 1000000 + provinceIndex * 1000 + regionIndex;
		}
		
		public void HighlightProvinceRegion (int provinceIndex, int regionIndex, bool refreshGeometry) {
			if (provinceRegionHighlightedObj!=null) {
				if (!refreshGeometry && _provinceHighlightedIndex==provinceIndex && _provinceRegionHighlightedIndex == regionIndex) return;
				HideProvinceRegionHighlight();
			}
			if (provinceIndex<0 || provinceIndex>=provinces.Length || provinces[provinceIndex].regions==null || regionIndex<0 || regionIndex>= provinces[provinceIndex].regions.Count) return;
			
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = surfaces [cacheIndex];
				surfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			if (_enableCountryHighlight) {
			if (existsInCache) {
				provinceRegionHighlightedObj = surfaces [cacheIndex];
				if (provinceRegionHighlightedObj!=null) {
					provinceRegionHighlightedObj.SetActive (true);
					provinceRegionHighlightedObj.GetComponent<Renderer> ().sharedMaterial = hudMatProvince;
				}
			} else {
				provinceRegionHighlightedObj = GenerateProvinceRegionSurface (provinceIndex, regionIndex, hudMatProvince);
			}
			}
			_provinceHighlighted = provinces[provinceIndex];
			_provinceHighlightedIndex = provinceIndex;
			_provinceRegionHighlighted = _provinceHighlighted.regions[regionIndex];
			_provinceRegionHighlightedIndex = regionIndex;

		}

		void HideProvinceRegionHighlight () {
			if (provinceCountryOutlineRef!=null && countryRegionHighlighted==null) provinceCountryOutlineRef.SetActive(false);
			if (_provinceHighlightedIndex<0) return;

			if (_provinceRegionHighlighted != null &&  provinceRegionHighlightedObj != null) {
				if (provinceRegionHighlighted.customMaterial!=null) {
					ApplyMaterialToSurface (provinceRegionHighlightedObj,provinceRegionHighlighted.customMaterial);
				} else {
					provinceRegionHighlightedObj.SetActive (false);
				}
				provinceRegionHighlightedObj = null;
			}

			// Raise exit event
			if (OnProvinceExit!=null) OnProvinceExit(_provinceHighlightedIndex, _provinceRegionHighlightedIndex);

			_provinceHighlighted = null;
			_provinceHighlightedIndex = -1;
			_provinceRegionHighlighted = null;
			_provinceRegionHighlightedIndex = -1;
		}


		public void HideProvinces () {
			if (provincesLayer != null)
				provincesLayer.SetActive(false);
			countryProvincesDrawnIndex = -1;
			HideProvinceHighlight ();
		}

		void HideProvinceHighlight () {
			if (provinceHighlighted == null)
				return;
			if (provinceRegionHighlightedObj != null) {
				if (provinceRegionHighlighted.customMaterial!=null) {
					ApplyMaterialToSurface (provinceRegionHighlightedObj,provinceRegionHighlighted.customMaterial);
				} else {
					provinceRegionHighlightedObj.SetActive (false);
				}
				provinceRegionHighlightedObj = null;
			}
			_provinceHighlighted = null;
			_provinceHighlightedIndex = -1;
			_provinceRegionHighlighted = null;
			_provinceRegionHighlightedIndex = -1;
		}
		
		GameObject GenerateProvinceRegionSurface (int provinceIndex, int regionIndex, Material material) {
			Region region = provinces [provinceIndex].regions [regionIndex];

			Polygon poly = new Polygon(region.latlon);

			// Antarctica, Saskatchewan (Canada), British Columbia (Canada), Krasnoyarsk (Russia) - special cases due to its geometry
			if (provinceIndex==218 || provinceIndex == 220 || provinceIndex == 224 || provinceIndex == 3423 ) { 
				float step = 5;
				List<TriangulationPoint> steinerPoints = new List<TriangulationPoint>();
				for (double x = region.minMaxLat.x + step/2; x<region.minMaxLat.y - step/2;x += step) {
					for (double y = region.minMaxLon.x + step /2;y<region.minMaxLon.y - step / 2;y += step) {
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
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			string cacheIndexSTR = cacheIndex.ToString();
			// Deletes potential residual surface
			Transform t = surfacesLayer.transform.FindChild(cacheIndexSTR);
			if (t!=null) DestroyImmediate(t.gameObject);
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, revisedSurfPoints, revIndex, material);									
			surf.transform.SetParent (transform, false);
			surf.transform.localPosition = MiscVector.Vector3zero;
			if (_earthInvertedMode) {
				surf.transform.localScale = MiscVector.Vector3one * 0.998f;
			}
			if (surfaces.ContainsKey(cacheIndex)) surfaces.Remove(cacheIndex);
			surfaces.Add (cacheIndex, surf);
			return surf;
		}

		#endregion
	}

}