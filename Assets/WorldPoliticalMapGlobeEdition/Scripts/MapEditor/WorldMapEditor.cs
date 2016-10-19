using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WPM;
using WPM.Geom;
using Poly2Tri;

namespace WPM_Editor {

	public enum OPERATION_MODE {
		SELECTION = 0,
		RESHAPE = 1,
		CREATE = 2,
		UNDO = 3,
		CONFIRM = 4
	}

	public enum RESHAPE_REGION_TOOL {
		POINT = 0,
		CIRCLE = 1,
		SPLITV = 2,
		SPLITH = 3,
		MAGNET = 4,
		SMOOTH = 5,
		ERASER = 6,
		DELETE = 7
	}

	public enum RESHAPE_CITY_TOOL {
		MOVE = 0,
		DELETE = 1
	}

	public enum RESHAPE_MOUNT_POINT_TOOL {
		MOVE = 0,
		DELETE = 1
	}

	public enum CREATE_TOOL {
		CITY = 0,
		COUNTRY = 1,
		COUNTRY_REGION = 2,
		PROVINCE = 3,
		PROVINCE_REGION = 4,
		MOUNT_POINT = 5
	}


	public enum EDITING_MODE {
		COUNTRIES,
		PROVINCES
	}

	public enum EDITING_COUNTRY_FILE {
		COUNTRY_HIGHDEF=0,
		COUNTRY_LOWDEF=1
	}


	public static class ReshapeToolExtensons {
		public static bool hasCircle(this RESHAPE_REGION_TOOL r) {
			return r== RESHAPE_REGION_TOOL.CIRCLE || r == RESHAPE_REGION_TOOL.MAGNET || r == RESHAPE_REGION_TOOL.ERASER;
		}
	}

	[RequireComponent(typeof(WorldMapGlobe))]
	[ExecuteInEditMode]
	public partial class WorldMapEditor : MonoBehaviour {

		public int entityIndex { get { if (editingMode == EDITING_MODE.PROVINCES) return provinceIndex; else return countryIndex; } }
		public int regionIndex { get { if (editingMode == EDITING_MODE.PROVINCES) return provinceRegionIndex; else return countryRegionIndex; } }
		public OPERATION_MODE operationMode;
		public RESHAPE_REGION_TOOL reshapeRegionMode;
		public RESHAPE_CITY_TOOL reshapeCityMode;
		public RESHAPE_MOUNT_POINT_TOOL reshapeMountPointMode;
		public CREATE_TOOL createMode;
		public Vector3 cursor;
		public bool circleMoveConstant, circleCurrentRegionOnly;
		public float reshapeCircleWidth = 0.01f;
		public bool shouldHideEditorMesh;
		public bool magnetAgressiveMode = false;

		public string infoMsg = "";
		public DateTime infoMsgStartTime;
		public EDITING_MODE editingMode;
		public EDITING_COUNTRY_FILE editingCountryFile;

		[NonSerialized]
		public List<Region> highlightedRegions;

		List<List<Region>> _undoRegionsList;
		List<List<Region>> undoRegionsList { get { if (_undoRegionsList==null) _undoRegionsList = new List<List<Region>>(); return _undoRegionsList; } }
		public int undoRegionsDummyFlag;

		List<List<City>> _undoCitiesList;
		List<List<City>> undoCitiesList { get { if (_undoCitiesList==null) _undoCitiesList = new List<List<City>>(); return _undoCitiesList; } }
		public int undoCitiesDummyFlag;

		List<List<MountPoint>> _undoMountPointsList;
		List<List<MountPoint>> undoMountPointsList { get { if (_undoMountPointsList==null) _undoMountPointsList = new List<List<MountPoint>>(); return _undoMountPointsList; } }
		public int undoMountPointsDummyFlag;

		public IAdminEntity[] entities { get {
				if (editingMode == EDITING_MODE.PROVINCES) return map.provinces; else return map.countries;
			}
		}

		public List<Vector3>newShape;	// for creating new regions
	
		#region Editor functionality

		WorldMapGlobe _map;

		/// <summary>
		/// Accesor to the World Map Globe core API
		/// </summary>
		public WorldMapGlobe map { get { 
			if (_map==null) _map = GetComponent<WorldMapGlobe> ();
				return _map;
			}
		}

