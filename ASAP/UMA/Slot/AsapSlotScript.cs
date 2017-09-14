using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UMA.PoseTools;
using UMA;

namespace UnityAsapIntegration.ASAP.UMA.Slot {
    public class AsapSlotScript : MonoBehaviour {

        bool isConfigured = false;

        public void OnCharacterBegun(UMAData umaData) {
            isConfigured = false;
        }


        public void OnDnaApplied(UMAData umaData) {}

        public void OnCharacterCompleted(UMAData umaData) {
            UmaAgent asapAgent = umaData.gameObject.GetComponentInChildren<UmaAgent>();
            if (asapAgent == null) {
                asapAgent = umaData.gameObject.AddComponent<UmaAgent>();
                // need HumanoidRoot?/
            }
            if (!isConfigured) {
                isConfigured = true;
                asapAgent.UMAConfigure(umaData);
            }
        }
    }
}

