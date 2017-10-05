﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UMA.PoseTools;
using UMA;


namespace UnityAsapIntegration.ASAP.UMA {
    public class ExpressionTargetEditor : MonoBehaviour {
        public static string projectFolder = "UnityAsapIntegration/Resources/DefaultUMAFaceTargets";
        public static string resourcesFolder = "DefaultUMAFaceTargets";

        public ExpressionPlayer ep;
        public static string INDEX_FILE = "__INDEX";
        public string[] nonDefaultRanges = new string[] {"tongueCurl", "noseSneer", "browsIn"};
        public float[] currentTargetValues;
        public bool[] currentUseControl;
        public static string[] ExpressionTargets;
        public static string[] ExpressionTargetDescriptions;

        public int currentlyEditing;

        void Awake() {
            LoadIndex();
        }

        // Use this for initialization
        void Start() {
            currentlyEditing = 0;

            Load();
        }

        // Update is called once per frame
        void Update() {
            if (ep == null) {
                ep = GetComponent<ExpressionPlayer>();
                return;
            }

            for (int i = 0; i < ExpressionPlayer.PoseCount; i++) {
                if (!currentUseControl[i]) {
                    currentTargetValues[i] = 0.0f;
                }
            }
            ep.Values = currentTargetValues;
        }

        public void Rename(string newName) {
            ExpressionTargets[currentlyEditing] = newName;
            Save();
        }

        public void Duplicate(string newName) {
            Save();
            AddTarget(newName, ExpressionTargetDescriptions[currentlyEditing]);
        }

        public void AddTarget(string name, string description) {
            var newNameList = new List<string>(ExpressionTargets);
            var newDescList = new List<string>(ExpressionTargetDescriptions);
            newNameList.Insert(currentlyEditing, name);
            newDescList.Insert(currentlyEditing, description);
            ExpressionTargets = newNameList.ToArray();
            ExpressionTargetDescriptions = newDescList.ToArray();
            //LoadValues(currentlyEditing);
            Save();
            SaveIndex();
        }

        public void RemoveCurrentTarget() {
            var newNameList = new List<string>(ExpressionTargets);
            var newDescList = new List<string>(ExpressionTargetDescriptions);
            newNameList.RemoveAt(currentlyEditing);
            newDescList.RemoveAt(currentlyEditing);
            ExpressionTargets = newNameList.ToArray();
            ExpressionTargetDescriptions = newDescList.ToArray();
            SaveIndex();
        }

        public void LoadIndex() {
            string[] lines;
            List<string> targetNames = new List<string>();
            List<string> targetDescriptions = new List<string>();
            

            
#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_STANDALONE_OSX))
            /*
            string indexFile = System.IO.Path.Combine(System.IO.Path.Combine(Application.streamingAssetsPath, "DefaultUMAFaceTargets"), "__INDEX.txt");
            WWW www = new WWW(indexFile);
            while (!www.isDone) { }
            if (!string.IsNullOrEmpty(www.text)) { 
                lines = www.text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines) {
                    string[] elems = line.Split(new char[] { ' ' }, 2);
                    if (elems.Length >= 2) {
                        targetNames.Add(elems[0]);
                        targetDescriptions.Add(elems[1]);
                    }
                }
            }*/
            TextAsset indexFile = Resources.Load(resourcesFolder+"/__INDEX") as TextAsset;
            if (indexFile != null) {
                lines = indexFile.text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines) {
                    string[] elems = line.Split(new char[] { ' ' }, 2);
                    if (elems.Length >= 2) {
                        targetNames.Add(elems[0]);
                        targetDescriptions.Add(elems[1]);
                    }
                }
            }
#else
            string path = GetFileName(INDEX_FILE);
            if (System.IO.File.Exists(path)) {
                lines = System.IO.File.ReadAllLines(path);
                foreach (string line in lines) {
                    string[] elems = line.Split(new char[] {' '}, 2);
                    targetNames.Add(elems[0]);
                    targetDescriptions.Add(elems[1]);
                }
            } else {
                SaveIndex();
            }
#endif
            ExpressionTargets = targetNames.ToArray();
            ExpressionTargetDescriptions = targetDescriptions.ToArray();
        }

        public void SaveIndex() {
            string index = "";
            for (int t = 0; t < ExpressionTargets.Length; t++) {
                index += ExpressionTargets[t] + " " + ExpressionTargetDescriptions[t] + "\r\n";
            }

#if UNITY_EDITOR
            System.IO.File.WriteAllText(GetFileName(INDEX_FILE), index);
#endif
        }

        public static string GetFileName(string name) {
            return Application.dataPath + "/" + projectFolder + "/" + name + ".txt";
        }

        public static ExpressionControlMapping LoadMapping(string name) {
            string[] lines;
#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_STANDALONE_OSX))
            /*
            string mappingFile = System.IO.Path.Combine(System.IO.Path.Combine(Application.streamingAssetsPath, "DefaultUMAFaceTargets"), name + ".txt");
            WWW www = new WWW(mappingFile);
            while (!www.isDone) { }
            if (!string.IsNullOrEmpty(www.text)) {
                lines = www.text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            } else {
                lines = new string[] { };
            } */

            TextAsset indexFile = Resources.Load(resourcesFolder+"/"+name) as TextAsset;
            if (indexFile != null) {
                lines = indexFile.text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            } else {
                lines = new string[] { };
            }
#else
            string path = GetFileName(name);
            if (System.IO.File.Exists(path)) {
                lines = System.IO.File.ReadAllLines(path);
            } else {
                //System.IO.File.Create(path);
                return null;
            }
#endif
            List<string> names = new List<string>();
            List<float> values = new List<float>();
            foreach (string line in lines) {
                string[] elems = line.Split(' ');
                if (elems.Length >= 2) {
                    names.Add(elems[0]);
                    values.Add(float.Parse(elems[1]));
                }
            }

            return new ExpressionControlMapping(names.ToArray(), values.ToArray());
        }

        public void LoadValues(int altLoad) {
            Defaults();
            ExpressionControlMapping mapping = LoadMapping(ExpressionTargets[altLoad]);
            if (mapping == null) {
                Save(); // Write defaults
            } else {
                for (int i = 0; i < mapping.indexes.Length; i++) {
                    currentTargetValues[mapping.indexes[i]] = mapping.values[i];
                    currentUseControl[mapping.indexes[i]] = true;
                }
            }
        }

        public void Load() {
            Defaults();
            ExpressionControlMapping mapping = LoadMapping(ExpressionTargets[currentlyEditing]);

            if (mapping == null) {
                Save(); // Write defaults
            } else {
                for (int i = 0; i < mapping.indexes.Length; i++) {
                    currentTargetValues[mapping.indexes[i]] = mapping.values[i];
                    currentUseControl[mapping.indexes[i]] = true;
                }
            }
        }

        public void Defaults() {
            currentTargetValues = new float[ExpressionPlayer.PoseCount];
            currentUseControl = new bool[ExpressionPlayer.PoseCount];
        }

        public void Reset() {
            Load();
        }

        public void Save() {
            string file = "";
            for (int i = 0; i < ExpressionPlayer.PoseCount; i++) {
                if (currentUseControl[i]) {
                    file += ExpressionPlayer.PoseNames[i];
                    file += " ";
                    file += currentTargetValues[i].ToString("F2");
                    file += "\r\n";
                }
            }
#if UNITY_EDITOR
            System.IO.File.WriteAllText(GetFileName(ExpressionTargets[currentlyEditing]), file);
#endif
        }

        void OnApplicationQuit() {
            SaveIndex();
        }
    }
}