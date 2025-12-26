# AtQrExtractor

AT QR Code Extractor is a command-line tool and desktop application for extracting and validating QR codes from Portuguese fiscal documents (invoices, receipts, and other ATCUD-compliant documents) according to the specifications defined in Portaria n.º 195/2020.

## Features

- **Multi-format Support**: Process PDF files and images (PNG, JPEG, BMP, TIFF)
- **Batch Processing**: Handle multiple files and directories in a single operation
- **QR Code Detection**: Advanced detection with multi-scale analysis and image enhancements
- **Compliance Validation**: Validates QR payloads against Portuguese fiscal regulations
- **Detailed Reports**: Generates comprehensive Excel reports with:
  - Summary statistics and document type breakdown
  - Complete field-by-field analysis with validation status
  - Color-coded compliance indicators
  - Non-compliant QR code tracking for quick remediation
- **Flexible Input Modes**: Command-line interface or drag-and-drop interaction
- **DPI Control**: Configurable PDF rendering quality (300, 600, or 1200 DPI)
- **Logging**: Detailed processing logs with configurable verbosity

## Requirements

- .NET 8.0 Runtime or later
- Windows operating system (for self-contained deployment)

## Installation

### Option 1: Download Pre-built Executable

Download the latest release from the [Releases page](https://github.com/yourusername/AtQrExtractor/releases). The executable is self-contained and includes all necessary dependencies.

### Option 2: Build from Source

```powershell
# Clone the repository
git clone https://github.com/yourusername/AtQrExtractor.git
cd AtQrExtractor

# Build the release executable
./Build.ps1

# The executable will be available at:
# ./bin/Release/net8.0/win-x64/atqr.exe
```

## Usage

### Command-Line Mode

Extract QR codes from invoices and generate an Excel report:

```powershell
atqr extract --input "C:\invoices\*.pdf" --output "C:\reports"
```

#### Available Options

| Option | Description | Default |
| ------ | ----------- | ------- |
| `--input` | Paths to files or folders containing invoices (PDF or Images). Can be specified multiple times. | Required |
| `--output` | Output directory for Excel report and logs | Required |
| `--dpi` | DPI for PDF image extraction (300, 600, or 1200). Higher DPI improves detection of small/dense QR codes. | 600 |
| `--log-level` | Log level (Debug, Information, Warning, Error) | Information |
| `--fail-on-noncompliant` | Exit with code 1 if non-compliant QR codes detected | false |
| `--multi-scale` | Enable multi-scale detection (tries different image sizes for better detection) | true |
| `--enhancements` | Enable image enhancements (contrast, sharpening) for better QR detection | true |

#### Examples

```powershell
# Process a single file
atqr extract --input "invoice.pdf" --output ".\output"

# Process multiple files
atqr extract --input "doc1.pdf" --input "doc2.png" --output ".\reports"

# Process entire directory with debug logging
atqr extract --input "C:\invoices" --output "C:\reports" --log-level Debug

# High-quality processing for small QR codes
atqr extract --input ".\invoices" --output ".\reports" --dpi 1200

# Fail if any non-compliant QR codes are found
atqr extract --input ".\invoices" --output ".\reports" --fail-on-noncompliant
```

### Drag & Drop Mode

For ease of use, you can drag and drop files or folders directly onto the `atqr.exe` executable. The application will:

1. Display a user-friendly interface showing the dropped items
2. Prompt you to select DPI quality
3. Process all files with default optimized settings
4. Generate the Excel report in the same location as the input files

## Output

The tool generates an Excel report (`atqr_report_YYYYMMDD_HHMMSS.xlsx`) containing three sheets:

1. **Summary**: Aggregate statistics, document type breakdown, and most common validation issues
2. **Detailed Analysis**: Complete field-by-field breakdown with validation status for each QR code
3. **Issues Only**: Filtered view of non-compliant QR codes for quick remediation

A log file (`atqr_YYYYMMDD_HHMMSS.log`) is also created in the output directory for troubleshooting.

## QR Code Format

The tool processes QR codes from Portuguese fiscal documents following Portaria n.º 195/2020 specification. The QR code format uses a structured payload:

```txt
A:123456789*B:987654321*PT*D:FT*E:N*F:20241220*G:2024/1*H:12345*I1:ADC*I7:100.00*...
```

### Field Categories

| Category | Fields | Description |
| -------- | ------ | ----------- |
| Mandatory | A-H, N, O, Q, R | Required fields for all documents |
| Tax Region 1 | I1-I8 | First regional tax breakdown (optional) |
| Tax Region 2 | J1-J8 | Second regional tax breakdown (optional) |
| Tax Region 3 | K1-K8 | Third regional tax breakdown (optional) |
| Optional | L, M, P, S | Additional document information |

At least one tax region (I1, J1, or K1) must be present per specification.

## Troubleshooting

### No QR Codes Found

If the tool reports no QR codes found, check:

- Files contain visible QR codes
- QR codes are at least 2cm x 2cm (0.8in x 0.8in) in size
- QR codes have sufficient contrast
- Images are not overly compressed or degraded
- Try increasing DPI to 1200 for better detection

### Non-Compliant QR Codes

Non-compliant QR codes may indicate:

- Missing mandatory fields
- Invalid field formats
- Incorrect document type codes
- Malformed tax region data

Review the "Issues Only" sheet in the Excel report for specific remediation guidance.

## Building

### Prerequisites

- .NET 8.0 SDK
- PowerShell (for build script)

### Build Commands

```powershell
# Debug build
dotnet build --configuration Debug

# Release build (self-contained Windows executable)
./Build.ps1

# Or manually:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Dependencies

| Package | License | Purpose |
| ------- | ------- | ------- |
| SixLabors.ImageSharp | Apache 2.0 | Image processing and QR detection |
| ZXing.Net | Apache 2.0 | QR code reading and decoding |
| EPPlus | Commercial/Non-Commercial | Excel report generation |
| Serilog | Apache 2.0 | Structured logging |
| System.CommandLine | MIT | Command-line interface |

For commercial use of EPPlus, please review and obtain the appropriate license from [EPPlus Software](https://www.epplussoftware.com/).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Portuguese Tax Authority (Autoridade Tributária e Aduaneira) for the AT QR Code specification
- Portaria n.º 195/2020 for defining the QR Code format for ATCUD
