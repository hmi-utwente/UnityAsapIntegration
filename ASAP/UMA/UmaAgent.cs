using UnityEngine;
using System.Collections.Generic;
using UMA.PoseTools;
using UMA;

namespace UnityAsapIntegration.ASAP.UMA {

    public class UmaAgent : ASAPAgent {

        ExpressionPlayer ep;
        TwistBones twistBones;

        // Hack added for logging gaze...
        public Transform headcenter;
        public Transform leftEye;
        public Transform rightEye;

        private Vector3 rootToHipsOffset;


        private Transform hipShouldBeHere;
        
        public Dictionary<string, string> HAnimMappingDefaults_UMA = new Dictionary<string, string> {
            {"HumanoidRoot", HAnimMapping.HumanoidRoot},
            {"Hips", HAnimMapping.vl5},
            {"LowerBack", HAnimMapping.vt10},
            {"Spine", HAnimMapping.vt6},
            {"Spine1", HAnimMapping.vt1},
            {"Neck", HAnimMapping.vc4},
            {"Head", HAnimMapping.skullbase},
            {"RightForeArmTwist", HAnimMapping.r_forearm_roll},
            {"LeftForeArmTwist", HAnimMapping.l_forearm_roll},
            {"LeftEye", "_LeftEye"}, // Include because child of actual eye bones are below these non-hanim parents
            {"RightEye", "_RightEye"} // ... (and we're otherwise skipping non-hanim bones and their children)
        };


        // The UMA parent of the hip-bone is "Position", and allways grounded to floor.
        // We can't just add a new bone inbetween without breaking other things.
        // Instead, we create this "Virtual Bone" next to the position bone
        // And use it as if it was parent to Hips...
        Transform positionBone;

        float[] expressionControlValues;

        void Awake() {
            foreach (KeyValuePair<string, string> kvp in HAnimMappingDefaults_UMA) {
                HAnimMappingDefaults.Add(kvp.Key, kvp.Value);
            }
        }

        void Start() {}

        void Update() {}

        public void UMAConfigure(UMAData umaData) {
            animator = umaData.animator;
            ep = umaData.GetComponent<ExpressionPlayer>();
            expressionControlValues = new float[ExpressionPlayer.PoseCount];
            poseHandler = new HumanPoseHandler(animator.avatar, transform);

            Transform head = umaData.GetBoneGameObject("Head").transform;
            Transform skulltop = head.Find("skulltop");
            if (skulltop == null) {
                skulltop = new GameObject("skulltop").transform;
                skulltop.parent = head;
                skulltop.localPosition = new Vector3(-0.225f, 0.0f, 0.0f);
                skulltop.localRotation = Quaternion.identity;
            }

            headcenter = head.Find("head_"+id);
            if (headcenter == null) {
                headcenter = new GameObject("head_"+id).transform;
                headcenter.parent = head;
                headcenter.localPosition = new Vector3(-0.115f, 0.0f, 0.0f);
                headcenter.localRotation = Quaternion.identity;
                headcenter.gameObject.AddComponent<WorldObject>();
            }
            
            Transform LeftToeBase = umaData.GetBoneGameObject("LeftToeBase").transform;
            Transform l_forefoot_tip = LeftToeBase.Find("l_forefoot_tip");
            if (l_forefoot_tip == null) {
                l_forefoot_tip = new GameObject("l_forefoot_tip").transform;
                l_forefoot_tip.parent = LeftToeBase;
                l_forefoot_tip.localRotation = Quaternion.identity;
                l_forefoot_tip.localPosition = new Vector3(-0.1f, 0.0f, 0.0f);
            }

            Transform RightToeBase = umaData.GetBoneGameObject("RightToeBase").transform;
            Transform r_forefoot_tip = RightToeBase.Find("r_forefoot_tip");
            if (r_forefoot_tip == null) {
                r_forefoot_tip = new GameObject("r_forefoot_tip").transform;
                r_forefoot_tip.parent = RightToeBase;
                r_forefoot_tip.localRotation = Quaternion.identity;
                r_forefoot_tip.localPosition = new Vector3(-0.1f, 0.0f, 0.0f);
            }

            positionBone = umaData.GetBoneGameObject("Position").transform;
            humanoidRoot = new GameObject("HumanoidRoot").transform;
            rootToHipsOffset = new Vector3(0.0f, 0.025f, 0f);
            humanoidRoot.position = umaData.GetBoneGameObject("Hips").transform.position - rootToHipsOffset;
            humanoidRoot.parent = positionBone;
            humanoidRoot.localRotation = Quaternion.identity;
            
            //hipShouldBeHere = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            //hipShouldBeHere.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            //hipShouldBeHere.position = humanoidRoot.position;
            
            twistBones = GetComponentInChildren<TwistBones>();
            umaData.GetBoneGameObject("Hips").transform.parent = humanoidRoot;
            Initialize();
            umaData.GetBoneGameObject("Hips").transform.parent = positionBone;
        }

