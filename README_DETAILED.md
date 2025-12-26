# AT QR Code Extractor

## Overview

AT QR Code Extractor is a professional-grade command-line tool designed to extract, validate, and analyze QR codes from Portuguese fiscal documents in accordance with the specifications defined in Portaria n.º 195/2020. This regulatory document establishes the standards for QR codes on invoices and other fiscal documents in Portugal, ensuring compliance with the country's electronic invoicing requirements administered by the Autoridade Tributária e Aduaneira (AT).

The application processes PDF files and various image formats including PNG, JPEG, BMP, and TIFF, employing multiple detection strategies to maximize QR code recognition accuracy. Upon extraction, each QR code payload undergoes rigorous validation against the Portuguese fiscal specification, checking for mandatory field presence, correct data formats, and proper structure. The results are compiled into comprehensive Excel reports that provide detailed compliance analysis, field-by-field breakdowns, and identification of any issues requiring remediation.

This tool proves invaluable for businesses, auditors, and software developers who need to verify the compliance of fiscal documents, audit invoice batches for regulatory adherence, or integrate Portuguese fiscal QR code validation into their workflows. The combination of batch processing capabilities, command-line interface, and drag-and-drop functionality makes the tool accessible to both technical and non-technical users alike.

## Features

AT QR Code Extractor offers a comprehensive suite of features designed to handle the complete lifecycle of fiscal QR code processing. The tool supports processing of multiple files and entire directories in a single operation, enabling efficient batch processing of large invoice collections. The detection system utilizes multiple strategies including multi-scale image analysis and contrast enhancement preprocessing, significantly improving recognition rates for QR codes of varying sizes and quality levels.

The validation engine implements the complete Portaria n.º 195/2020 specification, checking all mandatory fields (A-H, N, O, Q, R), optional fields (L, M, P, S), and tax region breakdowns (I1-I8, J1-J8, K1-K8). Each field undergoes format validation including pattern matching for dates, document types, monetary values, and identification numbers. The system tracks validation failures with detailed error messages, enabling precise identification and correction of non-compliant documents.

Report generation produces multi-sheet Excel workbooks containing aggregate statistics, detailed field analysis, and focused issue listings. The summary sheet provides overall compliance metrics, document type breakdowns, and the most common validation issues encountered across the processed batch. The detailed analysis sheet presents every QR code with its complete field structure, validation status, and color-coded compliance indicators. The issues sheet offers a filtered view of non-compliant documents for quick remediation workflows.

The application provides flexible operational modes including a full command-line interface with comprehensive options for programmatic integration and an interactive drag-and-drop mode for ad-hoc processing without requiring knowledge of command syntax. Detailed logging captures all processing steps, enabling troubleshooting and audit trail maintenance.

## System Requirements

The application requires the .NET 8.0 runtime or SDK to execute. The runtime alone suffices for running pre-built applications, while the SDK is necessary for compiling the source code. Ensure that your system meets these prerequisites before attempting to run the application. The tool is designed for Windows environments and has been built and tested on Windows 10 and Windows 11 systems.

Memory requirements scale with the number and size of files being processed. The application allocates approximately 512 MB of RAM for typical workloads processing documents up to 50 pages in length. For larger batches or high-resolution PDF rendering at 1200 DPI, allocating 1 GB or more of RAM provides smoother operation. Disk space requirements include room for the application itself (approximately 100 MB when self-contained), temporary files during processing, and output reports.

PDF processing relies on the PdfLibNet library, which requires the Microsoft Visual C++ Redistributable to be installed on the system. Most Windows installations include this component by default, but if PDF processing fails with library-related errors, installing the latest Visual C++ Redistributable from Microsoft's official website resolves the issue.

## Installation

### Option 1: Pre-built Release

Download the latest release package from the official repository releases page. The package contains a self-contained Windows executable that includes all necessary .NET runtime components, eliminating the need for separate runtime installation. Extract the downloaded archive to your preferred installation directory, such as `C:\Tools\AtQrExtractor` or `%LOCALAPPDATA%\Apps\AtQrExtractor`.

The executable file is named `atqr.exe` (when published as a self-contained package) or `AtQrExtractor.exe` (when published as a framework-dependent deployment). For convenience, add the installation directory to your system PATH environment variable, enabling command execution from any terminal location without specifying the full path.

### Option 2: Build from Source

Clone or download the source code repository to your local machine. Ensure that the .NET 8.0 SDK is installed and properly configured. Open a terminal or command prompt in the project root directory containing the solution file `AtQrExtractor.sln`. Restore dependencies by running `dotnet restore`, which downloads all NuGet packages referenced by the project.

