﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAsapIntegration.ASAP {

	[RequireComponent(typeof(IMiddleware))]
    [RequireComponent(typeof(BMLFeedback))]
    public class BMLManager : MonoBehaviour, IMiddlewareListener {
	    
	    public ASAPManager asapManager;
	    
		private BMLFeedback feedback;
	    private Middleware middleware;

        void Start() {
            if (asapManager == null) asapManager = FindObjectOfType<ASAPManager>();
            feedback = GetComponent<BMLFeedback>();
            middleware = GetComponent<Middleware>();
            if (middleware != null) middleware.Register(this);
        }

        public void SendBML(string bml) {
            Send(JsonUtility.ToJson(new BMLMiddlewareMessage {
                bml = new MiddlewareContent { content = System.Uri.EscapeDataString(bml) }
            }));
        }

        public void SendBML_noEscape(string escapedBML) {
            Send(JsonUtility.ToJson(new BMLMiddlewareMessage {
                bml = new MiddlewareContent { content = escapedBML }
            }));
        }

        public void Send(string data) {
            middleware.Send(data);
        }

        public void OnMessage(string rawMsg) {
            if (rawMsg.Length == 0) return;
            try {
                FeedbackMiddlewareMessage msg = JsonUtility.FromJson<FeedbackMiddlewareMessage>(rawMsg);
                feedback.HandleFeedback(System.Uri.UnescapeDataString(msg.feedback.content).Replace('+', ' '));
            } catch (System.ArgumentException ae) {
                Debug.Log("Message not valid JSON:\n" + rawMsg + "\n\n" + ae);
            }
        }

        void OnApplicationQuit() {
        }
    }

    [System.Serializable]
    public class FeedbackMiddlewareMessage {
        public MiddlewareContent feedback;
    }

    [System.Serializable]
    public class BMLMiddlewareMessage {
        public MiddlewareContent bml;
    }

    [System.Serializable]
    public class MiddlewareContent {
        public string content;
    }
}