//#define LIGHTSPEED

using UnityEngine;
using UnityEngine.Experimental.Networking;
using UnityEngine.UI;
using System.Collections;
using SimpleJSON;
using System.IO;

namespace WPM {

	public class SenseConnectorMenu : MonoBehaviour {

		public GameObject	SelectedOrgPanel;
		public GameObject	SelectedCountryPanel;
		public GameObject	SelectedCapabilityPanel;
		public GameObject	SelectedTypePanel;
		public GameObject	SelectedIndustryPanel;
		public GameObject	OptionPrefab;
		public GameObject	MarkerPrefab;
		public Text 		SelectedOrgText;
		public Text 		NumPotentialPartners;

		public WorldMapGlobe map;
		bool animatingField;
		private string selectedOrgs;
		public JSONClass AllOffices = new JSONClass();
		private bool locationInit = false;

		// Event Handlers //

		void Awake () {
			#if LIGHTSPEED
				Camera.main.fieldOfView = 180;
				animatingField = true;
			#endif
			map.earthInvertedMode = true;
			getOrgs ();
			getLocations ();
		}

		// initialize whatnot
		void Start () {
			//		StartCoroutine (GetText ());
			//		StartCoroutine (Select());

			Font ArialFont = (Font)Resources.GetBuiltinResource (typeof(Font), "Arial.ttf");
			// Map Event Handlers
			map.OnCountryEnter += (int countryIndex, int regionIndex) => Debug.Log ("Entered country " + map.countries [countryIndex].name);
			map.OnCountryClick += (int countryIndex, int regionIndex) => selectCountry (map.countries [countryIndex].name);
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

		void getLocations(){
			Debug.Log ("getting locations");
			StartCoroutine (GetWWWlocations());
		}

		void getCapabilities(){
			Debug.Log ("getting capabilities");
			StartCoroutine (GetWWWcapabilities());
		}

		void getTypes(){
			Debug.Log ("getting types");
			StartCoroutine (GetWWWtypes());
		}

		void getIndustries(){
			Debug.Log ("getting industries");
			StartCoroutine (GetWWWindustries());
		}

		public void selectOrg(string org){
			Debug.Log ("selecting org " + org);
			StartCoroutine (SelectO(org));
		}

		public void selectCapability(string cap){
			Debug.Log ("selecting capability " + cap);
			GameObject btn = GameObject.Find ("CapabilityButton");
			btn.GetComponentInChildren<Text> ().text = "Capabilities:" + '\n' + cap;

			StartCoroutine (FilterSelect("Organization Capability",cap));
		}

		public void selectType(string type){
			Debug.Log ("selecting type " + type);
			GameObject btn = GameObject.Find ("TypeButton");
			btn.GetComponentInChildren<Text> ().text = "Types:" + '\n' + type;

			StartCoroutine (FilterSelect("Organization Type",type));
		}

		public void selectIndustry(string industry){
			Debug.Log ("selecting industry " + industry);
			GameObject btn = GameObject.Find ("IndustryButton");
			btn.GetComponentInChildren<Text> ().text = "Industries:" + '\n' + industry;

			StartCoroutine (FilterSelect("Organization Industry",industry));
		}

		public void selectCountry(string cty){
			if (cty == "United States of America") {
				cty = "United States";
			}
			Debug.Log ("selecting country" + cty);
			GameObject btn = GameObject.Find ("CountryButton");
			btn.GetComponentInChildren<Text> ().text = "Countries:" + '\n' + cty;

			StartCoroutine (FilterSelect("Country",cty));
		}

		public void clearSelect(){
			Debug.Log ("clearing org selection");
			GameObject btn;
			btn = GameObject.Find ("CapabilityButton");
			btn.GetComponentInChildren<Text> ().text = "Capabilities:" + '\n';
			btn = GameObject.Find ("TypeButton");
			btn.GetComponentInChildren<Text> ().text = "Types:" + '\n';
			btn = GameObject.Find ("IndustryButton");
			btn.GetComponentInChildren<Text> ().text = "Industries:" + '\n';
			btn = GameObject.Find ("CountryButton");
			btn.GetComponentInChildren<Text> ().text = "Countries:" + '\n';
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
				selectedOrgs = s;

				Debug.Log (s);
				getCountries ();
				getCapabilities ();
				getTypes ();
				getIndustries ();
				fillOrgMenu(s);
				if (locationInit) {
					addOfficeMarkers (s);
				}
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

		IEnumerator GetWWWlocations() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listLocations");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				//Debug.Log (s);

				//initOfficeMarkers (s);
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

		IEnumerator GetWWWtypes() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listTypes");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				Debug.Log (s);

				fillTypeMenu(s);
			}
		}

