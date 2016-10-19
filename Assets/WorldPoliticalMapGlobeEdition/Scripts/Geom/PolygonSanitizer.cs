/// <summary>
/// Several ancilliary functions to sanitize polygons
/// </summary>
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;
using WPM;

namespace WPM
{
	public class PolygonSanitizer
	{

		/// <summary>
		/// Searches for segments that crosses themselves and removes the shorter until there're no one else
		/// </summary>
		public static List<PolygonPoint> RemoveCrossingSegments (List<PolygonPoint> pointList)
		{

			while (pointList.Count>5) {
				Line2D invalidSegment = DetectCrossingSegment (pointList);
				if (invalidSegment != null) {
					pointList.Remove (invalidSegment.P1);
					pointList.Remove (invalidSegment.P2);
				} else
					return pointList;
			}
			return pointList;
		}

		static Line2D DetectCrossingSegment (List<PolygonPoint> pointList)
		{
			Line2D[] lines = new Line2D[pointList.Count - 1];
			for (int k=0; k<pointList.Count-1; k++) {
				lines [k] = new Line2D (pointList [k], pointList [k + 1]);
			}

			int max = pointList.Count - 1;
			for (int k=0; k<max; k++) {
				Line2D line1 = lines [k]; // new Line2D(pointList [k], pointList [k + 1]);
				for (int j=k+2; j<max; j++) {
					Line2D line2 = lines [j]; // new Line2D(pointList [j], pointList [j + 1]);
					if (line2.intersectsLine (line1)) {
						if (line1.sqrMagnitude < line2.sqrMagnitude)
							return line1;
						else
							return line2;
					}
				}
			}
			return null;
		}

	}

}

