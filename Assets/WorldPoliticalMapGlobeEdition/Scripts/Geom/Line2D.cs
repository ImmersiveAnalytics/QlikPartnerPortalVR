using UnityEngine;
using System.Collections;
using Poly2Tri;

namespace WPM
{
	public class Line2D
	{
		public double X1 = 0;
		public double X2 = 0;
		public double Y1 = 0;
		public double Y2 = 0;
		public PolygonPoint P1;
		public PolygonPoint P2;
	
		public Line2D (PolygonPoint p1, PolygonPoint p2)
		{
			P1 = p1; 
			P2 = p2;
			X1 = p1.X;
			X2 = p2.X;
			Y1 = p1.Y;
			Y2 = p2.Y;
		}
	
		public PolygonPoint getP1 ()
		{
			return P1;
		}
	
		public PolygonPoint getP2 ()
		{
			return P2;
		}

		public double sqrMagnitude {
			get {
				return (P2-P1).MagnitudeSquared();
			}
		}
	
		public bool intersectsLine (Line2D comparedLine)
		{
			if (X2 == comparedLine.X1 && Y2 == comparedLine.Y1) {
				return false;
			}
		
			if (X1 == comparedLine.X2 && Y1 == comparedLine.Y2) {
				return false;
			}
			double firstLineSlopeX, firstLineSlopeY, secondLineSlopeX, secondLineSlopeY;
		
			firstLineSlopeX = X2 - X1;
			firstLineSlopeY = Y2 - Y1;
		
			secondLineSlopeX = comparedLine.X2 - comparedLine.X1;
			secondLineSlopeY = comparedLine.Y2 - comparedLine.Y1;
		
			double s, t;
			s = (-firstLineSlopeY * (X1 - comparedLine.X1) + firstLineSlopeX * (Y1 - comparedLine.Y1 )) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
			t = (secondLineSlopeX * (Y1 - comparedLine.Y1) - secondLineSlopeY * (X1 - comparedLine.X1)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
		
			if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
				return true;
			}
		
			return false; // No collision
		}
	
		public override int GetHashCode ()
		{
			return (X1 * 1000 + X2 * 1000 + Y1 * 1000 + Y2 * 1000).GetHashCode ();
		}
	
		public override bool Equals (object obj)
		{
			return (obj.GetHashCode () == this.GetHashCode ());
		}
	
	}
}