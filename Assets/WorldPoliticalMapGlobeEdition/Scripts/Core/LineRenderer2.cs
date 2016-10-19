using UnityEngine;
using System.Collections;

namespace WPM {

/// <summary>
/// Replacement class for Unity's standard LineRenderer
/// </summary>
	public class LineRenderer2 : MonoBehaviour {

		public float width;
		public Material material;
		public Color color;
		public Vector3[] vertices;
		public bool useWorldSpace;

		bool needRedraw;
		GameObject line;

		// Update is called once per frame
		void Update () {
			if (needRedraw) {
				if (line != null)
					DestroyImmediate (line);
				if (material!=null && material.color!=color) {
					material = Instantiate(material);
					material.hideFlags = HideFlags.DontSave;
					material.color = color;
				}
				line = Drawing.DrawLine (vertices, width, material);
				line.transform.SetParent(transform, false);
				needRedraw = false;
			}
	
		}

		public void SetWidth (float startWidth, float endWidth) {
			this.width = startWidth;
			needRedraw = true;
		}

		public void SetColors (Color startColor, Color endColor) {
			this.color = startColor;
			needRedraw = true;
		}

		public void SetVertexCount (int vertexCount) {
			vertices = new Vector3[vertexCount];
			needRedraw = true;
		}

		public void SetPosition (int index, Vector3 position) {
			vertices [index] = position;
			needRedraw = true;
		}

	}

}