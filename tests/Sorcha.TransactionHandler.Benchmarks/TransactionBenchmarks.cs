using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Serialization;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TransactionBenchmarks
{
    private CryptoModule _cryptoModule = null!;
    private HashProvider _hashProvider = null!;
    private string _privateKeyWif = null!;
    private Transaction _signedTransaction = null!;
    private BinaryTransactionSerializer _binarySerializer = null!;
    private JsonTransactionSerializer _jsonSerializer = null!;
    private byte[] _binaryData = null!;
    private string _jsonData = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _binarySerializer = new BinaryTransactionSerializer(_cryptoModule, _hashProvider);
        _jsonSerializer = new JsonTransactionSerializer(_cryptoModule, _hashProvider);

        // Generate a test wallet
        var keyManager = new KeyManager(_cryptoModule);
        var result = await keyManager.CreateMasterKeyRingAsync(WalletNetworks.ED25519);
        var keyRing = result.Value!;
        _privateKeyWif = Convert.ToBase64String(keyRing.MasterKeySet.PrivateKey.Key!);

        // Create a signed transaction for benchmarking
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider);
        var builderResult = await builder
            .Create(TransactionVersion.V4)
            .WithRecipients("ws1recipient1", "ws1recipient2")
            .WithMetadata("{\"type\": \"transfer\", \"amount\": 1000}")
            .AddPayload(new byte[1024], new[] { "ws1recipient1" })
            .SignAsync(_privateKeyWif);

        _signedTransaction = (Transaction)builderResult.Build().Value!;

        // Pre-serialize for deserialization benchmarks
        _binaryData = _binarySerializer.SerializeToBinary(_signedTransaction);
        _jsonData = _jsonSerializer.SerializeToJson(_signedTransaction);
    }

    [Benchmark]
    public async Task<Transaction> CreateTransaction()
    {
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider);
        var result = await builder
            .Create(TransactionVersion.V4)
            .WithRecipients("ws1recipient")
            .SignAsync(_privateKeyWif);

        return (Transaction)result.Build().Value!;
    }

    [Benchmark]
    public async Task<Transaction> CreateTransactionWithMetadata()
    {
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider);
        var result = await builder
            .Create(TransactionVersion.V4)
            .WithRecipients("ws1recipient")
            .WithMetadata("{\"type\": \"test\"}")
            .SignAsync(_privateKeyWif);

        return (Transaction)result.Build().Value!;
    }

    [Benchmark]
    public async Task<Transaction> CreateTransactionWithPayload()
    {
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider);
        var payloadData = new byte[1024]; // 1KB payload

        var result = await builder
            .Create(TransactionVersion.V4)
            .WithRecipients("ws1recipient")
            .AddPayload(payloadData, new[] { "ws1recipient" })
            .SignAsync(_privateKeyWif);

        return (Transaction)result.Build().Value!;
    }

    [Benchmark]
    public async Task<TransactionStatus> SignTransaction()
    {
        var payloadManager = new Payload.PayloadManager();
        var transaction = new Transaction(_cryptoModule, _hashProvider, payloadManager);
        transaction.Recipients = new[] { "ws1recipient" };

        return await transaction.SignAsync(_privateKeyWif);
    }

    [Benchmark]
    public async Task<TransactionStatus> VerifyTransaction()
    {
        return await _signedTransaction.VerifyAsync();
    }

    [Benchmark]
    public byte[] SerializeToBinary()
    {
        return _binarySerializer.SerializeToBinary(_signedTransaction);
    }

    [Benchmark]
    public string SerializeToJson()
    {
        return _jsonSerializer.SerializeToJson(_signedTransaction);
    }

    [Benchmark]
    public Transaction DeserializeFromBinary()
    {
        return (Transaction)_binarySerializer.DeserializeFromBinary(_binaryData);
    }

    [Benchmark]
    public Transaction DeserializeFromJson()
    {
        return (Transaction)_jsonSerializer.DeserializeFromJson(_jsonData);
    }

    [Benchmark]
    public async Task<Transaction> CompleteWorkflow()
    {
        // Complete workflow: Create -> Add Payload -> Sign -> Serialize -> Deserialize
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider);
        var payloadData = new byte[512];

        var builderResult = await builder
            .Create(TransactionVersion.V4)
            .WithRecipients("ws1recipient")
            .WithMetadata("{\"type\": \"benchmark\"}")
            .AddPayload(payloadData, new[] { "ws1recipient" })
            .SignAsync(_privateKeyWif);

        var transaction = (Transaction)builderResult.Build().Value!;

        // Serialize and deserialize
        var binary = _binarySerializer.SerializeToBinary(transaction);
        return (Transaction)_binarySerializer.DeserializeFromBinary(binary);
    }
}
