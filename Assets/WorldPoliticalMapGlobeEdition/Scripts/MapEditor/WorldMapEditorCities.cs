using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM_Editor {

	public partial class WorldMapEditor : MonoBehaviour {

		public int GUICityIndex;
		public string GUICityName = "";
		public string GUICityNewName = "";
		public string GUICityPopulation = "";
		public CITY_CLASS GUICityClass = CITY_CLASS.CITY;
		public int cityIndex = -1;
		public bool cityChanges;  // if there's any pending change to be saved

		// private fields
		int lastCityCount = -1;
		string[] _cityNames;
				    

		public string[] cityNames {
			get {
				if (map.cities!=null && lastCityCount != map.cities.Count) {
					cityIndex =-1;
					ReloadCityNames ();
				}
				return _cityNames;
			}
		}

		
		#region Editor functionality

		
		public void ClearCitySelection() {
			map.HideCityHighlights();
			cityIndex = -1;
			GUICityName = "";
			GUICityIndex = -1;
			GUICityNewName = "";
		}


		/// <summary>
		/// Adds a new city to current country.
		/// </summary>
		public void CityCreate(Vector3 newPoint) {
			if (countryIndex<0) return;
			GUICityName = "New City " + (map.cities.Count+1);
			City newCity = new City(GUICityName, GUIProvinceName, countryIndex, 100, newPoint, GUICityClass);
			map.cities.Add (newCity);
			map.DrawCities();
			lastCityCount = -1;
			ReloadCityNames();
			cityChanges = true;
		}


		public bool CityRename () {
			if (cityIndex<0) return false;
			string prevName = map.cities[cityIndex].name;
			GUICityNewName = GUICityNewName.Trim ();
			if (prevName.Equals(GUICityNewName)) return false;
			map.cities[cityIndex].name = GUICityNewName;
			GUICityName = GUICityNewName;
			lastCityCount = -1;
			ReloadCityNames();
			map.DrawCities();
			cityChanges = true;
			return true;
		}

		public bool CityClassChange() {
			if (cityIndex<0) return false;
			map.cities[cityIndex].cityClass = GUICityClass;
			map.DrawCities();
			cityChanges = true;
			return true;
		}

		public bool CityChangePopulation (int newPopulation) {
			if (cityIndex<0) return false;
			map.cities[cityIndex].population = newPopulation;
			cityChanges = true;
			return true;
		}



		public void CityMove(Vector3 destination) {
			if (cityIndex<0) return;
			map.cities[cityIndex].unitySphereLocation = destination;

			Transform t = map.transform.Find(map.GetCityHierarchyName(cityIndex));
			if (t!=null) t.localPosition = destination * 1.001f;
			cityChanges = true;
		}

		public void CitySelectByCombo (int selection) {
			GUICityName = "";
			GUICityIndex = selection;
			if (GetCityIndexByGUISelection()) {
				if (Application.isPlaying) {
					map.BlinkCity (cityIndex, Color.black, Color.green, 1.2f, 0.2f);
				}
			}
			CitySelect ();
		}

		bool GetCityIndexByGUISelection() {
			if (GUICityIndex<0 || GUICityIndex>=cityNames.Length) return false;
			string[] s = cityNames [GUICityIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				GUICityName = s [0].Trim ();
				if (int.TryParse (s [1], out cityIndex)) {
					return true;
				}
			}
			return false;
		}

		public void CitySelect() {
			if (cityIndex < 0 || cityIndex > map.cities.Count)
				return;

			// If no country is selected (the city could be at sea) select it
			City city = map.cities[cityIndex];
			int cityCountryIndex = city.countryIndex;
			if (cityCountryIndex<0) {
				SetInfoMsg("Country not found in this country file.");
			}

			if (countryIndex!=cityCountryIndex && cityCountryIndex>=0) {
				ClearSelection();
				countryIndex = cityCountryIndex;
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				CountryRegionSelect();
			} 

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUICityName = city.name;
			GUICityPopulation = city.population.ToString();
			GUICityClass = city.cityClass;
			SyncGUICitySelection();
			if (cityIndex>=0) {
				GUICityNewName = city.name;
				CityHighlightSelection();
			}
		}

		public bool CitySelectByScreenClick(Ray ray) {
			int targetCityIndex;
			if (map.GetCityIndex (ray, out targetCityIndex)) {
				cityIndex = targetCityIndex;
				CitySelect();
				return true;
			}
			return false;
		}

		void CityHighlightSelection() {

			if (cityIndex<0 || cityIndex>=map.cities.Count) return;

			// Colorize city
			map.HideCityHighlights();
			map.ToggleCityHighlight(cityIndex, Color.blue, true);
	    }

		
		public void ReloadCityNames () {
			if (map == null || map.cities == null) {
				lastCityCount = -1;
				return;
			}
			lastCityCount = map.cities.Count; // check this size, and not result from GetCityNames because it could return additional rows (separators and so)
			_cityNames = map.GetCityNames(countryIndex, true);
			SyncGUICitySelection();
			CitySelect(); // refresh selection
		}

		void SyncGUICitySelection() {
			// recover GUI city index selection
			if (GUICityName.Length>0) {
				for (int k=0; k<cityNames.Length; k++) { 
					if (_cityNames [k].TrimStart ().StartsWith (GUICityName)) {
						GUICityIndex = k;
						cityIndex = map.GetCityIndex(countryIndex, GUICityName);
						return;
					}
				}
				if (map.GetCityIndex(GUICityName)<0) {
					SetInfoMsg("City " + GUICityName + " not found in database.");
				}
			}
			GUICityIndex = -1;
			GUICityName = "";
		}

		/// <summary>
		/// Deletes current city
		/// </summary>
		public void DeleteCity() {
			if (cityIndex<0 || cityIndex>=map.cities.Count) return;

			map.HideCityHighlights();
			map.cities.RemoveAt(cityIndex);
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}

		/// <summary>
		/// Deletes all cities of current selected country
		/// </summary>
		public void DeleteCountryCities() {
			if (countryIndex<0) return;
			
			map.HideCityHighlights();
			int k=-1;
			while(++k<map.cities.Count) {
				if (map.cities[k].countryIndex == countryIndex) {
					map.cities.RemoveAt(k);
					k--;
				}
			}
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}


		/// <summary>
		/// Deletes all cities of current selected country's continent
		/// </summary>
		public void DeleteCitiesSameContinent() {
			if (countryIndex<0) return;
			
			map.HideCityHighlights();
			int k=-1;
			string continent = map.countries[countryIndex].continent;
			while(++k<map.cities.Count) {
				int cindex = map.cities[k].countryIndex;
				if (cindex>=0) {
					string cityContinent = map.countries[cindex].continent;
					if (cityContinent.Equals(continent)) {
						map.cities.RemoveAt(k);
						k--;
					}
				}
			}
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}

	
		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current city data file
		/// </summary>
		public string GetCityGeoDataFileName() {
			return "cities10.txt";
		}
		
		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetCityGeoData () {
			StringBuilder sb = new StringBuilder ();
			for (int k=0; k<map.cities.Count; k++) {
				City city = map.cities[k];
				if (k > 0)
					sb.Append ("|");
				sb.Append (city.name + "$");
				if (city.province!=null && city.province.Length>0) {
					sb.Append (city.province + "$");
				} else {
					sb.Append ("$");
				}
				sb.Append (map.countries[city.countryIndex].name + "$");
				sb.Append (city.population + "$");
				sb.Append (city.unitySphereLocation.x * WorldMapGlobe.MAP_PRECISION + "$");
				sb.Append (city.unitySphereLocation.y * WorldMapGlobe.MAP_PRECISION + "$");
				sb.Append (city.unitySphereLocation.z * WorldMapGlobe.MAP_PRECISION + "$");
				sb.Append ((int)city.cityClass);
			}
			return sb.ToString ();
		}

		#endregion

	}
}
