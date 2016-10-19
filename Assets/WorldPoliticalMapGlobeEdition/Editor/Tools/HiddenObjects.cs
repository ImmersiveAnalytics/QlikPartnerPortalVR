using UnityEngine;
using UnityEditor;
using System.Collections;

namespace WPM {

	public class HiddenObjects : EditorWindow {
		
		[MenuItem("GameObject/Hidden GameObjects Tool")]
		public static void Create(){
			GetWindow<HiddenObjects>("Hidden Tool");
		}


		
		void OnGUI(){

			GUILayout.Label("This tools deal with hidden GameObjects under the WPM hierarchy (those with the HideFlags.HideInHierarchy flag set).", EditorStyles.wordWrappedLabel);

			if(GUILayout.Button("Count Hidden GameObjects")) {
				GameObject g = GameObject.FindObjectOfType<WorldMapGlobe>().gameObject;
				int count=0;
				foreach(Transform t in g.transform) {
					if ( (t.gameObject.hideFlags & HideFlags.HideInHierarchy)!=0 ) {
						Debug.Log (t.gameObject.name + " is invisible in the hierarchy.");
						count++;
					} 
				}
				Debug.Log (count + " hidden GameObject(s) found.");
			}

			if(GUILayout.Button("Show Hidden GameObjects")){
				GameObject g = GameObject.FindObjectOfType<WorldMapGlobe>().gameObject;
				int count=0;
				foreach(Transform t in g.transform) {
					if ( (t.gameObject.hideFlags & HideFlags.HideInHierarchy)!=0 ) {
						t.gameObject.hideFlags ^= HideFlags.HideInHierarchy;
						count++;
						Debug.Log (g.name + " is now visible in the hierarchy.");
					}
				}
				Debug.Log (count + " GameObject(s) found.");
			}
			
			if(GUILayout.Button("Destroy Hidden GameObjects")){
				GameObject g = GameObject.FindObjectOfType<WorldMapGlobe>().gameObject;
				int count=0;
				foreach(Transform t in g.transform) {
					if ( (t.gameObject.hideFlags & HideFlags.HideInHierarchy)!=0 ) {
						count++;
						Debug.Log (t.gameObject.name + " destroyed.");
						GameObject.DestroyImmediate(t.gameObject);
					}
				}
				Debug.Log (count + " GameObject(s) destroyed.");
			}
		}
	}

}