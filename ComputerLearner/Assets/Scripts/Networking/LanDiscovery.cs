/**
 * Author: TeamCompass
 * Description: UDP LAN Host Discovery & NAT/Firewall-friendly UDP Envelope service supporting
 *              Teacher Host multi-interface broadcasting (subnet broadcast + global broadcast +
 *              ARP neighbor unicast hole-punching + 127.0.0.1 loopback for single-PC testing),
 *              Student Client automatic host discovery with immediate discovery ACK, and
 *              bidirectional UDP session envelope relay over the discovery port.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ComputerLearning
{
    public class LanDiscovery : MonoBehaviour
    {
        private static LanDiscovery instance;

        public static LanDiscovery Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<LanDiscovery>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("LanDiscovery");
                        instance = go.AddComponent<LanDiscovery>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public static LanDiscovery ExistingInstance => instance;

        public bool IsBroadcasting { get; private set; }
        public bool IsListening { get; private set; }
        public float LastSearchRestartTime { get; private set; }

        public string LocalClientUid { get; set; } = string.Empty;
        public int LocalCallbackTcpPort { get; set; } = NetworkProtocol.DefaultClientCallbackPort;
        public string LocalSenderUid { get; set; } = string.Empty;

        public event Action<IReadOnlyList<DiscoveredHostInfo>> OnHostsUpdated;
        public event Action<IPEndPoint, UdpEnvelopePacket> OnUdpEnvelopeReceived;
        public event Action<string, int> OnStudentDiscoveryAckReceived;

        private const float BroadcastIntervalSeconds = 1.2f;
        private const float HostStaleTimeoutSeconds = 6.5f;
        private const float KnownEndpointTimeoutSeconds = 30.0f;

        private struct InterfaceBroadcaster
        {
            public UdpClient client;
            public IPAddress localIp;
            public IPAddress subnetMask;
            public IPAddress subnetBroadcast;
        }

        private struct IncomingUdpDatagram
        {
            public string senderIp;
            public int senderPort;
            public string rawText;
        }

        private UdpClient broadcastClient;
        private readonly List<InterfaceBroadcaster> interfaceBroadcasters = new List<InterfaceBroadcaster>();
        private string broadcastHostName = "Teacher";
        private int broadcastTcpPort = NetworkProtocol.DefaultSessionPort;
        private int activeDiscoveryPort = NetworkProtocol.DiscoveryPort;
        private Func<int> studentCountProvider;
        private float nextBroadcastTime;
        private bool startedListenerForHost;

        private UdpClient listenClient;
        private Thread listenThread;
        private volatile bool stopListenRequested;
        private readonly object socketSendLock = new object();

        private readonly ConcurrentQueue<IncomingUdpDatagram> incomingDatagrams = new ConcurrentQueue<IncomingUdpDatagram>();
        private readonly List<DiscoveredHostInfo> discoveredHosts = new List<DiscoveredHostInfo>();
        private readonly Dictionary<string, (int port, float lastSeenRealtime)> knownStudentEndpoints =
            new Dictionary<string, (int port, float lastSeenRealtime)>(StringComparer.OrdinalIgnoreCase);
        private float nextPruneTime;

        public IReadOnlyList<DiscoveredHostInfo> DiscoveredHosts => discoveredHosts;

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int GetIpNetTable(IntPtr pIpNetTable, ref int pdwSize, bool bOrder);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.runInBackground = true;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (IsBroadcasting)
            {
                PollBroadcasterSockets();
                if (Time.unscaledTime >= nextBroadcastTime)
                {
                    nextBroadcastTime = Time.unscaledTime + BroadcastIntervalSeconds;
                    SendBroadcastBeaconOnce();
                }
            }

            bool listChanged = false;
            while (incomingDatagrams.TryDequeue(out IncomingUdpDatagram dgram))
            {
                if (ProcessIncomingDatagram(dgram))
                {
                    listChanged = true;
                }
            }

            if (IsListening && Time.unscaledTime >= nextPruneTime)
            {
                nextPruneTime = Time.unscaledTime + 1.0f;
                float now = Time.realtimeSinceStartup;
                for (int i = discoveredHosts.Count - 1; i >= 0; i--)
                {
                    if (now - discoveredHosts[i].lastSeenRealtime > HostStaleTimeoutSeconds)
                    {
                        discoveredHosts.RemoveAt(i);
                        listChanged = true;
                    }
                }

                if (knownStudentEndpoints.Count > 0)
                {
                    List<string> staleKeys = null;
                    foreach (var kvp in knownStudentEndpoints)
                    {
                        if (now - kvp.Value.lastSeenRealtime > KnownEndpointTimeoutSeconds)
                        {
                            if (staleKeys == null) staleKeys = new List<string>();
                            staleKeys.Add(kvp.Key);
                        }
                    }
                    if (staleKeys != null)
                    {
                        for (int i = 0; i < staleKeys.Count; i++)
                        {
                            knownStudentEndpoints.Remove(staleKeys[i]);
                        }
                    }
                }
            }

            if (listChanged)
            {
                OnHostsUpdated?.Invoke(discoveredHosts);
            }
        }

        private void OnApplicationQuit()
        {
            StopAllDiscovery();
        }

        private void OnDestroy()
        {
            StopAllDiscovery();
            if (instance == this)
            {
                instance = null;
            }
        }

        public void StartBroadcasting(string hostName, int tcpPort, Func<int> connectedStudentCountProvider = null)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning("[LanDiscovery] UDP broadcast is not supported on WebGL.");
                return;
            }

            StopBroadcasting();

            try
            {
                broadcastHostName = string.IsNullOrWhiteSpace(hostName) ? GetDefaultDeviceName() : hostName.Trim();
                broadcastTcpPort = tcpPort;
                studentCountProvider = connectedStudentCountProvider;

                broadcastClient = new UdpClient();
                broadcastClient.EnableBroadcast = true;
                BuildPerInterfaceBroadcasters();

                IsBroadcasting = true;
                nextBroadcastTime = 0f;

                if (!IsListening)
                {
                    StartListening(activeDiscoveryPort);
                    startedListenerForHost = IsListening;
                }

                Debug.Log($"[LanDiscovery] Started UDP broadcasting as '{broadcastHostName}' for TCP port {broadcastTcpPort} on {interfaceBroadcasters.Count} interface(s).");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Failed to start UDP broadcaster: {e.Message}");
                StopBroadcasting();
            }
        }

        public void UpdateBroadcastHostName(string hostName)
        {
            broadcastHostName = string.IsNullOrWhiteSpace(hostName) ? GetDefaultDeviceName() : hostName.Trim();
            if (IsBroadcasting)
            {
                nextBroadcastTime = 0f;
            }
        }

        public void StopBroadcasting()
        {
            IsBroadcasting = false;
            if (broadcastClient != null)
            {
                try { broadcastClient.Close(); } catch { }
                broadcastClient = null;
            }

            for (int i = 0; i < interfaceBroadcasters.Count; i++)
            {
                try { interfaceBroadcasters[i].client?.Close(); } catch { }
            }
            interfaceBroadcasters.Clear();
            knownStudentEndpoints.Clear();

            if (startedListenerForHost)
            {
                startedListenerForHost = false;
                StopListening();
            }
        }

        public void StartListening(int discoveryPort = NetworkProtocol.DiscoveryPort)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning("[LanDiscovery] UDP listener is not supported on WebGL.");
                return;
            }

            activeDiscoveryPort = discoveryPort;
            if (IsListening) return;

            try
            {
                stopListenRequested = false;
                startedListenerForHost = false;
                discoveredHosts.Clear();

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.ExclusiveAddressUse = false;
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                sock.EnableBroadcast = true;
                sock.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));

                listenClient = new UdpClient { Client = sock, EnableBroadcast = true };

                IsListening = true;
                listenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "CL_LanDiscoveryListener"
                };
                listenThread.Start();

                OnHostsUpdated?.Invoke(discoveredHosts);
                Debug.Log($"[LanDiscovery] Listening on UDP port {discoveryPort}...");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Failed to start UDP listener: {e.Message}");
                StopListening();
            }
        }

        public void RestartDiscovery(int discoveryPort = NetworkProtocol.DiscoveryPort)
        {
            LastSearchRestartTime = Time.unscaledTime;
            discoveredHosts.Clear();
            while (incomingDatagrams.TryDequeue(out _)) { }
            StopListening();
            StartListening(discoveryPort);
            OnHostsUpdated?.Invoke(discoveredHosts);
        }

        public void StopListening()
        {
            stopListenRequested = true;
            IsListening = false;
            startedListenerForHost = false;

            if (listenClient != null)
            {
                try { listenClient.Close(); } catch { }
                listenClient = null;
            }

            if (listenThread != null)
            {
                listenThread = null;
            }
        }

        public void StopAllDiscovery()
        {
            startedListenerForHost = false;
            StopBroadcasting();
            StopListening();
        }

        public void SendUdpEnvelopeToHost(IReadOnlyList<string> hostIps, int hostUdpPort, UdpEnvelopePacket packet)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer || packet == null) return;

            try
            {
                packet.protocol = NetworkProtocol.ProtocolId;
                string raw = NetworkProtocol.UdpEnvelopePrefix + JsonUtility.ToJson(packet);
                byte[] bytes = Encoding.UTF8.GetBytes(raw);

                HashSet<string> sentTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                void TrySend(UdpClient sender, IPEndPoint ep)
                {
                    if (sender == null || ep == null) return;
                    string key = $"{ep.Address}:{ep.Port}";
                    if (!sentTargets.Add(key)) return;
                    try
                    {
                        lock (socketSendLock)
                        {
                            sender.Send(bytes, bytes.Length, ep);
                        }
                    }
                    catch { }
                }

                UdpClient primarySocket = listenClient ?? broadcastClient;
                bool createdTemp = false;
                if (primarySocket == null)
                {
                    primarySocket = new UdpClient { EnableBroadcast = true };
                    createdTemp = true;
                }

                try
                {
                    if (hostIps != null)
                    {
                        for (int i = 0; i < hostIps.Count; i++)
                        {
                            if (IPAddress.TryParse(hostIps[i], out IPAddress addr))
                            {
                                if (hostUdpPort > 0 && hostUdpPort != activeDiscoveryPort)
                                {
                                    TrySend(primarySocket, new IPEndPoint(addr, hostUdpPort));
                                }
                                TrySend(primarySocket, new IPEndPoint(addr, activeDiscoveryPort));
                            }
                        }
                    }

                    foreach (IPAddress bcast in GetBroadcastTargets())
                    {
                        if (IPAddress.IsLoopback(bcast)) continue;
                        TrySend(primarySocket, new IPEndPoint(bcast, activeDiscoveryPort));
                    }
                }
                finally
                {
                    if (createdTemp)
                    {
                        try { primarySocket.Close(); } catch { }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] SendUdpEnvelopeToHost warning: {e.Message}");
            }
        }

        public void SendUdpEnvelopeToStudent(string studentIp, int studentUdpPort, UdpEnvelopePacket packet)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer || packet == null) return;

            try
            {
                packet.protocol = NetworkProtocol.ProtocolId;
                string raw = NetworkProtocol.UdpEnvelopePrefix + JsonUtility.ToJson(packet);
                byte[] bytes = Encoding.UTF8.GetBytes(raw);

                IPAddress parsedStudentIp = null;
                if (!string.IsNullOrWhiteSpace(studentIp))
                {
                    IPAddress.TryParse(studentIp.Trim(), out parsedStudentIp);
                }

                HashSet<string> sentBySocketAndTarget = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                void TrySend(UdpClient sender, int socketIdx, IPEndPoint ep)
                {
                    if (sender == null || ep == null) return;
                    string key = $"{socketIdx}|{ep.Address}:{ep.Port}";
                    if (!sentBySocketAndTarget.Add(key)) return;
                    try
                    {
                        lock (socketSendLock)
                        {
                            sender.Send(bytes, bytes.Length, ep);
                        }
                    }
                    catch { }
                }

                if (parsedStudentIp != null)
                {
                    if (listenClient != null)
                    {
                        TrySend(listenClient, -1, new IPEndPoint(parsedStudentIp, activeDiscoveryPort));
                        if (studentUdpPort > 0 && studentUdpPort != activeDiscoveryPort)
                        {
                            TrySend(listenClient, -1, new IPEndPoint(parsedStudentIp, studentUdpPort));
                        }
                    }

                    if (knownStudentEndpoints.TryGetValue(parsedStudentIp.ToString(), out var knownEp) &&
                        knownEp.port > 0 && knownEp.port != activeDiscoveryPort)
                    {
                        if (listenClient != null)
                        {
                            TrySend(listenClient, -1, new IPEndPoint(parsedStudentIp, knownEp.port));
                        }
                    }

                    for (int i = 0; i < interfaceBroadcasters.Count; i++)
                    {
                        InterfaceBroadcaster entry = interfaceBroadcasters[i];
                        if (entry.client == null) continue;
                        if (IsSameSubnet(parsedStudentIp, entry.localIp, entry.subnetMask) || IPAddress.IsLoopback(parsedStudentIp))
                        {
                            TrySend(entry.client, i, new IPEndPoint(parsedStudentIp, activeDiscoveryPort));
                            if (studentUdpPort > 0 && studentUdpPort != activeDiscoveryPort)
                            {
                                TrySend(entry.client, i, new IPEndPoint(parsedStudentIp, studentUdpPort));
                            }
                            if (knownStudentEndpoints.TryGetValue(parsedStudentIp.ToString(), out var known) && known.port > 0)
                            {
                                TrySend(entry.client, i, new IPEndPoint(parsedStudentIp, known.port));
                            }
                        }
                    }
                }

                // Also send via active interface broadcasters (subnet broadcast + global broadcast)
                for (int i = 0; i < interfaceBroadcasters.Count; i++)
                {
                    InterfaceBroadcaster entry = interfaceBroadcasters[i];
                    if (entry.client == null) continue;
                    if (entry.subnetBroadcast != null)
                    {
                        TrySend(entry.client, i, new IPEndPoint(entry.subnetBroadcast, activeDiscoveryPort));
                    }
                    TrySend(entry.client, i, new IPEndPoint(IPAddress.Broadcast, activeDiscoveryPort));
                }

                if (broadcastClient != null && parsedStudentIp != null && IPAddress.IsLoopback(parsedStudentIp))
                {
                    TrySend(broadcastClient, -2, new IPEndPoint(IPAddress.Loopback, activeDiscoveryPort));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] SendUdpEnvelopeToStudent warning: {e.Message}");
            }
        }

        private void SendBroadcastBeaconOnce(string priorityUnicastIp = null, int priorityUnicastPort = 0)
        {
            if (broadcastClient == null) return;

            try
            {
                string[] allLocalIps = GetAllLocalIPv4Addresses();
                string primaryIp = allLocalIps.Length > 0 ? allLocalIps[0] : GetPrimaryLocalIPv4();
                int count = studentCountProvider != null ? Mathf.Max(0, studentCountProvider()) : 0;

                List<IPAddress> arpNeighbors = GetArpNeighborIPv4Addresses();

                byte[] BuildBeaconBytes(string ipForBeacon)
                {
                    LanHostBeacon beacon = new LanHostBeacon
                    {
                        protocol = NetworkProtocol.ProtocolId,
                        hostName = broadcastHostName,
                        hostIp = ipForBeacon,
                        alternateIps = allLocalIps,
                        tcpPort = broadcastTcpPort,
                        connectedStudents = count
                    };
                    return Encoding.UTF8.GetBytes(JsonUtility.ToJson(beacon));
                }

                byte[] primaryBytes = BuildBeaconBytes(primaryIp);

                HashSet<string> sentTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IPAddress target in GetBroadcastTargets())
                {
                    string key = target.ToString();
                    if (!sentTargets.Add(key)) continue;

                    try
                    {
                        lock (socketSendLock)
                        {
                            broadcastClient.Send(primaryBytes, primaryBytes.Length, new IPEndPoint(target, activeDiscoveryPort));
                        }
                    }
                    catch { }
                }

                // Send per-interface beacons with each adapter's exact IPv4 address + unicast to ARP neighbors
                for (int i = 0; i < interfaceBroadcasters.Count; i++)
                {
                    InterfaceBroadcaster entry = interfaceBroadcasters[i];
                    if (entry.client == null) continue;

                    byte[] ifaceBytes = BuildBeaconBytes(entry.localIp.ToString());

                    try
                    {
                        lock (socketSendLock)
                        {
                            entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(IPAddress.Broadcast, activeDiscoveryPort));
                        }
                    }
                    catch { }

                    if (entry.subnetBroadcast != null)
                    {
                        try
                        {
                            lock (socketSendLock)
                            {
                                entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(entry.subnetBroadcast, activeDiscoveryPort));
                            }
                        }
                        catch { }
                    }

                    // Unicast to ARP neighbors on this subnet to punch stateful UDP pinhole in Windows Firewall
                    for (int n = 0; n < arpNeighbors.Count; n++)
                    {
                        IPAddress neighbor = arpNeighbors[n];
                        if (!IsSameSubnet(neighbor, entry.localIp, entry.subnetMask)) continue;
                        try
                        {
                            lock (socketSendLock)
                            {
                                entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(neighbor, activeDiscoveryPort));
                            }
                        }
                        catch { }
                    }

                    // Also unicast to recently discovered student endpoints
                    foreach (var kvp in knownStudentEndpoints)
                    {
                        if (!IPAddress.TryParse(kvp.Key, out IPAddress studentAddr)) continue;
                        if (!IsSameSubnet(studentAddr, entry.localIp, entry.subnetMask) && !IPAddress.IsLoopback(studentAddr)) continue;

                        try
                        {
                            lock (socketSendLock)
                            {
                                entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(studentAddr, activeDiscoveryPort));
                                if (kvp.Value.port > 0 && kvp.Value.port != activeDiscoveryPort)
                                {
                                    entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(studentAddr, kvp.Value.port));
                                }
                            }
                        }
                        catch { }
                    }

                    if (!string.IsNullOrWhiteSpace(priorityUnicastIp) &&
                        IPAddress.TryParse(priorityUnicastIp, out IPAddress prioAddr) &&
                        (IsSameSubnet(prioAddr, entry.localIp, entry.subnetMask) || IPAddress.IsLoopback(prioAddr)))
                    {
                        try
                        {
                            lock (socketSendLock)
                            {
                                entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(prioAddr, activeDiscoveryPort));
                                if (priorityUnicastPort > 0 && priorityUnicastPort != activeDiscoveryPort)
                                {
                                    entry.client.Send(ifaceBytes, ifaceBytes.Length, new IPEndPoint(prioAddr, priorityUnicastPort));
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Broadcast send warning: {e.Message}");
            }
        }

        private void PollBroadcasterSockets()
        {
            PollSingleNonBlockingSocket(broadcastClient);
            for (int i = 0; i < interfaceBroadcasters.Count; i++)
            {
                PollSingleNonBlockingSocket(interfaceBroadcasters[i].client);
            }
        }

        private void PollSingleNonBlockingSocket(UdpClient client)
        {
            if (client == null) return;
            try
            {
                int guard = 0;
                while (client.Available > 0 && guard++ < 32)
                {
                    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = client.Receive(ref remoteEp);
                    if (data != null && data.Length > 0)
                    {
                        incomingDatagrams.Enqueue(new IncomingUdpDatagram
                        {
                            senderIp = remoteEp.Address.ToString(),
                            senderPort = remoteEp.Port,
                            rawText = Encoding.UTF8.GetString(data)
                        });
                    }
                }
            }
            catch { }
        }

        private void BuildPerInterfaceBroadcasters()
        {
            interfaceBroadcasters.Clear();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    IPInterfaceProperties props = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicast in props.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(unicast.Address)) continue;
                        if (IsApipaAddress(unicast.Address)) continue;

                        IPAddress mask = unicast.IPv4Mask;
                        IPAddress subnetBroadcast = ComputeSubnetBroadcast(unicast.Address, mask);

                        try
                        {
                            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            sock.EnableBroadcast = true;
                            sock.Bind(new IPEndPoint(unicast.Address, 0));
                            UdpClient client = new UdpClient { Client = sock, EnableBroadcast = true };
                            interfaceBroadcasters.Add(new InterfaceBroadcaster
                            {
                                client = client,
                                localIp = unicast.Address,
                                subnetMask = mask,
                                subnetBroadcast = subnetBroadcast
                            });
                        }
                        catch
                        {
                            // Skip adapters that refuse binding
                        }
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }
        }

        private void ListenLoop()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (!stopListenRequested && listenClient != null)
            {
                try
                {
                    byte[] data = listenClient.Receive(ref remoteEp);
                    if (data != null && data.Length > 0)
                    {
                        string text = Encoding.UTF8.GetString(data);
                        incomingDatagrams.Enqueue(new IncomingUdpDatagram
                        {
                            senderIp = remoteEp.Address.ToString(),
                            senderPort = remoteEp.Port,
                            rawText = text
                        });
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (stopListenRequested) break;
                }
                catch
                {
                    if (stopListenRequested) break;
                }
            }
        }

        private bool ProcessIncomingDatagram(IncomingUdpDatagram dgram)
        {
            if (string.IsNullOrWhiteSpace(dgram.rawText)) return false;

            string raw = dgram.rawText.Trim();
            if (raw.StartsWith(NetworkProtocol.UdpEnvelopePrefix, StringComparison.Ordinal))
            {
                HandleIncomingUdpEnvelope(dgram.senderIp, dgram.senderPort, raw.Substring(NetworkProtocol.UdpEnvelopePrefix.Length));
                return false;
            }

            if (raw.IndexOf(NetworkProtocol.MsgDiscoveryAck, StringComparison.Ordinal) >= 0)
            {
                if (TryHandleDiscoveryAck(dgram.senderIp, dgram.senderPort, raw))
                {
                    return false;
                }
            }

            if (IsBroadcasting)
            {
                // Host does not populate Student discoveredHosts list
                return false;
            }

            return ProcessIncomingBeacon(dgram.senderIp, dgram.senderPort, raw);
        }

        private void HandleIncomingUdpEnvelope(string senderIp, int senderPort, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                UdpEnvelopePacket pkt = JsonUtility.FromJson<UdpEnvelopePacket>(json);
                if (pkt == null || pkt.protocol != NetworkProtocol.ProtocolId) return;
                if (!string.IsNullOrEmpty(LocalSenderUid) && pkt.senderUid == LocalSenderUid) return;

                if (IsBroadcasting && !string.IsNullOrWhiteSpace(senderIp) && senderIp != "0.0.0.0")
                {
                    knownStudentEndpoints[senderIp] = (senderPort, Time.realtimeSinceStartup);
                }

                IPAddress addr = IPAddress.TryParse(senderIp, out IPAddress parsed) ? parsed : IPAddress.Loopback;
                OnUdpEnvelopeReceived?.Invoke(new IPEndPoint(addr, senderPort), pkt);
            }
            catch { }
        }

        private bool TryHandleDiscoveryAck(string senderIp, int senderPort, string json)
        {
            try
            {
                LanDiscoveryAck ack = JsonUtility.FromJson<LanDiscoveryAck>(json);
                if (ack == null || ack.protocol != NetworkProtocol.ProtocolId || ack.msgType != NetworkProtocol.MsgDiscoveryAck)
                {
                    return false;
                }

                if (IsBroadcasting && !string.IsNullOrWhiteSpace(senderIp) && senderIp != "0.0.0.0")
                {
                    bool isNewStudent = !knownStudentEndpoints.ContainsKey(senderIp);
                    knownStudentEndpoints[senderIp] = (senderPort, Time.realtimeSinceStartup);

                    if (isNewStudent)
                    {
                        SendBroadcastBeaconOnce(senderIp, senderPort);
                    }

                    int callbackPort = ack.callbackTcpPort > 0 ? ack.callbackTcpPort : NetworkProtocol.DefaultClientCallbackPort;
                    OnStudentDiscoveryAckReceived?.Invoke(senderIp, callbackPort);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ProcessIncomingBeacon(string senderIp, int senderPort, string rawJson)
        {
            try
            {
                LanHostBeacon beacon = JsonUtility.FromJson<LanHostBeacon>(rawJson);
                if (beacon == null || beacon.protocol != NetworkProtocol.ProtocolId) return false;
                if (string.IsNullOrWhiteSpace(beacon.hostName) && string.IsNullOrWhiteSpace(beacon.hostIp)) return false;

                List<string> candidates = BuildOrderedCandidateIps(senderIp, beacon);
                if (candidates.Count == 0) return false;

                string resolvedIp = candidates[0];

                // Immediately send a Discovery ACK back so the Host learns our IP/ARP & keeps its UDP hole open
                SendDiscoveryAckToHost(senderIp, senderPort, beacon.hostIp);

                float now = Time.realtimeSinceStartup;

                for (int i = 0; i < discoveredHosts.Count; i++)
                {
                    DiscoveredHostInfo existing = discoveredHosts[i];
                    if (IsSameDiscoveredHost(existing, beacon, candidates))
                    {
                        bool changed = existing.hostName != beacon.hostName ||
                                       existing.connectedStudents != beacon.connectedStudents ||
                                       existing.hostIp != resolvedIp;

                        MergeCandidateIps(existing.candidateIps, candidates);
                        existing.hostIp = resolvedIp;
                        existing.hostName = beacon.hostName;
                        existing.connectedStudents = beacon.connectedStudents;
                        existing.lastSenderUdpPort = senderPort;
                        existing.lastSeenRealtime = now;
                        return changed;
                    }
                }

                DiscoveredHostInfo added = new DiscoveredHostInfo
                {
                    hostName = beacon.hostName,
                    hostIp = resolvedIp,
                    tcpPort = beacon.tcpPort,
                    connectedStudents = beacon.connectedStudents,
                    lastSenderUdpPort = senderPort,
                    lastSeenRealtime = now
                };
                added.candidateIps.AddRange(candidates);
                discoveredHosts.Add(added);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SendDiscoveryAckToHost(string senderIp, int senderPort, string beaconHostIp)
        {
            if (listenClient == null) return;
            try
            {
                LanDiscoveryAck ack = new LanDiscoveryAck
                {
                    protocol = NetworkProtocol.ProtocolId,
                    msgType = NetworkProtocol.MsgDiscoveryAck,
                    clientUid = LocalClientUid ?? string.Empty,
                    callbackTcpPort = LocalCallbackTcpPort > 0 ? LocalCallbackTcpPort : NetworkProtocol.DefaultClientCallbackPort
                };
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(ack));

                HashSet<string> sent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                void SendIfValid(string ip, int port)
                {
                    if (string.IsNullOrWhiteSpace(ip) || port <= 0) return;
                    if (!IPAddress.TryParse(ip, out IPAddress addr)) return;
                    string key = $"{addr}:{port}";
                    if (!sent.Add(key)) return;
                    try
                    {
                        lock (socketSendLock)
                        {
                            listenClient.Send(bytes, bytes.Length, new IPEndPoint(addr, port));
                        }
                    }
                    catch { }
                }

                SendIfValid(senderIp, senderPort);
                SendIfValid(senderIp, activeDiscoveryPort);
                SendIfValid(beaconHostIp, activeDiscoveryPort);
            }
            catch { }
        }

        private static List<string> BuildOrderedCandidateIps(string senderIp, LanHostBeacon beacon)
        {
            List<string> rawIps = new List<string>();
            void AddCandidate(string ip)
            {
                if (string.IsNullOrWhiteSpace(ip)) return;
                string trimmed = ip.Trim();
                if (trimmed == "0.0.0.0" || !IPAddress.TryParse(trimmed, out _)) return;
                if (!rawIps.Contains(trimmed))
                {
                    rawIps.Add(trimmed);
                }
            }

            AddCandidate(senderIp);
            AddCandidate(beacon.hostIp);
            if (beacon.alternateIps != null)
            {
                for (int i = 0; i < beacon.alternateIps.Length; i++)
                {
                    AddCandidate(beacon.alternateIps[i]);
                }
            }

            List<(IPAddress ip, IPAddress mask)> localSubnets = GetLocalActiveSubnets();
            List<string> ordered = new List<string>(rawIps.Count);

            // 1. Same-subnet non-loopback non-APIPA IPs first
            for (int i = 0; i < rawIps.Count; i++)
            {
                if (!IPAddress.TryParse(rawIps[i], out IPAddress addr)) continue;
                if (IPAddress.IsLoopback(addr) || IsApipaAddress(addr)) continue;
                for (int s = 0; s < localSubnets.Count; s++)
                {
                    if (IsSameSubnet(addr, localSubnets[s].ip, localSubnets[s].mask))
                    {
                        ordered.Add(rawIps[i]);
                        break;
                    }
                }
            }

            // 2. Other non-loopback non-APIPA IPs (preferring senderIp next)
            for (int i = 0; i < rawIps.Count; i++)
            {
                if (ordered.Contains(rawIps[i])) continue;
                if (!IPAddress.TryParse(rawIps[i], out IPAddress addr)) continue;
                if (IPAddress.IsLoopback(addr) || IsApipaAddress(addr)) continue;
                ordered.Add(rawIps[i]);
            }

            // 3. Loopback only if explicitly observed/advertised
            for (int i = 0; i < rawIps.Count; i++)
            {
                if (ordered.Contains(rawIps[i])) continue;
                if (!IPAddress.TryParse(rawIps[i], out IPAddress addr)) continue;
                if (IPAddress.IsLoopback(addr))
                {
                    ordered.Add(rawIps[i]);
                }
            }

            // 4. APIPA fallback only if nothing else exists
            for (int i = 0; i < rawIps.Count; i++)
            {
                if (!ordered.Contains(rawIps[i]))
                {
                    ordered.Add(rawIps[i]);
                }
            }

            return ordered;
        }

        private static bool IsSameDiscoveredHost(DiscoveredHostInfo existing, LanHostBeacon beacon, List<string> newCandidates)
        {
            if (existing.tcpPort != beacon.tcpPort) return false;
            if (existing.hostName == beacon.hostName) return true;

            for (int i = 0; i < newCandidates.Count; i++)
            {
                string cand = newCandidates[i];
                if (cand == "127.0.0.1") continue;
                if (existing.hostIp == cand) return true;
                if (existing.candidateIps != null && existing.candidateIps.Contains(cand)) return true;
            }
            return false;
        }

        private static void MergeCandidateIps(List<string> target, List<string> additions)
        {
            if (target == null || additions == null) return;
            for (int i = 0; i < additions.Count; i++)
            {
                if (!target.Contains(additions[i]))
                {
                    target.Add(additions[i]);
                }
            }
        }

        private static List<IPAddress> GetBroadcastTargets()
        {
            List<IPAddress> targets = new List<IPAddress>
            {
                IPAddress.Loopback,
                IPAddress.Broadcast
            };

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    IPInterfaceProperties props = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicast in props.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IsApipaAddress(unicast.Address)) continue;

                        IPAddress bcast = ComputeSubnetBroadcast(unicast.Address, unicast.IPv4Mask);
                        if (bcast != null)
                        {
                            targets.Add(bcast);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to global broadcast + loopback already in list
            }

            return targets;
        }

        public static List<IPAddress> GetArpNeighborIPv4Addresses()
        {
            List<IPAddress> neighbors = new List<IPAddress>();
            if (Application.platform != RuntimePlatform.WindowsEditor &&
                Application.platform != RuntimePlatform.WindowsPlayer)
            {
                return neighbors;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                int bytesNeeded = 0;
                GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);
                if (bytesNeeded <= 0) return neighbors;

                buffer = Marshal.AllocHGlobal(bytesNeeded);
                if (GetIpNetTable(buffer, ref bytesNeeded, false) != 0)
                {
                    return neighbors;
                }

                int rowCount = Marshal.ReadInt32(buffer);
                if (rowCount <= 0 || rowCount > 4096) return neighbors;

                List<(IPAddress ip, IPAddress mask)> localSubnets = GetLocalActiveSubnets();
                HashSet<string> localIpSet = new HashSet<string>(GetAllLocalIPv4Addresses(), StringComparer.OrdinalIgnoreCase);
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < rowCount; i++)
                {
                    int rowOffset = 4 + i * 24;
                    if (rowOffset + 24 > bytesNeeded) break;

                    uint addrVal = (uint)Marshal.ReadInt32(buffer, rowOffset + 16);
                    int entryType = Marshal.ReadInt32(buffer, rowOffset + 20);
                    // 3 = Dynamic, 4 = Static
                    if (entryType != 3 && entryType != 4) continue;

                    IPAddress ip = new IPAddress(addrVal);
                    if (IPAddress.IsLoopback(ip) || IsApipaAddress(ip)) continue;

                    byte[] bytes = ip.GetAddressBytes();
                    if (bytes.Length != 4) continue;
                    if (bytes[0] >= 224 || bytes[0] == 0 || bytes[3] == 255 || bytes[3] == 0) continue;

                    string ipStr = ip.ToString();
                    if (localIpSet.Contains(ipStr)) continue;

                    bool onActiveSubnet = localSubnets.Count == 0;
                    for (int s = 0; s < localSubnets.Count; s++)
                    {
                        if (IsSameSubnet(ip, localSubnets[s].ip, localSubnets[s].mask))
                        {
                            onActiveSubnet = true;
                            break;
                        }
                    }

                    if (onActiveSubnet && seen.Add(ipStr))
                    {
                        neighbors.Add(ip);
                    }
                }
            }
            catch
            {
                // ARP table lookup optional
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return neighbors;
        }

        public static string[] GetAllLocalIPv4Addresses()
        {
            List<string> preferredWithGateway = new List<string>();
            List<string> physicalOthers = new List<string>();
            List<string> fallbackOthers = new List<string>();

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    IPInterfaceProperties props = ni.GetIPProperties();
                    bool hasGateway = props.GatewayAddresses != null && props.GatewayAddresses.Count > 0;
                    bool isVirtual = IsLikelyVirtualAdapter(ni);

                    foreach (UnicastIPAddressInformation ip in props.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip.Address) || IsApipaAddress(ip.Address)) continue;

                        string addrStr = ip.Address.ToString();
                        if (!isVirtual && hasGateway)
                        {
                            if (!preferredWithGateway.Contains(addrStr)) preferredWithGateway.Add(addrStr);
                        }
                        else if (!isVirtual)
                        {
                            if (!physicalOthers.Contains(addrStr)) physicalOthers.Add(addrStr);
                        }
                        else
                        {
                            if (!fallbackOthers.Contains(addrStr)) fallbackOthers.Add(addrStr);
                        }
                    }
                }
            }
            catch { }

            List<string> combined = new List<string>(preferredWithGateway.Count + physicalOthers.Count + fallbackOthers.Count);
            combined.AddRange(preferredWithGateway);
            foreach (string s in physicalOthers)
            {
                if (!combined.Contains(s)) combined.Add(s);
            }
            foreach (string s in fallbackOthers)
            {
                if (!combined.Contains(s)) combined.Add(s);
            }

            return combined.ToArray();
        }

        public static string GetPrimaryLocalIPv4()
        {
            string[] all = GetAllLocalIPv4Addresses();
            if (all.Length > 0)
            {
                return all[0];
            }

            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && !IsApipaAddress(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }

        private static List<(IPAddress ip, IPAddress mask)> GetLocalActiveSubnets()
        {
            List<(IPAddress ip, IPAddress mask)> result = new List<(IPAddress ip, IPAddress mask)>();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    if (IsLikelyVirtualAdapter(ni)) continue;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip.Address) || IsApipaAddress(ip.Address)) continue;
                        result.Add((ip.Address, ip.IPv4Mask));
                    }
                }
            }
            catch { }
            return result;
        }

        private static bool IsLikelyVirtualAdapter(NetworkInterface ni)
        {
            string desc = ni.Description ?? string.Empty;
            string name = ni.Name ?? string.Empty;
            return ContainsIgnoreCase(desc, "Virtual") ||
                   ContainsIgnoreCase(desc, "Pseudo") ||
                   ContainsIgnoreCase(desc, "Hyper-V") ||
                   ContainsIgnoreCase(desc, "VMware") ||
                   ContainsIgnoreCase(desc, "VBox") ||
                   ContainsIgnoreCase(desc, "TAP-") ||
                   ContainsIgnoreCase(desc, "WSL") ||
                   ContainsIgnoreCase(desc, "Loopback") ||
                   ContainsIgnoreCase(name, "vEthernet");
        }

        private static bool ContainsIgnoreCase(string source, string term)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsApipaAddress(IPAddress addr)
        {
            byte[] b = addr.GetAddressBytes();
            return b.Length == 4 && b[0] == 169 && b[1] == 254;
        }

        private static IPAddress ComputeSubnetBroadcast(IPAddress ip, IPAddress mask)
        {
            if (ip == null || mask == null) return null;
            byte[] ipBytes = ip.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();
            if (ipBytes.Length != 4 || maskBytes.Length != 4) return null;

            byte[] broadcastBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | (~maskBytes[i]));
            }
            return new IPAddress(broadcastBytes);
        }

        private static bool IsSameSubnet(IPAddress a, IPAddress b, IPAddress mask)
        {
            if (a == null || b == null) return false;
            if (mask == null) return true;
            byte[] ab = a.GetAddressBytes();
            byte[] bb = b.GetAddressBytes();
            byte[] mb = mask.GetAddressBytes();
            if (ab.Length != 4 || bb.Length != 4 || mb.Length != 4) return false;

            for (int i = 0; i < 4; i++)
            {
                if ((ab[i] & mb[i]) != (bb[i] & mb[i])) return false;
            }
            return true;
        }

        public static string GetDefaultDeviceName()
        {
            try
            {
                string name = SystemInfo.deviceName;
                if (!string.IsNullOrWhiteSpace(name) && name != "<unknown>")
                {
                    return name.Trim();
                }
            }
            catch { }

            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "Device";
            }
        }
    }
}
