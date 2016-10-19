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

	public enum FRONTIERS_DETAIL {
		Low = 0,
		High = 1
	}

	public enum LABELS_QUALITY {
		Low = 0,
		Medium =1,
		High = 2
	}


	/* Event definitions */
	public delegate void OnCountryEnter(int countryIndex, int regionIndex);
	public delegate void OnCountryExit(int countryIndex, int regionIndex);
	public delegate void OnCountryClick(int countryIndex, int regionIndex);

	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		public event OnCountryEnter OnCountryEnter;
		public event OnCountryExit OnCountryExit;
		public event OnCountryClick OnCountryClick;


		/// <summary>
		/// Complete list of countries and the continent name they belong to.
		/// </summary>
		[NonSerialized]
		public Country[] countries;
	
		Country _countryHighlighted;
		/// <summary>
		/// Returns Country under mouse position or null if none.
		/// </summary>
		public Country countryHighlighted { get { return _countryHighlighted; } }
		
		int	_countryHighlightedIndex = -1;
		/// <summary>
		/// Returns currently highlighted country index in the countries list.
		/// </summary>
		public int countryHighlightedIndex { get { return _countryHighlightedIndex; } }
		
		Region _countryRegionHighlighted;
		/// <summary>
		/// Returns currently highlightd country's region.
		/// </summary>
		/// <value>The country region highlighted.</value>
		public Region countryRegionHighlighted { get { return _countryRegionHighlighted; } }
		
		int _countryRegionHighlightedIndex = -1;
		/// <summary>
		/// Returns currently highlighted region of the country.
		/// </summary>
		public int countryRegionHighlightedIndex { get { return _countryRegionHighlightedIndex; } }
		
		int _countryLastClicked = -1;
		/// <summary>
		/// Returns the last clicked country.
		/// </summary>
		public int countryLastClicked { get { return _countryLastClicked; } }

		int _countryRegionLastClicked = -1;
		/// <summary>
		/// Returns the last clicked country region index.
		/// </summary>
		public int countryRegionLastClicked { get { return _countryRegionLastClicked; } }

		[SerializeField]
		bool
			_enableCountryHighlight = true;
		/// <summary>
		/// Enable/disable country highlight when mouse is over.
		/// </summary>
		public bool enableCountryHighlight {
			get {
				return _enableCountryHighlight;
			}
			set {
				if (_enableCountryHighlight != value) {
					_enableCountryHighlight = value;
					isDirty = true;
				}
			}
		}


		[SerializeField]
		bool
			_showFrontiers = true;
		
		/// <summary>
		/// Toggle frontiers visibility.
		/// </summary>
		public bool showFrontiers { 
			get {
				return _showFrontiers; 
			}
			set {
				if (value != _showFrontiers) {
					_showFrontiers = value;
					isDirty = true;
					
					if (frontiersLayer != null) {
						frontiersLayer.SetActive (_showFrontiers);
					} else if (_showFrontiers) {
						DrawFrontiers ();
					}
				}
			}
		}
	
		
		[SerializeField]
		Color
			_fillColor = new Color (1, 0, 0, 0.7f);
		
		/// <summary>
		/// Fill color to use when the mouse hovers a country's region.
		/// </summary>
		public Color fillColor {
			get {
				if (hudMatCountry != null) {
					return hudMatCountry.color;
				} else {
					return _fillColor;
				}
			}
			set {
				if (_fillColor!=value) {
					_fillColor = value;
					isDirty = true;
					if (hudMatCountry != null && _fillColor != hudMatCountry.color) {
						hudMatCountry.color = _fillColor;
					}
				}
			}
		}
	
		
		
		[SerializeField]
		Color
			_frontiersColor = Color.green;
		
		/// <summary>
		/// Global color for frontiers.
		/// </summary>
		public Color frontiersColor {
			get {
				if (frontiersMat != null) {
					return frontiersMat.color;
				} else {
					return _frontiersColor;
				}
			}
			set {
				if (value != _frontiersColor) {
					_frontiersColor = value;
					isDirty = true;
					
					if (frontiersMat != null && _frontiersColor != frontiersMat.color) {
						frontiersMat.color = _frontiersColor;
					}
				}
			}
		}
	

		[SerializeField]
		bool
			_showOutline = true;
		
		/// <summary>
		/// Toggle frontiers visibility.
		/// </summary>
		public bool showOutline { 
			get {
				return _showOutline; 
			}
			set {
				if (value != _showOutline) {
					_showOutline = value;
					Redraw (); // recreate surfaces layer
					isDirty = true;
				}
			}
		}
		
		[SerializeField]
		Color
			_outlineColor = Color.black;
		
		/// <summary>
		/// Global color for frontiers.
		/// </summary>
		public Color outlineColor {
			get {
				if (outlineMat != null) {
					return outlineMat.color;
				} else {
					return _outlineColor;
				}
			}
			set {
				if (value != _outlineColor) {
					_outlineColor = value;
					isDirty = true;
					
					if (outlineMat != null && _outlineColor != outlineMat.color) {
						outlineMat.color = _outlineColor;
					}
				}
			}
		}

		[SerializeField]
		FRONTIERS_DETAIL
			_frontiersDetail = FRONTIERS_DETAIL.Low;
		
		public FRONTIERS_DETAIL frontiersDetail {
			get { return _frontiersDetail; }
			set { 
				if (_frontiersDetail != value) {
					_frontiersDetail = value;
					isDirty = true;
					ReloadData ();
				}
			}
		}

		[SerializeField]
		bool
			_showCountryNames = false;
		
		public bool showCountryNames {
			get {
				return _showCountryNames;
			}
			set {
				if (value != _showCountryNames) {
					#if TRACE_CTL
					Debug.Log ("CTL " + DateTime.Now + ": showcountrynames!");
					#endif
					_showCountryNames = value;
					isDirty = true;
					if (gameObject.activeInHierarchy) {
						if (!showCountryNames) {
							DestroyMapLabels ();
						} else {
							DrawMapLabels ();
							// Cool scrolling animation for map labels following...
							if (Application.isPlaying) {
								for (int k=0; k<countries.Length; k++) {
									GameObject o = countries [k].labelGameObject.gameObject;
									LabelAnimator anim = o.AddComponent<LabelAnimator> ();
									anim.destPos = o.transform.localPosition;
									anim.startPos = o.transform.localPosition + Vector3.right * 100.0f * Mathf.Sign (o.transform.localPosition.x);
									anim.duration = 1.0f;
								}
							}
						}
					}
				}
			}
		}


		[SerializeField]
		bool _countryLabelsEnableAutomaticFade = true;

		/// <summary>
		/// Automatic fading of country labels depending on camera distance and label screen size
		/// </summary>
		public bool countryLabelsEnableAutomaticFade {
			get { return _countryLabelsEnableAutomaticFade; }
			set { if (_countryLabelsEnableAutomaticFade!=value) { 
					_countryLabelsEnableAutomaticFade = value; 
					FadeCountryLabels();
					isDirty = true; } 
			}
		}

		[SerializeField]
		float
			_countryLabelsAutoFadeMaxHeight = 0.3f;

		/// <summary>
		/// Max height of a label relative to screen height (0..1) at which fade out starts
		/// </summary>
		public float countryLabelsAutoFadeMaxHeight {
			get {
				return _countryLabelsAutoFadeMaxHeight;
			} 
			set {
				if (value != _countryLabelsAutoFadeMaxHeight) {
					_countryLabelsAutoFadeMaxHeight = value;
					_countryLabelsAutoFadeMinHeight = Mathf.Min (_countryLabelsAutoFadeMaxHeight, _countryLabelsAutoFadeMinHeight);
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		
		[SerializeField]
		float
			_countryLabelsAutoFadeMaxHeightFallOff = 0.2f;
		
		/// <summary>
		/// Fall off for fade labels when height is greater than min height
		/// </summary>
		public float countryLabelsAutoFadeMaxHeightFallOff {
			get {
				return _countryLabelsAutoFadeMaxHeightFallOff;
			} 
			set {
				if (value != _countryLabelsAutoFadeMaxHeightFallOff) {
					_countryLabelsAutoFadeMaxHeightFallOff = value;
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}


		[SerializeField]
		float
			_countryLabelsAutoFadeMinHeight = 0.02f;
		
		/// <summary>
		/// Min height of a label relative to screen height (0..1) at which fade out starts
		/// </summary>
		public float countryLabelsAutoFadeMinHeight {
			get {
				return _countryLabelsAutoFadeMinHeight;
			} 
			set {
				if (value != _countryLabelsAutoFadeMinHeight) {
					_countryLabelsAutoFadeMinHeight = value;
					_countryLabelsAutoFadeMaxHeight = Mathf.Max (_countryLabelsAutoFadeMaxHeight, _countryLabelsAutoFadeMinHeight);
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		[SerializeField]
		float
			_countryLabelsAutoFadeMinHeightFallOff = 0.02f;
		
		/// <summary>
		/// Fall off for fade labels when height is less than min height
		/// </summary>
		public float countryLabelsAutoFadeMinHeightFallOff {
			get {
				return _countryLabelsAutoFadeMinHeightFallOff;
			} 
			set {
				if (value != _countryLabelsAutoFadeMinHeightFallOff) {
					_countryLabelsAutoFadeMinHeightFallOff = value;
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}


		[SerializeField]
		float
			_countryLabelsAbsoluteMinimumSize = 0.5f;
		
		public float countryLabelsAbsoluteMinimumSize {
			get {
				return _countryLabelsAbsoluteMinimumSize;
			} 
			set {
				if (value != _countryLabelsAbsoluteMinimumSize) {
					_countryLabelsAbsoluteMinimumSize = value;
					isDirty = true;
					if (_showCountryNames)
						DrawMapLabels ();
				}
			}
		}
		
		[SerializeField]
		float
			_countryLabelsSize = 0.25f;
		
		public float countryLabelsSize {
			get {
				return _countryLabelsSize;
			} 
			set {
				if (value != _countryLabelsSize) {
					_countryLabelsSize = value;
					isDirty = true;
					if (_showCountryNames)
						DrawMapLabels ();
				}
			}
		}
		
		[SerializeField]
		LABELS_QUALITY
			_labelsQuality = LABELS_QUALITY.Medium;
		
		public LABELS_QUALITY labelsQuality {
			get {
				return _labelsQuality;
			}
			set {
				if (value != _labelsQuality) {
					_labelsQuality = value;
					isDirty = true;
					if (_showCountryNames) {
						DestroyOverlay (); // needs to recreate the render texture
						DrawMapLabels ();
					}
				}
			}
		}
		
		
		[SerializeField]
		float _labelsElevation = 0;
		
		public float labelsElevation {
			get {
				return _labelsElevation;
			}
			set {
				if (value != _labelsElevation) {
					_labelsElevation = value;
					isDirty = true;
					if (sphereOverlayLayer!=null) {
						AdjustSphereOverlayLayerScale();
					}
				}
			}
		}
		
		
		[SerializeField]
		bool
			_showLabelsShadow = true;
		
		/// <summary>
		/// Draws a shadow under map labels. Specify the color using labelsShadowColor.
		/// </summary>
		/// <value><c>true</c> if show labels shadow; otherwise, <c>false</c>.</value>
		public bool showLabelsShadow {
			get {
				return _showLabelsShadow;
			}
			set {
				if (value != _showLabelsShadow) {
					_showLabelsShadow = value;
					isDirty = true;
					if (gameObject.activeInHierarchy) {
						RedrawMapLabels();
					}
				}
			}
		}
		
		[SerializeField]
		Color
			_countryLabelsColor = Color.white;
		
		/// <summary>
		/// Color for map labels.
		/// </summary>
		public Color countryLabelsColor {
			get {
				return _countryLabelsColor;
			}
			set {
				if (value != _countryLabelsColor) {
					_countryLabelsColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy) {
						labelsFont.material.color = _countryLabelsColor;
					}
				}
			}
		}
		
		[SerializeField]
		Color
			_countryLabelsShadowColor = Color.black;
		
		/// <summary>
		/// Color for map labels.
		/// </summary>
		public Color countryLabelsShadowColor {
			get {
				return _countryLabelsShadowColor;
			}
			set {
				if (value != _countryLabelsShadowColor) {
					_countryLabelsShadowColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy) {
						labelsShadowMaterial.color = _countryLabelsShadowColor;
					}
				}
			}
		}
	

	#region Public API area

		/// <summary>
		/// Adds a new country which has been properly initialized. Used by the Map Editor. Name must be unique.
		/// </summary>
		/// <returns><c>true</c> if country was added, <c>false</c> otherwise.</returns>
		public bool CountryAdd (Country country) {
			int countryIndex = GetCountryIndex (country.name);
			if (countryIndex >= 0)
				return false;
			Country[] newCountries = new Country[countries.Length + 1];
			for (int k=0; k<countries.Length; k++) {
				newCountries [k] = countries [k];
			}
			newCountries [newCountries.Length - 1] = country;
			countries = newCountries;
			lastCountryLookupCount = -1;
			return true;
		}

		/// <summary>
		/// Renames the country. Name must be unique, different from current and one letter minimum.
		/// </summary>
		/// <returns><c>true</c> if country was renamed, <c>false</c> otherwise.</returns>
		public bool CountryRename (string oldName, string newName) {
			if (newName == null || newName.Length == 0)
				return false;
			int countryIndex = GetCountryIndex (oldName);
			int newCountryIndex = GetCountryIndex (newName);
			if (countryIndex < 0 || newCountryIndex >= 0)
				return false;
			countries [countryIndex].name = newName;
			lastCountryLookupCount = -1;
			return true;
			
		}
		
		
		/// <summary>
		/// Deletes the country. Optionally also delete its dependencies (provinces, cities, mountpoints).
		/// </summary>
		/// <returns><c>true</c> if country was deleted, <c>false</c> otherwise.</returns>
		public bool CountryDelete (int countryIndex, bool deleteDependencies) {
			if (countryIndex <0 || countryIndex>=countries.Length)
				return false;
			
			// Update dependencies
			if (deleteDependencies) {
				HideProvinceRegionHighlights(true);
				List<Province>newProvinces = new List<Province>(provinces.Length);
				int k;
				for (k=0;k<provinces.Length;k++) {
					if (provinces[k].countryIndex!=countryIndex) newProvinces.Add (provinces[k]);
				}
				provinces = newProvinces.ToArray();
				lastProvinceLookupCount = -1;

				HideCityHighlights();
				k=-1;
				while(++k<cities.Count) {
					if (cities[k].countryIndex == countryIndex) {
						cities.RemoveAt(k);
						k--;
					}
				}

				HideMountPointHighlights();
				k=-1;
				while(++k<mountPoints.Count) {
					if (mountPoints[k].countryIndex == countryIndex) {
						mountPoints.RemoveAt(k);
						k--;
					}
				}
			}

			// Updates provinces reference to country
			for (int k=0;k<provinces.Length;k++) {
				if (provinces[k].countryIndex>countryIndex)
					provinces[k].countryIndex--;
			}

			// Updates country index in cities
			for (int k=0;k<cities.Count;k++) {
				if (cities[k].countryIndex >countryIndex) {
					cities[k].countryIndex--;
				}
			}
			// Updates country index in mount points
			if (mountPoints!=null) {
				for (int k=0;k<mountPoints.Count;k++) {
					if (mountPoints[k].countryIndex >countryIndex) {
						mountPoints[k].countryIndex--;
					}
				}
			}

			// Excludes country from new array
			List<Country>newCountries = new List<Country>(countries.Length);
			for (int k=0;k<countries.Length;k++) {
				if (k!=countryIndex) newCountries.Add (countries[k]);
			}
			countries = newCountries.ToArray();
			
			// Update lookup dictionaries
			lastCountryLookupCount = -1;
			
			return true;
		}

		/// <summary>
		/// Deletes all provinces from a country.
		/// </summary>
		/// <returns><c>true</c>, if provinces where deleted, <c>false</c> otherwise.</returns>
		public bool CountryDeleteProvinces(int countryIndex) {
			int numProvinces = provinces.Length;
			List<Province> newProvinces = new List<Province>(numProvinces);
			for (int k=0;k<numProvinces;k++) {
				if (provinces[k]!=null && provinces[k].countryIndex!= countryIndex) {
					newProvinces.Add (provinces[k]);
				}
			}
			provinces = newProvinces.ToArray();
			lastProvinceLookupCount = -1;
			return true;
		}


		public void CountriesDeleteFromContinent(string continentName) {
			
			HideCountryRegionHighlights(true);
			
			ProvincesDeleteOfSameContinent(continentName);
			CitiesDeleteFromContinent(continentName);
			MountPointsDeleteFromSameContinent(continentName);
			
			List<Country> newAdmins = new List<Country>(countries.Length-1);
			for (int k=0;k<countries.Length;k++) {
				if (!countries[k].continent.Equals (continentName)) {
					newAdmins.Add (countries[k]);
				} else {
					int lastIndex = newAdmins.Count-1;
					// Updates country index in provinces
					if (provinces!=null) {
						for (int p=0;p<_provinces.Length;p++) {
							if (_provinces[p].countryIndex>lastIndex) {
								_provinces[p].countryIndex--;
							}
						}
					}
					// Updates country index in cities
					if (cities!=null) {
						for (int c=0;c<cities.Count;c++) {
							if (cities[c].countryIndex>lastIndex) {
								cities[c].countryIndex--;
							}
						}
					}
					// Updates country index in mount points
					if (mountPoints!=null) {
						for (int c=0;c<mountPoints.Count;c++) {
							if (mountPoints[c].countryIndex>lastIndex) {
								mountPoints[c].countryIndex--;
							}
						}
					}
				}
			}
			
			countries = newAdmins.ToArray();
			lastCountryLookupCount = -1;
			
		}


		/// <summary>
		/// Returns the index of a country in the countries collection by its name.
		/// </summary>
		public int GetCountryIndex (string countryName) {
			if (countryLookup!=null && countryLookup.ContainsKey(countryName)) 
				return countryLookup[countryName];
			else
				return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries collection by its reference.
		/// </summary>
		public int GetCountryIndex (Country country) {
			if (countryLookup.ContainsKey (country.name)) 
				return countryLookup [country.name];
			else
				return -1;
		}


		/// <summary>
		/// Used by Editor. Returns the country index by screen position defined by a ray in the Scene View.
		/// </summary>
		public bool GetCountryIndex (Ray ray, out int countryIndex, out int regionIndex) {
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 localHit = transform.InverseTransformPoint (hits [k].point);
						if (GetCountryUnderMouse (localHit, out countryIndex, out regionIndex)) {
							return true;
						}
					}
				}
			}
			countryIndex = -1;
			regionIndex = -1;
			return false;
		}

		/// <summary>
		/// Returns all neighbour countries
		/// </summary>
		public List<Country> CountryNeighbours (int countryIndex)
		{
			
			List<Country> countryNeighbours = new List<Country> ();
			
			// Get country object
			Country country = countries [countryIndex];
			
			// Iterate for all regions (a country can have several separated regions)
			for (int countryRegionIndex=0; countryRegionIndex<country.regions.Count; countryRegionIndex++) {
				Region countryRegion = country.regions [countryRegionIndex];
				
				// Get the neighbours for this region
				for (int neighbourIndex=0; neighbourIndex<countryRegion.neighbours.Count; neighbourIndex++) {
					Region neighbour = countryRegion.neighbours [neighbourIndex];
					Country neighbourCountry = (Country)neighbour.entity;	
					if (!countryNeighbours.Contains (neighbourCountry)) {
						countryNeighbours.Add (neighbourCountry);
					}
				}
			}
			
			return countryNeighbours;
		}
		
		
		/// <summary>
		/// Get neighbours of the main region of a country
		/// </summary>
		public List<Country> CountryNeighboursOfMainRegion (int countryIndex)
		{
			
			List<Country> countryNeighbours = new List<Country> ();
			
			// Get main region
			Country country = countries [countryIndex];
			Region countryRegion = country.regions [country.mainRegionIndex];
			
			// Get the neighbours for this region
			for (int neighbourIndex=0; neighbourIndex<countryRegion.neighbours.Count; neighbourIndex++) {
				Region neighbour = countryRegion.neighbours [neighbourIndex];
				Country neighbourCountry = (Country)neighbour.entity;	
				if (!countryNeighbours.Contains (neighbourCountry)) {
					countryNeighbours.Add (neighbourCountry);
				}
			}
			return countryNeighbours;
		}
		
		
		/// <summary>
		/// Get neighbours of the currently selected region
		/// </summary>
		public List<Country> CountryNeighboursOfCurrentRegion ()
		{
			
			List<Country> countryNeighbours = new List<Country> ();
			
			// Get main region
			Region selectedRegion = countryRegionHighlighted;
			if (selectedRegion == null)
				return countryNeighbours;
			
			// Get the neighbours for this region
			for (int neighbourIndex=0; neighbourIndex<selectedRegion.neighbours.Count; neighbourIndex++) {
				Region neighbour = selectedRegion.neighbours [neighbourIndex];
				Country neighbourCountry = (Country)neighbour.entity;	
				if (!countryNeighbours.Contains (neighbourCountry)) {
					countryNeighbours.Add (neighbourCountry);
				}
			}
			return countryNeighbours;
		}


		public bool GetCountryUnderSpherePosition(Vector3 spherePoint, out int countryIndex, out int countryRegionIndex) {
			return GetCountryUnderMouse(spherePoint, out countryIndex, out countryRegionIndex);
		}


		/// <summary>
		/// Starts navigation to target country. Returns false if country is not found.
		/// </summary>
		public bool FlyToCountry (string name) {
			int countryIndex = GetCountryIndex(name);
			if (countryIndex>=0) {
				FlyToCountry (countryIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target country. with specified duration, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// Returns false if country is not found. 
		/// </summary>
		public bool FlyToCountry (string name, float duration) {
			int countryIndex = GetCountryIndex(name);
			if (countryIndex>=0) {
				FlyToCountry (countryIndex, duration);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target country by index in the countries collection. Returns false if country is not found.
		/// </summary>
		public void FlyToCountry (int countryIndex) {
			FlyToCountry (countryIndex, _navigationTime);
		}

		/// <summary>
		/// Starts navigating to target country by index in the countries collection with specified duration, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// </summary>
		public void FlyToCountry (int countryIndex, float duration) {
			FlyToLocation(countries [countryIndex].center, duration);
		}



		/// <summary>
		/// Colorize all regions of specified country by name. Returns false if not found.
		/// </summary>
		public bool ToggleCountrySurface (string name, bool visible, Color color) {
			int countryIndex = GetCountryIndex(name);
			if (countryIndex>=0) {
				ToggleCountrySurface (countryIndex, visible, color);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Colorize all regions of specified country by index in the countries collection.
		/// </summary>
		public void ToggleCountrySurface (int countryIndex, bool visible, Color color) {
			ToggleCountrySurface(countryIndex, visible, color, null);
		}

		/// <summary>
		/// Colorize all regions of specified country and assings a texture.
		/// </summary>
		public void ToggleCountrySurface (int countryIndex, bool visible, Color color, Texture2D texture) {
			if (!visible) {
				HideCountrySurface(countryIndex);
				return;
			}
			if (countryIndex<0 || countryIndex>=countries.Length) return;
			for (int r=0; r<countries[countryIndex].regions.Count; r++) {
				ToggleCountryRegionSurface(countryIndex, r, visible, color, texture, MiscVector.Vector2one, MiscVector.Vector2zero, 0);
			}
		}

		/// <summary>
		/// Uncolorize/hide specified country by index in the countries collection.
		/// </summary>
		public void HideCountrySurface (int countryIndex) {
			if (countryIndex<0 || countryIndex>=countries.Length) return;
			for (int r=0; r<countries[countryIndex].regions.Count; r++) {
				HideCountryRegionSurface(countryIndex, r);
			}
		}

		/// <summary>
		/// Colorize main region of a country by index in the countries collection.
		/// </summary>
		public void ToggleCountryMainRegionSurface(int countryIndex, bool visible, Color color) {
			ToggleCountryRegionSurface(countryIndex, countries[countryIndex].mainRegionIndex,visible, color, null, MiscVector.Vector3one, MiscVector.Vector3zero, 0);
		}

		/// <summary>
		/// Colorize main region of a country by index in the countries collection.
		/// </summary>
		/// <param name="texture">Optional texture or null to colorize with single color</param>
		public void ToggleCountryMainRegionSurface(int countryIndex, bool visible, Color color, Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			ToggleCountryRegionSurface(countryIndex, countries[countryIndex].mainRegionIndex,visible, color, texture, textureScale, textureOffset, textureRotation);
		}

		/// <summary>
		/// Colorize specified region of a country by index in the countries collection.
		/// </summary>
		/// <param name="texture">Optional texture or null to colorize with single color</param>
		public void ToggleCountryRegionSurface(int countryIndex, int regionIndex, bool visible, Color color) {
			ToggleCountryRegionSurface(countryIndex, regionIndex,visible, color, null, MiscVector.Vector3one, MiscVector.Vector3zero, 0);
		}


		/// <summary>
		/// Colorize specified region of a country by indexes.
		/// </summary>
		public void ToggleCountryRegionSurface (int countryIndex, int regionIndex, bool visible, Color color, Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
			if (!visible) {
				HideCountryRegionSurface(countryIndex, regionIndex);
				return;
			}
			GameObject surf = null;
			Region region = countries[countryIndex].regions[regionIndex];
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex);
			// Checks if current cached surface contains a material with a texture, if it exists but it has not texture, destroy it to recreate with uv mappings
			if (surfaces.ContainsKey (cacheIndex) && surfaces[cacheIndex]!=null) 
				surf = surfaces [cacheIndex];

			// Should the surface be recreated?
			Material surfMaterial;
			if (surf!=null) {
				surfMaterial = surf.GetComponent<Renderer>().sharedMaterial;
				if (texture!=null && (region.customMaterial==null || textureScale != region.customTextureScale || textureOffset != region.customTextureOffset || 
			                      textureRotation!= region.customTextureRotation || !region.customMaterial.name.Equals(texturizedMat.name))) {
					surfaces.Remove(cacheIndex);
					DestroyImmediate(surf);
					surf=null;
				}
			}
			// If it exists, activate and check proper material, if not create surface
			bool isHighlighted = countryHighlightedIndex == countryIndex && countryRegionHighlightedIndex == regionIndex;
			if (surf!=null) {
				bool needMaterial = false;
				if (!surf.activeSelf) {
					surf.SetActive (true);
					needMaterial = true;
					UpdateSurfaceCount();
				} else {
					// Check if material is ok
					surfMaterial = surf.GetComponent<Renderer>().sharedMaterial;
					if ((texture==null && !surfMaterial.name.Equals(coloredMat.name)) || (texture!=null && !surfMaterial.name.Equals(texturizedMat.name)) 
				    	|| (surfMaterial.color!=color && !isHighlighted) || (texture!=null && region.customMaterial.mainTexture!=texture)) 
						needMaterial = true;
				}
				if (needMaterial) {
					Material goodMaterial = GetColoredTexturedMaterial(color, texture);
					region.customMaterial = goodMaterial;
					ApplyMaterialToSurface(surf, goodMaterial);
				}
			} else {
				surfMaterial = GetColoredTexturedMaterial(color, texture);
				surf = GenerateCountryRegionSurface (countryIndex, regionIndex, surfMaterial, textureScale, textureOffset, textureRotation, _showOutline);
				region.customMaterial = surfMaterial;
				region.customTextureOffset = textureOffset;
				region.customTextureRotation = textureRotation;
				region.customTextureScale = textureScale;
				UpdateSurfaceCount();
			}
			// If it was highlighted, highlight it again
			if (region.customMaterial!=null && isHighlighted && region.customMaterial.color!=hudMatCountry.color) {
				Material clonedMat = Instantiate(region.customMaterial);
				clonedMat.name = region.customMaterial.name;
				clonedMat.color = hudMatCountry.color;
				surf.GetComponent<Renderer>().sharedMaterial = clonedMat;
				countryRegionHighlightedObj = surf;
			}
		}

		
		/// <summary>
		/// Uncolorize/hide specified country by index in the countries collection.
		/// </summary>
		public void HideCountryRegionSurface (int countryIndex, int regionIndex) {
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex);
			if (surfaces.ContainsKey (cacheIndex)) {
				if (surfaces[cacheIndex]!=null) {
					surfaces [cacheIndex].SetActive (false);
				} else surfaces.Remove(cacheIndex);
				UpdateSurfaceCount();
			}
			countries[countryIndex].regions[regionIndex].customMaterial = null;
		}

		/// <summary>
		/// Highlights the country region specified.
		/// Internally used by the Editor component, but you can use it as well to temporarily mark a country region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		public GameObject ToggleCountryRegionSurfaceHighlight (int countryIndex, int regionIndex, Color color, bool drawOutline) {
			GameObject surf;
			Material mat = Instantiate (hudMatCountry);
			mat.hideFlags = HideFlags.DontSave;
			mat.color = color;
			mat.renderQueue--;
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (existsInCache) {
				surf = surfaces [cacheIndex];
				if (surf == null) {
					surfaces.Remove (cacheIndex);
				} else {
					surf.SetActive (true);
					surf.GetComponent<Renderer> ().sharedMaterial = mat;
				}
			} else {
				surf = GenerateCountryRegionSurface (countryIndex, regionIndex, mat, MiscVector.Vector2one, MiscVector.Vector2zero, 0, drawOutline);
			}
			return surf;
		}


		/// <summary>
		/// Hides all colorized regions of all countries.
		/// </summary>
		public void HideCountrySurfaces () {
			for (int c=0; c<countries.Length; c++) {
				HideCountrySurface (c);
			}
		}

	

		/// <summary>
		/// Flashes specified country by index in the countries collection.
		/// </summary>
		public void BlinkCountry (int countryIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			int mainRegionIndex = countries [countryIndex].mainRegionIndex;
			BlinkCountry(countryIndex, mainRegionIndex, color1, color2, duration, blinkingSpeed);
		}

		/// <summary>
		/// Flashes specified country's region.
		/// </summary>
		public void BlinkCountry (int countryIndex, int regionIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex);
			GameObject surf;
			bool disableAtEnd;
			if (surfaces.ContainsKey (cacheIndex)) {
				surf = surfaces [cacheIndex];
				disableAtEnd = !surf.activeSelf;
			} else {
				surf = GenerateCountryRegionSurface (countryIndex, regionIndex, hudMatCountry, _showOutline);
				disableAtEnd = true;
			}
			SurfaceBlinker sb = surf.AddComponent<SurfaceBlinker> ();
			sb.blinkMaterial = hudMatCountry;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
			sb.disableAtEnd = disableAtEnd;
			sb.customizableSurface = countries[countryIndex].regions[regionIndex];
			surf.SetActive (true);
		}

		/// <summary>
		/// Returns an array of country names. The returning list can be grouped by continent.
		/// </summary>
		public string[] GetCountryNames (bool groupByContinent) {
			return GetCountryNames(groupByContinent, true);
		}

		/// <summary>
		/// Returns an array of country names. The returning list can be grouped by continent.
		/// </summary>
		public string[] GetCountryNames (bool groupByContinent, bool includeCountryIndex) {
			List<string> c = new List<string> ();
			if (countries == null)
				return c.ToArray ();
			string previousContinent = "";
			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				if (groupByContinent) {
					if (!country.continent.Equals (previousContinent)) {
						c.Add (country.continent);
						previousContinent = country.continent;
					}
					if (includeCountryIndex) {
						c.Add (country.continent + "|" + country.name + " (" + k + ")");
					} else {
						c.Add (country.continent + "|" + country.name);
					}
				} else {
					if (includeCountryIndex) {
						c.Add (country.name + " (" + k + ")");
					} else {
						c.Add (country.name);
					}
				}
			}
			c.Sort ();

			if (groupByContinent) {
				int k = -1;
				while (++k<c.Count) {
					int i = c [k].IndexOf ('|');
					if (i > 0) {
						c [k] = "  " + c [k].Substring (i + 1);
					}
				}
			}
			return c.ToArray ();
		}

		#endregion


	}

}