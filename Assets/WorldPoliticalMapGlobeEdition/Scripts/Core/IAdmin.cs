using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	public interface IAdminEntity {

		/// <summary>
		/// Entity name.
		/// </summary>
		string name { get; set; }

		/// <summary>
		/// List of all regions for the admin entity.
		/// </summary>
		List<Region> regions { get; set; }
	
		/// <summary>
		/// Center of the admin entity in the plane
		/// </summary>
		Vector3 center { get; set; }

		int mainRegionIndex { get; set; }
		float mainRegionArea { get; set; }

	}
}