Build the application using the provided PowerShell script by executing `./Build.ps1` in a PowerShell terminal. This script performs a release build with self-contained deployment for Windows, producing a standalone executable in the `bin\Release\net8.0\win-x64\publish` directory. Alternatively, use `dotnet publish` directly with your preferred configuration options.

## Usage

The application supports two primary usage patterns: command-line operation for scripted and automated workflows, and drag-and-drop operation for interactive single-batch processing. Both modes produce identical output reports and perform the same validation logic, differing only in the interface for specifying inputs and options.

### Command-Line Mode

Command-line mode provides full access to all configuration options through named parameters. The general syntax follows the pattern `atqr extract --input <paths> --output <directory> [options]`, where the `extract` subcommand initiates the processing pipeline. Multiple input paths may be specified, and the tool processes both individual files and entire directories recursively.

The following example demonstrates processing a single invoice file and generating the report in the specified output directory:

```powershell
atqr extract --input "C:\Invoices\2024\invoice_001.pdf" --output "C:\Reports\AT_Analysis"
```

For batch processing of multiple documents, specify multiple paths or process an entire directory:

```powershell
atqr extract --input "C:\Invoices\2024" --input "C:\Invoices\2025" --output "C:\Reports\AT_Batch"
```

### Drag-and-Drop Mode

Drag-and-drop mode activates automatically when files or folders are dropped onto the executable without any command-line arguments. This mode presents an interactive wizard-style interface guiding users through the processing configuration. The system displays the items being processed, prompts for DPI quality selection, and shows progress during execution.

To use drag-and-drop mode, open Windows Explorer and locate the application executable. Select one or more PDF files or folders in Explorer, drag them onto the executable file, and release. The application window appears with the processing interface. Select the desired DPI setting when prompted, then wait for processing to complete. Results are saved to the same directory as the processed files by default.

### Command-Line Options

The `--input` option specifies the source files and directories to process. This option is required and may be specified multiple times to include items from different locations. Both individual files and directories are accepted; when a directory is specified, the application processes all supported file types within it recursively.

The `--output` option specifies the destination directory for generated reports and log files. This option is required. If the directory does not exist, the application creates it automatically. Reports are saved with timestamps in their filenames to prevent overwriting and enable correlation with processing sessions.

The `--dpi` option controls the resolution used when rendering PDF documents to images for QR code detection. Valid values are 300, 600, and 1200. Higher DPI values improve detection of small or dense QR codes but increase processing time and memory usage. The default value of 600 provides balanced performance for most documents.

The `--multi-scale` option enables multi-scale detection, which attempts QR code recognition at multiple image sizes. This option significantly improves detection rates for QR codes of varying sizes without substantially increasing processing time. Enabled by default; specify `--multi-scale:false` to disable.

The `--enhancements` option enables image preprocessing enhancements including contrast adjustment and sharpening before QR detection. These enhancements improve recognition rates for documents with lower contrast or slightly degraded image quality. Enabled by default; specify `--enhancements:false` to disable.

The `--log-level` option controls the verbosity of logging output. Valid values in increasing order of verbosity are Error, Warning, Information, and Debug. The default level of Information provides sufficient detail for most use cases while avoiding excessive output. Use Debug level when troubleshooting detection issues.

The `--fail-on-noncompliant` option changes the application exit code when non-compliant QR codes are detected. By default, the application exits with code 0 regardless of compliance results. When this option is enabled, the application exits with code 1 if any non-compliant QR codes are found, enabling integration with CI/CD pipelines and automated quality gates.

### Help and Documentation

Display complete command documentation by running the application with the help flag:

```powershell
atqr extract --help
```

This command displays all available options with their descriptions, default values, and acceptable input ranges. The help output serves as a quick reference for available functionality without requiring access to this documentation.

## Output Format

The application generates two primary output files for each processing session: an Excel report containing the analysis results and a log file capturing the processing details. Both files include timestamps in their filenames to distinguish between multiple runs.

### Excel Report Structure

The Excel workbook contains three worksheets providing different analytical perspectives on the processed data. The Summary worksheet presents aggregate statistics including total QR codes processed, compliance counts, document type breakdowns, and the most common validation issues encountered. This worksheet provides a quick overview of batch quality and identifies patterns in any non-compliance issues.

The Detailed Analysis worksheet contains comprehensive information for each QR code, organized by compliance status and hash value. Each QR code section includes the compliance status indicator, SHA-256 hash identifier, source file list, and detailed field tables. Fields are grouped by category (mandatory fields, tax region breakdowns, optional fields) with color highlighting for validation failures. The Issues Only worksheet provides a filtered view containing only non-compliant QR codes, facilitating focused remediation workflows.