		[SerializeField]
		int lastMinPopulation;
		
		void OnEnable() {
			lastMinPopulation = map.minPopulation;
			map.minPopulation = 0;
		}
		
		void OnDisable() {
			if (_map!=null) {
				if (_map.minPopulation == 0) _map.minPopulation = lastMinPopulation;
			}
		}

		public void ClearSelection() {
			map.HideCountryRegionHighlights(true);
			highlightedRegions = null;
			countryIndex = -1;
			countryRegionIndex = -1;
			GUICountryName = "";
			GUICountryNewName = "";
			GUICountryIndex = -1;
			ClearProvinceSelection();
			ClearCitySelection();
			ClearMountPointSelection();
		}

		/// <summary>
		/// Forces a hard redraw on the globe map and calls redraw frontiers again. This is for clearing any artifact.
		/// </summary>
		public void RedrawAll() {
			Transform[] t = _map.GetComponentsInChildren<Transform>();
			for (int k=0;k<t.Length;k++) {
				if (t[k]!=null && t[k]!=_map.transform && t[k].GetComponent<Light>()==null) DestroyImmediate(t[k].gameObject);
			}
			_map.Redraw();
			RedrawFrontiers();
		}

		/// <summary>
		/// Redraws all frontiers and highlights current selected regions.
		/// </summary>
		public void RedrawFrontiers() {
			RedrawFrontiers(highlightedRegions, true);
		}

		/// <summary>
		/// Redraws the frontiers and highlights specified regions filtered by provided list of regions. Also highlights current selected country/province.
		/// </summary>
		/// <param name="filterRegions">Regions.</param>
		/// <param name="highlightSelected">Pass false to just redraw borders.</param>
		public void RedrawFrontiers (List <Region> filterRegions, bool highlightSelected) {
			map.RefreshCountryDefinition (countryIndex, filterRegions);
			if (highlightSelected) {
				map.HideProvinces();
				CountryHighlightSelection(filterRegions);
			}
			if (editingMode == EDITING_MODE.PROVINCES) {
				map.RefreshProvinceDefinition (provinceIndex);
				if (highlightSelected) {
					ProvinceHighlightSelection();
				}
			}
		}

		public void DiscardChanges() {
			ClearSelection();
			map.ReloadData();
			RedrawFrontiers();
			cityChanges = false;
			countryChanges = false;
			provinceChanges = false;
			lastCityCount = -1;
			lastCountryCount = -1;
			lastProvinceCount = -1;
		}

		/// <summary>
		/// Moves any point inside circle.
		/// </summary>
		/// <returns>Returns a list with changed regions</returns>
		public List<Region> MoveCircle (Vector3 position, Vector3 dragAmount, float circleSize) {
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return null;

			float circleSizeSqr = circleSize * circleSize;
			List<Region> regions = new List<Region>(100); 
			// Current region
			Region currentRegion = entities [entityIndex].regions [regionIndex];
			regions.Add (currentRegion);
			// Current region's neighbours
			if (!circleCurrentRegionOnly) {
				for (int r=0; r<currentRegion.neighbours.Count; r++) {
					Region region = currentRegion.neighbours[r];
					if (!regions.Contains(region)) regions.Add (region);
				}
				// If we're editing provinces, check if country points can be moved as well
				if (editingMode == EDITING_MODE.PROVINCES) {
					// Moves current country
					for (int cr=0;cr<map.countries[countryIndex].regions.Count;cr++) {
						Region countryRegion = map.countries[countryIndex].regions[cr];
						if (!regions.Contains(countryRegion)) regions.Add (countryRegion);
						// Moves neighbours
						for (int r=0; r<countryRegion.neighbours.Count; r++) {
							Region region = countryRegion.neighbours[r];
							if (!regions.Contains(region)) regions.Add (region);
						}
					}
				}
			}
			// Execute move operation on each point
			List<Region> affectedRegions = new List<Region>(regions.Count);
			for (int r=0;r<regions.Count;r++) {
				Region region = regions[r];
				bool regionAffected = false;
				for (int p=0; p<region.points.Length; p++) {
					Vector3 rp = region.points[p];
					float dist = (rp-position).sqrMagnitude;
					if (dist < circleSizeSqr) {
						if (circleMoveConstant) {
							region.points [p] += dragAmount;
						} else {
							region.points [p] += dragAmount  - dragAmount* (dist / circleSizeSqr);
						}
						region.points[p] = region.points[p].normalized * 0.5f;
						PolygonPoint latlon = _map.GetLatLonFromSpherePoint(region.points[p]);
						region.latlon[p].Set (latlon.X, latlon.Y);
						regionAffected = true;
					}
				}
				if (regionAffected)
					affectedRegions.Add (region);
			}
			return affectedRegions;
		}


