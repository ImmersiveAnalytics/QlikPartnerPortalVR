using UnityEngine;
using System;
using System.Collections.Generic;

namespace WPM {
	class DelaunayTriangulation {

		public int numPoints;
		const float REAL_EPSILON = 0.00000011920929f;
		List<Triangle> tris;
		Vector2[] data;

		class Line {
			public Vector2 p, q;

			public Line (Vector2 p1, Vector2 p2) {
				p = p1;
				q = p2;
			}

			public static bool operator == (Line l1, Line l2) {
				return (l1.p == l2.p && l1.q == l2.q) || (l1.p == l2.q && l1.q == l2.p);
			}

			public static bool operator != (Line l1, Line l2) {
				return ! ((l1.p == l2.p && l1.q == l2.q) || (l1.p == l2.q && l1.q == l2.p));
			}

			public override bool Equals (object obj) {
				if (obj == null)
					return false;
				if (obj is Line) {
					Line l1 = (Line)obj;
					return (l1.p == p && l1.q == q) || (l1.p == q && l1.q == p);
				}
				return false;
			}

			public override int GetHashCode () {
				return Mathf.FloorToInt (p.x + p.y + q.x + q.y);
			}
		}

		class Triangle {

			public Vector2 p1, p2, p3;
			public Line l1, l2, l3;
			public Circle circle;

			public Triangle (Vector2 p1, Vector2 p2, Vector2 p3) {
				this.p1 = p1;
				this.p2 = p2;
				this.p3 = p3;
				l1 = new Line (p1, p2);
				l2 = new Line (p2, p3);
				l3 = new Line (p3, p1);
				circle = calcCircle;
			}

			Circle calcCircle {
				get {
					float x0 = p1.x;
					float y0 = p1.y;
					float x1 = p2.x;
					float y1 = p2.y;
					float x2 = p3.x;
					float y2 = p3.y;

					float y10 = y1 - y0;
					float y21 = y2 - y1;

					bool b21zero = y21 > -REAL_EPSILON && y21 < REAL_EPSILON;

					float ox, oy;

					if (y10 > -REAL_EPSILON && y10 < REAL_EPSILON) {
						if (b21zero) {   // All three vertices are on one horizontal line.
							if (x1 > x0) {
								if (x2 > x1)
									x1 = x2;
							} else {
								if (x2 < x0)
									x0 = x2;
							}
							ox = (x0 + x1) * 0.5f;
							oy = y0;
						} else {   // m_Vertices[0] and m_Vertices[1] are on one horizontal line.
							float m1 = -(x2 - x1) / y21;

							float mx1 = (x1 + x2) * 0.5f;
							float my1 = (y1 + y2) * 0.5f;

							ox = (x0 + x1) * 0.5f;
							oy = m1 * (ox - mx1) + my1;
						}
					} else if (b21zero) { // m_Vertices[1] and m_Vertices[2] are on one horizontal line.
						float m0 = -(x1 - x0) / y10;
						float mx0 = (x0 + x1) * 0.5f;
						float my0 = (y0 + y1) * 0.5f;

						ox = (x1 + x2) * 0.5f;
						oy = m0 * (ox - mx0) + my0;
					} else { // 'Common' cases, no multiple vertices are on one horizontal line.
						float m0 = -(x1 - x0) / y10;
						float m1 = -(x2 - x1) / y21;

						float mx0 = (x0 + x1) * 0.5f;
						float my0 = (y0 + y1) * 0.5f;

						float mx1 = (x1 + x2) * 0.5f;
						float my1 = (y1 + y2) * 0.5f;

						ox = (m0 * mx0 - m1 * mx1 + my1 - my0) / (m0 - m1);
						oy = m0 * (ox - mx0) + my0;
					}

					float dx = x0 - ox;
					float dy = y0 - oy;

					float m_R2 = dx * dx + dy * dy; // the radius of the circumcircle, squared
					return new Circle (ox, oy, m_R2);
				}
			}

			public bool ContainsInCircle (Vector2 p) {
				// Calcula el centro del círculo a partir de los tres puntos del triángulo
				return this.circle.Contains (p);
			}

		}

