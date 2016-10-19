//#define LIGHTSPEED

using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.UI;
using System.Collections;

namespace WPM {

	public class SenseConnector : MonoBehaviour {

		public GameObject orgCanvas;
		public GameObject countryCanvas;
		public GameObject capabilitiesCanvas;
		public static Text OrgText;
		public static Text CountryText;
		public static Text CapabilitiesText;

		public WorldMapGlobe map;
		bool animatingField;

		// Event Handlers //

		void Awake () {
			#if LIGHTSPEED
				Camera.main.fieldOfView = 180;
				animatingField = true;
			#endif
			map.earthInvertedMode = true;
			getOrgs ();
		}

		// initialize whatnot
		void Start () {
			//		StartCoroutine (GetText ());
			//		StartCoroutine (Select());

			Font ArialFont = (Font)Resources.GetBuiltinResource (typeof(Font), "Arial.ttf");

			OrgText = orgCanvas.AddComponent<Text> ();
			OrgText.font = ArialFont;
			OrgText.fontSize = 32;
			OrgText.material = ArialFont.material;

			CountryText = countryCanvas.AddComponent<Text> ();
			CountryText.font = ArialFont;
			CountryText.material = ArialFont.material;

			CapabilitiesText = capabilitiesCanvas.AddComponent<Text> ();
			CapabilitiesText.font = ArialFont;
			CapabilitiesText.material = ArialFont.material;

			// Map Event Handlers
			map.OnCountryEnter += (int countryIndex, int regionIndex) => Debug.Log ("Entered country " + map.countries [countryIndex].name);
		}

		void Update () {
			/*
			if (Input.GetMouseButtonDown (0)) {
				getOrgs ();
			}
			if (Input.GetButtonDown ("Fire1")) {
				getOrgs ();
			}
			*/
		}

		// Update is called once per frame
		void OnGUI () {

			// Animates the camera field of view (just a cool effect at the begining)
			if (animatingField) {
				if (Camera.main.fieldOfView > 60) {
					Camera.main.fieldOfView -= (181.0f - Camera.main.fieldOfView) / (220.0f - Camera.main.fieldOfView); 
				} else {
					Camera.main.fieldOfView = 60;
					//map.earthInvertedMode = true;
					animatingField = false;
				}
			}
		}

		void getOrgs(){
			Debug.Log ("getting orgs");
			StartCoroutine (GetWWWorgs ());
		}

		void getCountries(){
			Debug.Log ("getting countries");
			StartCoroutine (GetWWWcountries());
		}

		void getCapabilities(){
			Debug.Log ("getting capabilities");
			StartCoroutine (GetWWWcapabilities());
		}

		void selectOrg(){
			Debug.Log ("selecting org");
			StartCoroutine (Select());
		}

		// WWW Methods //

		IEnumerator GetWWWorgs() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listOrgs");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				s = s.Replace (',', '\n');
				Debug.Log (s);
				createOrgText(s);
				getCountries ();
				getCapabilities ();
			}
		}

		IEnumerator GetWWWcountries() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listCountries");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				s = s.Substring (2, s.Length - 4);
				s = s.Replace ("United States", "United States of America");
				s = s.Replace ("\"", string.Empty);
				s = s.Replace (',', '\n');
				Debug.Log (s);
				createCountryText(s);
				
				string[] countries;
				countries = s.Split ('\n');

				Color color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
				//map.ToggleCountrySurface ("Canada", true, color);
				//map.ToggleCountrySurface ("France", true, color);
				int countryIndex;
				foreach(string c in countries){
//					Debug.Log (c);
					countryIndex = map.GetCountryIndex(c);
					if (countryIndex > -1) {
						map.ToggleCountrySurface (c, true, color);
					} else {
						Debug.Log (c + " NOT FOUND");
					}
				}
	//			map.FlyToCountry("United States of America");

			}
		}

		IEnumerator GetWWWcapabilities() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listCapabilities");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				s = s.Substring (2, s.Length - 4);
				s = s.Replace ("\"", string.Empty);
				s = s.Replace (',', '\n');
				Debug.Log (s);
				createCapabilitiesText(s);
			}
		}

		IEnumerator Select() {
			WWWForm form = new WWWForm();
			form.AddField("fieldName", "Organization Name");
			form.AddField("fieldValue", "Deloitte"); // Armanino // Empathica

			UnityWebRequest www = UnityWebRequest.Post("http://localhost:8081/selectOrg", form);
			yield return www.Send();

			if(www.isError) {
				Debug.Log(www.error);
			}
			else {
				string s = www.downloadHandler.text;
				s = s.Replace (',', '\n');
				Debug.Log (s);
				Debug.Log("selection POST complete!");

				yield return new WaitForSeconds(1);
				getOrgs();
			}
		}

		// UI Methods //

		public static void createOrgText(string textString) {

			OrgText.text = "\n\n\n\n\n\t\t\t" + textString;
		}

		public static void createCapabilitiesText(string textString) {

			CapabilitiesText.text = textString;
		}

		public static void createCountryText(string textString) {

			CountryText.text = textString;
		}
	}

}