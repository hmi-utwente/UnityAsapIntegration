using UnityEngine;
using System.Collections.Generic;
using UMA.PoseTools;
using UMA;
using System.Collections;

namespace UnityAsapIntegration.ASAP.Toon {

    public class ToonAgent : ASAPAgent {
		
		public enum FaceTargetType { Expression, Mouth, Other };
		public ToonFaceConfiguration[] faceTargetConfig;


        void Start() {
			Initialize ();
		}

        void Update() {}


        public override void Initialize() {
            Debug.Log("Initializing ASAPAgent_ToonFace " + id);

			VJoint[] vJoints = new VJoint[0];

            List<IFaceTarget> faceTargets = new List<IFaceTarget>();

			foreach (ToonFaceConfiguration ftc in faceTargetConfig) {
				faceTargets.Add(new ToonFaceTarget(ftc.name, ftc));
            }


            agentSpec = new AgentSpec(id, vJoints, faceTargets.ToArray());
            Debug.Log("UMA Agent initialized, id=" + this.agentSpec.agentId + " Bones: " + this.agentSpec.skeleton.Length + " faceControls: " + this.agentSpec.faceTargets.Length);

            FindObjectOfType<ASAPManager>().OnAgentInitialized(this);
        }

        public override void ApplyAgentState() {
            // Expression Only
			if (agentState.faceTargetValues.Length < 1) return;

			int maxId = 0;
			float maxVal = float.NegativeInfinity;

			// Todo: sort by face target type?
            for (int f = 0; f < agentState.faceTargetValues.Length; f++) {
				if (typeof(ToonFaceTarget) == agentSpec.faceTargetsControls[maxId].GetType()) {
					ToonFaceTarget tft = ((ToonFaceTarget) agentSpec.faceTargetsControls[maxId]);
					tft.UnApply ();
				}
				if (maxVal < agentState.faceTargetValues [f]) {
					maxVal = agentState.faceTargetValues [f];
					maxId = f;
				}
            }

			if (typeof(ToonFaceTarget) == agentSpec.faceTargetsControls[maxId].GetType()) {
				ToonFaceTarget tft = ((ToonFaceTarget) agentSpec.faceTargetsControls[maxId]);
				tft.Apply ();
			}

        }
    }

	[System.Serializable]
	public struct ToonFaceConfiguration {

		public string name;
		public Renderer[] slots;
		public Texture[] textures;
		public ToonAgent.FaceTargetType type;

    }

    public class ToonFaceTarget : IFaceTarget {
		public ToonFaceConfiguration faceConfiguration;
        public float value;
        public string name;

		public ToonFaceTarget(string name, ToonFaceConfiguration faceConfiguration) {
            this.value = 0.0f;
            this.name = name;
			this.faceConfiguration = faceConfiguration;
        }

		public void UnApply() {
			for (int i = 0; i < faceConfiguration.slots.Length; i++) {
				faceConfiguration.slots [i].material.mainTexture = null;
			}
		}

		public void Apply() {
			for (int i = 0; i < faceConfiguration.slots.Length; i++) {
				faceConfiguration.slots [i].material.mainTexture = faceConfiguration.textures [i];
			}
		}

        public void SetValue(float v) {
            value = v;
        }

        public float GetValue() {
            return value;
        }

        public string GetName() {
            return name;
        }
    }

}

//faceTargetsControls.Add(new FaceTarget("Surprise", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "rightBrowUp_Down", "leftBrowUp_Down", "leftEyeOpen_Close", "rightEyeOpen_Close" }, new float[] { 1.0f, 1.0f, 1.0f, 0.6f, 0.6f })));
//faceTargetsControls.Add(new FaceTarget("Aggressive", new ExpressionControlMapping(new string[] { "midBrowUp_Down", "leftLowerLipUp_Down", "rightLowerLipUp_Down", "leftUpperLipUp_Down", "rightUpperLipUp_Down", "jawOpen_Close" }, new float[] { -1.0f, -0.3f, -0.3f, 0.4f, 0.4f, 0.1f })));