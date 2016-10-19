//#define LIGHTSPEED

using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.UI;
using System.Collections;
using SimpleJSON;

namespace WPM {

	public class SenseConnectorMenuBAK1 : MonoBehaviour {

		public GameObject	SelectedOrgPanel;
		public GameObject	SelectedCountryPanel;
		public GameObject	SelectedCapabilityPanel;
		public GameObject	OptionPrefab;
		public Text 	SelectedOrgText;

		public WorldMapGlobe map;
		bool animatingField;
		private string[] selectedOrgs;

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
/*
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
*/
			// Map Event Handlers
			map.OnCountryEnter += (int countryIndex, int regionIndex) => Debug.Log ("Entered country " + map.countries [countryIndex].name);
/*
			// Dropdown Event Handlers
			SelectedOrgDropdown.onValueChanged.AddListener (delegate {
				SelectedOrgDropdownValueChangedHandler (SelectedOrgDropdown);
			});
*/
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

		public void getOrgs(){
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

		public void selectOrg(string org){
			Debug.Log ("selecting org " + org);
			StartCoroutine (SelectO(org));
		}

		public void selectCapability(string cap){
			Debug.Log ("selecting capability " + cap);
			StartCoroutine (FilterSelect("Organization Capability",cap));
		}

		public void selectCountry(string cty){
			Debug.Log ("selecting country" + cty);
			StartCoroutine (FilterSelect("Country",cty));
		}

		public void clearSelect(){
			Debug.Log ("clearing org selection");
			StartCoroutine (ClearOrg());
		}

		// WWW Methods //

		IEnumerator GetWWWorgs() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listOrgs");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				Debug.Log (s);
				string[] orgs = s.Split ('^');

				if (orgs [0].Length > 2) {
					orgs [0] = orgs [0].Substring (2, orgs [0].Length - 4);
					orgs [0] = orgs [0].Replace (',', '\n');
				}
				selectedOrgs = orgs[0].Split ('\n');
				SelectedOrgText.text = selectedOrgs[0];
				Debug.Log ("selectedOrg: " + selectedOrgs[0]);

				if (orgs [1].Length > 2) {
					orgs [1] = orgs [1].Substring (2, orgs [1].Length - 3);
					orgs [1] = orgs [1].Replace (',', '\n');
				}
				getCountries ();
				getCapabilities ();
				fillOrgMenu(orgs [1]);
			}
		}

		IEnumerator GetWWWcountries() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listCountries");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				Debug.Log (s);

				fillCountryMenu(s);
			}
		}

		IEnumerator GetWWWcapabilities() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listCapabilities");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				Debug.Log (s);

				fillCapabilityMenu(s);
			}
		}

		IEnumerator SelectO(string org) {
			WWWForm form = new WWWForm();
			form.AddField("fieldName", "Organization Name");
//			form.AddField("fieldValue", "Deloitte"); // Armanino // Empathica
			form.AddField("fieldValue", org); // Armanino // Empathica

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

		IEnumerator ClearOrg() {
			WWWForm form = new WWWForm();
			form.AddField("fieldName", "Organization Name");

			UnityWebRequest www = UnityWebRequest.Post("http://localhost:8081/clearOrg", form);
			yield return www.Send();

			if(www.isError) {
				Debug.Log(www.error);
			}
			else {
				string s = www.downloadHandler.text;
				Debug.Log("selection POST complete! " + s);

				yield return new WaitForSeconds(1);
				getOrgs();
			}
		}

		IEnumerator FilterSelect(string field, string cap) {
			WWWForm form = new WWWForm();
			form.AddField("fieldName", field);
			form.AddField("fieldValue", cap);

			UnityWebRequest www = UnityWebRequest.Post("http://localhost:8081/filter", form);
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
		public void fillOrgMenu(string textString) {
			textString = textString.Replace ("\"", string.Empty);
			string[] SelectedOrgs = textString.Split ('\n');

			// Remove old orgs
			Transform[] allTransforms = SelectedOrgPanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedOrgPanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			// Add new orgs
			foreach (string o in SelectedOrgs) {
				GameObject btn = Instantiate (OptionPrefab) as GameObject;
				btn.transform.SetParent (SelectedOrgPanel.transform, false);
				btn.GetComponentInChildren<Text>().text = o;
				Button b = btn.GetComponent<Button> ();
				string org = o;
				b.onClick.AddListener(() => selectOrg(org));
			}
		}

		void fillCountryMenu(string textString) {
			JSONNode SelectedCountries = JSON.Parse (textString);

			map.HideCountrySurfaces ();
/*
			Color color = new Color (UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f), UnityEngine.Random.Range (0.0f, 1.0f));
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
*/
//			map.FlyToCountry("United States of America");

			// Remove old countries
			Transform[] allTransforms = SelectedCountryPanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedCountryPanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			// Add new countries
			for(int i=0; i < SelectedCountries.AsArray.Count; i++){
				string o = SelectedCountries[i]["c"];
				int s = SelectedCountries[i]["s"].AsInt;
				int t = SelectedCountries[i]["t"].AsInt;
				if (!string.IsNullOrEmpty(o)) {

					if (o == "United States") {
						o = "United States of America";
					}

					GameObject btn = Instantiate (OptionPrefab) as GameObject;
					btn.transform.SetParent (SelectedCountryPanel.transform, false);
					btn.GetComponentInChildren<Text> ().text = o;

					Button b = btn.GetComponent<Button> ();
					string cty = o;
					b.onClick.AddListener (() => selectCountry (cty));

					if (s == 1) {
						Color color = new Color (0.0f, 0.5f, 0.0f);
						btn.GetComponentInChildren<Button> ().image.color = color;
						b.image.color = Color.green;

						int countryIndex = map.GetCountryIndex (o);
						if (countryIndex > -1) {
							map.ToggleCountrySurface (o, true, Color.green);
						} else {
							Debug.Log (o + " NOT FOUND");
						}
					}
				}
			}
		}

		void fillCapabilityMenu(string textString) {
			JSONNode SelectedCapabilities = JSON.Parse (textString);

			// Remove old countries
			Transform[] allTransforms = SelectedCapabilityPanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedCapabilityPanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			// Add new capabilities
			for(int i=0; i < SelectedCapabilities.AsArray.Count; i++){
				string o = SelectedCapabilities[i]["c"];
				int s = SelectedCapabilities[i]["s"].AsInt;
				int t = SelectedCapabilities[i]["t"].AsInt;
				GameObject btn = Instantiate (OptionPrefab) as GameObject;
				btn.transform.SetParent (SelectedCapabilityPanel.transform, false);
				btn.GetComponentInChildren<Text> ().text = o;

				Button b = btn.GetComponent<Button> ();
				string cap = o;
				b.onClick.AddListener(() => selectCapability(cap));

				if (s == 1) {
					Color color = new Color (0.0f,0.5f,0.0f);
					btn.GetComponentInChildren<Button> ().image.color = color;
					b.image.color = Color.green;
				}
			}
		}

	}

}