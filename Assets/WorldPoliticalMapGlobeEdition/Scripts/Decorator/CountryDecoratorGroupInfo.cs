using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	[ExecuteInEditMode]
	[Serializable]
	public class CountryDecoratorGroupInfo : MonoBehaviour {

		[SerializeField]
		bool
			_active;
		
		public bool active {
			get {
				return _active;
			}
			set {
				if (value != _active) {
					_active = value;
					UpdateDecorators (true);
				}
			}
		}

		[HideInInspector]
		public int
			groupIndex;
		public List<CountryDecorator> decorators;
		WorldMapGlobe _map;

		WorldMapGlobe map {
			get {
				if (_map == null) {
					_map = transform.parent.parent.GetComponent<WorldMapGlobe> ();
				}
				return _map;
			}
		}

		int lastCheck;


		#region Lifecycle events

		void OnEnable () {
			if (decorators == null)
				decorators = new List<CountryDecorator> ();
		}

		void Start () {
			UpdateDecorators (true);
		}

		// Update is called once per frame
		void Update () {
			if (!active)
				return;
			if (++lastCheck % 10 == 0) {
				UpdateDecorators (false);
				lastCheck = 0;
			}
		}


		#endregion


		int GetDecoratorIndex (string countryName) {
			for (int k=0; k<decorators.Count; k++) {
				if (decorators [k].countryName.Equals (countryName))
					return k;
			}
			return -1;
		}

		public CountryDecorator GetDecorator (string countryName) {
			int k = GetDecoratorIndex (countryName);
			if (k >= 0)
				return decorators [k];
			else
				return null;
		}

		public void SetDecorator (CountryDecorator decorator) {
			int k = GetDecoratorIndex (decorator.countryName);
			if (k >= 0) {
				decorators [k] = decorator;
			} else {
				decorators.Add (decorator);
			}
			decorator.isNew = false;
			UpdateDecorators (true);
		}

		public void RemoveDecorator (string countryName) {
			int k = GetDecoratorIndex (countryName);
			if (k >= 0) {
				decorators [k].Reset ();
				UpdateDecorators (true);
				decorators.RemoveAt (k);
			}
		}

		public void RemoveAllDecorators() {
			for (int k=0;k<decorators.Count;k++) {
				decorators [k].Reset ();
			}
			UpdateDecorators (true);
			decorators.Clear();
		}

		public void UpdateDecorators (bool ignoreActive) {
			if (!active && !ignoreActive)
				return;
			if (decorators == null || map == null)
				return;
			
			bool needsLabelRedraw = false;
			bool needsFrontiersRedraw = false;

			for (int k=0; k<decorators.Count; k++) {
				CountryDecorator decorator = decorators [k];

				// Check if something needs to be changed
				int countryIndex = map.GetCountryIndex (decorator.countryName);
				if (countryIndex >= 0) {
					Country country = map.countries [countryIndex];
					Region mainRegion = country.regions[country.mainRegionIndex];
					if (active) {
						// label Font override
						if (country.labelFontOverride!= decorator.labelFontOverride) {
							country.labelFontOverride = decorator.labelFontOverride;
							needsLabelRedraw = true;
						}
						// label color override
						if (country.labelColorOverride != decorator.labelOverridesColor) {
							country.labelColorOverride = decorator.labelOverridesColor;
							needsLabelRedraw = true;
						}
						if (country.labelColorOverride && country.labelColor != decorator.labelColor) {
							country.labelColor = decorator.labelColor;
							needsLabelRedraw = true;
						}
						// label visible
						if (country.labelVisible!= decorator.labelVisible) {
							country.labelVisible = decorator.labelVisible;
							needsLabelRedraw = true;
						}
						// label rotation
						if (country.labelRotation!= decorator.labelRotation) {
							country.labelRotation = decorator.labelRotation;
							needsLabelRedraw = true;
						}
						// label offset
						if (country.labelOffset!= decorator.labelOffset) {
							country.labelOffset = decorator.labelOffset;
							needsLabelRedraw = true;
						}
						// custom label
						if ((country.customLabel == null && decorator.customLabel.Length > 0) || (country.customLabel != null && country.customLabel != decorator.customLabel)) {
							if (decorator.customLabel.Length > 0) {
								country.customLabel = decorator.customLabel;
							} else {
								country.customLabel = null;
							}
							if (country.labelGameObject != null) {
								DestroyImmediate (country.labelGameObject);
							}
							needsLabelRedraw = true;
						}
						// colorize
						if (decorator.isColorized) {
							map.ToggleCountryMainRegionSurface (countryIndex, true, decorator.fillColor, decorator.texture, decorator.textureScale, decorator.textureOffset, decorator.textureRotation);
						} else if (!decorator.isColorized && mainRegion.customMaterial != null) {
							map.HideCountrySurface (countryIndex);
						}
						// hidden
						if (country.hidden != decorator.hidden) {
							country.hidden = decorator.hidden;
							needsFrontiersRedraw = true;
						}
					} else {
						if (country.labelFontOverride!=null) {
							country.labelFontOverride = null;
							needsLabelRedraw = true;
						}
						if (country.labelColorOverride) {
							country.labelColorOverride = false;
							needsLabelRedraw = true;
						}
						if (!country.labelVisible) {
							country.labelVisible = true;
							needsLabelRedraw = true;
						}
						if (country.customLabel != null) {
							country.customLabel = null;
							needsLabelRedraw = true;
						}
						if (country.labelRotation>0) {
							country.labelRotation = 0;
							needsLabelRedraw = true;
						}
						if (country.labelOffset!=MiscVector.Vector2zero) {
							country.labelOffset = MiscVector.Vector2zero;
							needsLabelRedraw = true;
						}
						if (country.hidden) {
							country.hidden = false;
							needsFrontiersRedraw = true;
						}
						if (mainRegion.customMaterial != null) {
							mainRegion.customMaterial = null;
							map.HideCountrySurface (countryIndex);
						}
					} 
				}
			}

			if (needsFrontiersRedraw) {
				map.OptimizeFrontiers();
				map.Redraw();
			} else if (needsLabelRedraw) {
				map.RedrawMapLabels ();
			}
		}

		public List<string> GetDecoratedCountries (bool addCountryIndexSuffix) {
			List<string> decoratedCountries = new List<string> ();
			if (decorators == null || map == null)
				return decoratedCountries;
			for (int k=0; k<decorators.Count; k++) {
				string s = decorators [k].countryName;
				if (addCountryIndexSuffix) 
					s += " (" + map.GetCountryIndex (s) + ")";
				decoratedCountries.Add (s);
			}
			return decoratedCountries;
		}

	}


}