namespace AtQrExtractor.Services;

using System.Text;
using System.Text.RegularExpressions;
using AtQrExtractor.Models;
using Serilog;

/// <summary>
/// Interprets and validates AT QR code payloads against Portaria n.º 195/2020 specifications.
/// </summary>
/// <remarks>
/// <para>
/// Parses raw QR code payloads from Portuguese fiscal documents, validates them against
/// regulatory requirements, and produces structured field-level analysis with compliance reporting.
/// </para>
/// <para><b>QR Code Format:</b></para>
/// <code>A:123456789*B:987654321*PT*D:FT*E:N*F:20241220*G:2024/1*H:12345*...</code>
/// <para>Fields use a colon separator between code and value, with asterisks (*) delimiting fields.</para>
/// <para><b>Field Categories:</b></para>
/// <list type="bullet">
///   <item><description><b>Mandatory (A-H, N, O, Q, R):</b> Required for all documents</description></item>
///   <item><description><b>Tax Region 1 (I1-I8):</b> First regional tax breakdown (optional)</description></item>
///   <item><description><b>Tax Region 2 (J1-J8):</b> Second regional tax breakdown (optional)</description></item>
///   <item><description><b>Tax Region 3 (K1-K8):</b> Third regional tax breakdown (optional)</description></item>
///   <item><description><b>Optional (L, M, P, S):</b> Additional document information</description></item>
/// </list>
/// <para>At least one tax region (I1, J1, or K1) must be present per specification.</para>
/// </remarks>
public sealed class PortariaInterpreter
{
    /// <summary>
    /// Defines validation rules and metadata for a single QR field specification.
    /// </summary>
    private sealed class FieldDefinition
    {
        public string Code { get; }
        public string Description { get; }
        public bool IsMandatory { get; }
        public int MaxLengthBytes { get; }
        public int SequenceIndex { get; }
        public string? FormatPattern { get; }

        public FieldDefinition(string code, string description, bool isMandatory, int maxLengthBytes, int sequenceIndex, string? formatPattern = null)
        {
            Code = code;
            Description = description;
            IsMandatory = isMandatory;
            MaxLengthBytes = maxLengthBytes;
            SequenceIndex = sequenceIndex;
            FormatPattern = formatPattern;
        }
    }

