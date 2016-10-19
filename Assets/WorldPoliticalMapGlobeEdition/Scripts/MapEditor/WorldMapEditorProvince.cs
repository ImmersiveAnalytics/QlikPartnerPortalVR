using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WPM;
using WPM.Geom;
using Poly2Tri;

namespace WPM_Editor {

	public partial class WorldMapEditor : MonoBehaviour {

		public int provinceIndex = -1, provinceRegionIndex = -1;
		public int GUIProvinceIndex;
		public string GUIProvinceName = "";
		public string GUIProvinceNewName = "";
		public int GUIProvinceTransferToCountryIndex = -1;
		public bool provinceChanges; // if there's any pending change to be saved

		int lastProvinceCount = -1;
		string[] _provinceNames;

		public string[] provinceNames {
			get {
				if (countryIndex==-1) return new string[0];
				if (map.countries[countryIndex].provinces!=null && lastProvinceCount != map.countries[countryIndex].provinces.Length) {
					provinceIndex =-1;
					ReloadProvinceNames ();
				}
			return _provinceNames;
			}
		}

		
		#region Editor functionality

		
		public void ClearProvinceSelection() {
			map.HideProvinceRegionHighlights(true);
			map.HideProvinces();
			provinceIndex = -1;
			provinceRegionIndex = -1;
			GUIProvinceName = "";
			GUIProvinceNewName = "";
			GUIProvinceIndex = -1;
		}


		public bool ProvinceSelectByScreenClick(int countryIndex, Ray ray) {
			int targetProvinceIndex, targetRegionIndex;
			if (map.GetProvinceIndex (countryIndex, ray, out targetProvinceIndex, out targetRegionIndex)) {
				provinceIndex = targetProvinceIndex;
				if (provinceIndex>=0 && countryIndex!=map.provinces[provinceIndex].countryIndex) { // sanity check
					ClearSelection();
					countryIndex = map.provinces[provinceIndex].countryIndex;
					countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
					CountryRegionSelect();
				}
				provinceRegionIndex = targetRegionIndex;
				ProvinceRegionSelect();
				return true;
			}
			return false;
		}

		bool GetProvinceIndexByGUISelection() {
			if (GUIProvinceIndex<0 || GUIProvinceIndex>=provinceNames.Length) return false;
			string[] s = provinceNames [GUIProvinceIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				GUIProvinceName = s [0].Trim ();
				if (int.TryParse (s [1], out provinceIndex)) {
					provinceRegionIndex = map.provinces[provinceIndex].mainRegionIndex;
					return true;
				}
			}
			return false;
		}
		
		public void ProvinceSelectByCombo (int selection) {
			GUIProvinceName = "";
			GUIProvinceIndex = selection;
			if (GetProvinceIndexByGUISelection()) {
				if (Application.isPlaying) {
					map.BlinkProvince(provinceIndex, Color.black, Color.green, 1.2f, 0.2f);
				}
			}
			ProvinceRegionSelect ();
		}

		public void ReloadProvinceNames () {
			if (map == null || map.provinces == null || countryIndex<0 || countryIndex>=map.countries.Length) {
				return;
			}
			_provinceNames = map.GetProvinceNames (countryIndex);
			lastProvinceCount = _provinceNames.Length; 
			SyncGUIProvinceSelection();
			ProvinceRegionSelect(); // refresh selection
		}

		
		public void ProvinceRegionSelect() {
			if (countryIndex < 0 || countryIndex >= map.countries.Length || provinceIndex<0 || provinceIndex>=map.provinces.Length || editingMode!= EDITING_MODE.PROVINCES)
				return;

			// Checks country selected is correct
			Province province = map.provinces[provinceIndex];
			if (province.countryIndex!=countryIndex) {
				ClearSelection();
				countryIndex = province.countryIndex;
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				CountryRegionSelect();
			}

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUIProvinceName = province.name;
			SyncGUIProvinceSelection();
			if (provinceIndex>=0 && provinceIndex<map.provinces.Length) {
				GUIProvinceNewName = province.name;
				ProvinceHighlightSelection();
			}
		}

