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

	public void SetValues(float[] values) {
		Values = values; // Overrides gui sliders
		SetValues();
	}

	void OnRenderObject() {
		if (expressionSet == null) return;
		if (skeletonRoot == null) return;
		if (skeleton == null) return;
		expressionSet.RestoreBones(skeleton);
	}

	void Update() {
		if (skeletonRoot == null) return;
		if (skeleton == null) {
			skeleton = new UMASkeleton(skeletonRoot);
		}

		if (!Application.isPlaying) SetValues();
	}

	public void SetValues() {
		if (expressionSet == null) return;
		if (skeleton == null) return;

		float[] values = Values;

		MecanimJoint mecanimMask = MecanimJoint.None;
		if (!overrideMecanimNeck)
			mecanimMask |= MecanimJoint.Neck;
		if (!overrideMecanimHead)
			mecanimMask |= MecanimJoint.Head;
		if (!overrideMecanimEyes)
			mecanimMask |= MecanimJoint.Eye;

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
