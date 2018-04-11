using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UMA;
using UMA.PoseTools;

[ExecuteInEditMode]
public class UMA_Pregenerated_ExpressionPlayer : ExpressionPlayer {

	public UMAExpressionSet expressionSet;
	public Transform skeletonRoot;
	protected UMASkeleton skeleton;

	private int jawHash = -1;
	private int neckHash = -1;
	private int headHash = -1;
	Animator animator;
	bool initialized = false;

	void OnRenderObject() {
		if (!initialized) return;
		Quaternion headRotation = new Quaternion();
		Quaternion neckRotation = new Quaternion();
		// Fix for animation systems which require consistent values frame to frame
		if (headHash >= 0 && neckHash >= 0) {
			headRotation = skeleton.GetRotation(headHash);
			neckRotation = skeleton.GetRotation(neckHash);
		}

		// Need to reset bones here if we want Mecanim animation
		expressionSet.RestoreBones(skeleton);
		
		if (headHash >= 0 && neckHash >= 0) {
			if (!overrideMecanimNeck)
				skeleton.SetRotation(neckHash, neckRotation);
			if (!overrideMecanimHead)
				skeleton.SetRotation(headHash, headRotation);
		}
	}

	void LateUpdate() {
		if (!initialized) return;

		if (!Application.isPlaying) SetValues();
	}
	

	void Update() {

		if (!initialized) {
			Initialize();
			return;
		}

		if (!Application.isPlaying) SetValues();
	}

	public void Initialize() {
		blinkDelay = Random.Range(minBlinkDelay, maxBlinkDelay);
		if (skeletonRoot == null) return;
		animator = GetComponent<Animator>();
		
		if (skeleton == null) {
			skeleton = new UMASkeleton(skeletonRoot);
		}

		if (animator != null && expressionSet != null && skeleton != null) {
			Transform jaw = animator.GetBoneTransform(HumanBodyBones.Jaw);
			if (jaw != null)
				jawHash = UMAUtils.StringToHash(jaw.name);

			Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
			if (neck != null)
				neckHash = UMAUtils.StringToHash(neck.name);

			Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
			if (head != null)
				headHash = UMAUtils.StringToHash(head.name);
			initialized = true;
		}
	}

	public void SetValues() {
		if (expressionSet == null) return;
		if (skeleton == null) return;
		if (!initialized) return;

		float[] values = Values;


		MecanimJoint mecanimMask = MecanimJoint.None;
		if (!overrideMecanimNeck)
			mecanimMask |= MecanimJoint.Neck;
		if (!overrideMecanimHead)
			mecanimMask |= MecanimJoint.Head;
		if (!overrideMecanimJaw)
			mecanimMask |= MecanimJoint.Jaw;
		if (!overrideMecanimEyes)
			mecanimMask |= MecanimJoint.Eye;
		if (overrideMecanimJaw)
			skeleton.Restore(jawHash);

		for (int i = 0; i < values.Length; i++) {
			if ((MecanimAlternate[i] & mecanimMask) != MecanimJoint.None) {
				continue;
			}

			float weight = values[i];

			UMABonePose pose = null;
			if (weight > 0) {
				pose = expressionSet.posePairs[i].primary;
			} else {
				weight = -weight;
				pose = expressionSet.posePairs[i].inverse;
			}
			if (pose == null) continue;

			//Debug.Log("SETTING VALUES: "+pose.name);
			pose.ApplyPose(skeleton, weight);
		}
	}
	
}
