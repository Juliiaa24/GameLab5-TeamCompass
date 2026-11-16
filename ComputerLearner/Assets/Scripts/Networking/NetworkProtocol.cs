/**
 * Author: TeamCompass
 * Description: Shared LAN protocol constants and JSON-serializable message envelopes/payloads
 *              for Teacher Host <-> Student Client communication.
 *              Contains zero gameplay dependencies beyond pure data models (StudentConfiguration, LearningPath, SkillScoreEntry).
 */
using System;
using System.Collections.Generic;

namespace ComputerLearning
{
    public static class NetworkProtocol
    {
        public const string ProtocolId = "CL-LAN-1";
        public const int DefaultDiscoveryPort = 47777;
        public const int DiscoveryPort = DefaultDiscoveryPort;
        public const int DefaultSessionPort = 47778;
        public const int DefaultClientCallbackPort = 47779;

        public const string MsgDiscoveryAck = "DISC_ACK";
        public const string UdpEnvelopePrefix = "CL_ENV:";

        public const string MsgHello = "HELLO";
        public const string MsgWelcome = "WELCOME";
        public const string MsgAssignConfig = "ASSIGN_CONFIG";
        public const string MsgClassroomPermissions = "CLASSROOM_PERMISSIONS";
        public const string MsgStartSession = "START_SESSION";
        public const string MsgResetStudent = "RESET_STUDENT";
        public const string MsgTelemetry = "TELEMETRY";
        public const string MsgPing = "PING";
        public const string MsgPong = "PONG";
    }

    [Serializable]
    public class NetworkEnvelope
    {
        public string type;
        public string payloadJson;
        public string packetId;

        public NetworkEnvelope() { }

        public NetworkEnvelope(string type, string payloadJson, string packetId = null)
        {
            this.type = type;
            this.payloadJson = payloadJson;
            this.packetId = packetId;
        }
    }

    [Serializable]
    public class LanHostBeacon
    {
        public string protocol = NetworkProtocol.ProtocolId;
        public string hostName = "Teacher PC";
        public string hostIp = string.Empty;
        public string[] alternateIps;
        public int tcpPort = NetworkProtocol.DefaultSessionPort;
        public int connectedStudents = 0;
    }

    [Serializable]
    public class LanDiscoveryAck
    {
        public string protocol = NetworkProtocol.ProtocolId;
        public string msgType = NetworkProtocol.MsgDiscoveryAck;
        public string clientUid;
        public int callbackTcpPort = NetworkProtocol.DefaultClientCallbackPort;
    }

    [Serializable]
    public class UdpEnvelopePacket
    {
        public string protocol = NetworkProtocol.ProtocolId;
        public string packetId;
        public string senderUid;
        public string targetUid;
        public int callbackTcpPort;
        public string type;
        public string payloadJson;
    }

    [Serializable]
    public class DiscoveredHostInfo
    {
        public string hostName;
        public string hostIp;
        public int tcpPort;
        public int connectedStudents;
        public List<string> candidateIps = new List<string>();
        [NonSerialized] public int lastSenderUdpPort;
        [NonSerialized] public float lastSeenRealtime;

        public string EndpointKey => $"{hostIp}:{tcpPort}";
    }

    [Serializable]
    public class ClientHelloPayload
    {
        public string protocol = NetworkProtocol.ProtocolId;
        public string clientUid;
        public string studentId;
        public string deviceName;
        public string activeScene;
        public LearningPath currentPath;
        public bool runAssessment;
    }

    [Serializable]
    public class HostWelcomePayload
    {
        public string hostName;
        public string assignedStudentId;
        public StudentConfiguration assignedConfiguration;
        public bool lockStudentNames;
        public bool lockLeaveClass;
    }

    [Serializable]
    public class AssignConfigPayload
    {
        public StudentConfiguration configuration;
        public bool startImmediately = false;
        public bool resetProgress = true;
        public bool lockStudentNames;
        public bool lockLeaveClass;
    }

    [Serializable]
    public class ClassroomPermissionsPayload
    {
        public string className;
        public bool lockStudentNames;
        public bool lockLeaveClass;
    }

    [Serializable]
    public class ResetStudentPayload
    {
        public string nextStudentId;
        public StudentConfiguration configuration;
    }

    [Serializable]
    public class StudentTelemetrySnapshot
    {
        public string clientUid;
        public string studentId;
        public string deviceName;
        public LearningPath learningPath;
        public bool runAssessment;
        public string currentScene;
        public bool assessmentCompleted;
        public float assessmentAverageScore;
        public float currentSkillsAverageScore;
        public int completedLevelsCount;
        public string currentNextLevelId;
        public string lastCompletedLevelId;
        public string lastMedal;
        public int goldMedalsCount;
        public int silverMedalsCount;
        public int bronzeMedalsCount;
        public int totalTasksAttempted;
        public int totalTasksSucceeded;
        public string lastUpdateTimestamp;
        public List<SkillScoreEntry> currentSkillScores = new List<SkillScoreEntry>();
    }
}