		class Circle {
			float x, y, radius;

			public Circle (float x, float y, float radius) {
				this.x = x;
				this.y = y;
				this.radius = radius;
			}

			public bool Contains (Vector2 p) {
				float dx = p.x - x;
				float dy = p.y - y;
				return dx * dx + dy * dy <= radius;
			}
		}

		class SideList {

			private class UniqueSide {
				public Line side;
				public int count;

				public UniqueSide (Line value) {
					side = value;
					count = 1;
				}

			}

			private List<UniqueSide> list;

			public SideList () {
				list = new List<UniqueSide> ();
			}

			public List<Line> sideList {
				get {
					List<Line> r = new List<Line> (list.Count);
					for (int k=0; k<list.Count; k++) {
						if (list [k].count == 1) {
							r.Add (list [k].side);
						}
					}
					return r;
				}
			}

			public void AddSide (Line value) {
				for (int k=0; k<list.Count; k++) {
					UniqueSide l = list [k];
					if (l.side == value) {
						l.count ++;
						return;
					}
				}
				list.Add (new UniqueSide (value));
			}

		}




		public void DoTriangulation () {
		
			SideList sides;
			int triIndex;
			Triangle t;
		
			// Añade el triángulo de base a la lista de triángulos
			Vector2 p1 = new Vector2 (-2000, -2000);
			Vector2 p2 = new Vector2 (2000, -2000);
			Vector2 p3 = new Vector2 (0, 2000);
			Triangle tri = new Triangle (p1, p2, p3);
			tris = new List<Triangle> (numPoints / 2);
			tris.Add (tri);
		
			for (int v=0; v<numPoints; v++) {
				// por cada vértice (punto) comprobar si está contenido en uno o más triángulos
				Vector2 vertex = data [v];
				triIndex = 0;
				sides = new SideList ();
				while (triIndex < tris.Count) {
					t = tris [triIndex];
					if (t.ContainsInCircle (vertex)) {
						sides.AddSide (t.l1);
						sides.AddSide (t.l2);
						sides.AddSide (t.l3);
						tris.Remove (t);
					} else {
						triIndex ++;
					}
				}
				// Agrega un triángulo nuevo por cada lado
				for (int k=0; k<sides.sideList.Count; k++) {
					Line side = sides.sideList [k];
					tris.Add (new Triangle (side.p, side.q, vertex));
				}
			}
		
			// Elimina los triángulos conexos con el inicial
			triIndex = 0;
			while (triIndex < tris.Count) {
				t = tris [triIndex];
				if (t.p1 == tri.p1 || t.p1 == tri.p2 || t.p1 == tri.p3 ||
					t.p2 == tri.p1 || t.p2 == tri.p2 || t.p2 == tri.p3 ||
					t.p3 == tri.p1 || t.p3 == tri.p2 || t.p3 == tri.p3) {
					tris.Remove (t);
				} else {
					triIndex ++;
				}
			}
		}
		
		public void AssignData (Vector2[] points) {
			data = points;
//			List<Vector2>datum = new List<Vector2>(data);
//			datum.Sort (compareX);
//			data = datum.ToArray();
		}

//		int compareX (Vector2 p1, Vector2 p2) {
//			if (p1.x == p2.x) {
//				return p1.y.CompareTo (p2.y);
//			} else {
//				return p1.x.CompareTo (p2.x);
//			}
//		}

		public List<Vector2> GetPoints () {
			List<Vector2> points = new List<Vector2> (tris.Count);// Vector2[pointCount];

			for (int k=0; k<tris.Count; k++) {
				Triangle tri = tris [k];
				points.Add (tri.p1);
				points.Add (tri.p2);
				points.Add (tri.p3);
			}
			return points;
		}

		public static List<Vector2>  GetPoints (Vector2[] points, int numPoints) {
			DelaunayTriangulation dt = new DelaunayTriangulation ();
			dt.numPoints = numPoints;
			dt.AssignData (points);
			dt.DoTriangulation ();
			return dt.GetPoints ();
		}
	
	}

}