// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Sorcha.Peer.Service.Network;

/// <summary>
/// STUN client for NAT traversal and external address detection
/// Based on RFC 5389 (STUN - Session Traversal Utilities for NAT)
/// </summary>
public class StunClient
{
    private readonly ILogger<StunClient> _logger;
    private const int StunPort = 3478;
    private const ushort BindingRequest = 0x0001;
    private const ushort BindingResponse = 0x0101;
    private const ushort MappedAddress = 0x0001;
    private const ushort XorMappedAddress = 0x0020;

    public StunClient(ILogger<StunClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries a STUN server to discover the external IP address and port
    /// </summary>
    public async Task<StunResult?> QueryAsync(string stunServer, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Querying STUN server: {Server}", stunServer);

            // Parse server address
            var parts = stunServer.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : StunPort;

            // Resolve server address
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            var serverAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            if (serverAddress == null)
            {
                _logger.LogWarning("Could not resolve STUN server: {Server}", stunServer);
                return null;
            }

            var serverEndpoint = new IPEndPoint(serverAddress, port);

            // Create UDP socket
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 5000;

            // Build STUN binding request
            var transactionId = GenerateTransactionId();
            var request = BuildBindingRequest(transactionId);

            // Send request
            await udpClient.SendAsync(request, request.Length, serverEndpoint);
            _logger.LogTrace("Sent STUN binding request to {Endpoint}", serverEndpoint);

            // Receive response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(5000);

            var receiveTask = udpClient.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(5000, cts.Token));

            if (completedTask != receiveTask)
            {
                _logger.LogWarning("STUN request timeout for server: {Server}", stunServer);
                return null;
            }

            var response = await receiveTask;
            _logger.LogTrace("Received STUN response from {Endpoint}, {Length} bytes",
                response.RemoteEndPoint, response.Buffer.Length);

            // Parse response
            var result = ParseBindingResponse(response.Buffer, transactionId);

            if (result != null)
            {
                _logger.LogInformation("STUN query successful: {Address}:{Port}",
                    result.PublicAddress, result.PublicPort);
            }

            return result;
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Socket error querying STUN server: {Server}", stunServer);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying STUN server: {Server}", stunServer);
            return null;
        }
    }

    /// <summary>
    /// Determines the NAT type by performing multiple STUN queries
    /// </summary>
    public async Task<NatType> DetermineNatTypeAsync(string stunServer, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform basic STUN query
            var result = await QueryAsync(stunServer, cancellationToken);

            if (result == null)
            {
                return NatType.Unknown;
            }

            // Get local address
            var localAddress = GetLocalIPAddress();
            if (localAddress == null)
            {
                return NatType.Unknown;
            }

            // Compare local and public addresses
            if (result.PublicAddress == localAddress)
            {
                _logger.LogInformation("No NAT detected - public IP matches local IP");
                return NatType.None;
            }

            // Behind NAT - for now we classify as symmetric or full cone
            // A full NAT type detection would require multiple STUN servers
            // and change port/IP tests (RFC 5780)
            _logger.LogInformation("NAT detected - simplified detection indicates FullCone");
            return NatType.FullCone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining NAT type");
            return NatType.Unknown;
        }
    }

    /// <summary>
    /// Builds a STUN binding request packet
    /// </summary>
    private byte[] BuildBindingRequest(byte[] transactionId)
    {
        var packet = new byte[20]; // STUN header is 20 bytes

        // Message Type (Binding Request = 0x0001)
        packet[0] = 0x00;
        packet[1] = 0x01;

        // Message Length (0 for no attributes)
        packet[2] = 0x00;
        packet[3] = 0x00;

        // Magic Cookie (0x2112A442)
        packet[4] = 0x21;
        packet[5] = 0x12;
        packet[6] = 0xA4;
        packet[7] = 0x42;

        // Transaction ID (12 bytes)
        Array.Copy(transactionId, 0, packet, 8, 12);

        return packet;
    }

    /// <summary>
    /// Parses a STUN binding response packet
    /// </summary>
    private StunResult? ParseBindingResponse(byte[] data, byte[] expectedTransactionId)
    {
        if (data.Length < 20)
        {
            _logger.LogWarning("STUN response too short: {Length} bytes", data.Length);
            return null;
        }

        // Verify message type (Binding Response = 0x0101)
        var messageType = (ushort)((data[0] << 8) | data[1]);
        if (messageType != BindingResponse)
        {
            _logger.LogWarning("Invalid STUN message type: 0x{Type:X4}", messageType);
            return null;
        }

        // Verify transaction ID
        var transactionId = new byte[12];
        Array.Copy(data, 8, transactionId, 0, 12);
        if (!transactionId.SequenceEqual(expectedTransactionId))
        {
            _logger.LogWarning("Transaction ID mismatch");
            return null;
        }

        // Get message length
        var messageLength = (ushort)((data[2] << 8) | data[3]);

        // Parse attributes
        var offset = 20;
        while (offset < 20 + messageLength && offset + 4 <= data.Length)
        {
            var attrType = (ushort)((data[offset] << 8) | data[offset + 1]);
            var attrLength = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
            offset += 4;

            if (offset + attrLength > data.Length)
                break;

            // Check for XOR-MAPPED-ADDRESS or MAPPED-ADDRESS
            if (attrType == XorMappedAddress && attrLength >= 8)
            {
                return ParseXorMappedAddress(data, offset);
            }
            else if (attrType == MappedAddress && attrLength >= 8)
            {
                return ParseMappedAddress(data, offset);
            }

            // Move to next attribute (attributes are padded to 4-byte boundary)
            offset += attrLength;
            if (attrLength % 4 != 0)
                offset += 4 - (attrLength % 4);
        }

        _logger.LogWarning("No mapped address found in STUN response");
        return null;
    }

    /// <summary>
    /// Parses XOR-MAPPED-ADDRESS attribute
    /// </summary>
    private StunResult? ParseXorMappedAddress(byte[] data, int offset)
    {
        var family = data[offset + 1];
        if (family != 0x01) // IPv4
        {
            _logger.LogWarning("Unsupported address family: {Family}", family);
            return null;
        }

        // XOR port with magic cookie high 16 bits (0x2112)
        var xorPort = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        var port = (ushort)(xorPort ^ 0x2112);

        // XOR address with magic cookie (0x2112A442)
        var xorAddress = new byte[4];
        Array.Copy(data, offset + 4, xorAddress, 0, 4);
        var address = new byte[4];
        address[0] = (byte)(xorAddress[0] ^ 0x21);
        address[1] = (byte)(xorAddress[1] ^ 0x12);
        address[2] = (byte)(xorAddress[2] ^ 0xA4);
        address[3] = (byte)(xorAddress[3] ^ 0x42);

        var ipAddress = new IPAddress(address);

        return new StunResult
        {
            PublicAddress = ipAddress.ToString(),
            PublicPort = port,
            NatType = NatType.Unknown // Determined separately
        };
    }

    /// <summary>
    /// Parses MAPPED-ADDRESS attribute
    /// </summary>
    private StunResult? ParseMappedAddress(byte[] data, int offset)
    {
        var family = data[offset + 1];
        if (family != 0x01) // IPv4
        {
            _logger.LogWarning("Unsupported address family: {Family}", family);
            return null;
        }

        var port = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        var address = new byte[4];
        Array.Copy(data, offset + 4, address, 0, 4);
        var ipAddress = new IPAddress(address);

        return new StunResult
        {
            PublicAddress = ipAddress.ToString(),
            PublicPort = port,
            NatType = NatType.Unknown
        };
    }

    /// <summary>
    /// Generates a random 12-byte transaction ID
    /// </summary>
    private byte[] GenerateTransactionId()
    {
        var id = new byte[12];
        Random.Shared.NextBytes(id);
        return id;
    }

    /// <summary>
    /// Gets the local IP address
    /// </summary>
    private string? GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var localAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return localAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Result of a STUN query
/// </summary>
public class StunResult
{
    public string PublicAddress { get; set; } = string.Empty;
    public int PublicPort { get; set; }
    public NatType NatType { get; set; }
}

/// <summary>
/// Types of NAT (Network Address Translation)
/// </summary>
public enum NatType
{
    Unknown,
    None,           // No NAT, direct internet connection
    FullCone,       // Full cone NAT (easiest to traverse)
    RestrictedCone, // Restricted cone NAT
    PortRestricted, // Port-restricted cone NAT
    Symmetric       // Symmetric NAT (hardest to traverse, requires TURN)
}
