#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Xml;

namespace UnityAsapIntegration.ASAP.Editor {
	
	public class AnimationExporter : EditorWindow {
	
		private int maxOutputLength = 15000;

		private string[] output;

        private ManualAnimationRig[] rigs;
		private string[] labels;

        private ManualAnimationRig animationRig;
        private AnimationClip[] animationClips;
        private AnimationClip blankPose;
        private AnimationClip restPose;

        private int currentClipIndex = 0;
        private Animation animation;
		
		private static AnimationExporter w;
		
		private BMLRequests bmlRequests;
	
		private bool initializedInPlay;

		int selectedRig = 0;
		
		void Update() {
			if (EditorApplication.isPlaying && !initializedInPlay) {
				initializedInPlay = true;
				Register();
			} else if (!EditorApplication.isPlaying) {
				initializedInPlay = false;
			}
		}
		
		[MenuItem("ASAP/Animation Exporter")]
		static void Init() {
			w = GetWindow<AnimationExporter>(false, "Animation Exporter", true);
			w.Show();
			w.Register();
		}
		
		void Register() {
			if (bmlRequests == null) {
				bmlRequests = FindObjectOfType<BMLRequests>();
			}

            rigs = FindObjectsOfType<ManualAnimationRig>();

			List<string> names = new List<string>();
			for (int r = 0; r < rigs.Length; r++) {
				names.Add(rigs[r].name);
			}
			if (rigs.Length >= selectedRig) selectedRig = 0;
			labels = names.ToArray();
			if (rigs.Length > 0) {
				if (animationRig == null) {
					animationRig = rigs[selectedRig];
				}
				Populate();
			}
		}

		void Populate() { // animationRig needs to be set before doing this...
            List<AnimationClip> _animationClips = new List<AnimationClip>();
            string[] animationLocations = {"Assets/UnityAsapIntegration/Resources/Animations"};
            string[] clipGUIDs = AssetDatabase.FindAssets("t:AnimationClip", animationLocations);

            foreach (string guid in clipGUIDs) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
                _animationClips.Add(clip);
            }

            animationClips = _animationClips.ToArray();

            animation = animationRig.vjointRoot.parent.gameObject.GetComponent<Animation>();
            if (animation == null) {
                animation = animationRig.vjointRoot.parent.gameObject.AddComponent<Animation>();
            }
		}
		
		private void OnGUI() {
			if (rigs.Length < 1) return;
			
			GUIStyle popStyle = new GUIStyle(EditorStyles.popup);
			popStyle.margin = new RectOffset(5, 5, 5, 5);

			int curr = selectedRig;
			selectedRig = EditorGUILayout.Popup(selectedRig, labels, popStyle);
			if (curr != selectedRig) Populate();

            if (animationRig == null || animation == null) return;
            animationRig.ManualAnimation = GUILayout.Toggle(animationRig.ManualAnimation, "ManualAnimation", GUILayout.Width(130));
            animationRig.skipHumanoidRoot = GUILayout.Toggle(animationRig.skipHumanoidRoot, "SkipHumanoidRoot", GUILayout.Width(130));
            int newCurrentClipIndex = EditorGUILayout.Popup(currentClipIndex, (from clip in animationClips select clip.name).ToArray());
            if (newCurrentClipIndex != currentClipIndex || animation.clip == null) {
                foreach (AnimationClip clip in animationClips) {
                    if (animation.GetClip(clip.name) != null) { 
                        animation.RemoveClip(clip);
                    }
                }

                animation.AddClip(animationClips[newCurrentClipIndex], animationClips[newCurrentClipIndex].name);
                animation.clip = animationClips[newCurrentClipIndex];
                Selection.activeGameObject = animationRig.gameObject;
                EditorApplication.ExecuteMenuItem("Window/Animation");
                //if (blankPose != null) blankPose.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
                //if (restPose != null) restPose.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
                animationRig.ResetToBlankPose();
                if (animation.clip != null) animation.clip.SampleAnimation(animationRig.vjointRoot.parent.gameObject, 0.0f);
            }

            currentClipIndex = newCurrentClipIndex;

            // GUILayout.Label(AnimationUtility.);
            List<string> results = new List<string>();

            if (GUILayout.Button("Export Proc. Anim (BML/Play)")) {
                string bml = ExportAnimation(animationRig, animationClips[currentClipIndex], 30, ManualAnimationRig.ExportMode.ProcAnimationGesture);
				results.Add(bml);
			}
            
            if (GUILayout.Button("Export Proc. Anim (Binding)")) {
                string animFile = ExportAnimation(animationRig, animationClips[currentClipIndex], 30, ManualAnimationRig.ExportMode.GestureBindingProcAnim);
				results.Add("TODO: PUT SAMPLE GESTUREBINDING ENTRY HERE");
				results.Add("TODO: PUT SAMPLE BML HERE");
				results.Add(animFile);
			}
            
            if (GUILayout.Button("Export Rest Pose (Binding)")) {
                string restPoseFile = ExportAnimation(animationRig, animationClips[currentClipIndex], 30, ManualAnimationRig.ExportMode.GestureBindingRestPose);
				results.Add("TODO: PUT SAMPLE GESTUREBINDING ENTRY HERE");
				results.Add(restPoseFile);
			}
			
			if (results.Count > 0) {
				output = results.ToArray();
			}


			GUI.skin.textArea.wordWrap = false;

			for (int i = 0; i < output.Length; i++) {
				GUILayout.BeginHorizontal();
				string render = output[i];
				if (render.Length > maxOutputLength) {
					string msg = "\n\n{{TRUNCATED HERE: TOO LONG FOR UNITY}}";
					render = render.Substring(0, maxOutputLength-msg.Length)+msg;
				}
				GUILayout.TextArea(render, GUILayout.ExpandHeight(true));
				GUILayout.EndHorizontal();
			}
		}
		
