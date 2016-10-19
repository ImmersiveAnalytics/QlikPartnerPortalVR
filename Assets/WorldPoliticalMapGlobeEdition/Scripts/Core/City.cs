using UnityEngine;
using System.Collections;

namespace WPM {

	public enum CITY_CLASS {
		CITY = 1,
		REGION_CAPITAL = 2,
		COUNTRY_CAPITAL = 4
	}

	public class City {
		public string name;
		public int countryIndex;
		public string province;
		public Vector3 unitySphereLocation;
		public int population;
		public CITY_CLASS cityClass;

		/// <summary>
		/// Returns if city is visible on the map based on minimum population filter.
		/// </summary>
		public bool isShown;

		public City (string name, string province, int countryIndex, int population, Vector3 location, CITY_CLASS cityClass) {
			this.name = name;
			this.province = province;
			this.countryIndex = countryIndex;
			this.population = population;
			this.unitySphereLocation = location;
			this.cityClass = cityClass;
		}

		public City Clone() {
			City c = new City(name, province, countryIndex, population, unitySphereLocation, cityClass);
			return c;
		}
	}
}