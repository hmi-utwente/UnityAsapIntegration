using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASAP {

	public class TestSpeech : MonoBehaviour {

		string txt = "Hello.";
		string bmlPrefix = "speechbml";
		int idCounter;
		BMLRequests bmlreq;
		string bmlBodyTemplate = "<bml xmlns=\"http://www.bml-initiative.org/bml/bml-1.0\"  id=\"{prefix}{bmlid}\" xmlns:bmlt=\"http://hmi.ewi.utwente.nl/bmlt\">\n{content}\n</bml>";
		string speechContentTemplate = "\t<speech id=\"speech1\">\n\t\t<text>{text}</text>\n\t</speech>";


		// Use this for initialization
		void Start () {
			bmlreq = FindObjectOfType<BMLRequests> ();
			idCounter = 0;
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		public void Say(string text) {
			idCounter++;
			string bmlBody = bmlBodyTemplate.Replace ("{prefix}", bmlPrefix).Replace ("{bmlid}", idCounter.ToString ());
			string bmlContent = speechContentTemplate.Replace ("{text}", text);
			string speechBml = bmlBody.Replace ("{content}", bmlContent);
			Debug.Log (speechBml);
			bmlreq.SendBML (speechBml);
		}

		void OnGUI() {
			if (bmlreq != null) {
				txt = GUI.TextField(new Rect(10, 10, 200, 20), txt, 25);
				if (GUI.Button (new Rect (220, 10, 40, 20), "SEND")) {
					Say (txt);
				}
			}
		}
	}

}
