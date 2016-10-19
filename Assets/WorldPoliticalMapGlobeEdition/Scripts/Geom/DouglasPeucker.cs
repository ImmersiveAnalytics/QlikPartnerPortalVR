/// <summary>
/// Douglas peucker algorithm for curve simplification
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;


namespace WPM
{
	public class DouglasPeucker
	{

		public static List<PolygonPoint> SimplifyCurve (List<PolygonPoint> pointList, double epsilon)
		{

			// Find the point with the maximum distance
			double dmax = 0;
			int index = 0;
			int last = pointList.Count - 1;
			for (int i = 2; i<last; i++) {
				double d = LineToPointDistance2D (pointList [0], pointList [last], pointList [i], true); 
				if (d > dmax) {
					index = i;
					dmax = d;
				}
			}
			// If max distance is greater than epsilon, recursively simplify
			List<PolygonPoint> recResults;
			if (dmax > epsilon) {
				// Recursive call
				recResults = SimplifyCurve (pointList.GetRange (0, index + 1), epsilon);
				List<PolygonPoint> recResults2 = SimplifyCurve (pointList.GetRange (index, last - index + 1), epsilon);
					
				// Build the result list
				for (int k=1; k<recResults2.Count; k++)
					recResults.Add (recResults2 [k]);
			} else {
				recResults = new List<PolygonPoint> ();
				recResults.Add (pointList [0]);
				recResults.Add (pointList [last]);
			}
			// Return the result
			return recResults;
		}

		//Compute the dot product AB . AC
		private static double DotProduct (PolygonPoint pointA, PolygonPoint pointB, PolygonPoint pointC)
		{
			double ABx = pointB.X - pointA.X;
			double ABy = pointB.Y - pointA.Y;
			double BCx = pointC.X - pointB.X;
			double BCy = pointC.Y - pointB.Y;
			double dot = ABx * BCx + ABy * BCy;
		
			return dot;
		}
	
		//Compute the cross product AB x AC
		private static double CrossProduct (PolygonPoint pointA, PolygonPoint pointB, PolygonPoint pointC)
		{
			double ABx = pointB.X - pointA.X;
			double ABy = pointB.Y - pointA.Y;
			double ACx = pointC.X - pointA.X;
			double ACy = pointC.Y - pointA.Y;
			double cross = ABx * ACy - ABy * ACx;

			return cross;
		}
	
		//Compute the distance from A to B
		static double  Distance (PolygonPoint pointA, PolygonPoint pointB)
		{
			double d1 = pointA.X - pointB.X;
			double d2 = pointA.Y - pointB.Y;
		
			return Math.Sqrt (d1 * d1 + d2 * d2);
		}
	
		//Compute the distance from AB to C
		//if isSegment is true, AB is a segment, not a line.
		static double  LineToPointDistance2D (PolygonPoint pointA, PolygonPoint pointB, PolygonPoint pointC, bool isSegment)
		{
			double dist = CrossProduct (pointA, pointB, pointC) / Distance (pointA, pointB);
			if (isSegment) {
				double dot1 = DotProduct (pointA, pointB, pointC);
				if (dot1 > 0) 
					return Distance (pointB, pointC);
			
				double dot2 = DotProduct (pointB, pointA, pointC);
				if (dot2 > 0) 
					return Distance (pointA, pointC);
			}
			return Math.Abs (dist);
		} 


	}
}
