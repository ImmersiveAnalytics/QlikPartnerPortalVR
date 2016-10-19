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

	public delegate void OnCityEnter(int cityIndex);
	public delegate void OnCityExit(int cityIndex);
	public delegate void OnCityClick(int cityIndex);

	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		public event OnCityEnter OnCityEnter;
		public event OnCityEnter OnCityExit;
		public event OnCityClick OnCityClick;

		public const int CITY_CLASS_FILTER_REGION_CAPITAL_CITY = 2;
		public const int CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY = 4;

		/// <summary>
		/// Complete list of cities with their names and country names.
		/// </summary>
		[NonSerialized]
		public List<City>
			cities;


		City _cityHighlighted;
		/// <summary>
		/// Returns City under mouse position or null if none.
		/// </summary>
		public City cityHighlighted { get { return _cityHighlighted; } }

		int _cityHighlightedIndex = -1;
		/// <summary>
		/// Returns City index mouse position or null if none.
		/// </summary>
		public int cityHighlightedIndex { get { return _cityHighlightedIndex; } }

		int _cityLastClicked = -1;
		/// <summary>
		/// Returns the last clicked city index.
		/// </summary>
		public int cityLastClicked { get { return _cityLastClicked; } }
		
	
		
		[SerializeField]
		bool
			_showCities = true;
		
		/// <summary>
		/// Toggle cities visibility.
		/// </summary>
		public bool showCities { 
			get {
				return _showCities; 
			}
			set {
				if (_showCities != value) {
					_showCities = value;
					isDirty = true;
					if (citiesLayer != null) {
						citiesLayer.SetActive (_showCities);
					} else if (_showCities) {
						DrawCities ();
					}
				}
			}
		}
		
		[NonSerialized]
		int
			_numCitiesDrawn = 0;
		
		/// <summary>
		/// Gets the number cities drawn.
		/// </summary>
		public int numCitiesDrawn { get { return _numCitiesDrawn; } }
		
		
	

		[SerializeField]
		Color
			_citiesColor = Color.white;
		
		/// <summary>
		/// Global color for cities.
		/// </summary>
		public Color citiesColor {
			get {
				if (citiesNormalMat != null) {
					return citiesNormalMat.color;
				} else {
					return _citiesColor;
				}
			}
			set {
				if (value != _citiesColor) {
					_citiesColor = value;
					isDirty = true;
					
					if (citiesNormalMat != null && _citiesColor != citiesNormalMat.color) {
						citiesNormalMat.color = _citiesColor;
					}
				}
			}
		}

		[SerializeField]
		Color
			_citiesRegionCapitalColor = Color.cyan;
		
		/// <summary>
		/// Global color for region capitals.
		/// </summary>
		public Color citiesRegionCapitalColor {
			get {
				if (citiesRegionCapitalMat != null) {
					return citiesRegionCapitalMat.color;
				} else {
					return _citiesRegionCapitalColor;
				}
			}
			set {
				if (value != _citiesRegionCapitalColor) {
					_citiesRegionCapitalColor = value;
					isDirty = true;
					
					if (citiesRegionCapitalMat != null && _citiesRegionCapitalColor != citiesRegionCapitalMat.color) {
						citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
					}
				}
			}
		}
		
		
		[SerializeField]
		Color
			_citiesCountryCapitalColor = Color.yellow;
		
		/// <summary>
		/// Global color for country capitals.
		/// </summary>
		public Color citiesCountryCapitalColor {
			get {
				if (citiesCountryCapitalMat != null) {
					return citiesCountryCapitalMat.color;
				} else {
					return _citiesCountryCapitalColor;
				}
			}
			set {
				if (value != _citiesCountryCapitalColor) {
					_citiesCountryCapitalColor = value;
					isDirty = true;
					
					if (citiesCountryCapitalMat != null && _citiesCountryCapitalColor != citiesCountryCapitalMat.color) {
						citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
					}
				}
			}
		}		


		[SerializeField]
		float _cityIconSize = 0.2f;
		
		/// <summary>
		/// The size of the cities icon (dot).
		/// </summary>
		public float cityIconSize {
			get {
				return _cityIconSize;
			}
			set {
				if (value!=_cityIconSize) {
					_cityIconSize = value;
					isDirty = true;
					ScaleCities();
					ScaleMountPoints();
				}
			}
		}

		
		[Range(0, 17000)]
		[SerializeField]
		int
			_minPopulation = 1500;
		
		public int minPopulation {
			get {
				return _minPopulation;
			}
			set {
				if (value != _minPopulation) {
					_minPopulation = value;
					isDirty = true;
					DrawCities ();
				}
			}
		}


		[SerializeField]
		int _cityClassAlwaysShow;

		/// <summary>
		/// Flags for specifying the class of cities to always show irrespective of other filters like minimum population. Can assign a combination of bit flags defined by CITY_CLASS_FILTER* constants.
		/// </summary>
		public int cityClassAlwaysShow {
			get { return _cityClassAlwaysShow; }
			set { if (_cityClassAlwaysShow!=value) {
					_cityClassAlwaysShow = value;
					isDirty = true;
					DrawCities();
				}
			}
		}


	#region Public API area

	

		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public bool FlyToCity (string name) {
			int cityIndex = GetCityIndex(name);
			if (cityIndex<0) return false;
			FlyToCity (cities[cityIndex]);
			return false;
		}

		/// <summary>
		/// Starts navigation to target city by index in the cities collection. Returns false if not found.
		/// </summary>
		public void FlyToCity (int cityIndex) {
			FlyToCity(cities[cityIndex]);
		}

		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public void FlyToCity (City city) {
			FlyToCity(city, _navigationTime);
		}

		/// <summary>
		/// Starts navigation to target city with duration (seconds). Returns false if not found.
		/// </summary>
		public void FlyToCity (City city, float duration) {
			FlyToLocation(city.unitySphereLocation, duration);
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames () {
			return GetCityNames(true);
		}
	
		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames (bool includeCityIndex) {
			List<string> c = new List<string> (cities.Count);
			for (int k=0; k<cities.Count; k++) {
				if (includeCityIndex) {
					c.Add (cities [k].name + " (" + k + ")");
				} else {
					c.Add (cities [k].name);
				}
			}
			c.Sort ();
			return c.ToArray ();
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames (int countryIndex, bool includeCityIndex) {
			List<string> c = new List<string> (cities.Count);
			for (int k=0; k<cities.Count; k++) {
				if (cities[k].countryIndex == countryIndex) {
					if (includeCityIndex) {
						c.Add (cities [k].name + " (" + k + ")");
					} else {
						c.Add (cities [k].name);
					}				}
			}
			c.Sort ();
			return c.ToArray ();
		}

		/// <summary>
		/// Returns the index of the city by its name in the cities collection.
		/// </summary>
		public int GetCityIndex(string cityName) {
			for (int k=0; k<cities.Count; k++) {
				if (cityName.Equals (cities [k].name)) {
					return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the index of a city in the cities collection by its reference.
		/// </summary>
		public int GetCityIndex(City city) {
			return GetCityIndex(city, true);
		}

		/// <summary>
		/// Returns the index of a city in the cities collection by its reference.
		/// </summary>
		public int GetCityIndex (City city, bool includeNotVisible)
		{
			if (includeNotVisible) return cities.IndexOf(city);
			if (cityLookup.ContainsKey (city)) 
				return cityLookup [city];
			else
				return -1;
		}

		/// <summary>
		/// Returns the index of a city in the global countries collection. Note that country index needs to be supplied due to repeated city names.
		/// </summary>
		public int GetCityIndex (int countryIndex, string cityName) {
			if (countryIndex >= 0 && countryIndex < countries.Length) {
				for (int k=0; k<cities.Count; k++) {
					if (cities [k].name.Equals (cityName) && cities [k].countryIndex == countryIndex)
						return k;
				}
			} else {
				// Try to select city by its name alone
				for (int k=0; k<cities.Count; k++) {
					if (cities [k].name.Equals (cityName))
						return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the city index by screen position.
		/// </summary>
		public bool GetCityIndex (Ray ray, out int cityIndex) {
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 localHit = transform.InverseTransformPoint (hits [k].point);
						int c = GetCityNearPoint (localHit);
						if (c >= 0) {
							cityIndex = c;
							return true;
						}
					}
				}
			}
			cityIndex = -1;
			return false;
		}


		/// <summary>
		/// Returns the index of the nearest city to a location (lat/lon).
		/// </summary>
		public int GetCityIndex (float lat, float lon) {
			Vector3 spherePosition = GetSpherePointFromLatLon(lat, lon);
			return GetCityIndex(spherePosition);
		}


		/// <summary>
		/// Returns the index of the nearest city to a location (sphere position).
		/// </summary>
		public int GetCityIndex (Vector3 spherePosition) {
			float minDist = float.MaxValue;
			int cityIndex = -1;

			for (int k=0;k<cities.Count;k++) {
				float dist = (cities[k].unitySphereLocation - spherePosition).sqrMagnitude;
				if (dist<minDist) {
					minDist = dist;
					cityIndex = k;
				}
			}
			return cityIndex;
		}

		/// <summary>
		/// Clears any city highlighted (color changed) and resets them to default city color
		/// </summary>
		public void HideCityHighlights () {
			if (citiesLayer == null)
				return;
			Renderer[] rr = citiesLayer.GetComponentsInChildren<Renderer>(true);
			for (int k=0;k<rr.Length;k++) {
				string matName = rr[k].sharedMaterial.name;
				if (matName.Equals("Cities")) {
					rr[k].sharedMaterial = citiesNormalMat;
				} else if (matName.Equals("CitiesCapitalRegion")) {
					rr[k].sharedMaterial = citiesRegionCapitalMat;
				} else if (matName.Equals("CitiesCapitalCountry")) {
					rr[k].sharedMaterial = citiesCountryCapitalMat;
				}
			}
		}

		/// <summary>
		/// Toggles the city highlight.
		/// </summary>
		/// <param name="cityIndex">City index.</param>
		/// <param name="color">Color.</param>
		/// <param name="highlighted">If set to <c>true</c> the color of the city will be changed. If set to <c>false</c> the color of the city will be reseted to default color</param>
		public void ToggleCityHighlight (int cityIndex, Color color, bool highlighted) {
			if (citiesLayer == null)
				return;
			string cobj = GetCityHierarchyName(cityIndex);
			Transform t = transform.FindChild (cobj);
			if (t == null)
				return;
			Renderer rr = t.gameObject.GetComponent<Renderer> ();
			if (rr == null)
				return;
			Material mat;
			if (highlighted) {
				mat = Instantiate (rr.sharedMaterial);
				mat.name = rr.sharedMaterial.name;
				mat.hideFlags = HideFlags.DontSave;
				mat.color = color;
				rr.sharedMaterial = mat;
			} else {
				switch(cities[cityIndex].cityClass) {
				case CITY_CLASS.COUNTRY_CAPITAL: mat = citiesCountryCapitalMat; break;
				case CITY_CLASS.REGION_CAPITAL: mat = citiesRegionCapitalMat; break;
				default: mat = citiesNormalMat; break;
				}
				rr.sharedMaterial = mat;
			}
		}
		
		/// <summary>
		/// Flashes specified city by index in the global city collection.
		/// </summary>
		public void BlinkCity (int cityIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			if (citiesLayer == null)
				return;
			string cobj = GetCityHierarchyName(cityIndex);
			Transform t = transform.FindChild (cobj);
			if (t == null)
				return;
			CityBlinker sb = t.gameObject.AddComponent<CityBlinker> ();
			sb.blinkMaterial = t.GetComponent<Renderer>().sharedMaterial;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
		}

		/// <summary>
		/// Deletes all cities of current selected country's continent
		/// </summary>
		public void CitiesDeleteFromContinent(string continentName) {
			HideCityHighlights();
			int k=-1;
			while(++k<cities.Count) {
				int cindex = cities[k].countryIndex;
				if (cindex>=0) {
					string cityContinent = countries[cindex].continent;
					if (cityContinent.Equals(continentName)) {
						cities.RemoveAt(k);
						k--;
					}
				}
			}
		}

		#endregion


	}

}