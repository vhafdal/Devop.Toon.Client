#nullable enable
using DevOp.Toon.Core;
using DevOp.Toon;

namespace DevOp.Toon.Client;

/// <summary>
/// Per-request response encoding overrides sent through <c>X-Toon-Option-*</c> headers.
/// </summary>
public sealed class ToonResponseEncodeOverrideOptions
{
    /// <summary>
    /// Overrides whether the server should indent the TOON response.
    /// </summary>
    public bool? Indent { get; set; }

    /// <summary>
    /// Overrides the delimiter used when the server writes the TOON response.
    /// </summary>
    public ToonDelimiter? Delimiter { get; set; }

    /// <summary>
    /// Overrides the key folding strategy used for the TOON response.
    /// </summary>
    public ToonKeyFolding? KeyFolding { get; set; }

    /// <summary>
    /// Overrides the flatten depth used for the TOON response.
    /// </summary>
    public int? FlattenDepth { get; set; }

    /// <summary>
    /// Overrides the object array layout used for the TOON response.
    /// </summary>
    public ToonObjectArrayLayout? ObjectArrayLayout { get; set; }

    /// <summary>
    /// Overrides whether the server should ignore null or empty values in the TOON response.
    /// </summary>
    public bool? IgnoreNullOrEmpty { get; set; }

    /// <summary>
    /// Overrides whether the server should exclude empty arrays in the TOON response.
    /// </summary>
    public bool? ExcludeEmptyArrays { get; set; }

    internal ToonResponseEncodeOverrideOptions Clone()
    {
        return new ToonResponseEncodeOverrideOptions
        {
            Indent = Indent,
            Delimiter = Delimiter,
            KeyFolding = KeyFolding,
            FlattenDepth = FlattenDepth,
            ObjectArrayLayout = ObjectArrayLayout,
            IgnoreNullOrEmpty = IgnoreNullOrEmpty,
            ExcludeEmptyArrays = ExcludeEmptyArrays
        };
    }
}
