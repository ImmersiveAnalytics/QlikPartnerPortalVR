// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM


using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Poly2Tri;

namespace WPM {

	public partial class WorldMapGlobe : MonoBehaviour {

		const float CITY_HIT_PRECISION = 0.00085f;

		#region Internal variables

		// resources
		Material citiesNormalMat, citiesRegionCapitalMat, citiesCountryCapitalMat;
		GameObject citiesLayer, citySpot, citySpotCapitalRegion, citySpotCapitalCountry;

		#endregion

		// internal cache
		City[] visibleCities;
		
		/// <summary>
		/// City look up dictionary. Used internally for fast searching of city objects.
		/// </summary>
		Dictionary<City, int>_cityLookup;
		int lastCityLookupCount = -1;
		
		Dictionary<City, int>cityLookup {
			get {
				if (_cityLookup != null && cities.Count == lastCityLookupCount)
					return _cityLookup;
				if (_cityLookup == null) {
					_cityLookup = new Dictionary<City,int> ();
				} else {
					_cityLookup.Clear ();
				}
				if (cities!=null) {
					for (int k=0; k<cities.Count; k++)
						_cityLookup.Add (cities [k], k);
				}
				lastCityLookupCount = _cityLookup.Count;
				return _cityLookup;
			}
		}


		#region System initialization

		void ReadCitiesPackedString () {
			string cityCatalogFileName = "Geodata/cities10";
			TextAsset ta = Resources.Load<TextAsset> (cityCatalogFileName);
			string s = ta.text;
			ReadCitiesPackedString(s);
		}

		/// <summary>
		/// Reads the cities data from a packed string.
		/// </summary>
		void ReadCitiesPackedString (string s) {
			string[] cityList = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int cityCount = cityList.Length;
			cities = new List<City> (cityCount);
			for (int k=0; k<cityCount; k++) {
				string[] cityInfo = cityList [k].Split (new char[] { '$' });
				string country = cityInfo [2];
				int countryIndex = GetCountryIndex(country);
				if (countryIndex>=0) {
					string name = cityInfo [0];
					string province = cityInfo [1];
					int population = int.Parse (cityInfo [3]);
					float x = float.Parse (cityInfo [4]) / MAP_PRECISION;
					float y = float.Parse (cityInfo [5]) / MAP_PRECISION;
					float z = float.Parse (cityInfo [6]) / MAP_PRECISION;
					CITY_CLASS cityClass = (CITY_CLASS)int.Parse(cityInfo[7]);
					City city = new City (name, province, countryIndex, population, new Vector3 (x, y, z), cityClass);
					cities.Add (city);
				}
			}
		}
	
	#endregion

	#region Drawing stuff

		/// <summary>
		/// Redraws the cities. This is automatically called by Redraw(). Used internally by the Map Editor. You should not need to call this method directly.
		/// </summary>
		public void DrawCities () {

			if (!_showCities || !gameObject.activeInHierarchy) return;

			// Create cities layer
			Transform t = transform.FindChild ("Cities");
			if (t != null)
				DestroyImmediate (t.gameObject);
			citiesLayer = new GameObject ("Cities");
			citiesLayer.transform.SetParent (transform, false);
			if (_earthInvertedMode) citiesLayer.transform.localScale *= 0.99f;

			// Create cityclass parents
			GameObject countryCapitals = new GameObject("Country Capitals");
			countryCapitals.hideFlags = HideFlags.DontSave;
			countryCapitals.transform.SetParent(citiesLayer.transform, false);
			GameObject regionCapitals = new GameObject("Region Capitals");
			regionCapitals.hideFlags = HideFlags.DontSave;
			regionCapitals.transform.SetParent(citiesLayer.transform, false);
			GameObject normalCities = new GameObject("Normal Cities");
			normalCities.hideFlags = HideFlags.DontSave;
			normalCities.transform.SetParent(citiesLayer.transform, false);

			// Draw city marks
			_numCitiesDrawn = 0;
			int minPopulation = _minPopulation * 1000;
			int visibleCount = 0;

			// flip localscale.x to prevent transform issues
			if (_earthInvertedMode)	transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

			for (int k=0; k<cities.Count; k++) {
				City city = cities [k];
				Country country = countries[city.countryIndex];
				city.isShown = !country.hidden && ( (((int)city.cityClass & _cityClassAlwaysShow) != 0) || (minPopulation==0 || city.population >= minPopulation) );
				if (city.isShown) {
					GameObject cityObj, cityParent;
					switch(city.cityClass) {
					case CITY_CLASS.COUNTRY_CAPITAL: 
						cityObj = Instantiate (citySpotCapitalCountry); 
						cityObj.GetComponent<Renderer> ().sharedMaterial = citiesCountryCapitalMat;
						cityParent = countryCapitals;
						break;
					case CITY_CLASS.REGION_CAPITAL: 
						cityObj = Instantiate (citySpotCapitalRegion); 
						cityObj.GetComponent<Renderer> ().sharedMaterial = citiesRegionCapitalMat;
						cityParent = regionCapitals;
						break;
					default:
						cityObj = Instantiate (citySpot); 
						cityObj.GetComponent<Renderer> ().sharedMaterial = citiesNormalMat;
						cityParent = normalCities;
						break;
					}
					cityObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
					cityObj.name = k.ToString();
					cityObj.transform.SetParent (cityParent.transform, false);
					cityObj.transform.localPosition = city.unitySphereLocation; // transform.TransformPoint(city.unitySphereLocation);
					if (_earthInvertedMode) {
						cityObj.transform.LookAt(transform.TransformPoint( city.unitySphereLocation * 2));
					} else {
						cityObj.transform.LookAt (transform.position);
					}
					_numCitiesDrawn++;
					visibleCount++;
				}
			}

			if (_earthInvertedMode)	transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

			// Cache visible cities (this is faster than iterate through the entire collection)
			if (visibleCities==null || visibleCities.Length!=visibleCount) 
				visibleCities = new City[visibleCount];
			for (int k=0;k<cities.Count;k++) {
				City city = cities[k];
				if (city.isShown) visibleCities[--visibleCount] = city;
			}

			// Toggle cities layer visibility according to settings
			citiesLayer.SetActive (_showCities);

			CityScaler cityScaler = citiesLayer.GetComponent<CityScaler>() ?? citiesLayer.AddComponent<CityScaler>();
			cityScaler.map = this;
			cityScaler.ScaleCities();
		}

