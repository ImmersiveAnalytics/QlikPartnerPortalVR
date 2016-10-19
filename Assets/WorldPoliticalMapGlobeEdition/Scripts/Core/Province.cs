using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {

	public class Province: IAdminEntity {
		public string name { get; set; }
		public int countryIndex;

		List<Region> _regions;

		/// <summary>
		/// List of land regions belonging to the province. Main/biggest region is defined by mainRegionIndex field.
		/// </summary>
		/// <value>The regions.</value>
		public List<Region> regions { get {
				LazyLoadCheck();
				return _regions;
			}
			set { _regions = value; }
		}

		Vector3 _center;
		/// <summary>
		/// Geometric center of the province area, including all regions.
		/// </summary>
		public Vector3 center { get {
				LazyLoadCheck();
				return _center;
			}
			set { _center = value; }
		}

		int _mainRegionIndex;
		/// <summary>
		/// Index of the biggest region
		/// </summary>
		public int mainRegionIndex { get {
				LazyLoadCheck();
				return _mainRegionIndex;
			}
			set { _mainRegionIndex = value; }
		}

		/// <summary>
		/// Area of the main region.
		/// </summary>
		/// <value>The main region area.</value>
		public float mainRegionArea { get; set; }

		#region internal fields
		/// Used internally. Don't change this value.
//		public Material customMaterial { get; set; }
		public string packedRegions;
		#endregion

		public Province (string name, int countryIndex) {
			this.name = name;
			this.countryIndex = countryIndex;
			this.regions = null; // lazy load during runtime due to size of data
			this.center = MiscVector.Vector3zero;
		}

		/// <summary>
		/// Checks if province regions info has been loaded before one of its accesor gets called and reads the info from disk if needed.
		/// </summary>
		void LazyLoadCheck() {
			if (_regions==null) {
				WorldMapGlobe.instance.ReadProvincePackedString(this);
			}
		}

	}
}

