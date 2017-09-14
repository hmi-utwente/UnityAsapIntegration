using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityAsapIntegration.ASAP;

public class TestSpeech : MonoBehaviour {
	public string agentId1 = "agent_a";
	public string agentId2 = "agent_b";

	string txtA = "Hello, I am A.";
	string txtB = "Hello, I am B.";
	
	string bmlPrefix = "speechbml";
	int idCounter;
	BMLRequests bmlreq;
	string bmlBodyTemplate = "<bml xmlns=\"http://www.bml-initiative.org/bml/bml-1.0\"  id=\"{prefix}{bmlid}\" characterId=\"{vhId}\" xmlns:bmlt=\"http://hmi.ewi.utwente.nl/bmlt\">\n{content}\n</bml>";
	string speechContentTemplate = "\t<speech id=\"speech1\">\n\t\t<text>{text}</text>\n\t</speech>";

	private string test1 = "<bml xmlns=\"http://www.bml-initiative.org/bml/bml-1.0\"  id=\"testBML_a_1\" characterId=\"{vhId}\" xmlns:bmlt=\"http://hmi.ewi.utwente.nl/bmlt\">\n\t<speech id=\"speech1\">\n\t\t<text>Hello, I am A.</text>\n\t</speech>\n</bml>";
	private string test2 = "<bml xmlns=\"http://www.bml-initiative.org/bml/bml-1.0\"  id=\"testBML_b_1\" characterId=\"{vhId}\" xmlns:bmlt=\"http://hmi.ewi.utwente.nl/bmlt\">\n\t<speech id=\"speech1\" start=\"testBML_a_1:speech1:end+0.4\">\n\t\t<text>And I am B.</text>\n\t</speech>\n</bml>";
	
	
	// Use this for initialization
	void Start () {
		bmlreq = FindObjectOfType<BMLRequests> ();
		idCounter = 0;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Say(string text, string vhId) {
		idCounter++;
		string bmlBody = bmlBodyTemplate.Replace ("{prefix}", bmlPrefix).Replace ("{bmlid}", idCounter.ToString ());
		string bmlContent = speechContentTemplate.Replace ("{text}", text);
		string speechBml = bmlBody.Replace ("{content}", bmlContent);
		speechBml = speechBml.Replace ("{vhId}", vhId);
		Debug.Log (speechBml);
		bmlreq.SendBML (speechBml);
	}

	void OnGUI() {
		if (bmlreq != null) {
			txtA = GUI.TextField(new Rect(10, 10, 200, 20), txtA, 25);
			if (GUI.Button (new Rect (220, 10, 80, 20), "SEND 1")) {
				Say (txtA, agentId1);
			}
			
			txtB = GUI.TextField(new Rect(10, 60, 200, 20), txtB, 25);
			if (GUI.Button (new Rect (220, 60, 80, 20), "SEND 2")) {
				Say (txtB, agentId2);
			}
			
			if (GUI.Button (new Rect (210, 130, 100, 20), "SEND Sript")) {
				bmlreq.SendBML (test1.Replace("{vhId}", agentId1));
				bmlreq.SendBML (test2.Replace("{vhId}", agentId2));
			}
		}
	}
}