		void HighlightCity(int cityIndex) {
			if (cityIndex == _cityHighlightedIndex) return;
			_cityHighlightedIndex = cityIndex;
			_cityHighlighted = cities[cityIndex];

			// Raise event
			if (OnCityEnter!=null) OnCityEnter(_cityHighlightedIndex);
		}

		void HideCityHighlight() {
			if (_cityHighlightedIndex<0) return;

			// Raise event
			if (OnCityExit!=null) OnCityExit(_cityHighlightedIndex);
			_cityHighlighted = null;
			_cityHighlightedIndex = -1;
		}

	#endregion

		#region Internal Cities API

		/// <summary>
		/// Returns any city near the point specified in local coordinates.
		/// </summary>
		public int GetCityNearPoint(Vector3 localPoint) {
			if (visibleCities==null) return -1;

			for (int c=0;c<visibleCities.Length;c++) {
				City city = visibleCities[c];
				Vector3 cityLoc = city.unitySphereLocation;
				if ( (cityLoc-localPoint).magnitude < CITY_HIT_PRECISION) {
					return GetCityIndex (city, false);
				}
			}
			return -1;
		}

		bool GetCityUnderMouse(int countryIndex, Vector3 localPoint, out int cityIndex) {
			float hitPrecission = CITY_HIT_PRECISION * _cityIconSize * 5.0f;
			for (int c=0;c<visibleCities.Length;c++) {
				City city = visibleCities[c];
				if (city.countryIndex == countryIndex && city.isShown) {
					if ( (city.unitySphereLocation-localPoint).magnitude < hitPrecission) {
						cityIndex = GetCityIndex (city, false);
						return true;
					}
				}
			}
			cityIndex = -1;
			return false;
		}


		/// <summary>
		/// Returns cities belonging to a provided country.
		/// </summary>
		public List<City>GetCities(int countryIndex) {
			List<City>results = new List<City>(20);
			for (int c=0;c<cities.Count;c++) {
				if (cities[c].countryIndex==countryIndex) results.Add (cities[c]);
			}
			return results;
		}

		public string GetCityHierarchyName(int cityIndex) {
			if (cityIndex<0 || cityIndex>=cities.Count) return "";
			switch(cities[cityIndex].cityClass) {
			case CITY_CLASS.COUNTRY_CAPITAL: return "Cities/Country Capitals/" + cityIndex.ToString();
			case CITY_CLASS.REGION_CAPITAL: return "Cities/Region Capitals/" + cityIndex.ToString();
			default: return "Cities/Normal Cities/" + cityIndex.ToString();
			}
		}

		/// <summary>
		/// Updates the city scale.
		/// </summary>
		public void ScaleCities() {
			if (citiesLayer!=null) {
				CityScaler scaler = citiesLayer.GetComponent<CityScaler>();
				if (scaler!=null) scaler.ScaleCities();
			}
		}

		#endregion
	}

}