    /// <summary>
    /// Field definitions from Portaria n.º 195/2020 (Version 1.1, October 2020).
    /// </summary>
    /// <remarks>
    /// Contains all recognized field codes with their metadata including mandatory status,
    /// maximum byte lengths, regex validation patterns, and sequential ordering.
    /// </remarks>
    private static readonly Dictionary<string, FieldDefinition> FieldDefinitions = new()
    {
        ["A"] = new("A", "Tax Registration Number (Seller)", true, 9, 0),
        ["B"] = new("B", "Tax Registration Number (Buyer)", true, 30, 1),
        ["C"] = new("C", "Country Code (Buyer)", true, 12, 2),
        ["D"] = new("D", "Document Type", true, 2, 3, @"^(FT|FS|FR|ND|NC|VD|TV|TD|AA|DA|GR|GT|GA|GC|GD|CM|CC|FC|FO|NE|OU|OR|PF|RP|RE|CS|LD|RA)$"),
        ["E"] = new("E", "Document Status", true, 1, 4, @"^[NSAR]$"),
        ["F"] = new("F", "Document Date", true, 8, 5, @"^\d{8}$"),
        ["G"] = new("G", "Document ID", true, 60, 6),
        ["H"] = new("H", "ATCUD", true, 70, 7),

        ["I1"] = new("I1", "Tax Region 1 - Fiscal Space", false, 5, 8),
        ["I2"] = new("I2", "Tax Region 1 - Tax Base (Exempt)", false, 16, 9, @"^\d+\.\d{2}$"),
        ["I3"] = new("I3", "Tax Region 1 - Tax Base (Reduced Rate)", false, 16, 10, @"^\d+\.\d{2}$"),
        ["I4"] = new("I4", "Tax Region 1 - Tax Total (Reduced Rate)", false, 16, 11, @"^\d+\.\d{2}$"),
        ["I5"] = new("I5", "Tax Region 1 - Tax Base (Intermediate Rate)", false, 16, 12, @"^\d+\.\d{2}$"),
        ["I6"] = new("I6", "Tax Region 1 - Tax Total (Intermediate Rate)", false, 16, 13, @"^\d+\.\d{2}$"),
        ["I7"] = new("I7", "Tax Region 1 - Tax Base (Normal Rate)", false, 16, 14, @"^\d+\.\d{2}$"),
        ["I8"] = new("I8", "Tax Region 1 - Tax Total (Normal Rate)", false, 16, 15, @"^\d+\.\d{2}$"),

        ["J1"] = new("J1", "Tax Region 2 - Fiscal Space", false, 5, 16),
        ["J2"] = new("J2", "Tax Region 2 - Tax Base (Exempt)", false, 16, 17, @"^\d+\.\d{2}$"),
        ["J3"] = new("J3", "Tax Region 2 - Tax Base (Reduced Rate)", false, 16, 18, @"^\d+\.\d{2}$"),
        ["J4"] = new("J4", "Tax Region 2 - Tax Total (Reduced Rate)", false, 16, 19, @"^\d+\.\d{2}$"),
        ["J5"] = new("J5", "Tax Region 2 - Tax Base (Intermediate Rate)", false, 16, 20, @"^\d+\.\d{2}$"),
        ["J6"] = new("J6", "Tax Region 2 - Tax Total (Intermediate Rate)", false, 16, 21, @"^\d+\.\d{2}$"),
        ["J7"] = new("J7", "Tax Region 2 - Tax Base (Normal Rate)", false, 16, 22, @"^\d+\.\d{2}$"),
        ["J8"] = new("J8", "Tax Region 2 - Tax Total (Normal Rate)", false, 16, 23, @"^\d+\.\d{2}$"),

        ["K1"] = new("K1", "Tax Region 3 - Fiscal Space", false, 5, 24),
        ["K2"] = new("K2", "Tax Region 3 - Tax Base (Exempt)", false, 16, 25, @"^\d+\.\d{2}$"),
        ["K3"] = new("K3", "Tax Region 3 - Tax Base (Reduced Rate)", false, 16, 26, @"^\d+\.\d{2}$"),
        ["K4"] = new("K4", "Tax Region 3 - Tax Total (Reduced Rate)", false, 16, 27, @"^\d+\.\d{2}$"),
        ["K5"] = new("K5", "Tax Region 3 - Tax Base (Intermediate Rate)", false, 16, 28, @"^\d+\.\d{2}$"),
        ["K6"] = new("K6", "Tax Region 3 - Tax Total (Intermediate Rate)", false, 16, 29, @"^\d+\.\d{2}$"),
        ["K7"] = new("K7", "Tax Region 3 - Tax Base (Normal Rate)", false, 16, 30, @"^\d+\.\d{2}$"),
        ["K8"] = new("K8", "Tax Region 3 - Tax Total (Normal Rate)", false, 16, 31, @"^\d+\.\d{2}$"),

        ["L"] = new("L", "Non-taxable / Not subject to VAT / Other situations", false, 16, 32, @"^\d+\.\d{2}$"),
        ["M"] = new("M", "Stamp Duty", false, 16, 33, @"^\d+\.\d{2}$"),
        ["N"] = new("N", "Total Taxes (VAT + Stamp Duty)", true, 16, 34, @"^\d+\.\d{2}$"),
        ["O"] = new("O", "Grand Total with Taxes", true, 16, 35, @"^\d+\.\d{2}$"),
        ["P"] = new("P", "Withholding Tax", false, 16, 36, @"^\d+\.\d{2}$"),
        ["Q"] = new("Q", "Hash Segment (4 chars)", true, 4, 37),
        ["R"] = new("R", "Certificate Number", true, 4, 38),
        ["S"] = new("S", "Other Information", false, 65, 39)
    };