		public void ProvinceSanitize() {
			if (provinceIndex<0 || provinceIndex>=_map.provinces.Length) return;
			
			Province province = _map.provinces[provinceIndex];
			for (int k=0;k<province.regions.Count;k++) {
				Region region = province.regions[k];
				RegionSanitize(region);
			}
			_map.RefreshProvinceDefinition(provinceIndex);
			provinceChanges = true;
		}

		void ProvinceHighlightSelection() {
			
			if (highlightedRegions==null) highlightedRegions = new List<Region>(); else highlightedRegions.Clear();
			map.HideProvinceRegionHighlights(true);

			if (provinceIndex<0 || provinceIndex>=map.provinces.Length || countryIndex<0 || countryIndex>=map.countries.Length || map.countries[countryIndex].provinces==null || 
			    provinceRegionIndex<0 || map.provinces[provinceIndex].regions==null || provinceRegionIndex>=map.provinces[provinceIndex].regions.Count) return;

			// Highlight current province
			for (int p=0;p<map.countries[countryIndex].provinces.Length;p++) {
				Province province = map.countries[countryIndex].provinces[p];
				if (province.regions==null) continue;
				// if province is current province then highlight it
				if (province.name.Equals(map.provinces[provinceIndex].name)) {
					map.HighlightProvinceRegion (provinceIndex, provinceRegionIndex, false);
					highlightedRegions.Add (map.provinces[provinceIndex].regions[provinceRegionIndex]);
				} else {
					// if this province belongs to the country but it's not current province, add to the collection of highlighted (not colorize because country is already colorized and that includes provinces area)
					highlightedRegions.Add (province.regions[province.mainRegionIndex]);
				}
			}
			shouldHideEditorMesh = true;
	    }

		void SyncGUIProvinceSelection() {
			// recover GUI country index selection
			if (GUIProvinceName.Length>0 && provinceNames!=null) {
				for (int k=0; k<_provinceNames.Length; k++) {
					if (_provinceNames [k].TrimStart ().StartsWith (GUIProvinceName)) {
						GUIProvinceIndex = k;
						provinceIndex = map.GetProvinceIndex(countryIndex, GUIProvinceName);
						return;
					}
				}
			}
			GUIProvinceIndex = -1;
			GUIProvinceName = "";
		}

		
		public bool ProvinceRename () {
			if (countryIndex<0 || provinceIndex<0) return false;
			string prevName = map.provinces[provinceIndex].name;
			GUIProvinceNewName = GUIProvinceNewName.Trim ();
			if (prevName.Equals(GUIProvinceNewName)) return false;
			if (map.ProvinceRename(countryIndex, prevName, GUIProvinceNewName)) {
				GUIProvinceName = GUIProvinceNewName;
				lastProvinceCount = -1;
				ReloadProvinceNames();
				provinceChanges = true;
				return true;
			}
			return false;
		}

	
		/// <summary>
		/// Delete all provinces of current country. Called from DeleteCountry.
		/// </summary>
		void mDeleteCountryProvinces() {
			if (map.provinces==null) return;
			if (countryIndex<0) return;

			map.HideProvinceRegionHighlights(true);
			map.countries[countryIndex].provinces = new Province[0];
			map.CountryDeleteProvinces(countryIndex);
			provinceChanges = true;
		}

		public void DeleteCountryProvinces() {
			mDeleteCountryProvinces();
			ClearSelection();
			RedrawFrontiers();
			map.RedrawMapLabels();
		}


		/// <summary>
		/// Delete all provinces of current country's continent. Called from DeleteCountrySameContinent.
		/// </summary>
		void DeleteProvincesSameContinent() {
			if (map.provinces==null) return;
			int numProvinces = map.provinces.Length;
			List<Province> newProvinces = new List<Province>(numProvinces);
			string continent = map.countries[countryIndex].continent;
			for (int k=0;k<numProvinces;k++) {
				if (map.provinces[k]!=null) {
					int c = map.provinces[k].countryIndex;
					if (!map.countries[c].continent.Equals(continent)) {
						newProvinces.Add (map.provinces[k]);
					}
				}
			}
			map.provinces = newProvinces.ToArray();
			provinceChanges = true;
		}

