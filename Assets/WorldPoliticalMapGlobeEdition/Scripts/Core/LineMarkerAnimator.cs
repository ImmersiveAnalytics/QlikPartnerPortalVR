using UnityEngine;
using System.Collections;

namespace WPM {
	public class LineMarkerAnimator : MonoBehaviour {

		public Vector3 start, end;
		public Color color;
		public float lineWidth = 0.01f;
		public float arcElevation;
		public float duration;
		public int numPoints = 64;	// increase to improve line resolution
		public Material lineMaterial;
		public float autoFadeAfter = 0; // specified the duration for the line before it fades out
		public float fadeOutDuration = 1.0f; // fade out speed
		public bool earthInvertedMode = false;

		float startTime, startAutoFadeTime;
		Vector3[] vertices;
		LineRenderer2 lr;
		Color colorTransparent;

		// Use this for initialization
		void Start () {
			// Create the line mesh
			vertices = new Vector3[numPoints + 1];
			startTime = Time.time;
			lr = transform.GetComponent<LineRenderer2> ();
			if (lr == null) {
				lr = gameObject.AddComponent<LineRenderer2> ();
			}
			lr.useWorldSpace = false;
			lr.SetWidth (lineWidth, lineWidth);
			lineMaterial = Instantiate (lineMaterial);
			lineMaterial.color = color;
			lr.material = lineMaterial; // needs to instantiate to preserve individual color so can't use sharedMaterial
			lr.SetColors (color, color);

			vertices = new Vector3[numPoints + 1];
			for (int s=0; s<=numPoints; s++) {
				float t = (float)s / numPoints;
				float elevation = Mathf.Sin (t * Mathf.PI) * arcElevation;
				Vector3 sPos;
				if (earthInvertedMode) {
					sPos = Vector3.Lerp (start, end, t).normalized * 0.5f * (1.0f - elevation);
				} else {
					sPos = Vector3.Lerp (start, end, t).normalized * 0.5f * (1.0f + elevation);
				}
				vertices [s] = sPos;
			}
			startAutoFadeTime = float.MaxValue;
			colorTransparent = new Color(color.r, color.g, color.b, 0);

			if (duration == 0)
				UpdateLine ();
		}
	
		// Update is called once per frame
		void Update () {
			if (Time.time >= startAutoFadeTime) {
				UpdateFade ();
			} else {
				UpdateLine ();
			}
		}

		void UpdateLine () {
			float t;
			if (duration == 0) 
				t = 1.0f;
			else
				t = (Time.time - startTime) / duration;
			if (t >= 1.0f) {
				t = 1.0f;
				if (autoFadeAfter == 0) {
					Destroy (this); // will destroy this behaviour at the end of the frame
				} else {
					startAutoFadeTime = Time.time;
				}
			}

			// pass current vertices
			float vertexIndex = 1 + (vertices.Length - 2) * t;
			int currentVertex = (int)(vertexIndex);
			lr.SetVertexCount (currentVertex + 1);
			for (int k=0; k<currentVertex; k++) {
				lr.SetPosition (k, vertices [k]);
			}
			// adjust last segment
			Vector3 nextVertex = vertices [currentVertex];
			float subt = vertexIndex - currentVertex;
			Vector3 progress = Vector3.Lerp (vertices [currentVertex], nextVertex, subt);
			lr.SetPosition (currentVertex, progress);
		}

		void UpdateFade () {
			float t = Time.time - startAutoFadeTime;
			if (t < autoFadeAfter)
				return;

			t = (t - autoFadeAfter) / fadeOutDuration;
			if (t >= 1.0f) {
				t = 1.0f;
				Destroy (gameObject);
			}

			Color fadeColor = Color.Lerp (color, colorTransparent, t);
			lineMaterial.color = fadeColor;

		}

	}
}