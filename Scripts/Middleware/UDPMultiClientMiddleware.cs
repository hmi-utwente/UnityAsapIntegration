﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

#if !UNITY_EDITOR && UNITY_METRO
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;
using System.Threading.Tasks;
#else
using System.Net;
using System.Net.Sockets;
using System.Threading;
#endif


public class UDPMultiClientMiddleware : Middleware {
    public string _asapIP = "127.0.0.1";
    public int _dataPort = 6652;
    
    private bool _listening = false;
    private bool _running = true;

#if !UNITY_EDITOR && UNITY_METRO
    private Task _sendTask;
    private Task _listenTask;
    private Task _heartbeatTask;
    private DatagramSocket udpClient;
#else
    private UdpClient udpClient;
    private Thread _sendTask;
    private Thread _listenTask;
    private Thread _heartbeatTask;
#endif

    private void Start() {
        _listening = false;
#if !UNITY_EDITOR && UNITY_METRO
        _sendTask = Task.Run(() => DataSender());
        _listenTask = Task.Run(() => DataListener());
        _heartbeatTask = Task.Run(() => HeartBeat());
#else
        _sendTask = new Thread(DataSender);
        _listenTask = new Thread(DataListener);
        _heartbeatTask = new Thread(HeartBeat);
        _sendTask.Start();
        _listenTask.Start();
        _heartbeatTask.Start();
#endif
    }

    public void OnApplicationQuit() {
        _running = false;
        if (udpClient != null) udpClient.Close();
        _sendTask.Join(500);
        _listenTask.Join(500);
        _heartbeatTask.Join(500);
    }

#if !UNITY_EDITOR && UNITY_METRO
    private async Task HeartBeat() {
        while (!_listening) { await Task.Delay(100); }
        while (_running) {
            await Task.Delay(3000);
            if (udpClient == null) continue;
            using (var stream = await udpClient.GetOutputStreamAsync(new HostName(_asapIP), _dataPort.ToString())) {
                using (var writer = new DataWriter(stream)) {
                    writer.WriteBytes(Encoding.UTF8.GetBytes("{}"));
                    await writer.StoreAsync();
                    //Debug.Log("Heartbeat to : " + _asapIP + ":" + _dataPort.ToString());
                }
            }

        }
    }
#else
    private void HeartBeat() {
        while (!_listening) { Thread.Sleep(100); }
        while (_running) {
            Thread.Sleep(3000);
            if (udpClient == null) continue;
            udpClient.Send(new byte[] { }, 0, _asapIP, _dataPort);
        }
    }
#endif

#if !UNITY_EDITOR && UNITY_METRO
    private async Task DataListener() {
        udpClient = new DatagramSocket();
        udpClient.MessageReceived += Listener_MessageReceived;
        try {
            var icp = NetworkInformation.GetInternetConnectionProfile();
            HostName IP = NetworkInformation.GetHostNames().SingleOrDefault(hn =>
                       hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                       == icp.NetworkAdapter.NetworkAdapterId);
            Debug.Log("UDPMultiClientMiddleware asking for socket on device: " + IP.ToString());
            await udpClient.BindEndpointAsync(IP, "0");
            _listening = true;
            Debug.Log("UDPMultiClientMiddleware  listening on " + IP.ToString() + ":" + udpClient.Information.LocalPort);
        } catch (Exception e) {
            Debug.Log("DATA LISTENER START EXCEPTION: "+e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }
    }

    private async void Listener_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args) {
        try {
            Stream streamIn = args.GetDataStream().AsStreamForRead();
            StreamReader reader = new StreamReader(streamIn);
            string message = await reader.ReadToEndAsync();
            lock (_receiveQueueLock) {
                _receiveQueue.Enqueue(message);
            }
        } catch (Exception e) {
            Debug.Log("DATA LISTENER EXCEPTION: " + e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }
    }
#else
    private void DataListener() {
        IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 0);
        udpClient = new UdpClient(localEndpoint);
        udpClient.Client.ReceiveBufferSize = 65507;
        udpClient.Client.SendBufferSize = 65507;
        int listenPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        _listening = true;
        Debug.Log("Client listening on " + listenPort);
        
        while (_running) {
            byte[] buffer = udpClient.Receive(ref localEndpoint);
            lock (_receiveQueueLock) {
                _receiveQueue.Enqueue(Encoding.ASCII.GetString(buffer));
            }
        }
        udpClient.Close();
    }
#endif

#if !UNITY_EDITOR && UNITY_METRO
    private async Task DataSender() {
        try {
            while (!_listening) { await Task.Delay(100); }
            while (_running) {
                if (udpClient == null) continue;
                string nextPacket = "";
                lock (_sendQueueLock) {
                    if (_sendQueue.Count > 0) {
                        nextPacket = _sendQueue.Dequeue();
                    }
                }

                if (nextPacket.Length != 0) {
                    using (var stream = await udpClient.GetOutputStreamAsync(new HostName(_asapIP), _dataPort.ToString())) {
                        using (var writer = new DataWriter(stream)) {
                            writer.WriteBytes(Encoding.UTF8.GetBytes(nextPacket));
                            await writer.StoreAsync();
                            //Debug.Log("SENT: " + nextPacket + " to : " + _asapIP + ":" + _dataPort.ToString());
                        }
                    }

                }
                
                await Task.Delay(1);
            }
        } catch (Exception e) {
            Debug.Log("DATA SENDER EXCEPTION: " + e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }
    }
#else
    private void DataSender() {
        try {
            while (!_listening) { Thread.Sleep(100); }
            while (_running) {
                if (udpClient == null) continue;
                string nextPacket = "";
                lock (_sendQueueLock) {
                    if (_sendQueue.Count > 0) {
                        nextPacket = _sendQueue.Dequeue();
                    }
                }

                if (nextPacket.Length != 0) {
                    byte[] sendBytes = Encoding.ASCII.GetBytes(nextPacket);
                    udpClient.Send(sendBytes, sendBytes.Length, _asapIP, _dataPort);

                }

                Thread.Sleep(1);
            }
        } catch (Exception e) {
            Debug.Log("DATA SENDER EXCEPTION: " + e.ToString());
            return;
        }
    }
#endif

}