		/// <summary>
		/// Moves a single point.
		/// </summary>
		/// <returns>Returns a list of affected regions</returns>
		public List<Region> MovePoint(Vector3 position, Vector3 dragAmount) {
			return MoveCircle(position, dragAmount, 0.0001f);
		}

		/// <summary>
		/// Moves points of other regions towards current frontier
		/// </summary>
		public bool Magnet (Vector3 position, float circleSize)
		{
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return false;

			Region currentRegion = entities [entityIndex].regions [regionIndex];
			float circleSizeSqr = circleSize * circleSize;

			Dictionary<Vector3, bool>attractorsUse = new Dictionary<Vector3, bool>();
			// Attract points of other regions/countries
			List<Region> regions = new List<Region>();
			for (int c=0; c<entities.Length; c++) {
				IAdminEntity entity = entities [c];
				if (entity.regions==null) continue;
				for (int r=0; r<entity.regions.Count; r++) {
					if (c!=entityIndex || r!=regionIndex) {
						regions.Add (entities[c].regions[r]);
					}
				}
			}
			if (editingMode == EDITING_MODE.PROVINCES) {
				// Also add regions of current country and neighbours
				for (int r=0;r<map.countries[countryIndex].regions.Count;r++) {
					Region region = map.countries[countryIndex].regions[r];
					regions.Add (region);
					for (int n=0;n<region.neighbours.Count;n++) {
						Region nregion = region.neighbours[n];
						if (!regions.Contains(nregion)) regions.Add (nregion);
					}
				}
			}

			bool changes = false;
			Vector3 goodAttractor = MiscVector.Vector3zero;

			for (int r=0; r<regions.Count; r++) {
				Region region = regions [r];
				bool changesInThisRegion = false;
				for (int p=0; p<region.points.Length; p++) {
					Vector3 rp = region.points [p];
					float dist = (rp-position).sqrMagnitude; // (rp.x - position.x) * (rp.x - position.x) * 4.0f + (rp.y - position.y) * (rp.y - position.y);
					if (dist < circleSizeSqr) {
						float minDist = float.MaxValue;
						int nearest = -1;
						for (int a=0; a<currentRegion.points.Length; a++) {
							Vector3 attractor = currentRegion.points [a];
							dist = (rp-attractor).sqrMagnitude; // (rp.x - attractor.x) * (rp.x - attractor.x) * 4.0f + (rp.y - attractor.y) * (rp.y - attractor.y);
							if (dist < circleSizeSqr && dist < minDist) {
								minDist = dist;
								nearest = a;
								goodAttractor = attractor;
							}
						}
						if (nearest >= 0) {
							changes = true;
							// Check if this attractor is being used by other point
							bool used = attractorsUse.ContainsKey (goodAttractor);
							if (!used || magnetAgressiveMode) {
								region.points [p] = goodAttractor;
								if (!used)
									attractorsUse.Add (goodAttractor, true);
								changesInThisRegion = true;
							}
						}
					}
				}
				if (changesInThisRegion) {
					// Remove duplicate points in this region
					Dictionary<Vector3, bool> repeated = new Dictionary<Vector3, bool> ();
					for (int k=0; k<region.points.Length; k++)
						if (!repeated.ContainsKey (region.points [k]))
							repeated.Add (region.points [k], true);
					region.points = new List<Vector3> (repeated.Keys).ToArray ();
					UpdateLatLonFromPoints(region);
				}
			}
			return changes;
		}


		void UpdateLatLonFromPoints(Region region) {
			int numPoints = region.points.Length;
			PolygonPoint[] latlons = new PolygonPoint[numPoints];
			for (int k=0;k<numPoints;k++) {
				latlons[k] = _map.GetLatLonFromSpherePoint(region.points[k]);
			}
			region.latlon = latlons;
		}