		IEnumerator GetWWWindustries() {
			UnityWebRequest www = UnityWebRequest.Get ("http://localhost:8081/listIndustries");
			yield return www.Send ();

			if (www.isError) {
				Debug.Log (www.error);
			} else {
				string s = www.downloadHandler.text;
				Debug.Log (s);

				fillIndustryMenu(s);
			}
		}

		IEnumerator SelectO(string org) {
			WWWForm form = new WWWForm();
			form.AddField("fieldName", "Partner Name");
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
			form.AddField("fieldName", "Partner Name");

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
			JSONNode SelectedOrgs = JSON.Parse (textString);

//			textString = textString.Replace ("\"", string.Empty);
//			string[] SelectedOrgs = textString.Split ('\n');

			// Remove old orgs
			Transform[] allTransforms = SelectedOrgPanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedOrgPanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			int ctr = 0;
			for (int i = 0; i < SelectedOrgs.AsArray.Count; i++) {
				string o = SelectedOrgs [i] ["org"];
				int s = SelectedOrgs [i] ["s"].AsInt;
				int t = SelectedOrgs [i] ["t"].AsInt;

				if (!string.IsNullOrEmpty (o)) {
					if (s > 0) {
						SelectedOrgText.text = o;
					}

					GameObject btn = Instantiate (OptionPrefab) as GameObject;
					btn.transform.SetParent (SelectedOrgPanel.transform, false);
					btn.GetComponentInChildren<Text> ().text = o;

					Button b = btn.GetComponent<Button> ();
					string org = o;
					b.onClick.AddListener (() => selectOrg (org));

					if (s == 1) {
						Color color = new Color (0.0f, 0.5f, 0.0f);
						btn.GetComponentInChildren<Button> ().image.color = color;
						b.image.color = Color.green;
						ctr++;
					}
				}
			}
			NumPotentialPartners.text = "(number of potential partners: " + (SelectedOrgs.AsArray.Count-ctr).ToString() + ")";

		}

