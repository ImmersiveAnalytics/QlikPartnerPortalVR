using UnityEngine;
using System.Collections;

namespace WPM {
	public class LabelAnimator : MonoBehaviour {

		public Vector3 destPos;
		public Vector3 startPos;
		public float duration;
		float startTime;

		void Start () {
			startTime = Time.time;

		}

		void Update () {
			float t = (Time.time - startTime) / duration;
			transform.localPosition = Vector3.Lerp (startPos, destPos, Mathf.SmoothStep (0, 1, t));
			if (t >= 1) {
				Destroy (this);
			}
		}

	}
}