		void UpdatePointsFromLatLon(Region region) {
			int numPoints = region.latlon.Length;
			Vector3[] points = new Vector3[numPoints];
			for (int k=0;k<numPoints;k++) {
				points[k] = _map.GetSpherePointFromLatLon(region.latlon[k].X, region.latlon[k].Y);
			}
			region.points = points;
		}

		
		void ReplacePointsFromPolygon(Region region, WPM.Geom.Polygon polygon) {
			List<Point> points = polygon.contours[0].points;
			int numPoints = points.Count;
			PolygonPoint[] latlons = new PolygonPoint[numPoints];
			for (int k=0;k<numPoints;k++) latlons[k] = new PolygonPoint(points[k].x, points[k].y);
			region.latlon = latlons;
			UpdatePointsFromLatLon(region);
		}


		/// <summary>
		/// Erase points inside circle.
		/// </summary>
		public bool Erase (Vector3 position, float circleSize) {
			if (entityIndex < 0 || entityIndex >= entities.Length)
				return false;

			float circleSizeSqr = circleSize * circleSize;
			List<Region> regions = new List<Region>(100); 

			// Current region
			Region currentRegion = entities [entityIndex].regions [regionIndex];
			if (currentRegion.points.Length <= 3)
				return false;

			regions.Add (currentRegion);
			// Current region's neighbours
			if (!circleCurrentRegionOnly) {
				for (int r=0; r<currentRegion.neighbours.Count; r++) {
					Region region = currentRegion.neighbours[r];
					if (!regions.Contains(region)) regions.Add (region);
				}
				// If we're editing provinces, check if country points can be deleted as well
				if (editingMode == EDITING_MODE.PROVINCES) {
					// Current country
					for (int cr=0;cr<map.countries[countryIndex].regions.Count;cr++) {
						Region countryRegion = map.countries[countryIndex].regions[cr];
						if (!regions.Contains(countryRegion)) regions.Add (countryRegion);
						// Neighbours
						for (int r=0; r<countryRegion.neighbours.Count; r++) {
							Region region = countryRegion.neighbours[r];
							if (!regions.Contains(region)) regions.Add (region);
						}
					}
				}
			}
			// Execute delete operation on each point
			List<Vector3> temp = new List<Vector3> (currentRegion.points.Length);
			bool someChange = false;
			for (int r=0;r<regions.Count;r++) {
				Region region = regions[r];
				temp.Clear ();
				bool changes = false;
				for (int p=0; p<region.points.Length; p++) {
					Vector3 rp = region.points[p];
					float dist = (rp-position).sqrMagnitude;
					if (dist > circleSizeSqr) {
						temp.Add (rp);
					} else {
						changes = true;
//						double maxDist = 1000000;
//						int elegido = -1;
//						for (int k=0;k<region.latlon.Length;k++) {
//							if (k!=p) {
//								double dist1 = (region.latlon[k] - region.latlon[p]).MagnitudeSquared();
//								if (dist1<maxDist){
//									maxDist = dist1;
//									elegido = k;
//								}
//							}
//						}
//						Debug.Log (elegido);
					}
				}
				if (changes) {
					Vector3[] newPoints = temp.ToArray();
					if (newPoints.Length>=5) {
						region.points = newPoints;
						UpdateLatLonFromPoints(region);
						someChange = true;
					} else {
						SetInfoMsg("Can't delete more points. To delete it completely use the DELETE tool.");
					}
				}
			}
			return someChange;
		}


		public void UndoRegionsPush(List<Region> regions) {
			UndoRegionsInsertAtCurrentPos(regions);
			undoRegionsDummyFlag++;
			if (editingMode == EDITING_MODE.COUNTRIES) {
				countryChanges = true;
			} else
				provinceChanges = true;
		}

		public void UndoRegionsInsertAtCurrentPos(List<Region> regions) {
			if (regions==null) return;
			List<Region> clonedRegions = new List<Region>();
			for (int k=0;k<regions.Count;k++) {
				clonedRegions.Add (regions[k].Clone());
			}
			if (undoRegionsDummyFlag>undoRegionsList.Count) undoRegionsDummyFlag = undoRegionsList.Count;
			undoRegionsList.Insert(undoRegionsDummyFlag, clonedRegions);
		}