		void fillCountryMenu(string textString) {
			JSONNode SelectedCountries = JSON.Parse (textString);

			map.HideCountrySurfaces ();
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

					Color green = new Color (0.0f, 0.75f, 0.0f);
					Color grey 	= new Color (0.15f, 0.15f, 0.15f);

					if (s == 1) {
						b.image.color = Color.green;

						int countryIndex = map.GetCountryIndex (o);
						if (countryIndex > -1) {
							map.ToggleCountrySurface (o, true, green);
						} else {
							Debug.Log (o + " NOT FOUND");
						}
					} else if (t < 1) {
						b.image.color = Color.grey;

						int countryIndex = map.GetCountryIndex (o);
						if (countryIndex > -1) {
							map.ToggleCountrySurface (o, true, grey);
						}
					} else {
						b.image.color = Color.white;
					}
				}
			}
		}

		void initOfficeMarkers(string textString) {
			if (true) {
				bool newData = false;

				if (newData) {
					JSONNode SelectedOffices = JSON.Parse (textString);
					string fileOutput = "";

					// Add new offices
					for (int i = 0; i < SelectedOffices.AsArray.Count; i++) {
						string o = SelectedOffices [i] ["org"];
						float lat = SelectedOffices [i] ["lat"].AsFloat;
						float lon = SelectedOffices [i] ["lon"].AsFloat;
						//Debug.Log (lat + " / " + lon);
						if (!string.IsNullOrEmpty (o)) {

							map.calc.fromUnit = UNIT_TYPE.DecimalDegrees;
							map.calc.fromLatDec = lat;
							map.calc.fromLonDec = lon;
							map.calc.Convert ();
							Vector3 markerLoc = map.calc.toSphereLocation;

							JSONArray location = new JSONArray ();
							location.Add (new JSONData (markerLoc.x));
							location.Add (new JSONData (markerLoc.y));
							location.Add (new JSONData (markerLoc.z));
							AllOffices.Add (o, location);

							fileOutput = fileOutput + o + '\t' + markerLoc.x + '\t' + markerLoc.y + '\t' + markerLoc.z + '\n';
							//Debug.Log (AllOffices[o][0].AsFloat);
						}
					}

					using (StreamWriter sw = new StreamWriter ("OfficeLocations.txt")) {
						sw.WriteLine (fileOutput);
					}
					Debug.Log ("Finished writing file");
				} else {
					StreamReader theReader = new StreamReader ("OfficeLocations.txt");
					string line;
					using (theReader) {
						do {
							line = theReader.ReadLine ();

							if (line != null) {
								string[] entries = line.Split ('\t');
								if (entries.Length == 4) {
									JSONArray location = new JSONArray ();
									location.Add (new JSONData (entries [1]));
									location.Add (new JSONData (entries [2]));
									location.Add (new JSONData (entries [3]));

									if (AllOffices [entries [0]] != null) {
										AllOffices [entries [0]].Add (location);
									} else {
										JSONArray office = new JSONArray ();
										office.Add (location);
										AllOffices.Add (entries [0], office);
									}
								}
							}
						} while (line != null);
						theReader.Close ();
					}
					Debug.Log (AllOffices);
				}

				locationInit = true;
				addOfficeMarkers (selectedOrgs);
			}
		}

		void addOfficeMarkers(string textString) {
			if (true) {
				JSONNode SelectedOffices = JSON.Parse (textString);

				int ctr = 0;
				// Remove old offices
				Debug.Log ("Clearing markers");
				map.ClearMarkers ();
				// Add new offices
				for (int i = 0; i < SelectedOffices.AsArray.Count; i++) {
					string o = SelectedOffices [i] ["org"];

					if (!string.IsNullOrEmpty (o)) {

						if (AllOffices [o] != null) {
							for (int j = 0; j < AllOffices [o].AsArray.Count; j++) {
								//							GameObject marker = Instantiate (MarkerPrefab) as GameObject;
								float x = AllOffices [o] [j] [0].AsFloat;
								float y = AllOffices [o] [j] [1].AsFloat;
								float z = AllOffices [o] [j] [2].AsFloat;
								Vector3 markerLoc = new Vector3 (x, y, z);
								//							map.AddMarker (marker, markerLoc, 0.02f, false, -1.1f); 
								ctr++;
							}
						}
					}
				}
				Debug.Log ("Added " + ctr + " markers");
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
					b.image.color = Color.green;
				} else if (t < 1) {
					b.image.color = Color.grey;
				} else {
					b.image.color = Color.white;
				}
			}
		}

		void fillTypeMenu(string textString) {
			JSONNode SelectedTypes = JSON.Parse (textString);

			// Remove old countries
			Transform[] allTransforms = SelectedTypePanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedTypePanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			// Add new types
			for(int i=0; i < SelectedTypes.AsArray.Count; i++){
				string o = SelectedTypes[i]["c"];
				int s = SelectedTypes[i]["s"].AsInt;
				int t = SelectedTypes[i]["t"].AsInt;
				GameObject btn = Instantiate (OptionPrefab) as GameObject;
				btn.transform.SetParent (SelectedTypePanel.transform, false);
				btn.GetComponentInChildren<Text> ().text = o;

				Button b = btn.GetComponent<Button> ();
				string type = o;
				b.onClick.AddListener(() => selectType(type));

				if (s == 1) {
					b.image.color = Color.green;
				} else if (t < 1) {
					b.image.color = Color.grey;
				} else {
					b.image.color = Color.white;
				}
			}
		}

		void fillIndustryMenu(string textString) {
			JSONNode SelectedIndustries = JSON.Parse (textString);

			// Remove old industries
			Transform[] allTransforms = SelectedIndustryPanel.GetComponentsInChildren<Transform>();
			foreach(Transform childObjects in allTransforms){
				if(SelectedIndustryPanel.transform.IsChildOf(childObjects.transform) == false)
					Destroy(childObjects.gameObject);
			}

			// Add new industries
			for(int i=0; i < SelectedIndustries.AsArray.Count; i++){
				string o = SelectedIndustries[i]["c"];
				int s = SelectedIndustries[i]["s"].AsInt;
				int t = SelectedIndustries[i]["t"].AsInt;
				GameObject btn = Instantiate (OptionPrefab) as GameObject;
				btn.transform.SetParent (SelectedIndustryPanel.transform, false);
				btn.GetComponentInChildren<Text> ().text = o;

				Button b = btn.GetComponent<Button> ();
				string type = o;
				b.onClick.AddListener(() => selectIndustry(type));

				if (s == 1) {
					b.image.color = Color.green;
				} else if (t < 1) {
					b.image.color = Color.grey;
				} else {
					b.image.color = Color.white;
				}
			}

		}

	}

}