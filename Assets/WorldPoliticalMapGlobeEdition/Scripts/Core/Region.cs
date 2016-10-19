using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;

namespace WPM {
	public class Region {
		public Vector3[] points;
		public PolygonPoint[] latlon;
		public Vector3 center;
		public Vector3 minMaxLat, minMaxLon;

		/// <summary>
		/// 2D rect in the billboard
		/// </summary>
		public Rect rect2D;

		public List<Region>neighbours { get; set; }
		public IAdminEntity entity { get; set; }	// country or province index
		public int regionIndex { get; set; }

		public Material customMaterial;

		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;

		public Region(IAdminEntity entity, int regionIndex) {
			this.entity = entity;
			this.regionIndex = regionIndex;
			neighbours = new List<Region>();
		}
		
		public Region Clone() {
			Region c = new Region(entity, regionIndex);
			c.center = this.center;
			c.rect2D = this.rect2D;
			c.minMaxLat = this.minMaxLat;
			c.minMaxLon = this.minMaxLon;
			c.customMaterial = this.customMaterial;
			c.customTextureScale = this.customTextureScale;
			c.customTextureOffset = this.customTextureOffset;
			c.customTextureRotation = this.customTextureRotation;
			c.points = new Vector3[points.Length];
			Array.Copy(points, c.points, points.Length); // copy value-types
			c.neighbours = new List<Region>(this.neighbours);
			// Instantiate latlon points
			c.latlon = new PolygonPoint[this.latlon.Length];
			for (int k=0;k<this.latlon.Length;k++) {
				PolygonPoint latlon = this.latlon[k];
				c.latlon[k] = new PolygonPoint(latlon.X, latlon.Y);
			}
			return c;
		}

		public bool ContainsPoint (double x, double y) { 
			int i, j;
			int nvert = latlon.Length; 
			bool inside = false;
			for (i = 0, j = nvert-1; i < nvert; j = i++) {
				if ( ((latlon[i].Y>y) != (latlon[j].Y>y)) &&
				    (x < (latlon[j].X-latlon[i].X) * (y-latlon[i].Y) / (latlon[j].Y-latlon[i].Y) + latlon[i].X) )
					inside = !inside;
			}
			return inside; 
		}


	}

}