		public void UndoCitiesPush() {
			UndoCitiesInsertAtCurrentPos();
			undoCitiesDummyFlag++;
		}

		public void UndoCitiesInsertAtCurrentPos() {
			List<City> cities = new List<City>(map.cities.Count);
			for (int k=0;k<map.cities.Count;k++) cities.Add (map.cities[k].Clone());
			if (undoCitiesDummyFlag>undoCitiesList.Count) undoCitiesDummyFlag = undoCitiesList.Count;
			undoCitiesList.Insert(undoCitiesDummyFlag, cities);
		}

		public void UndoMountPointsPush() {
			UndoMountPointsInsertAtCurrentPos();
			undoMountPointsDummyFlag++;
		}
		
		public void UndoMountPointsInsertAtCurrentPos() {
			if (map.mountPoints==null) map.mountPoints = new List<MountPoint>();
			List<MountPoint> mountPoints = new List<MountPoint>(map.mountPoints.Count);
			for (int k=0;k<map.mountPoints.Count;k++) mountPoints.Add (map.mountPoints[k].Clone());
			if (undoMountPointsDummyFlag>undoMountPointsList.Count) undoMountPointsDummyFlag = undoMountPointsList.Count;
			undoMountPointsList.Insert(undoMountPointsDummyFlag, mountPoints);
		}


		public void UndoHandle() {
			if (undoRegionsList!=null && undoRegionsList.Count>=2) {
				if (undoRegionsDummyFlag >= undoRegionsList.Count) {
					undoRegionsDummyFlag = undoRegionsList.Count-2;
				}
				List<Region> savedRegions = undoRegionsList[undoRegionsDummyFlag];
				RestoreRegions(savedRegions);
			}
			if (undoCitiesList!=null && undoCitiesList.Count>=2) {
				if (undoCitiesDummyFlag >= undoCitiesList.Count) {
					undoCitiesDummyFlag = undoCitiesList.Count-2;
				}
				List<City> savedCities = undoCitiesList[undoCitiesDummyFlag];
				RestoreCities(savedCities);
			}
			if (undoMountPointsList!=null && undoMountPointsList.Count>=2) {
				if (undoMountPointsDummyFlag >= undoMountPointsList.Count) {
					undoMountPointsDummyFlag = undoMountPointsList.Count-2;
				}
				List<MountPoint> savedMountPoints = undoMountPointsList[undoMountPointsDummyFlag];
				RestoreMountPoints(savedMountPoints);
			}
		}

		
		void RestoreRegions(List<Region> savedRegions) {
			for (int k=0;k<savedRegions.Count;k++) {
				IAdminEntity entity = savedRegions[k].entity;
				int regionIndex = savedRegions[k].regionIndex;
				entity.regions[regionIndex] = savedRegions[k];
			}
			RedrawFrontiers();
		}

		void RestoreCities(List<City> savedCities) {
			map.cities = savedCities;
			lastCityCount = -1;
			ReloadCityNames();
			map.DrawCities();
		}

		
		void RestoreMountPoints(List<MountPoint> savedMountPoints) {
			map.mountPoints = savedMountPoints;
			lastMountPointCount = -1;
			ReloadMountPointNames();
			map.DrawMountPoints();
		}

		void EntityAdd (IAdminEntity newEntity) {
			if (newEntity is Country) {
				map.CountryAdd((Country)newEntity);
			} else {
				map.ProvinceAdd((Province)newEntity);
			}
		}
		


