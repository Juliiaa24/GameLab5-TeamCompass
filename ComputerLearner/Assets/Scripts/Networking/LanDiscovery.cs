/**
 * Author: TeamCompass
 * Description: UDP LAN Host Discovery service supporting both Teacher Host broadcasting
 *              (subnet broadcast + global broadcast + 127.0.0.1 loopback for single-PC testing)
 *              and Student Client automatic host discovery with ReuseAddress socket sharing.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        public event Action<IReadOnlyList<DiscoveredHostInfo>> OnHostsUpdated;

        private const float BroadcastIntervalSeconds = 1.5f;
        private const float HostStaleTimeoutSeconds = 5.0f;

        private UdpClient broadcastClient;
        private string broadcastHostName = "Teacher";
        private int broadcastTcpPort = NetworkProtocol.DefaultSessionPort;
        private Func<int> studentCountProvider;
        private float nextBroadcastTime;

        private UdpClient listenClient;
        private Thread listenThread;
        private volatile bool stopListenRequested;

        private readonly ConcurrentQueue<DiscoveredHostPacket> incomingBeacons = new ConcurrentQueue<DiscoveredHostPacket>();
        private readonly List<DiscoveredHostInfo> discoveredHosts = new List<DiscoveredHostInfo>();
        private float nextPruneTime;

        private struct DiscoveredHostPacket
        {
            public string senderIp;
            public string rawJson;
        }

        public IReadOnlyList<DiscoveredHostInfo> DiscoveredHosts => discoveredHosts;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (IsBroadcasting && Time.unscaledTime >= nextBroadcastTime)
            {
                nextBroadcastTime = Time.unscaledTime + BroadcastIntervalSeconds;
                SendBroadcastBeaconOnce();
            }

            bool listChanged = false;
            while (incomingBeacons.TryDequeue(out DiscoveredHostPacket pkt))
            {
                if (ProcessIncomingBeacon(pkt.senderIp, pkt.rawJson))
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

                IsBroadcasting = true;
                nextBroadcastTime = 0f;
                Debug.Log($"[LanDiscovery] Started UDP broadcasting as '{broadcastHostName}' for TCP port {broadcastTcpPort}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Failed to start UDP broadcaster: {e.Message}");
                StopBroadcasting();
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
        }

        public void StartListening(int discoveryPort = NetworkProtocol.DefaultDiscoveryPort)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning("[LanDiscovery] UDP listener is not supported on WebGL.");
                return;
            }

            if (IsListening) return;

            try
            {
                stopListenRequested = false;
                discoveredHosts.Clear();

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                sock.EnableBroadcast = true;
                sock.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));

                listenClient = new UdpClient { Client = sock };

                IsListening = true;
                listenThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "CL_LanDiscoveryListener"
                };
                listenThread.Start();

                OnHostsUpdated?.Invoke(discoveredHosts);
                Debug.Log($"[LanDiscovery] Listening for Teacher Hosts on UDP port {discoveryPort}...");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Failed to start UDP listener: {e.Message}");
                StopListening();
            }
        }

        public void StopListening()
        {
            stopListenRequested = true;
            IsListening = false;

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
            StopBroadcasting();
            StopListening();
        }

        private void SendBroadcastBeaconOnce()
        {
            if (broadcastClient == null) return;

            try
            {
                string localIp = GetPrimaryLocalIPv4();
                LanHostBeacon beacon = new LanHostBeacon
                {
                    protocol = NetworkProtocol.ProtocolId,
                    hostName = broadcastHostName,
                    hostIp = localIp,
                    tcpPort = broadcastTcpPort,
                    connectedStudents = studentCountProvider != null ? Mathf.Max(0, studentCountProvider()) : 0
                };

                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(beacon));

                HashSet<string> sentAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IPAddress target in GetBroadcastTargets())
                {
                    string key = target.ToString();
                    if (!sentAddresses.Add(key)) continue;

                    try
                    {
                        broadcastClient.Send(bytes, bytes.Length, new IPEndPoint(target, NetworkProtocol.DefaultDiscoveryPort));
                    }
                    catch
                    {
                        // Ignore unreachable adapter broadcast targets
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanDiscovery] Broadcast send warning: {e.Message}");
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
                        string json = Encoding.UTF8.GetString(data);
                        incomingBeacons.Enqueue(new DiscoveredHostPacket
                        {
                            senderIp = remoteEp.Address.ToString(),
                            rawJson = json
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

        private bool ProcessIncomingBeacon(string senderIp, string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return false;

            try
            {
                LanHostBeacon beacon = JsonUtility.FromJson<LanHostBeacon>(rawJson);
                if (beacon == null || beacon.protocol != NetworkProtocol.ProtocolId) return false;

                string resolvedIp = !string.IsNullOrWhiteSpace(senderIp) && senderIp != "0.0.0.0"
                    ? senderIp
                    : (!string.IsNullOrWhiteSpace(beacon.hostIp) ? beacon.hostIp : "127.0.0.1");

                // Prefer real LAN IP if loopback duplicate also arrived from same host
                float now = Time.realtimeSinceStartup;

                for (int i = 0; i < discoveredHosts.Count; i++)
                {
                    DiscoveredHostInfo existing = discoveredHosts[i];
                    bool sameEndpoint = existing.hostIp == resolvedIp && existing.tcpPort == beacon.tcpPort;
                    bool sameLocalHost = existing.tcpPort == beacon.tcpPort &&
                                         existing.hostName == beacon.hostName &&
                                         ((existing.hostIp == "127.0.0.1" && resolvedIp == beacon.hostIp) ||
                                          (resolvedIp == "127.0.0.1" && existing.hostIp == beacon.hostIp));

                    if (sameEndpoint || sameLocalHost)
                    {
                        bool changed = existing.hostName != beacon.hostName ||
                                       existing.connectedStudents != beacon.connectedStudents;
                        if (existing.hostIp == "127.0.0.1" && resolvedIp != "127.0.0.1")
                        {
                            existing.hostIp = resolvedIp;
                            changed = true;
                        }
                        existing.hostName = beacon.hostName;
                        existing.connectedStudents = beacon.connectedStudents;
                        existing.lastSeenRealtime = now;
                        return changed;
                    }
                }

                discoveredHosts.Add(new DiscoveredHostInfo
                {
                    hostName = beacon.hostName,
                    hostIp = resolvedIp,
                    tcpPort = beacon.tcpPort,
                    connectedStudents = beacon.connectedStudents,
                    lastSeenRealtime = now
                });
                return true;
            }
            catch
            {
                return false;
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
                        IPAddress mask = unicast.IPv4Mask;
                        if (mask == null) continue;

                        byte[] ipBytes = unicast.Address.GetAddressBytes();
                        byte[] maskBytes = mask.GetAddressBytes();
                        if (ipBytes.Length == 4 && maskBytes.Length == 4)
                        {
                            byte[] broadcastBytes = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                broadcastBytes[i] = (byte)(ipBytes[i] | (~maskBytes[i]));
                            }
                            targets.Add(new IPAddress(broadcastBytes));
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

        public static string GetPrimaryLocalIPv4()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    string desc = ni.Description ?? string.Empty;
                    if (desc.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        desc.IndexOf("Pseudo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                        {
                            return ip.Address.ToString();
                        }
                    }
                }

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
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
