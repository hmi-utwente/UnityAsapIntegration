﻿#if !UNITY_EDITOR && UNITY_METRO
#else
using Apache.NMS;
using Apache.NMS.Util;
using Apache.NMS.Stomp;
#endif

using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class STOMPMiddleware : Middleware {

#if !UNITY_EDITOR && UNITY_METRO
    // TODO: UWP implementation of STOMP
#else
    public string topicRead;
    public string topicWrite;
    public string address;
    public string user;
    public string pass;

    //bool durable;
    bool onlyLast;
    bool networkOpen;
    ISession session;
    IConnectionFactory factory;
    IConnection connection;
    Thread apolloWriterThread;
    Thread apolloReaderThread;
    AutoResetEvent semaphore = new AutoResetEvent(false);
    System.TimeSpan receiveTimeout = System.TimeSpan.FromMilliseconds(250);
#endif

    public void Awake() {
#if !UNITY_EDITOR && UNITY_METRO
        throw new System.NotImplementedException();
#else
        STOMPStart();
#endif
    }

    public void OnApplicationQuit() {
#if !UNITY_EDITOR && UNITY_METRO
        throw new System.NotImplementedException();
#else
        networkOpen = false;
        if (apolloWriterThread != null && !apolloWriterThread.Join(500)) {
            Debug.LogWarning("Could not close apolloWriterThread");
            apolloWriterThread.Abort();
        }

        if (apolloReaderThread != null && !apolloReaderThread.Join(500)) {
            Debug.LogWarning("Could not close apolloReaderThread");
            apolloWriterThread.Abort();
        }
        if (connection != null) connection.Close();
#endif
    }

    void STOMPStart() {
#if !UNITY_EDITOR && UNITY_METRO
        throw new System.NotImplementedException();
#else
        try {
            System.Uri connecturi = new System.Uri("stomp:" + address);
            Debug.Log("Apollo connecting to " + connecturi + " (" + address + ")");
            factory = new NMSConnectionFactory(connecturi);
            // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
            connection = factory.CreateConnection(user, pass);
            session = connection.CreateSession();
            networkOpen = true;
            connection.Start();
        } catch (System.Exception e) {
            Debug.Log("Apollo Start Exception " + e);
        }

        apolloWriterThread = new Thread(new ThreadStart(ApolloWriter));
        apolloWriterThread.Start();

        apolloReaderThread = new Thread(new ThreadStart(ApolloReader));
        apolloReaderThread.Start();
#endif
    }

    void ApolloWriter() {
#if !UNITY_EDITOR && UNITY_METRO
        throw new System.NotImplementedException();
#else
        try {
            IDestination destination_Write = SessionUtil.GetDestination(session, topicWrite);
            IMessageProducer producer = session.CreateProducer(destination_Write);
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            producer.RequestTimeout = receiveTimeout;
            while (networkOpen) {
               string msg = "";
               lock (_sendQueueLock) {
                    if (_sendQueue.Count > 0) {
                        msg = _sendQueue.Dequeue();
                    }
                }

                if (msg != null && msg.Length > 0)
                        producer.Send(session.CreateTextMessage(msg));
                }
        } catch (System.Exception e) {
            Debug.Log("ApolloWriter Exception " + e);
        }
#endif
    }

    void ApolloReader() {
#if !UNITY_EDITOR && UNITY_METRO
        throw new System.NotImplementedException();
#else
        try {
            //IDestination destination_Read = SessionUtil.GetDestination(session, topicRead);
            //destination_Read
            ITopic destination_Read = SessionUtil.GetTopic(session, topicRead);
            IMessageConsumer consumer = session.CreateConsumer(destination_Read);
            Debug.Log("Apollo subscribing to " + destination_Read);
            /*
            IMessageConsumer consumer;
            if (durable) {
                consumer = session.CreateConsumer(destination_Read);
            } else {
                consumer = session.CreateDurableConsumer(destination_Read, "test", null, false);
            }*/
            consumer.Listener += new MessageListener(OnSTOMPMessage);
            while (networkOpen) {
                semaphore.WaitOne((int)receiveTimeout.TotalMilliseconds, true);
            }
        } catch (System.Exception e) {
            Debug.Log("ApolloReader Exception " + e);
        }
#endif
    }

#if !UNITY_EDITOR && UNITY_METRO
#else
    void OnSTOMPMessage(IMessage receivedMsg) {
        lock (_receiveQueueLock) {
            _receiveQueue.Enqueue((receivedMsg as ITextMessage).Text);
        }
        semaphore.Set();
    }
#endif

}