		public void SplitHorizontally() {
			if (entityIndex<0 || entityIndex>=entities.Length) return;

			IAdminEntity currentEntity = entities[entityIndex];
			Region currentRegion = currentEntity.regions[regionIndex];
			Vector3 center = currentRegion.center;
			List<Vector3>half1 = new List<Vector3>();
			List<Vector3>half2 = new List<Vector3>();
			int prevSide = 0;
			for (int k=0;k<currentRegion.points.Length;k++) {
				Vector3 p = currentRegion.points[k];
				if (p.y>currentRegion.center.y) {
					half1.Add (p);
					if (prevSide == -1) half2.Add (p);
					prevSide = 1;
				}
				if (p.y<=currentRegion.center.y) {
					half2.Add (p);
					if (prevSide == 1) half1.Add (p);
					prevSide = -1;
				}
			}
			// Setup new entity
			IAdminEntity newEntity;
			if (currentEntity is Country) {
				newEntity = new Country("New " + currentEntity.name, ((Country)currentEntity).continent);
			} else {
				newEntity = new Province("New " + currentEntity.name, ((Province)currentEntity).countryIndex);
				newEntity.regions = new List<Region>();
			}
			EntityAdd(newEntity);

			// Update polygons
			Region newRegion = new Region(newEntity, 0);
			if (entities[countryIndex].center.y>center.y) {
				currentRegion.points = half1.ToArray();
				newRegion.points = half2.ToArray();
			} else {
				currentRegion.points = half2.ToArray();
				newRegion.points = half1.ToArray();
			}
			UpdateLatLonFromPoints(currentRegion);
			UpdateLatLonFromPoints(newRegion);
			newEntity.regions.Add (newRegion);

			// Refresh old entity and selects the new
			if (currentEntity is Country) {
				map.RefreshCountryDefinition(countryIndex, highlightedRegions);
				countryIndex = map.countries.Length-1;
				countryRegionIndex = 0;
			} else {
				map.RefreshProvinceDefinition(provinceIndex);
				provinceIndex = map.provinces.Length-1;
				provinceRegionIndex = 0;
			}

			// Refresh lines
			highlightedRegions.Add (newRegion);
			RedrawFrontiers();
			map.RedrawMapLabels();
		}

		public void SplitVertically () {
			if (entityIndex<0 || entityIndex>=entities.Length) return;

			IAdminEntity currentEntity = entities[entityIndex];
			Region currentRegion = currentEntity.regions[regionIndex];
			Vector3 center = currentRegion.center;
			List<Vector3>half1 = new List<Vector3>();
			List<Vector3>half2 = new List<Vector3>();
			int prevSide = 0;
			double centerX = (currentRegion.minMaxLon.x + currentRegion.minMaxLon.y)*0.5;
			for (int k=0;k<currentRegion.latlon.Length;k++) {
				Vector3 p = currentRegion.points[k];
				PolygonPoint latlon = currentRegion.latlon[k];
				if (latlon.Y>centerX) { // compare longitudes
					half1.Add (p);
					if (prevSide == -1) half2.Add (p);
					prevSide = 1;
				}
				if (latlon.Y<=centerX) {
					half2.Add (p);
					if (prevSide == 1) half1.Add (p);
					prevSide = -1;
				}
			}

			// Setup new entity
			IAdminEntity newEntity;
			if (currentEntity is Country) {
				newEntity = new Country("New " + currentEntity.name, ((Country)currentEntity).continent);
			} else {
				newEntity = new Province("New " + currentEntity.name, ((Province)currentEntity).countryIndex);
				newEntity.regions = new List<Region>();
			}
			EntityAdd(newEntity);
			
			// Update polygons
			Region newRegion = new Region(newEntity, 0);
			if (entities[countryIndex].center.x>center.x) {
				currentRegion.points = half1.ToArray();
				newRegion.points = half2.ToArray();
			} else {
				currentRegion.points = half2.ToArray();
				newRegion.points = half1.ToArray();
			}
			UpdateLatLonFromPoints(currentRegion);
			UpdateLatLonFromPoints(newRegion);
			newEntity.regions.Add (newRegion);
			
			// Refresh old entity and selects the new
			if (currentEntity is Country) {
				map.RefreshCountryDefinition(countryIndex, highlightedRegions);
				countryIndex = map.countries.Length-1;
				countryRegionIndex = 0;
			} else {
				map.RefreshProvinceDefinition(provinceIndex);
				provinceIndex = map.provinces.Length-1;
				provinceRegionIndex = 0;
			}
			
			// Refresh lines
			highlightedRegions.Add (newRegion);
			RedrawFrontiers();
			map.RedrawMapLabels();
		}

	
		/// <summary>
		/// Adds the new point to currently selected region.
		/// </summary>
		public void AddPoint(Vector3 newPoint) {
			if (entities==null || entityIndex<0 || entityIndex>=entities.Length || regionIndex<0 || entities[entityIndex].regions == null || regionIndex>=entities[entityIndex].regions.Count) return;
			List<Region> affectedRegions = new List<Region>();
			Region region =  entities[entityIndex].regions[regionIndex];
			float minDist = float.MaxValue;
			int nearest = -1, previous = -1;
			int max = region.points.Length;
			for (int p=0; p<max; p++) {
				int q = p == 0 ? max-1: p-1;
				Vector3 rp = (region.points [p] + region.points[q]) * 0.5f;
				float dist = (rp - newPoint).sqrMagnitude; // (rp.x - newPoint.x) * (rp.x - newPoint.x)*4  + (rp.y - newPoint.y) * (rp.y - newPoint.y);
				if (dist < minDist) {
					// Get nearest point
					minDist = dist;
					nearest = p;
					previous = q;
				}
			}

			if (nearest>=0) {
				Vector3 pointToInsert = (region.points[nearest] + region.points[previous]) * 0.5f;
				pointToInsert = pointToInsert.normalized * 0.5f;

				// Check if nearest and previous exists in any neighbour
				int nearest2 = -1, previous2 = -1;
				for (int n=0;n<region.neighbours.Count;n++) {
					Region nregion = region.neighbours[n];
					for (int p=0;p<nregion.points.Length;p++) {
						if (nregion.points[p] == region.points[nearest]) {
							nearest2 = p;
						}
						if (nregion.points[p] == region.points[previous]) {
							previous2 = p;
						}
					}
					if (nearest2>=0 && previous2>=0) {
						nregion.points = InsertPoint(nregion.points, previous2, pointToInsert);
						nregion.latlon = InsertLatLon(nregion.latlon, previous2, _map.GetLatLonFromSpherePoint(pointToInsert));
						affectedRegions.Add (nregion);
						break;
					}
				}

				// Insert the point in the current region (must be done after inserting in the neighbour so nearest/previous don't unsync)
				region.points= InsertPoint(region.points, nearest, pointToInsert);
				region.latlon = InsertLatLon(region.latlon, nearest, _map.GetLatLonFromSpherePoint(pointToInsert));
				affectedRegions.Add (region);
			} 
		}