        public override void Initialize() {
            Debug.Log("Initializing ASAPAgent_UMA " + id);
            AddMecanimToHAnimDefaults();
            if (retarget != null) {
                GetBoneList(retarget.transform);
            } else if (humanoidRoot != null) {
                GetBoneList(humanoidRoot);
            } else {
                GetBoneList(transform);
            }

            AlignBones();
            AlignCos();
            VJoint[] vJoints = GenerateVJoints();

            List<IFaceTarget> faceTargets = new List<IFaceTarget>();
            faceTargets.Add(new ExpressionPlayerFaceTarget("Surprise",
                new ExpressionControlMapping(new string[] {"midBrowUp_Down", "rightBrowUp_Down", "leftBrowUp_Down", "leftEyeOpen_Close", "rightEyeOpen_Close"},
                    new float[] {1.0f, 1.0f, 1.0f, 0.6f, 0.6f})));
            faceTargets.Add(new ExpressionPlayerFaceTarget("Aggressive",
                new ExpressionControlMapping(new string[] {"midBrowUp_Down", "leftLowerLipUp_Down", "rightLowerLipUp_Down", "leftUpperLipUp_Down", "rightUpperLipUp_Down", "jawOpen_Close"},
                    new float[] {-1.0f, -0.3f, -0.3f, 0.4f, 0.4f, 0.1f})));

            foreach (string target in ExpressionTargetEditor.ExpressionTargets) {
                faceTargets.Add(new ExpressionPlayerFaceTarget(target, ExpressionTargetEditor.LoadMapping(target)));
            }

            agentSpec = new AgentSpec(id, vJoints, faceTargets.ToArray());
            Debug.Log("UMA Agent initialized, id=" + this.agentSpec.agentId + " Bones: " + this.agentSpec.skeleton.Length + " faceControls: " + this.agentSpec.faceTargets.Length);

            FindObjectOfType<ASAPManager>().OnAgentInitialized(this);

            if (debug) {
                CreateManualAnimationRig();
            }
        }

        public override void ApplyAgentState() {
            //Debug.Log("Would Set AGent Sate...");
            for (int b = 0; b < agentState.boneValues.Length; b++) {
                //bones[b].localPosition = agentState.positions[b];
                //bones[b].localRotation = agentState.rotations[b];
                Vector3 newPosition = Vector3.zero;
                if (b < 2) {
                    newPosition = new Vector3(
                        -agentState.boneTranslations[b].t[0], // Minus x value b/c of different COS in ASAP
                         agentState.boneTranslations[b].t[1],
                         agentState.boneTranslations[b].t[2]);
                }
                Quaternion newRotation = new Quaternion(
                    -agentState.boneValues[b].r[0], // Same with order and sign of quat values
                     agentState.boneValues[b].r[1],
                     agentState.boneValues[b].r[2],
                    -agentState.boneValues[b].r[3]);
                if (b == 0) {
                    Quaternion newRot = qInit[b] * RGi[b] * newRotation * RG[b];
                    positionBone.position = new Vector3(newPosition.x, 0.0f, newPosition.z) * positionBone.localScale.x;
                    bones[b].localPosition = new Vector3(-newPosition.y, 0.0f, 0.0f); //* positionBone.localScale.x;
                    positionBone.localRotation = Quaternion.Euler(newRot.eulerAngles.x, 0.0f, 0.0f);
                    bones[b].localRotation =  Quaternion.Inverse(positionBone.localRotation) * newRot;
                } else if (b == 1) {
                    //hipShouldBeHere.parent = bones[b];
                    //hipShouldBeHere.localPosition = newPosition;
                    // Hip Bone
                    
                    bones[b].position = bones[b-1].TransformPoint(Quaternion.Euler(0.0f,0.0f,90.0f)*rootToHipsOffset); // humanoid root is a fake parent
                    Quaternion hipRotation = qInit[b] * RGi[b] * newRotation * RG[b];
                    bones[b].rotation = bones[b-1].rotation * hipRotation; // humanoid root is a fake parent
                } else {
                    bones[b].localRotation = qInit[b] * RGi[b] * newRotation * RG[b];
                }
                //bones[b].localRotation = agentState.rotations[b];
            }

            if (retarget != null) {
                retarget.StorePose();
                HumanPose pose = retarget.GetPose();
                poseHandler.SetHumanPose(ref pose);
            }

            float[] zeroes = new float[ExpressionPlayer.PoseCount];
            expressionControlValues = zeroes;

            for (int f = 0; f < agentState.faceTargetValues.Length; f++) {
                if (Mathf.Approximately(agentState.faceTargetValues[f], 0.0f)) continue;

                if (typeof(ExpressionPlayerFaceTarget) == agentSpec.faceTargetsControls[f].GetType()) {
                    ExpressionPlayerFaceTarget epft = ((ExpressionPlayerFaceTarget) agentSpec.faceTargetsControls[f]);
                    for (int c = 0; c < epft.expressionControlMapping.indexes.Length; c++) {
                        int idx = epft.expressionControlMapping.indexes[c];
                        expressionControlValues[idx] += epft.expressionControlMapping.values[c] * agentState.faceTargetValues[f];
                        // TODO: Average/dampen/....
                    }
                }
            }
            ep.Values = expressionControlValues;

            if (twistBones != null) {
                // Don't want to change the default UMA script to have
                // a Twist() function that is called when we want it...
                // Better to make a clone instead.
                //twistBones.Twist ();
            }
        }
    }

    public class ExpressionControlMapping {

        public float[] values;
        public int[] indexes;

        public ExpressionControlMapping(string[] controls, float[] values) {
            this.values = values;
            this.indexes = new int[values.Length];
            for (int i = 0; i < values.Length; i++) {
                indexes[i] = System.Array.IndexOf(ExpressionPlayer.PoseNames, controls[i]);
            }
        }
    }

    public class ExpressionPlayerFaceTarget : IFaceTarget {
        public ExpressionControlMapping expressionControlMapping;
        public float value;
        public string name;

        public ExpressionPlayerFaceTarget(string name, ExpressionControlMapping expressionControlMapping) {
            this.value = 0.0f;
            this.name = name;
            this.expressionControlMapping = expressionControlMapping;
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