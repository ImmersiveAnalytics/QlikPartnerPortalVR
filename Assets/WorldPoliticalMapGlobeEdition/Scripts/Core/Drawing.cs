using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WPM {
	public static class Drawing {
		static Dictionary<Vector3, int>hit;
		static int [][] contourX;

		public static Vector2 SphereToBillboardCoordinates (Vector3 p) {
			float u = 1.25f - (Mathf.Atan2 (p.z, -p.x) / (2.0f * Mathf.PI) + 0.5f);
			if (u > 1)
				u -= 1.0f;
			float v = Mathf.Asin (p.y * 2.0f) / Mathf.PI;
			return new Vector2 (u * 2.0f - 1.0f, v) * 100.0f;
		}


		/// <summary>
		/// Rotates one point around another
		/// </summary>
		/// <param name="pointToRotate">The point to rotate.</param>
		/// <param name="centerPoint">The centre point of rotation.</param>
		/// <param name="angleInDegrees">The rotation angle in degrees.</param>
		/// <returns>Rotated point</returns>
		static Vector2 RotatePoint (Vector2 pointToRotate, Vector2 centerPoint, float angleInDegrees) {
			float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
			float cosTheta = Mathf.Cos (angleInRadians);
			float sinTheta = Mathf.Sin (angleInRadians);
			return new Vector2 (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x,
			                   sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y);
		}

		public static GameObject CreateSurface (string name, Vector3[] surfPoints, int maxIndex, Material material) {
			Rect dummyRect = new Rect ();
			return CreateSurface (name, surfPoints, maxIndex, material, dummyRect, MiscVector.Vector2one, MiscVector.Vector2zero, 0);
		}

		public static GameObject CreateSurface (string name, Vector3[] surfPoints, int maxIndex, Material material, Rect rect, Vector2 textureScale, Vector2 textureOffset, float textureRotation) {
		
			GameObject hexa = new GameObject (name, typeof(MeshRenderer), typeof(MeshFilter));
			hexa.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

			int pointCount = maxIndex + 1;
			List<Vector3> newPoints = new List<Vector3> (pointCount);
			int[] triNew = new int[pointCount];
			int newPointsCount = -1;
			if (hit == null)
				hit = new Dictionary<Vector3, int> (2000);
			else
				hit.Clear ();
			for (int k=0; k<=maxIndex; k++) {
				Vector3 p = surfPoints [k];
				if (hit.ContainsKey (p)) {
					triNew [k] = hit [p];
				} else {
					newPoints.Add (p);
					hit.Add (p, ++newPointsCount);
					triNew [k] = newPointsCount;
				}
			}
			Mesh mesh = new Mesh ();
			mesh.hideFlags = HideFlags.DontSave;
			Vector3[] newPoints2 = newPoints.ToArray ();
			mesh.vertices = newPoints2;
			// uv mapping
			if (material.mainTexture != null) {
				Vector2[] uv = new Vector2[newPoints2.Length];
				for (int k=0; k<uv.Length; k++) {
					Vector2 coor = SphereToBillboardCoordinates (newPoints2 [k]);
					coor.x /= textureScale.x;
					coor.y /= textureScale.y;
					if (textureRotation != 0) 
						coor = RotatePoint (coor, MiscVector.Vector2zero, textureRotation);
					coor += textureOffset;
					Vector2 normCoor = new Vector2 ((coor.x - rect.xMin) / rect.width, (coor.y - rect.yMax) / rect.height);
					uv [k] = normCoor;
				}
				mesh.uv = uv;
			}
			mesh.triangles = triNew;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			mesh.Optimize ();
		
			MeshFilter meshFilter = hexa.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
		
			hexa.GetComponent<Renderer> ().sharedMaterial = material;
			return hexa;
		
		}

		public static TextMesh CreateText (string text, GameObject parent, int layer, Vector2 center, Font labelFont, Color textColor, bool showShadow, Material shadowMaterial, Color shadowColor) {
			// create base text
			GameObject textObj = new GameObject (text);
			if (parent != null) {
				textObj.transform.SetParent (parent.transform, false);
			} 
			textObj.layer = layer;
			textObj.transform.localPosition = new Vector3 (center.x, center.y, 0);
			textObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			TextMesh tm = textObj.AddComponent<TextMesh> ();
			tm.font = labelFont;
			textObj.GetComponent<Renderer> ().sharedMaterial = tm.font.material;
			tm.alignment = TextAlignment.Center;
			tm.anchor = TextAnchor.MiddleCenter;
			tm.color = textColor;
			tm.text = text;

			// add shadow
			if (showShadow) {
				GameObject shadow = GameObject.Instantiate (textObj);
				shadow.name = "shadow";
				shadow.transform.SetParent (textObj.transform, false);
				shadow.transform.localScale = MiscVector.Vector3one;
//				shadow.transform.localPosition = new Vector3 (0.01f, -0.01f, 0);
				shadow.transform.localPosition = new Vector3 (Mathf.Max (center.x / 100.0f, 1), Mathf.Min (center.y / 100.0f, -1), 0);
				shadow.layer = textObj.layer;
				shadow.GetComponent<Renderer> ().sharedMaterial = shadowMaterial;
				shadow.GetComponent<TextMesh> ().color = shadowColor;
				shadow.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			}
			return tm;
		}

		public static void ReverseSphereNormals (GameObject gameObject, bool inverted, bool useHighDensityMesh) {

			// Adjust normals
			MeshFilter mf = gameObject.GetComponent (typeof(MeshFilter)) as MeshFilter;
			if (mf != null) {
				Mesh mesh = mf.sharedMesh;
				// Check mesh is valid
				bool reloadedMesh = false;
				bool currentMeshIsHighDensity = (mesh!=null && mf.sharedMesh.vertexCount>10000);
				if (mesh==null || (currentMeshIsHighDensity && !useHighDensityMesh)) {
					mesh = GameObject.Instantiate(Resources.Load<Mesh>("Meshes/SphereMedTris"));
					mesh.hideFlags = HideFlags.DontSave;
					mf.sharedMesh = mesh;
					gameObject.transform.localRotation = Quaternion.Euler(0f,0,0);
					reloadedMesh = true;
				} else if (!currentMeshIsHighDensity && useHighDensityMesh) {
					mesh = GameObject.Instantiate(Resources.Load<Mesh>("Meshes/SphereHighTris"));
					mesh.hideFlags = HideFlags.DontSave;
					mf.sharedMesh = mesh;
					gameObject.transform.localRotation = Quaternion.Euler(-90f,0,0);
					reloadedMesh = true;
				}
//				if (mesh == null) {
//					mesh = GameObject.Instantiate (Resources.Load<Mesh> ("Meshes/SphereMedTris"));
//					mesh.name = "Sphere";
//					mesh.hideFlags = HideFlags.DontSave;
//					filter.sharedMesh = mesh;
//					reloadedMesh = true;
//				} 
				Vector3[] normals = mesh.normals;

				bool invertedNormal = Vector3.Dot (normals[0], mesh.vertices[0].normalized)<0;
				if (invertedNormal == inverted) // normals [0].y > 0 && !inverted || normals [0].y < 0 && inverted)
					return; // security check - normals are aligned to the state

				if (!reloadedMesh) {
					mesh = GameObject.Instantiate (mesh);
					mesh.name = "Sphere";
					mesh.hideFlags = HideFlags.DontSave;
					mf.sharedMesh = mesh;
				}

				for (int i=0; i<normals.Length; i++) normals [i] = -normals [i];
				mesh.normals = normals;
				
				for (int m=0; m<mesh.subMeshCount; m++) {
					int[] triangles = mesh.GetTriangles (m);
					for (int i=0; i<triangles.Length; i+=3) {
						int temp = triangles [i + 0];
						triangles [i + 0] = triangles [i + 1];
						triangles [i + 1] = temp;
					}
					mesh.SetTriangles (triangles, m);
				}
			}		
		}


		static int ABS(int x) {
			return x>=0 ? x: -x;
		}
	
		// Scans a side of a triangle setting min X and max X in ContourX[][]
		// (using the Bresenham's line drawing algorithm).
		static void ScanLine (int x1, int y1, int x2, int y2, int height, int[][] contourX) {
			int sx, sy, dx1, dy1, dx2, dy2, x, y, m, n, k, cnt;
		
			sx = x2 - x1;
			sy = y2 - y1;
		
			if (sx > 0)
				dx1 = 1;
			else if (sx < 0)
				dx1 = -1;
			else {
				dy1 = 0;
				dx1 = 0;
			}
		
			if (sy > 0)
				dy1 = 1;
			else if (sy < 0)
				dy1 = -1;
			else
				dy1 = 0;
		
			m = ABS (sx);
			n = ABS (sy);
			dx2 = dx1;
			dy2 = 0;
		
			if (m < n) {
				m = ABS (sy);
				n = ABS (sx);
				dx2 = 0;
				dy2 = dy1;
			}
		
			x = x1;
			y = y1;
			cnt = m + 1;
			k = n / 2;
		
			while (cnt-->0) {
				if ((y >= 0) && (y < height)) {
					if (x < contourX [y] [0])
						contourX [y] [0] = x;
					if (x > contourX [y] [1])
						contourX [y] [1] = x;
				}
			
				k += n;
				if (k < m) {
					x += dx2;
					y += dy2;
				} else {
					k -= m;
					x += dx1;
					y += dy1;
				}
			}
		}
	
		public static void DrawTriangle (Color[] colors, int width, int height, Vector2 p1, Vector2 p2, Vector2 p3, Color color) {
			int y;
			if (contourX==null) {
				contourX = new int [height][];
				for (int k=0;k<height;k++) {
					contourX[k] = new int[2];
				}
			}
			for (y = 0; y < height; y++) {
				contourX [y] [0] = int.MaxValue; // min X
				contourX [y] [1] = int.MinValue; // max X
			}

			ScanLine ((int)p1.x, (int)p1.y, (int)p2.x, (int)p2.y, height, contourX);
			ScanLine ((int)p2.x, (int)p2.y, (int)p3.x, (int)p3.y, height, contourX);
			ScanLine ((int)p3.x, (int)p3.y, (int)p1.x, (int)p1.y, height, contourX);

			for (y = 0; y < height; y++) {
				if (contourX [y] [1] >= contourX [y] [0]) {
					if (contourX[y][0]<0) contourX[y][0] = 0;
					if (contourX[y][1]>=width) contourX[y][1] = width-1;
				}
			}

			float ca = color.a;
			float invca = 1.0f - ca;
			float cr = color.r * ca;
			float cg = color.g * ca;
			float cb = color.b * ca;
			if (ca < 1) { // blend operation
				for (y = 0; y < height; y++) {
					if (contourX [y] [1] >= contourX [y] [0]) {
						int x = contourX [y] [0];
						int len = 1 + contourX [y] [1] - contourX [y] [0];
						int bufferStart = y * width + x;
						int bufferEnd = bufferStart + len;
						while (bufferStart<bufferEnd) {
							Color currentColor = colors [bufferStart];
							float r = currentColor.r * invca + cr;
							float g = currentColor.g * invca + cg;
							float b = currentColor.b * invca + cb;
							currentColor.r = r;
							currentColor.g = g;
							currentColor.b = b;
							currentColor.a = 1;
							colors [bufferStart] =  currentColor;
						}
					}
				}
			} else {
				for (y = 0; y < height; y++) {
					if (contourX [y] [1] >= contourX [y] [0]) {
						int x = contourX [y] [0];
						int len = 1 + contourX [y] [1] - contourX [y] [0];
						int bufferStart = y * width + x;
						int bufferEnd = bufferStart + len;
						while (bufferStart< bufferEnd) {
							colors [bufferStart++] = color;
						}
					}
				}
			}
		}


		/// <summary>
		/// Creates a 2D pie
		/// </summary>
		public static GameObject DrawCircle (string name, Vector3 localPosition, float width, float height, float angleStart, float angleEnd, float ringWidthMin, float ringWidthMax, int numSteps, Material material, bool correctDistortion) {
			
			GameObject hexa = new GameObject (name, typeof(MeshRenderer), typeof(MeshFilter));
			hexa.isStatic = true;
			
			// create the points - start with a circle
			numSteps = Mathf.FloorToInt (32.0f * (angleEnd - angleStart) / (2 * Mathf.PI));
			numSteps = Mathf.Clamp (numSteps, 12, 32);
			
			// if ringWidthMin == 0 we only need one triangle per step
			int numPoints = ringWidthMin == 0 ? numSteps * 3 : numSteps * 6;
			Vector3[] points = new Vector3[numPoints];
			Vector2[] uv = new Vector2[numPoints];
			int pointIndex = -1;
			
			width *= 0.5f;
			height *= 0.5f;
			
			float angleStep = (angleEnd - angleStart) / numSteps;
			float px, py;
			for (int stepIndex = 0; stepIndex < numSteps; stepIndex++) {
				float angle0 = angleStart + stepIndex * angleStep;
				float angle1 = angle0 + angleStep;
				
				// first triangle
				// 1
				py = Mathf.Sin (angle0) * (ringWidthMax * height);
				px = Mathf.Cos (angle0) * (ringWidthMax * width);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (1, 1);
				// 2
				px = Mathf.Cos (angle0) * (ringWidthMin * width);
				py = Mathf.Sin (angle0) * (ringWidthMin * height);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (1, 0);
				// 3
				px = Mathf.Cos (angle1) * (ringWidthMax * width);
				py = Mathf.Sin (angle1) * (ringWidthMax * height);
				points [++pointIndex] = new Vector3 (px, py, 0);
				uv [pointIndex] = new Vector2 (0, 1);
				
				// second triangle
				if (ringWidthMin != 0) {
					// 1
					points [++pointIndex] = points [pointIndex - 2];
					uv [pointIndex] = new Vector2 (1, 0);
					// 2
					px = Mathf.Cos (angle1) * (ringWidthMin * width);
					py = Mathf.Sin (angle1) * (ringWidthMin * height);
					points [++pointIndex] = new Vector3 (px, py, 0);
					uv [pointIndex] = new Vector2 (0, 0);
					// 3
					points [++pointIndex] = points [pointIndex - 3];
					uv [pointIndex] = new Vector2 (0, 1);
				}
			}

			// triangles
			int[] triPoints = new int[numPoints];
			for (int p=0; p<numPoints; p++) {
				if (correctDistortion) {
					float dy = Mathf.Abs(points[p].y + localPosition.y) / 50f;
					float cx = Mathf.Cos ( Mathf.PI * 0.5f * dy );
					if (dy>0.5f) {
						cx *= 1f + (dy-0.5f) * 0.33f;
					}
					points[p].x *= 1f / cx;
				}
				triPoints [p] = p;
			}
			
			Mesh mesh = new Mesh ();
			mesh.vertices = points;
			mesh.triangles = triPoints;
			mesh.uv = uv;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			mesh.Optimize ();
			
//			TangentSolver (mesh);
			
			MeshFilter meshFilter = hexa.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
			hexa.GetComponent<Renderer> ().sharedMaterial = material;
			
			hexa.transform.localPosition = localPosition;
			hexa.transform.localScale = MiscVector.Vector3one;
			
			return hexa;
			
		}

		public static GameObject DrawLine(Vector3[] points, float thickness, Material sharedMaterial) {

			int numPoints = 8 * 3 * points.Length;
			Vector3[] meshPoints = new Vector3[numPoints];
			int[] triPoints = new int[numPoints];
			int mp=0;
			Vector2[] uv = new Vector2[numPoints];
			thickness *= 0.5f;
			float y0 = Mathf.Sin (0.0f * Mathf.Deg2Rad);
			float x0 = Mathf.Cos (0.0f * Mathf.Deg2Rad);
			float y1 = Mathf.Sin (120.0f * Mathf.Deg2Rad);
			float x1 = Mathf.Cos (120.0f * Mathf.Deg2Rad);
			float y2 = Mathf.Sin (240.0f * Mathf.Deg2Rad);
			float x2 = Mathf.Cos (240.0f * Mathf.Deg2Rad);
			for (int p=0;p<points.Length-1;p++) {
				Vector3 p0 = points[p];
				Vector3 p1 = points[p+1];

				Vector3 v01 = (p1-p0).normalized;
				p1 += v01 * thickness * 0.5f;
				Vector3 up = Quaternion.Euler(-90, 0, 0) * v01;
				Vector3 right = Vector3.Cross (up, v01);
				// Front triangle
				if (p==0) {
					meshPoints[mp+ 0] = p0 + (up * y0 + right * x0).normalized * thickness;
					meshPoints[mp+ 1] = p0 + (up * y2 + right * x2).normalized * thickness;
					meshPoints[mp+ 2] = p0 + (up * y1 + right * x1).normalized * thickness;
				} else {
					meshPoints[mp+ 0] = meshPoints[mp-24+3];
					meshPoints[mp+ 1] = meshPoints[mp-24+4];
					meshPoints[mp+ 2] = meshPoints[mp-24+5];
				}
				triPoints [mp+ 0] = mp+0;
				uv        [mp+ 0] = new Vector2(0,0);
				triPoints [mp+ 1] = mp+1;
				uv        [mp+ 1] = new Vector2(1,0);
				triPoints [mp+ 2] = mp+2;
				uv        [mp+ 2] = new Vector2(1,1);

				// Back triangle
				if (p<points.Length-2) {
					Vector3 v02 = (points[p+2]-p1).normalized;
					float dv = Vector3.Dot (v01, v02);
//					Debug.Log ("p = " + p + " dv = " + dv);
					if (dv>=-0.1f) {
						v01 = (v01 + v02) * 0.5f;
						up = Quaternion.Euler(90, 0, 0) * v01;
						right = Vector3.Cross (up, v01); 
					}
				}
				meshPoints[mp+ 3] = p1 + (up * y0 + right * x0).normalized * thickness;
				triPoints [mp+ 3] = mp+3;
				uv        [mp+ 3] = new Vector2(0,0);
				meshPoints[mp+ 4] = p1 + (up * y1 + right * x1).normalized * thickness;
				triPoints [mp+ 4] = mp+4;
				uv        [mp+ 4] = new Vector2(1,1);
				meshPoints[mp+ 5] = p1 + (up * y2 + right * x2).normalized * thickness;
				triPoints [mp+ 5] = mp+5;
				uv        [mp+ 5] = new Vector2(1,0);

				// One side
				meshPoints[mp+ 6] = meshPoints[mp+0];
				triPoints [mp+ 6] = mp+6;
				uv        [mp+ 6] = new Vector2(0,1);
				meshPoints[mp+ 7] = meshPoints[mp+3];
				triPoints [mp+ 7] = mp+7;
				uv        [mp+ 7] = new Vector2(1,1);
				meshPoints[mp+ 8] = meshPoints[mp+1];
				triPoints [mp+ 8] = mp+8;
				uv        [mp+ 8] = new Vector2(0,0);

				meshPoints[mp+ 9] = meshPoints[mp+1];
				triPoints [mp+ 9] = mp+9;
				uv        [mp+ 9] = new Vector2(0,0);
				meshPoints[mp+10] = meshPoints[mp+3];
				triPoints [mp+10] = mp+10;
				uv        [mp+10] = new Vector2(1,1);
				meshPoints[mp+11] = meshPoints[mp+5];
				triPoints [mp+11] = mp+11;
				uv        [mp+11] = new Vector2(0,0);
				
				// Second side
				meshPoints[mp+12] = meshPoints[mp+1];
				triPoints [mp+12] = mp+12;
				uv        [mp+12] = new Vector2(0,1);
				meshPoints[mp+13] = meshPoints[mp+5];
				triPoints [mp+13] = mp+13;
				uv        [mp+13] = new Vector2(1,1);
				meshPoints[mp+14] = meshPoints[mp+2];
				triPoints [mp+14] = mp+14;
				uv        [mp+14] = new Vector2(0,0);
				
				meshPoints[mp+15] = meshPoints[mp+2];
				triPoints [mp+15] = mp+15;
				uv        [mp+15] = new Vector2(0,0);
				meshPoints[mp+16] = meshPoints[mp+5];
				triPoints [mp+16] = mp+16;
				uv        [mp+16] = new Vector2(1,1);
				meshPoints[mp+17] = meshPoints[mp+4];
				triPoints [mp+17] = mp+17;
				uv        [mp+17] = new Vector2(0,0);

				// Third side
				meshPoints[mp+18] = meshPoints[mp+0];
				triPoints [mp+18] = mp+18;
				uv        [mp+18] = new Vector2(0,1);
				meshPoints[mp+19] = meshPoints[mp+4];
				triPoints [mp+19] = mp+19;
				uv        [mp+19] = new Vector2(1,1);
				meshPoints[mp+20] = meshPoints[mp+3];
				triPoints [mp+20] = mp+20;
				uv        [mp+20] = new Vector2(0,0);
				
				meshPoints[mp+21] = meshPoints[mp+0];
				triPoints [mp+21] = mp+21;
				uv        [mp+21] = new Vector2(0,0);
				meshPoints[mp+22] = meshPoints[mp+2];
				triPoints [mp+22] = mp+22;
				uv        [mp+22] = new Vector2(1,1);
				meshPoints[mp+23] = meshPoints[mp+4];
				triPoints [mp+23] = mp+23;
				uv        [mp+23] = new Vector2(0,0);

				mp += 24;
			}

			GameObject line = new GameObject ("line", typeof(MeshRenderer), typeof(MeshFilter));
			line.isStatic = true;
			line.hideFlags = HideFlags.DontSave; // | HideFlags.HideInHierarchy;

			Mesh mesh = new Mesh ();
			mesh.vertices = meshPoints;
			mesh.triangles = triPoints;
			mesh.uv = uv;
			mesh.RecalculateNormals ();
			mesh.RecalculateBounds ();
			mesh.Optimize ();
			
			MeshFilter meshFilter = line.GetComponent<MeshFilter> ();
			meshFilter.mesh = mesh;
			Renderer renderer = line.GetComponent<Renderer>();
			renderer.sharedMaterial = sharedMaterial;

			line.transform.localScale = MiscVector.Vector3one;
			
			return line;



		}
	}
}



