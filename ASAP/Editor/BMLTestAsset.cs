using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "BMLTest", menuName = "ASAP/BMLTestAsset", order = 1)]
public class BMLTestAsset : ScriptableObject {
	[SerializeField]
	public string[] bmls;
	
}

/*
[Serializable]
public class BMLData {
	public string[] bmls;
	
}*/