		void OnSelectionChange() { Register(); Repaint(); }
	
		void OnEnable() { Register(); }
		
		void OnFocus() { Register(); }


        // Export animation... current clip?
        public static string ExportAnimation(ManualAnimationRig animationRig, AnimationClip clip, float frameRate, ManualAnimationRig.ExportMode mode) {
            if (animationRig == null) return null;

            Dictionary<string, bool> animatedBones = new Dictionary<string, bool>();
            Dictionary<string, float> syncPoints = new Dictionary<string, float>();

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings (clip)) {
                //AnimationCurve curve = AnimationUtility.GetEditorCurve (clip, binding);

                string[] pathElems = binding.path.Split('/');
                string hAnimName = pathElems[pathElems.Length - 1];
                if (binding.propertyName.StartsWith("m_LocalRotation")) {
                    if (!animatedBones.ContainsKey(hAnimName))
                        animatedBones.Add(hAnimName, false);
                }

                if (binding.propertyName.StartsWith("m_LocalPosition")) {
                    if (!animatedBones.ContainsKey(hAnimName)) {
                        animatedBones.Add(hAnimName, true);
                    }
                    else animatedBones[hAnimName] = true;
                }
            }

            foreach (AnimationEvent ae in AnimationUtility.GetAnimationEvents(clip)) {
                if (ae.functionName.StartsWith("Sync_")) {
                    string syncType = ae.functionName.Substring(5);
                    if (syncType == "custom") syncType = ae.stringParameter;
                    syncPoints.Add(syncType, ae.time/clip.length);
                    Debug.Log(ae.functionName+" "+ syncType + " "+ae.time);
                }
            }

            string parts = "";
            List<Transform> partReferences = new List<Transform>();

            // Get a nice ordered list of the bones:
            foreach (BoneSpec boneSpec in animationRig.controlledAgent.agentSpec.bones) {
                foreach (KeyValuePair<string,bool> animatedBone in animatedBones) {
                    if (animatedBone.Key == boneSpec.hAnimName) {
                        parts += boneSpec.hAnimName + " ";
                        Transform boneObject = animationRig.FindDeepChild(animationRig.vjointRoot.parent, boneSpec.hAnimName);
                        partReferences.Add(boneObject);
                        break;
                    }
                }
            }
            parts = parts.Trim();

            string encoding = "R";
            string rotationEncoding = "quaternions";

            // if root has translation: we use T1R
            if (animatedBones.ContainsKey("HumanoidRoot") && animatedBones["HumanoidRoot"]) {
                encoding = "T1R";
                // Could also use "TR" if there is a non-root bone with translation...
                // but we don't support those non-root translations in animations atm anyway...
            }