		Vector3[] InsertPoint(Vector3[] pointArray, int index, Vector3 pointToInsert) {
			List<Vector3> temp = new List<Vector3>(pointArray.Length+1);
			for (int k=0;k<pointArray.Length;k++) {
				if (k==index) temp.Add (pointToInsert);
				temp.Add (pointArray[k]);
			}
			return temp.ToArray();
		}

		PolygonPoint[] InsertLatLon(PolygonPoint[] pointArray, int index, PolygonPoint latlonToInsert) {
			List<PolygonPoint> temp = new List<PolygonPoint>(pointArray.Length+1);
			for (int k=0;k<pointArray.Length;k++) {
				if (k==index) temp.Add (latlonToInsert);
				pointArray[k].Reset();
				temp.Add (pointArray[k]);
			}
			return temp.ToArray();
		}
		
		public void SetInfoMsg (string msg) {
			this.infoMsg = msg;
			infoMsgStartTime = DateTime.Now;
		}

		#endregion

		#region Misc tools


		public WPM.Geom.Polygon GetGeomPolygon(PolygonPoint[] points) {
			WPM.Geom.Polygon poly = new WPM.Geom.Polygon();
			WPM.Geom.Contour contour = new WPM.Geom.Contour();
			for (int k=0;k<points.Length;k++) {
				contour.Add(new Point(points[k].X, points[k].Y));
			}
			poly.AddContour(contour);
			return poly;
		}


		public void RegionSanitize(Region region) {
//			UpdateLatLonFromPoints(region); // reduces precision
			List<PolygonPoint> newLatLons = new List<PolygonPoint>(region.latlon);
			// removes points which are too near from others
			for (int k=0;k<newLatLons.Count;k++) {
				PolygonPoint p0 = newLatLons[k];
				for (int j=k+1;j<newLatLons.Count;j++) {
					PolygonPoint p1 = newLatLons[j];
					double distance = (p1-p0).MagnitudeSquared();
					if (distance<0.000001) {
						newLatLons.RemoveAt(j);
						j--;
					}
				}
			}
			// remove crossing segments
			region.latlon = PolygonSanitizer.RemoveCrossingSegments(newLatLons).ToArray();
			// updates points
			UpdatePointsFromLatLon(region);
		}

		#endregion

	}
}
