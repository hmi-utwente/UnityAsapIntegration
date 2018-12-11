﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAsapIntegration.ASAP {
    // Creates a rig based on an Agent(Spec) that can be used to pupeteer
    // the actual agent in a convenient way (fk/ik...) and that can produce output
    // that can be used on the ASAP side for Restposes, procedural animation, etc...
    public class ManualAnimationRig : MonoBehaviour {

        public enum ExportMode { ProcAnimationGesture, Keyframes, GestureBindingRestPose, GestureBindingKeyFrames, GestureBindingProcAnim };

        public bool ManualAnimation {
            get {
                if (controlledAgent != null)
                    return controlledAgent.manualAnimation;
                return false;
            }
            set {
                if (controlledAgent != null)
                    controlledAgent.manualAnimation = value;
            }
        }

        public bool skipHumanoidRoot = false;

        public ASAPAgent controlledAgent;
        public Transform vjointRoot;
        public Dictionary<string, Transform> hAnimLUT;

        public void Initialize(ASAPAgent agent, Transform root, Dictionary<string, Transform> lut) {
            controlledAgent = agent;
            vjointRoot = root;
            hAnimLUT = lut;
            if (agent.humanoidRoot.parent != null) {
                transform.localScale = agent.humanoidRoot.parent.localScale;
            }
            // Add controls to each bone...?
        }

        public void ResetToBlankPose() {
            foreach (VJoint joint in controlledAgent.agentSpec.skeleton) {
                if (hAnimLUT.ContainsKey(joint.hAnimName)) {
                    hAnimLUT[joint.hAnimName].localPosition = joint.position;
                    hAnimLUT[joint.hAnimName].localRotation = joint.rotation;
                }
            }
        }

        private void Update() {
            if (ManualAnimation) {
                List<BoneLocalRotation> boneValues = new List<BoneLocalRotation>();
                List<BoneTranslation> boneTranslations = new List<BoneTranslation>();

                int boneIdx = 0;
                foreach (BoneSpec boneSpec in controlledAgent.agentSpec.bones) {
                    if (hAnimLUT.ContainsKey(boneSpec.hAnimName)) {
                        if (skipHumanoidRoot && boneSpec.hAnimName == HAnimMapping.HumanoidRoot && controlledAgent.agentState != null) {
                            boneValues.Add(controlledAgent.agentState.boneValues[boneIdx]);
                            boneTranslations.Add(controlledAgent.agentState.boneTranslations[boneIdx]);
                            continue;
                        }
                        boneValues.Add(new BoneLocalRotation(hAnimLUT[boneSpec.hAnimName]));
                        if (boneIdx < 2) boneTranslations.Add(new BoneTranslation(hAnimLUT[boneSpec.hAnimName]));
                    } else Debug.LogError("Could not find bone from spec in animationRig: " + boneSpec.hAnimName);

                    boneIdx++;
                }

                if (controlledAgent.manualAgentState == null) controlledAgent.manualAgentState = new AgentState();

                controlledAgent.manualAgentState.boneTranslations = boneTranslations.ToArray();
                controlledAgent.manualAgentState.boneValues = boneValues.ToArray();
            }
        }

        public Transform FindDeepChild(Transform aParent, string aName) {
            var result = aParent.Find(aName);
            if (result != null)
                return result;
            foreach(Transform child in aParent) {
                result = FindDeepChild(child, aName);
                if (result != null)
                    return result;
            }
            return null;
        }

        public void Sync_custom(string s) {
            Debug.Log("SyncPoint*: " + s);
        }

        public void Sync_start() { Debug.Log("SyncPoint: start"); }
        public void Sync_ready() { Debug.Log("SyncPoint: ready"); }
        public void Sync_strokeStart() { Debug.Log("SyncPoint: strokeStart"); }
        public void Sync_stroke(){ Debug.Log("SyncPoint: stroke"); }
        public void Sync_strokeEnd() { Debug.Log("SyncPoint: strokeEnd"); }
        public void Sync_relax() { Debug.Log("SyncPoint: relax"); }
        public void Sync_end() { Debug.Log("SyncPoint: end"); }
    }

}