            List<float> times = new List<float>();
            List<float[]> frames = new List<float[]>();

            float delta = 1 / frameRate;
            for (int frame = 0; frame < Math.Max(1, clip.length * frameRate); frame++) {
                float t = delta * frame;
                clip.SampleAnimation(animationRig.vjointRoot.parent.gameObject, t);
                //yield return new WaitForSeconds(delta);
                times.Add(t);
                
                List<float> elems = new List<float>();
                foreach (Transform partReference in partReferences) {
                    if (encoding == "TR" || (encoding == "T1R" && partReference.name == "HumanoidRoot")) {
                        elems.AddRange(ExtractAsapVectorPosition(partReference));
                        elems.AddRange(ExtractAsapQuaternionRotation(partReference));
                    } else if (encoding == "R" || encoding == "T1R") {
                        elems.AddRange(ExtractAsapQuaternionRotation(partReference));
                    }
                }
                
                frames.Add(elems.ToArray());
            }
            Debug.Log("Exporting gesture " + clip.name + ". Duration: " + clip.length + " (" + frames.Count + " frames total)");
            return WriteXML(animationRig.controlledAgent.id, clip, parts, rotationEncoding, encoding, times, frames, syncPoints, mode);
        }

        // returns a float[4] quaternion ready for use in ASAP...
        private static float[] ExtractAsapQuaternionRotation(Transform t) {
            return new [] {
                -t.localRotation.w,
                -t.localRotation.x,
                 t.localRotation.y,
                 t.localRotation.z
            };
        }

        private static float[] ExtractAsapVectorPosition(Transform t) {
            return new [] {
                -t.position.x,
                 t.position.y,
                 t.position.z
            };
        }

        // TODO: differnt types of output (restpose, keyframe, handshape, ...)
        private static string WriteXML(string characterId, AnimationClip clip, string parts, string rotationEncoding, string encoding, List<float> times, List<float[]> frames, Dictionary<string,float> syncPoints, ManualAnimationRig.ExportMode mode) {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.OmitXmlDeclaration = true;


            using (XmlWriter w = XmlWriter.Create(ms, settings)) {
                w.WriteStartDocument();

                if (mode == ManualAnimationRig.ExportMode.Keyframes || mode == ManualAnimationRig.ExportMode.ProcAnimationGesture) {
                    w.WriteStartElement("bml", "http://www.bml-initiative.org/bml/bml-1.0"); // <bml ...>
                    w.WriteAttributeString("id", "exportedBml1");
                    w.WriteAttributeString("characterId", characterId);
                    w.WriteAttributeString("xmlns", "bmlt", null, "http://hmi.ewi.utwente.nl/bmlt");
                }

                if (mode == ManualAnimationRig.ExportMode.Keyframes) {
                    //w.WriteStartElement("bmlt:keyframe"); // <bmlt:keyframe ...>
                    w.WriteStartElement("bmlt", "keyframe", null); // <bmlt:keyframe ...>
                    w.WriteAttributeString("id", "keyframes1");

                    w.WriteStartElement("SkeletonInterpolator"); // <SkeletonInterpolator ...>
                    w.WriteAttributeString("encoding", encoding); // ... xmlns=""
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("parts", parts);
                    for (int fIdx = 0; fIdx < frames.Count; fIdx++) {
                        w.WriteString("\n      ");
                        w.WriteString(times.ElementAt(fIdx).ToString("0.0##")+" ");
                        w.WriteString(string.Join(" ", frames.ElementAt(fIdx).Select(f => f.ToString("0.0##")).ToArray()));
                    }
                    w.WriteString("\n    ");
                    w.WriteEndElement(); // </SkeletonInterpolator>
                    w.WriteEndElement(); // </bmlt:keyframe>
                } else if (mode == ManualAnimationRig.ExportMode.ProcAnimationGesture) {
                   // w.WriteStartElement("bmlt:procanimationgesture"); // <bmlt:procanimationgesture ...>
                    w.WriteStartElement("bmlt", "procanimationgesture", null); // <bmlt:procanimationgesture ...>
                    w.WriteAttributeString("id", "procgesture1");

                    w.WriteStartElement("ProcAnimation");  // < ProcAnimation ...>
                    w.WriteAttributeString("prefDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("minDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("maxDuration", clip.length.ToString("0.0##"));
                    w.WriteStartElement("SkeletonInterpolator"); // <SkeletonInterpolator ...>
                    w.WriteAttributeString("encoding", encoding); // ...
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("parts", parts);

                    for (int fIdx = 0; fIdx < frames.Count; fIdx++) {
                        w.WriteString("\n      ");
                        w.WriteString(times.ElementAt(fIdx).ToString("0.0##")+" ");
                        w.WriteString(string.Join(" ", frames.ElementAt(fIdx).Select(f => f.ToString("0.0##")).ToArray()));
                    }
                    w.WriteString("\n    ");
                    w.WriteEndElement(); // </SkeletonInterpolator>

                    foreach (KeyValuePair<string,float> syncPoint in syncPoints.OrderBy(pair => pair.Value)) {
                        w.WriteStartElement("KeyPosition");
                        w.WriteAttributeString("id", syncPoint.Key);
                        w.WriteAttributeString("weight", "1");
                        w.WriteAttributeString("time", syncPoint.Value.ToString("0.0##"));
                        w.WriteEndElement();
                    }

                    w.WriteEndElement(); // </ProcAnimation>
                    w.WriteEndElement(); // </bmlt:procanimationgesture>
                } else if (mode == ManualAnimationRig.ExportMode.GestureBindingRestPose) {
                    w.WriteStartElement("SkeletonPose"); // <SkeletonPose ...>
                    w.WriteAttributeString("encoding", encoding); // ... encoding=""
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("parts", parts);
                    w.WriteString("\n    ");
                    w.WriteString(string.Join(" ", frames.ElementAt(0).Select(f => f.ToString("0.0##")).ToArray()));
                    w.WriteString("\n");
                    w.WriteEndElement(); // </SkeletonPose>
                } else if (mode == ManualAnimationRig.ExportMode.GestureBindingKeyFrames) {
                    
                } else if (mode == ManualAnimationRig.ExportMode.GestureBindingProcAnim) {
                    w.WriteStartElement("ProcAnimation");  // < ProcAnimation ...>
                    w.WriteAttributeString("prefDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("minDuration", clip.length.ToString("0.0##"));
                    w.WriteAttributeString("maxDuration", clip.length.ToString("0.0##"));
                    w.WriteStartElement("SkeletonInterpolator"); // <SkeletonInterpolator ...>
                    w.WriteAttributeString("encoding", encoding); // ... 
                    w.WriteAttributeString("rotationEncoding", rotationEncoding);
                    w.WriteAttributeString("parts", parts);

                    for (int fIdx = 0; fIdx < frames.Count; fIdx++) {
                        w.WriteString("\n      ");
                        w.WriteString(times.ElementAt(fIdx).ToString("0.0##")+" ");
                        w.WriteString(string.Join(" ", frames.ElementAt(fIdx).Select(f => f.ToString("0.0##")).ToArray()));
                    }
                    w.WriteString("\n    ");
                    w.WriteEndElement(); // </SkeletonInterpolator>

                    foreach (KeyValuePair<string,float> syncPoint in syncPoints.OrderBy(pair => pair.Value)) {
                        w.WriteStartElement("KeyPosition");
                        w.WriteAttributeString("id", syncPoint.Key);
                        w.WriteAttributeString("weight", "1");
                        w.WriteAttributeString("time", syncPoint.Value.ToString("0.0##"));
                        w.WriteEndElement();
                    }

                    w.WriteEndElement(); // </ProcAnimation>
                }

                if (mode == ManualAnimationRig.ExportMode.Keyframes || mode == ManualAnimationRig.ExportMode.ProcAnimationGesture) {
                    w.WriteEndElement(); // </bml>
                }
                w.WriteEndDocument();
            }

            StreamReader sr = new StreamReader(ms);
            ms.Seek(0, SeekOrigin.Begin);
            string xml = sr.ReadToEnd();
            Debug.Log(xml);
            
            if (mode == ManualAnimationRig.ExportMode.Keyframes || mode == ManualAnimationRig.ExportMode.ProcAnimationGesture) {
                Transform.FindObjectOfType<BMLRequests>().SendBML(xml);
            }

            sr.Dispose();
            return xml;
        }

		
	}


}
#endif