### Log File Format

The log file captures all processing operations in a structured format suitable for troubleshooting and audit purposes. Each log entry includes a timestamp, log level, and message. Information-level entries document the processing workflow, including file processing results, QR code counts, and compliance statistics. Warning and error entries highlight issues requiring attention, such as files that could not be processed or QR codes that failed validation.

### Exit Codes

The application uses exit codes to indicate processing outcomes for integration with automated workflows and scripts. Exit code 0 indicates successful processing with no errors, regardless of whether non-compliant QR codes were found (unless `--fail-on-noncompliant` is specified). Exit code 1 indicates that non-compliant QR codes were detected and the `--fail-on-noncompliant` option was enabled. Exit code 2 indicates a general error such as invalid inputs, missing files, or processing failures.

## QR Code Specification Reference

The application validates QR codes against Portaria n.º 195/2020, which defines the structure and content requirements for QR codes on Portuguese fiscal documents. Understanding this specification helps interpret validation results and address any non-compliance issues discovered during processing.

The QR code payload consists of field code-value pairs separated by asterisks, where each field uses a single-letter or letter-number identifier. Mandatory fields include the seller's tax registration number (A), buyer's tax registration number (B), buyer's country code (C), document type (D), document status (E), document date (F), document ID (G), and ATCUD (H). Additional mandatory fields include total taxes (N), grand total with taxes (O), hash segment (Q), and certificate number (R).

Tax region fields I1-I8, J1-J8, and K1-K8 provide breakdowns of tax bases and totals for different fiscal spaces. At least one tax region must be present in each document according to the specification. Optional fields L (non-taxable amounts), M (stamp duty), P (withholding tax), and S (additional information) provide supplementary document details.

Field validation checks both presence of mandatory fields and format compliance for fields with defined patterns. Document type must match one of the permitted codes (FT for invoices, NC for credit notes, etc.). Document dates must use the YYYYMMDD format. Monetary values must use the XXXXXX.XX format with exactly two decimal places.

## Troubleshooting

When the application reports that no QR codes were found in files that should contain them, several factors may be responsible. First, verify that the QR codes are visible and unobstructed in the documents. QR codes smaller than 2 cm × 2 cm (approximately 0.8 inches × 0.8 inches) may fall below reliable detection thresholds. Documents with low contrast between the QR code and background, excessive compression artifacts, or physical damage may also resist detection.

Increasing the DPI setting improves detection of smaller codes but increases processing time. Try 600 DPI if 300 DPI fails, or 1200 DPI for particularly challenging documents. The `--multi-scale` and `--enhancements` options should remain enabled unless debugging specific detection issues.

If PDF processing fails, ensure that the PDF files are not password-protected or corrupted. The application uses PdfLibNet for PDF rendering, which supports standard PDF formats but may not handle some specialized PDF features or encryption schemes. Converting problematic PDFs to images using external tools before processing may resolve compatibility issues.

For persistent issues, enable debug-level logging (`--log-level debug`) to capture detailed diagnostic information about the detection process. The log file reveals whether files are being opened successfully, what detection attempts are being made, and why specific QR codes might fail recognition.

## License and Acknowledgments

This project is provided as free software under the terms of the MIT License. The license permits use, modification, and distribution of the software and its derivatives, subject to the license conditions included in the LICENSE file.

The application depends on several open-source libraries that enable its functionality. QrCodeNet provides the QR code detection and generation capabilities. PdfLibNet enables PDF document rendering. EPPlus generates the Excel workbooks with professional formatting. Serilog provides structured logging functionality. System.CommandLine powers the command-line interface. The developers of these libraries have created valuable tools that make projects like this one possible.

EPPlus requires licensing for commercial use. The application configures EPPlus for non-commercial personal use by default. Organizations using this tool for commercial purposes should acquire appropriate EPPlus licensing from the maintainers. Setting the `ExcelPackage.LicenseContext` property appropriately for your usage scenario ensures compliance with EPPlus licensing requirements.

## Support and Contributions

Bug reports, feature suggestions, and questions are welcome through the project's issue tracking system. When reporting issues, include the log file from the problematic run, the types of documents being processed, and any relevant error messages. Detailed reports enable faster investigation and resolution.

Contributions through pull requests are appreciated. Before submitting changes, ensure that code follows the existing style conventions, that new functionality includes appropriate tests, and that documentation is updated to reflect any user-facing changes. The project maintainers review submissions and provide feedback to ensure quality and alignment with project goals.
