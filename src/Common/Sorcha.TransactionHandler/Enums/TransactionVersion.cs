namespace Sorcha.TransactionHandler.Enums;

/// <summary>
/// Specifies the transaction version.
/// </summary>
public enum TransactionVersion : uint
{
    /// <summary>
    /// Transaction version 1
    /// </summary>
    V1 = 1,

    /// <summary>
    /// Transaction version 2
    /// </summary>
    V2 = 2,

    /// <summary>
    /// Transaction version 3
    /// </summary>
    V3 = 3,

    /// <summary>
    /// Transaction version 4 (current version)
    /// </summary>
    V4 = 4
}
