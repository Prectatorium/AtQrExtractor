namespace AtQrExtractor.Models;

/// <summary>
/// Represents a raw QR code payload extracted from a fiscal document.
/// </summary>
/// <remarks>
/// Contains the unprocessed UTF-8 data string along with a SHA-256 hash for deduplication
/// and source file tracking. This is the initial data structure before validation against
/// Portaria n.º 195/2020 specifications.
/// </remarks>
public sealed class QrPayload
{
    /// <summary>
    /// Gets or sets the raw UTF-8 encoded data extracted from the QR code.
    /// </summary>
    /// <value>
    /// The unprocessed payload string in AT format (e.g., "A:123456789*B:987654321*PT*D:FT*...").
    /// </value>
    public string RawData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA-256 hash computed from the raw payload.
    /// </summary>
    /// <value>
    /// A 64-character hexadecimal string used for deduplication across multiple files.
    /// </value>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source file paths where this QR code was detected.
    /// </summary>
    /// <value>
    /// A list of absolute file paths. Multiple entries indicate the same QR code
    /// appeared in different documents.
    /// </value>
    public List<string> SourceFiles { get; set; } = new();
}

/// <summary>
/// Represents the processing outcome for a single input file.
/// </summary>
/// <remarks>
/// Aggregates all QR payloads found in a file along with any errors encountered
/// during extraction or decoding operations.
/// </remarks>
public sealed class FileProcessingResult
{
    /// <summary>
    /// Gets or sets the absolute path of the processed file.
    /// </summary>
    /// <value>
    /// The full file system path of the source document (PDF or image).
    /// </value>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets all QR payloads extracted from this file.
    /// </summary>
    /// <value>
    /// A list containing zero or more QR payloads. Empty when no QR codes were detected.
    /// </value>
    public List<QrPayload> QrPayloads { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages encountered during file processing.
    /// </summary>
    /// <value>
    /// A list of error descriptions. Empty indicates successful processing without errors.
    /// </value>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Represents a fully validated and interpreted AT QR code payload.
/// </summary>
/// <remarks>
/// Contains structured field-level data parsed according to Portaria n.º 195/2020
/// specifications, including compliance validation results and detailed field analysis.
/// This is the primary output structure for Excel report generation.
/// </remarks>
public sealed class InterpretedQrData
{
    /// <summary>
    /// Gets or sets the unique SHA-256 hash identifier.
    /// </summary>
    /// <value>
    /// A 64-character hexadecimal string matching the hash from the original <see cref="QrPayload"/>.
    /// </value>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original unprocessed payload string.
    /// </summary>
    /// <value>
    /// The raw QR code data before parsing and validation.
    /// </value>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this QR code meets all AT compliance requirements.
    /// </summary>
    /// <value>
    /// <c>true</c> if all mandatory fields are present and valid according to Portaria n.º 195/2020;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// Gets or sets the parsed and validated field collection.
    /// </summary>
    /// <value>
    /// All fields (mandatory and optional) ordered by their sequence in the specification.
    /// Missing fields are represented with empty values.
    /// </value>
    public List<QrField> Fields { get; set; } = new();

    /// <summary>
    /// Gets or sets compliance validation notes and issues.
    /// </summary>
    /// <value>
    /// Human-readable descriptions of validation failures, missing mandatory fields,
    /// or format violations. Empty when <see cref="IsCompliant"/> is <c>true</c>.
    /// </value>
    public List<string> ComplianceNotes { get; set; } = new();

    /// <summary>
    /// Gets or sets the source file paths where this QR code was found.
    /// </summary>
    /// <value>
    /// Absolute file paths for traceability. Multiple entries indicate duplication across files.
    /// </value>
    public List<string> SourceFiles { get; set; } = new();
}

/// <summary>
/// Represents a single validated field within an AT QR code payload.
/// </summary>
/// <remarks>
/// Each field follows the Portaria n.º 195/2020 structure: a single-letter identifier (A-S)
/// followed by its value. Fields are classified as mandatory (marked with +) or optional (marked with ++).
/// </remarks>
public sealed class QrField
{
    /// <summary>
    /// Gets or sets the field identifier code.
    /// </summary>
    /// <value>
    /// A single letter (A-S) or numbered identifier (I1-I8, J1-J8, K1-K8) as defined
    /// in the AT specification.
    /// </value>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable field description.
    /// </summary>
    /// <value>
    /// A descriptive name following Portaria n.º 195/2020 terminology
    /// (e.g., "Tax Registration Number (Seller)" for field A).
    /// </value>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed field value.
    /// </summary>
    /// <value>
    /// The extracted value from the QR payload. Empty string for missing optional fields.
    /// </value>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this field is required by the AT specification.
    /// </summary>
    /// <value>
    /// <c>true</c> for mandatory fields (marked with + in specification);
    /// <c>false</c> for optional fields (marked with ++).
    /// </value>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed byte length for this field.
    /// </summary>
    /// <value>
    /// The byte length limit from the specification. Zero indicates no limit.
    /// </value>
    public int MaxLengthBytes { get; set; }

    /// <summary>
    /// Gets or sets the actual byte length of the field value.
    /// </summary>
    /// <value>
    /// The UTF-8 encoded byte count, which may differ from character count
    /// for non-ASCII characters.
    /// </value>
    public int ActualLengthBytes { get; set; }

    /// <summary>
    /// Gets or sets whether this field passes all validation rules.
    /// </summary>
    /// <value>
    /// <c>true</c> if the value meets format, length, and pattern requirements;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation result message.
    /// </summary>
    /// <value>
    /// A description of validation failure, or empty string if <see cref="IsValid"/> is <c>true</c>.
    /// </value>
    public string ValidationMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based position of this field in the original payload.
    /// </summary>
    /// <value>
    /// The sequential order used for consistent sorting and display.
    /// </value>
    public int SequenceIndex { get; set; }
}