    /// <summary>
    /// Interprets and validates a raw QR payload against AT specifications.
    /// </summary>
    /// <param name="payload">The QR payload to interpret, containing raw data and metadata.</param>
    /// <returns>
    /// An <see cref="InterpretedQrData"/> object with parsed fields, validation results,
    /// and compliance status.
    /// </returns>
    /// <remarks>
    /// Performs UTF-8 validation, field parsing, mandatory field checking, format validation,
    /// and tax region consistency verification. All validation issues are captured in the
    /// <see cref="InterpretedQrData.ComplianceNotes"/> collection.
    /// </remarks>
    public InterpretedQrData Interpret(QrPayload payload)
    {
        Log.Debug("Interpreting QR payload: {Hash}", payload.Hash);

        var result = new InterpretedQrData
        {
            Hash = payload.Hash,
            RawPayload = payload.RawData,
            SourceFiles = payload.SourceFiles
        };

        try
        {
            if (!IsValidUtf8(payload.RawData))
            {
                result.IsCompliant = false;
                result.ComplianceNotes.Add("Payload is not valid UTF-8");
                return result;
            }

            var fields = ParseFields(payload.RawData);
            var validationErrors = ValidateStructure(fields);

            result.IsCompliant = !validationErrors.Any();
            result.ComplianceNotes.AddRange(validationErrors);

            foreach (var def in FieldDefinitions.Values.OrderBy(d => d.SequenceIndex))
            {
                var fieldValue = fields.ContainsKey(def.Code) ? fields[def.Code] : string.Empty;
                result.Fields.Add(CreateField(def, fieldValue));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error interpreting QR payload");
            result.IsCompliant = false;
            result.ComplianceNotes.Add($"Critical error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Parses raw QR payload into a dictionary of field code-value pairs.
    /// </summary>
    /// <param name="payload">The raw payload string to parse (e.g., "A:123*B:456*...").</param>
    /// <returns>
    /// A dictionary mapping field codes to their extracted values. Fields without
    /// colon separators are skipped.
    /// </returns>
    /// <remarks>
    /// Fields are separated by asterisks (*). Each field uses colon (:) to separate
    /// the code from its value. Only fields with valid colon separators are included.
    /// </remarks>
    private Dictionary<string, string> ParseFields(string payload)
    {
        var fields = new Dictionary<string, string>();
        var parts = payload.Split('*', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = part.Substring(0, colonIndex).Trim();
                var value = part.Substring(colonIndex + 1);
                fields[key] = value;
            }
        }

        return fields;
    }

    /// <summary>
    /// Validates the complete field structure against AT requirements.
    /// </summary>
    /// <param name="fields">The parsed field dictionary to validate.</param>
    /// <returns>
    /// A list of validation error messages. An empty list indicates full compliance.
    /// </returns>
    /// <remarks>
    /// Performs comprehensive validation including: mandatory field presence, field format
    /// and length validation, tax region requirements (at least one I1/J1/K1 must exist),
    /// tax region consistency checks, and special field constraints (e.g., field S cannot contain asterisks).
    /// </remarks>
    private List<string> ValidateStructure(Dictionary<string, string> fields)
    {
        var errors = new List<string>();

        foreach (var def in FieldDefinitions.Values.Where(d => d.IsMandatory))
        {
            bool hasField = fields.ContainsKey(def.Code);
            string val = hasField ? fields[def.Code] : string.Empty;

            if (!hasField || string.IsNullOrWhiteSpace(val))
            {
                errors.Add($"Missing mandatory field: {def.Code} ({def.Description})");
                continue;
            }

            if (!ValidateField(def, val, out string? fieldError))
            {
                errors.Add(fieldError!);
            }
        }

        foreach (var def in FieldDefinitions.Values.Where(d => !d.IsMandatory))
        {
            if (fields.ContainsKey(def.Code))
            {
                string val = fields[def.Code];
                if (!string.IsNullOrEmpty(val) && !ValidateField(def, val, out string? fieldError))
                {
                    errors.Add(fieldError!);
                }
            }
        }

        bool hasI1 = fields.ContainsKey("I1") && !string.IsNullOrWhiteSpace(fields["I1"]);
        bool hasJ1 = fields.ContainsKey("J1") && !string.IsNullOrWhiteSpace(fields["J1"]);
        bool hasK1 = fields.ContainsKey("K1") && !string.IsNullOrWhiteSpace(fields["K1"]);

        if (!hasI1 && !hasJ1 && !hasK1)
        {
            errors.Add("At least one tax region (I1, J1, or K1) must be specified");
        }

        ValidateTaxRegionConsistency(fields, errors, "I");
        ValidateTaxRegionConsistency(fields, errors, "J");
        ValidateTaxRegionConsistency(fields, errors, "K");

        if (fields.ContainsKey("S") && fields["S"].Contains('*'))
        {
            errors.Add("Field S (Other Information) cannot contain asterisk (*)");
        }

        return errors;
    }

    /// <summary>
    /// Validates consistency within a tax region's field group.
    /// </summary>
    /// <param name="fields">The parsed field dictionary.</param>
    /// <param name="errors">The error list to append validation issues to.</param>
    /// <param name="region">The region identifier ("I", "J", or "K").</param>
    /// <remarks>
    /// Checks that if a tax region identifier (X1) is present, it either contains "0"
    /// (indicating no tax rate) or has accompanying tax base/total values (X2-X8).
    /// Logs warnings when regions are declared but contain no tax values.
    /// </remarks>
    private void ValidateTaxRegionConsistency(Dictionary<string, string> fields, List<string> errors, string region)
    {
        bool hasRegion = fields.ContainsKey($"{region}1") && !string.IsNullOrWhiteSpace(fields[$"{region}1"]);

        if (hasRegion)
        {
            if (region == "I" && fields["I1"] == "0")
            {
                return;
            }

            bool hasTaxValues = false;
            for (int i = 2; i <= 8; i++)
            {
                string fieldCode = $"{region}{i}";
                if (fields.ContainsKey(fieldCode) && !string.IsNullOrWhiteSpace(fields[fieldCode]))
                {
                    hasTaxValues = true;
                    break;
                }
            }

            if (!hasTaxValues)
            {
                Log.Warning("Tax region {Region}1 specified but no tax values (base/totals) provided", region);
            }
        }
    }

    /// <summary>
    /// Validates a single field value against its definition rules.
    /// </summary>
    /// <param name="def">The field definition containing validation rules.</param>
    /// <param name="value">The field value to validate.</param>
    /// <param name="error">
    /// When validation fails, contains a descriptive error message; otherwise <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value passes all validation rules; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Validates byte length constraints and format patterns (regex). Byte length is
    /// computed using UTF-8 encoding to properly handle multi-byte characters.
    /// </remarks>
    private bool ValidateField(FieldDefinition def, string value, out string? error)
    {
        error = null;

        int actualLength = Encoding.UTF8.GetByteCount(value);
        if (actualLength > def.MaxLengthBytes)
        {
            error = $"Field {def.Code} exceeds max length of {def.MaxLengthBytes} bytes (actual: {actualLength})";
            return false;
        }

        if (!string.IsNullOrEmpty(def.FormatPattern) && !Regex.IsMatch(value, def.FormatPattern))
        {
            string customMsg = GetCustomFormatErrorMessage(def.Code);
            error = $"Field {def.Code} invalid: {customMsg} (Received: '{value}')";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Generates a human-readable format validation error message.
    /// </summary>
    /// <param name="code">The field code that failed format validation.</param>
    /// <returns>
    /// A descriptive message explaining the expected format for the field.
    /// </returns>
    private string GetCustomFormatErrorMessage(string code)
    {
        return code switch
        {
            "D" => "Must be a valid document type (FT, FS, FR, ND, NC, VD, TV, TD, AA, DA, GR, GT, GA, GC, GD, CM, CC, FC, FO, NE, OU, OR, PF, RP, RE, CS, LD, RA)",
            "E" => "Must be N (Normal), S (Self-billing), A (Annulled), or R (Replacement)",
            "F" => "Must be in YYYYMMDD format",
            "I2" or "I3" or "I4" or "I5" or "I6" or "I7" or "I8" or
            "J2" or "J3" or "J4" or "J5" or "J6" or "J7" or "J8" or
            "K2" or "K3" or "K4" or "K5" or "K6" or "K7" or "K8" or
            "L" or "M" or "N" or "O" or "P"
                => "Numeric value must use '.' as decimal separator with exactly 2 decimal places",
            _ => "Invalid format"
        };
    }

    /// <summary>
    /// Creates a <see cref="QrField"/> with validation applied.
    /// </summary>
    /// <param name="def">The field definition containing metadata and validation rules.</param>
    /// <param name="value">The raw field value from the payload, or empty string if missing.</param>
    /// <returns>
    /// A fully populated <see cref="QrField"/> object with validation status and messages.
    /// </returns>
    /// <remarks>
    /// Computes byte length, applies validation rules, and populates all field properties
    /// including validation status and error messages.
    /// </remarks>
    private QrField CreateField(FieldDefinition def, string value)
    {
        var actualLength = Encoding.UTF8.GetByteCount(value);
        bool isValid = true;
        string msg = string.Empty;

        if (def.IsMandatory && string.IsNullOrEmpty(value))
        {
            isValid = false;
            msg = "Field is mandatory";
        }
        else if (!string.IsNullOrEmpty(value))
        {
            if (!ValidateField(def, value, out string? error))
            {
                isValid = false;
                msg = error ?? "Invalid format";
            }
        }

        return new QrField
        {
            Code = def.Code,
            Description = def.Description,
            Value = value,
            IsMandatory = def.IsMandatory,
            MaxLengthBytes = def.MaxLengthBytes,
            ActualLengthBytes = actualLength,
            IsValid = isValid,
            ValidationMessage = msg,
            SequenceIndex = def.SequenceIndex
        };
    }

    /// <summary>
    /// Validates that a string contains valid UTF-8 encoded characters.
    /// </summary>
    /// <param name="text">The string to validate.</param>
    /// <returns>
    /// <c>true</c> if the string round-trips correctly through UTF-8 encoding;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Tests for valid UTF-8 by encoding to bytes and decoding back to string,
    /// verifying the result matches the original.
    /// </remarks>
    private static bool IsValidUtf8(string text)
    {
        try
        {
            return text == Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text));
        }
        catch { return false; }
    }
}