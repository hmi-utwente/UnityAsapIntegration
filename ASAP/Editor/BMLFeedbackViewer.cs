﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;

namespace UnityAsapIntegration.ASAP.Editor {
	
	public class BMLFeedbackViewer : EditorWindow {
	
		private Vector2 scrollBlock;
		private Vector2 scrollPrediction;
		private Vector2 scrollSyncPoint;
		private Vector2 scrollWarning;
		
		private bool toggleBlock = true;
		private bool togglePrediction = false;
		private bool toggleSyncPoint = false;
		private bool toggleWarning = false;
		
		private static BMLFeedbackViewer w;
		
		private BMLFeedback bmlFeedback;
	
		private static List<string> listBlock = new List<string>();
		private static List<string> listPrediction = new List<string>();
		private static List<string> listSyncPoint = new List<string>();
		private static List<string> listWarning = new List<string>();
	
		private bool initializedInPlay;
		
		void Update() {
			if (EditorApplication.isPlaying && !initializedInPlay) {
				initializedInPlay = true;
				Register();
			} else if (!EditorApplication.isPlaying) {
				initializedInPlay = false;
			}
		}
		
		[MenuItem("ASAP/BML Feedback Viewer")]
		static void Init() {
			Debug.Log("INIT");
			w = GetWindow<BMLFeedbackViewer>(false, "BML Feedback", true);
			w.Show();
			w.Register();
		}
		
		void Register() {
			if (bmlFeedback == null) 
				bmlFeedback = FindObjectOfType<BMLFeedback>();

			if (bmlFeedback != null) {
				bmlFeedback.BlockProgressEventHandler += new BlockProgressCallback(OnBlockProgress);
				bmlFeedback.PredictionFeedbackEventHandler += new PredictionFeedbackCallback(OnPredictionFeedback);
				bmlFeedback.SyncPointProgressEventHandler += new SyncPointProgressCallback(OnSyncPointProgress);
				bmlFeedback.WarningFeedbackEventHandler += new WarningFeedbackCallback(OnWarningFeedback);
			} else {
				// .. disable?
			}
		}
		
		private void OnGUI() {
			GUI.skin.label.wordWrap = true;
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.BeginHorizontal();
			
			Color defaultColor = GUI.color;
			Color warnStyle = new Color(1.0f, 0.95f, 0.8f);
			Color predictionStyle = new Color(0.9f, 0.7f, 0.95f);
			Color blockStyle = new Color(0.8f, 0.95f, 0.7f);
			Color syncStyle = new Color(0.7f, 0.95f, 0.95f);
				
			GUIStyle warnLabelStyle = new GUIStyle(GUI.skin.label);
			if (listWarning.Count > 0) {
				warnLabelStyle.normal.textColor = new Color(0.55f, 0.35f, 0.05f);
			}
			EditorGUILayout.LabelField("Warning Feedback: ", warnLabelStyle);
			GUILayout.FlexibleSpace();
			if (!toggleWarning && GUILayout.Button("Show")) {
				toggleWarning = true;
				toggleBlock = false;
				togglePrediction = false;
				toggleSyncPoint = false;
			}
			if (GUILayout.Button("Clear")) {
				listWarning.Clear();
			}
			GUILayout.EndHorizontal();
			if (toggleWarning) {
				GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				scrollWarning = GUILayout.BeginScrollView(scrollWarning, false, false, GUILayout.ExpandWidth(true),
					GUILayout.ExpandHeight(false));
				foreach (string fb in listWarning) {
					GUI.color=warnStyle;
					GUILayout.BeginVertical(EditorStyles.helpBox);
					GUILayout.Label(fb);
					GUILayout.EndVertical();
					GUI.color=defaultColor;
					GUILayout.Space(5.0f);
				}
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Block Progress: ");
			GUILayout.FlexibleSpace();
			if (!toggleBlock && GUILayout.Button("Show")) {
				toggleBlock = true;
				toggleWarning = false;
				togglePrediction = false;
				toggleSyncPoint = false;
			}
			if (GUILayout.Button("Clear")) {
				listBlock.Clear();
			}
			GUILayout.EndHorizontal();
			if (toggleBlock) {
				GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				scrollBlock = GUILayout.BeginScrollView(scrollBlock, false, false, GUILayout.ExpandWidth(true),
					GUILayout.ExpandHeight(true));
				foreach (string fb in listBlock) {
					GUI.color=blockStyle;
					GUILayout.BeginVertical(EditorStyles.helpBox);
					GUILayout.Label(fb);
					GUILayout.EndVertical();
					GUI.color=defaultColor;
					GUILayout.Space(5.0f);
				}
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("SyncPoint Feedback: ");
			GUILayout.FlexibleSpace();
			if (!toggleSyncPoint && GUILayout.Button("Show")) {
				toggleSyncPoint = true;
				toggleWarning = false;
				toggleBlock = false;
				togglePrediction = false;
			}
			if (GUILayout.Button("Clear")) {
				listSyncPoint.Clear();
			}
			GUILayout.EndHorizontal();
			if (toggleSyncPoint) {
				GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				scrollSyncPoint = GUILayout.BeginScrollView(scrollSyncPoint, false, false, GUILayout.ExpandWidth(true),
					GUILayout.ExpandHeight(true));
				foreach (string fb in listSyncPoint) {
					GUI.color=syncStyle;
					GUILayout.BeginVertical(EditorStyles.helpBox);
					GUILayout.Label(fb);
					GUILayout.EndVertical();
					GUI.color=defaultColor;
					GUILayout.Space(5.0f);
				}
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Prediction Feedback: ");
			GUILayout.FlexibleSpace();
			if (!togglePrediction && GUILayout.Button("Show")) {
				togglePrediction = true;
				toggleWarning = false;
				toggleBlock = false;
				toggleSyncPoint = false;
			}
			if (GUILayout.Button("Clear")) {
				listPrediction.Clear();
			}
			GUILayout.EndHorizontal();
			if (togglePrediction) {
				GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				scrollPrediction = GUILayout.BeginScrollView(scrollPrediction, false, false, GUILayout.ExpandWidth(true),
					GUILayout.ExpandHeight(true));
				foreach (string fb in listPrediction) {
					GUI.color=predictionStyle;
					GUILayout.BeginVertical(EditorStyles.helpBox);
					GUILayout.Label(fb);
					GUILayout.EndVertical();
					GUI.color=defaultColor;
					GUILayout.Space(5.0f);
				}
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}
			GUILayout.EndVertical();
		}
		
		void OnBlockProgress(BlockProgress blockProgress) {
			listBlock.Insert(0, blockProgress.raw);
			Repaint();
		}
	
		void OnPredictionFeedback(PredictionFeedback predictionFeedback) {
			listPrediction.Insert(0, predictionFeedback.raw);
			Repaint();
		}
	
		void OnSyncPointProgress(SyncPointProgress syncPointProgress) {
			listSyncPoint.Insert(0, syncPointProgress.raw);
			Repaint();
		}
	
		void OnWarningFeedback(WarningFeedback warningFeedback) {
			listWarning.Insert(0, warningFeedback.raw);
			Repaint();
		}
	
		void OnSelectionChange() { Register(); Repaint(); }
	
		void OnEnable() { Register(); }
		
		void OnFocus() { Register(); }
	}


}