		/// <summary>
		/// Deletes current region or province if this was the last region
		/// </summary>
		public void ProvinceDelete() {
			if (provinceIndex<0 || provinceIndex>=map.provinces.Length) return;
			map.HideProvinceRegionHighlights(true);

				// Clears references from mount points
				if (map.mountPoints!=null) {
					for (int k=0;k<map.mountPoints.Count;k++) {
						map.mountPoints[k].provinceIndex = -1;
					}
				}
				// Remove it from the country array
				List<Province> newProvinces = new List<Province>(map.countries[countryIndex].provinces.Length-1);
				for (int k=0;k<map.countries[countryIndex].provinces.Length;k++)
					if (!map.countries[countryIndex].provinces[k].name.Equals (GUIProvinceName)) 
						newProvinces.Add (map.countries[countryIndex].provinces[k]);
				map.countries[countryIndex].provinces=newProvinces.ToArray();
				// Remove from the global array
				newProvinces = new List<Province>(map.provinces.Length-1);
				for (int k=0;k<map.provinces.Length;k++) {
					if (k!=provinceIndex) {
						newProvinces.Add (map.provinces[k]);
					}
				}
				map.provinces = newProvinces.ToArray();

			ClearProvinceSelection();
			RedrawFrontiers();
			provinceChanges = true;
		}

		/// <summary>
		/// Deletes current region or province if this was the last region
		/// </summary>
		public void ProvinceRegionDelete() {
			if (provinceIndex<0 || provinceIndex>=map.provinces.Length) return;
			map.HideProvinceRegionHighlights(true);
			
			if (map.provinces[provinceIndex].regions!=null && map.provinces[provinceIndex].regions.Count>1) {
				map.provinces[provinceIndex].regions.RemoveAt(provinceRegionIndex);
				map.RefreshProvinceDefinition(provinceIndex);
			} 
			ClearProvinceSelection();
			RedrawFrontiers();
			provinceChanges = true;
		}


		/// <summary>
		/// Creates a new province with the current shape
		/// </summary>
		public void ProvinceCreate() {
			if (newShape.Count<3 || countryIndex<0) return;

			provinceIndex = map.provinces.Length;
			provinceRegionIndex = 0;
			Province newProvince = new Province("New Province" + (provinceIndex+1).ToString(), countryIndex);
			Region region = new Region(newProvince, 0);
			region.points = newShape.ToArray();
			UpdateLatLonFromPoints(region);
			newProvince.regions = new List<Region>();
			newProvince.regions.Add (region);
			map.ProvinceAdd(newProvince);
			map.RefreshProvinceDefinition(provinceIndex);
			lastProvinceCount = -1;
			ReloadProvinceNames();
			ProvinceRegionSelect();
			provinceChanges = true;
		}
		
		/// <summary>
		/// Adds a new province to current province
		/// </summary>
		public void ProvinceRegionCreate() {
			if (newShape.Count<3 || provinceIndex<0) return;
			
			Province province = map.provinces[provinceIndex];
			if (province.regions==null) province.regions = new List<Region>();
			provinceRegionIndex  = province.regions.Count;
			Region region = new Region(province, provinceRegionIndex);
			region.points = newShape.ToArray();
			UpdateLatLonFromPoints(region);
			if (province.regions==null) province.regions = new List<Region>();
			province.regions.Add (region);
			map.RefreshProvinceDefinition(provinceIndex);
			provinceChanges = true;
			ProvinceRegionSelect();
		}

