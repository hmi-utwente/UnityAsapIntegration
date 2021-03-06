﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace UnityAsapIntegration.ASAP {
    public class ASAPAgent : MonoBehaviour, IASAPAgent {
        public string id;
        public bool debug;
        public AgentSpec agentSpec;
        public AgentState agentState;
        public AgentState manualAgentState;
        public Transform canonicalSkeletonRefRoot;

        public bool manualAnimation;
        public MecanimRetargetingSource retarget;

        // Will try to map according to HAnimMapping.MecanimToHAnimMap
        // Unless bone is already mapped in this dict...
        // TODO: serialize & expose to inspector.
        public Dictionary<string, string> HAnimMappingDefaults = new Dictionary<string, string> {};

        public Transform humanoidRoot;

        [HideInInspector] public Animator animator;
        [HideInInspector] protected HumanPoseHandler poseHandler;

        protected Transform[] bones;

        protected Quaternion[] qInit;
        // = zeroTestPosesLocal[t][b]; // qInit: local rotation in tPose (direction aligned with src tPose)

        protected Quaternion[] RG;
        // = zeroTestPosesGlobal[t][b];  // RG: global rotation in tPose (direction aligned with src tPose)

        protected Quaternion[] RGi; // = Quaternion.Inverse(zeroTestPosesGlobal[t][b]);


        void Start() {
            animator = GetComponent<Animator>();
            if (animator != null) {
                poseHandler = new HumanPoseHandler(animator.avatar, transform);
            }
            Initialize();
        }

        void Update() {}

        protected void AddMecanimToHAnimDefaults() {
            if (animator == null) return;
            HumanBodyBones[] values = HumanBodyBones.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];
            foreach (HumanBodyBones b in values) {
                Transform t = animator.GetBoneTransform(b);
                if (t != null && !HAnimMappingDefaults.ContainsKey(t.name) &&
                    HAnimMapping.MecanimToHAnimMap.ContainsKey(b)) {
                    HAnimMappingDefaults.Add(t.name, HAnimMapping.MecanimToHAnimMap[b]);
                }
            }
        }

        // Required: should be in t-pose/aligned with HAnim bones!!!
        protected void GetBoneList(Transform root) {
            List<Transform> transforms = new List<Transform>();
            AppendChildren(root, transforms);
            bones = transforms.ToArray();
        }

        protected void AlignCos() {
            qInit = new Quaternion[bones.Length];
            RG = new Quaternion[bones.Length];
            RGi = new Quaternion[bones.Length];
            for (int t = 0; t < bones.Length; t++) {
                qInit[t] = bones[t].localRotation;
                RG[t] = bones[t].rotation;
                RGi[t] = Quaternion.Inverse(RG[t]);
            }
        }

        protected VJoint[] GenerateVJoints() {
            VJoint[] res = new VJoint[bones.Length];
            Dictionary<string, VJoint> lut = new Dictionary<string, VJoint>();

            for (int b = 0; b < bones.Length; b++) {
                VJoint parent = null;
                if (b > 0) {
                    parent = lut[bones[b].parent.name];
                    if (GetHAnimName(parent.id) == "") {
                        Debug.Log(bones[b].name + " is not child of HAnim bone (" + parent.id + ").");
                    }
                }

                // Default HAnim skeleton has rotation that aligns with global Zero COS
                Quaternion rot = Quaternion.identity;


                Vector3 position = bones[b].position;
                if (b > 0) {
                    // For ASAP IK to work, we need to make sure that the local Position we pass is
                    //  relative to a parent bone with a COS that is also aligned with global Zero COS
                    //  ...so ".localPosition" is not enough.
                    position = Quaternion.Inverse(Quaternion.identity) * (bones[b].position - bones[b].parent.position);
                    // Note: Of course the inverse of the identity is the identity, however, just in case
                    //  we ever use a different global Zero COS, we might have a non-identity rotation to
                    //  throw in here
                }
                res[b] = new VJoint(bones[b].name, GetHAnimName(bones[b].name), position, rot, parent);
                lut.Add(bones[b].name, res[b]);
            }
            return res;
        }

        public virtual void AppendChildren(Transform root, List<Transform> transforms) {
            if (GetHAnimName(root.name) != "") {
                transforms.Add(root);
                foreach (Transform child in root) {
                    AppendChildren(child, transforms);
                }
            }
        }

        public virtual void ApplyAgentState() {
            
            AgentState _agentState;
            if (manualAnimation) _agentState = manualAgentState;
            else _agentState = agentState;
            
            for (int b = 0; b < _agentState.boneValues.Length; b++) {
                Quaternion newRotation = new Quaternion(
                    -_agentState.boneValues[b].r[0], // Same with order and sign of quat values
                    _agentState.boneValues[b].r[1],
                    _agentState.boneValues[b].r[2],
                    -_agentState.boneValues[b].r[3]);
                bones[b].localRotation = qInit[b] * RGi[b] * newRotation * RG[b];
            }

            if (retarget != null && poseHandler != null) {
                retarget.StorePose();
                HumanPose pose = retarget.GetPose();
                poseHandler.SetHumanPose(ref pose);
            }
        }

        public virtual string GetHAnimName(string boneName) {
            if (HAnimMappingDefaults != null && HAnimMappingDefaults.ContainsKey(boneName)) {
                return HAnimMappingDefaults[boneName];
            } else if (System.Array.IndexOf(HAnimMapping.HAnimBones, boneName) > -1) {
                return boneName;
            } else {
                return "";
            }
        }

        // Returns a transform object of the actual character model's bone
        // corresponding to HAnim name
        public virtual Transform GetBoneByHAnimName(string hAnimName) {
            if (agentSpec != null && agentSpec.bones != null && bones != null
                && bones.Length == agentSpec.bones.Length) {
                for (int i=0; i < agentSpec.bones.Length; i++) {
                    if (agentSpec.bones[i].hAnimName == hAnimName) {
                        return bones[i];
                    }
                }
            }
            return null;
        }

        public Transform FindChildRecursive(Transform root, string name) {
            if (root.name == name) return root;

            foreach (Transform child in root) {
                Transform deepRes = FindChildRecursive(child, name);
                if (deepRes != null) return deepRes;
            }

            return null;
        }

        public void AlignBones() {
            if (canonicalSkeletonRefRoot == null) {
                Debug.LogError("Cannot align bones of " + id + ", canonicalSkeletonRefRoot not Defined");
                return;
            }

            for (int b = 0; b < bones.Length; b++) {
                if (b == 0) continue;
                Transform refBone = FindChildRecursive(canonicalSkeletonRefRoot, GetHAnimName(bones[b].name));
                Transform refParentBone = FindChildRecursive(canonicalSkeletonRefRoot,
                    GetHAnimName(bones[b].parent.name));
                if (refBone == null || refParentBone == null || refParentBone.childCount > 1) continue;
                // Debug.Log (" Aligning: "+bones[b].parent.name+" to "+bones[b].name+" as "+refParentBone.name+" to "+refBone.name);
                Vector3 srcDirection = bones[b].parent.position - bones[b].position;
                Vector3 targetDirection = refParentBone.position - refBone.position;

                // Those odd feet... the HAnim has an extremely odd/high ankle, causing ankles and toes to
                // come out horribly odd with this procedure
                if (refParentBone.name == "r_ankle" || refParentBone.name == "l_ankle")
                {
                    srcDirection.y = targetDirection.y;
                }
                Quaternion alignRot = Quaternion.FromToRotation(srcDirection, targetDirection);
                bones[b].parent.rotation = alignRot * bones[b].parent.rotation;
            }
        }

        public virtual void Initialize() {
            Debug.Log("Initializing ASAPAgent " + id);
            AddMecanimToHAnimDefaults();
            if (retarget != null) {
                GetBoneList(retarget.transform);
            } else if (humanoidRoot != null) {
                GetBoneList(humanoidRoot);
            } else {
                GetBoneList(transform);
            }

            AlignCos();
            AlignBones();

            VJoint[] vJoints = GenerateVJoints();
            IFaceTarget[] faceTargets = new IFaceTarget[0] {};
            agentSpec = new AgentSpec(id, vJoints, faceTargets);
            Debug.Log("Agent initialized, id=" + this.agentSpec.agentId + " Bones: " + this.agentSpec.skeleton.Length +
                      " faceControls: " + this.agentSpec.faceTargets.Length);
            FindObjectOfType<ASAPManager>().OnAgentInitialized(this);
            if (debug) {
                CreateManualAnimationRig();
            }
        }

        public ManualAnimationRig CreateManualAnimationRig() {
            Dictionary<string, Transform> hAnimLUT = DebugVJointSkeleton(agentSpec.skeleton);
            if (!hAnimLUT.ContainsKey("HumanoidRoot")) {
                Debug.LogWarning("Cannot create ManualAnimationRig because Skeleton does not have HumanoidRoot");
            }

            Transform humanoidRoot = hAnimLUT["HumanoidRoot"];
            Transform rigParent = humanoidRoot.parent;
            rigParent.name = "ManualAnimationRig_" + id;
            rigParent.transform.rotation = transform.rotation;
            rigParent.transform.position = transform.position;// + transform.right * 0.5f;
            ManualAnimationRig rig = rigParent.gameObject.AddComponent<ManualAnimationRig>();
            rig.Initialize(this, humanoidRoot, hAnimLUT);
            return rig;
        }

        // Names should be HANIM!!
        public Dictionary<string, Transform> DebugVJointSkeleton(VJoint[] skeleton) {
            GameObject root = new GameObject("VJoints_" + id);
            Dictionary<string, Transform> lut = new Dictionary<string, Transform>();
            Dictionary<string, Transform> hAnimLUT = new Dictionary<string, Transform>();
            foreach (VJoint joint in skeleton) {
                GameObject bone = new GameObject(joint.hAnimName);
                if (!hAnimLUT.ContainsKey(joint.hAnimName))
                    hAnimLUT.Add(joint.hAnimName, bone.transform);
                else Debug.LogWarning("Already in LUT: " + joint.id + " as " + joint.hAnimName);
                if (joint.parent == null) {
                    bone.transform.parent = root.transform;
                } else {
                    bone.transform.parent = lut[joint.parent.id];
                }
                bone.transform.localPosition = joint.position;
                bone.transform.localRotation = joint.rotation;
                lut.Add(joint.id, bone.transform);
            }

            DebugSkeleton res = root.AddComponent<DebugSkeleton>();
            res.color = new Color(255, 255, 0);
            return hAnimLUT;
        }
    }
}