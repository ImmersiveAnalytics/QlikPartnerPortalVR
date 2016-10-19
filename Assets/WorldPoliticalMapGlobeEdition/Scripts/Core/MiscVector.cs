using UnityEngine;
using System.Collections;

namespace WPM
{
	public static class MiscVector
	{
		public static Vector4 Vector4back = new Vector4(0,0,-1,0);

		public static Vector3 Vector3one = Vector3.one;
		public static Vector3 Vector3zero = Vector3.zero;
		public static Vector3 Vector3back = Vector3.back;
		public static Vector3 Vector3left = Vector3.left;
		public static Vector3 Vector3up = Vector3.up;

		public static Vector2 Vector2left = Vector2.left;
		public static Vector2 Vector2right = Vector2.right;
		public static Vector2 Vector2one = Vector2.one;
		public static Vector2 Vector2zero = Vector2.zero;

		public static Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0.0f);
	}
}