		/// <summary>
		/// Changes province's owner to specified country
		/// </summary>
		public void ProvinceTransferTo() {
			if (provinceIndex<0 || GUIProvinceTransferToCountryIndex<0 || GUIProvinceTransferToCountryIndex>=countryNames.Length) return;

			// Get target country
			// recover GUI country index selection
			int targetCountryIndex = -1;
			string[] s = countryNames [GUIProvinceTransferToCountryIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				if (!int.TryParse (s [1], out targetCountryIndex)) {
					return;
				}
			}
			// Remove province form source country
			Province province = map.provinces[provinceIndex];
			Country sourceCountry = map.countries[countryIndex];
			if (map.countries[countryIndex].provinces!=null) {
				List<Province>sourceProvinces = new List<Province>(sourceCountry.provinces);
				int provIndex = -1;
				for (int k=0;k<sourceCountry.provinces.Length;k++)
					if (sourceCountry.provinces[k].name.Equals(province.name)) provIndex = k;
				if (provIndex>=0) {
					sourceProvinces.RemoveAt(provIndex);
					sourceCountry.provinces = sourceProvinces.ToArray();
				}
			}

			// Adds province to target country
			province.countryIndex = targetCountryIndex;
			Country targetCountry = map.countries[targetCountryIndex];
			List<Province>destProvinces = new List<Province>(targetCountry.provinces);
			destProvinces.Add (province);
			targetCountry.provinces = destProvinces.ToArray();

			// Apply boolean operations on country polygons
			Region provinceRegion = province.regions[provinceRegionIndex];
			Region sourceRegion = sourceCountry.regions[sourceCountry.mainRegionIndex];
			Region targetRegion = targetCountry.regions[targetCountry.mainRegionIndex];

			// Extract from source country - only if province is in the frontier or is crossing the country
			RegionSanitize(sourceRegion);
			RegionSanitize(provinceRegion);
			WPM.Geom.Polygon provincePolygon = GetGeomPolygon(provinceRegion.latlon);
			PolygonClipper pc = new PolygonClipper(GetGeomPolygon(sourceRegion.latlon), provincePolygon);
			if (pc.OverlapsSubjectAndClipping()) {
				pc.Compute(PolygonOp.DIFFERENCE);
				ReplacePointsFromPolygon(sourceRegion, pc.subject);
			}

			// Add region to target country's polygon - only if the province is touching or crossing target country frontier
			RegionSanitize(targetRegion);
			pc = new PolygonClipper(GetGeomPolygon(targetRegion.latlon), provincePolygon);
			if (pc.OverlapsSubjectAndClipping()) {
				pc.Compute(PolygonOp.UNION);
				ReplacePointsFromPolygon(targetRegion, pc.subject);
			} else {
				// Add new region to country
				Region newCountryRegion = new Region(targetCountry, targetCountry.regions.Count);
				newCountryRegion.points = new List<Vector3>(provinceRegion.points).ToArray();
				UpdateLatLonFromPoints(newCountryRegion);
				targetCountry.regions.Add (newCountryRegion);
			}

			// Finish operation
			map.HideCountryRegionHighlights(true);
			map.HideProvinceRegionHighlights(true);
			// Refresh definition of old country
			map.RefreshCountryDefinition(countryIndex, null);
			// Link province to target country and refresh definition of target country
			map.RefreshCountryDefinition(targetCountryIndex, null);
			map.RefreshProvinceDefinition(provinceIndex);
			countryChanges = true;
			provinceChanges = true;
			countryIndex = targetCountryIndex;
			countryRegionIndex = targetCountry.mainRegionIndex;
			ProvinceRegionSelect();
		}
		
		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current province data file
		/// </summary>
		public string GetProvinceGeoDataFileName() {
			return "provinces10.txt";
		}

		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetProvinceGeoData () {
			
			StringBuilder sb = new StringBuilder ();
			for (int k=0; k<map.provinces.Length; k++) {
				Province province = map.provinces [k];
				int countryIndex = province.countryIndex;
				if (countryIndex < 0 || countryIndex >= map.countries.Length)
					continue;
				string countryName = map.countries [countryIndex].name;
				if (k > 0)
					sb.Append ("|");
				sb.Append (province.name + "$");
				sb.Append (countryName + "$");
				if (province.regions==null) map.ReadProvincePackedString(province);
				for (int r = 0; r<province.regions.Count; r++) {
					if (r > 0)
						sb.Append ("*");
					Region region = province.regions [r];
					for (int p=0; p<region.latlon.Length; p++) {
						if (p > 0)
							sb.Append (";");
						Point2D point = region.latlon [p] * WorldMapGlobe.MAP_PRECISION;
						sb.Append ( Mathf.RoundToInt(point.Xf).ToString () + ",");
						sb.Append ( Mathf.RoundToInt(point.Yf).ToString ());
					}
				}
			}
			return sb.ToString ();
		}

		#endregion

	}
}
