namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a peer node in the network.
/// </summary>
public class PeerInfo
{
    public string PeerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> SupportedProtocols { get; set; } = new();
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public int FailureCount { get; set; }
    public bool IsBootstrapNode { get; set; }
    public int AverageLatencyMs { get; set; }
}

/// <summary>
/// Peer network statistics response.
/// </summary>
public class PeerServiceStatistics
{
    public DateTimeOffset Timestamp { get; set; }
    public PeerStatistics PeerStats { get; set; } = new();
    public QualityStatistics QualityStats { get; set; } = new();
    public QueueStatistics QueueStats { get; set; } = new();
    public CircuitBreakerSummary CircuitBreakerStats { get; set; } = new();
}

public class PeerStatistics
{
    public int TotalPeers { get; set; }
    public int HealthyPeers { get; set; }
    public int UnhealthyPeers { get; set; }
    public int BootstrapNodes { get; set; }
    public double AverageLatencyMs { get; set; }
    public int TotalFailures { get; set; }
}

public class QualityStatistics
{
    public int TotalTrackedPeers { get; set; }
    public int ExcellentPeers { get; set; }
    public int GoodPeers { get; set; }
    public int FairPeers { get; set; }
    public int PoorPeers { get; set; }
    public double AverageQualityScore { get; set; }
}

public class QueueStatistics
{
    public int QueueSize { get; set; }
    public bool IsEmpty { get; set; }
}

public class CircuitBreakerSummary
{
    public int TotalCircuitBreakers { get; set; }
    public int OpenCircuits { get; set; }
    public int HalfOpenCircuits { get; set; }
    public int ClosedCircuits { get; set; }
}

/// <summary>
/// Peer health response.
/// </summary>
public class PeerHealthResponse
{
    public int TotalPeers { get; set; }
    public int HealthyPeers { get; set; }
    public int UnhealthyPeers { get; set; }
    public double HealthPercentage { get; set; }
    public List<HealthyPeerInfo> Peers { get; set; } = new();
}

public class HealthyPeerInfo
{
    public string PeerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public int AverageLatencyMs { get; set; }
}
