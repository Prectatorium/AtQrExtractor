using System.CommandLine;
using Serilog;
using AtQrExtractor.Services;
using AtQrExtractor.Models;
using OfficeOpenXml;
namespace AtQrExtractor;

/// <summary>
/// Main program class for AT QR Code Extractor - Portuguese fiscal invoice processing tool.
/// </summary>
/// <remarks>
/// This application extracts and validates QR codes from Portuguese fiscal documents (invoices, receipts)
/// according to the specifications defined in Portaria n.º 195/2020. It supports both command-line
/// operation and drag-and-drop functionality for ease of use.
///
/// <para><b>Key Features:</b></para>
/// <list type="bullet">
///   <item><description>Processes PDF files and image formats (PNG, JPEG, BMP, TIFF)</description></item>
///   <item><description>Extracts QR codes using multiple detection strategies</description></item>
///   <item><description>Validates QR payloads against Portuguese fiscal regulations</description></item>
///   <item><description>Generates detailed Excel reports with compliance analysis</description></item>
///   <item><description>Supports batch processing of multiple files and directories</description></item>
///   <item><description>Offers user-friendly drag-and-drop interface</description></item>
/// </list>
///
/// <para><b>Usage Modes:</b></para>
/// <list type="number">
///   <item>
///     <term>Command Line Mode</term>
///     <description>Use `atqr extract --input [paths] --output [dir]` for programmatic processing</description>
///   </item>
///   <item>
///     <term>Drag &amp; Drop Mode</term>
///     <description>Drop files or folders onto the executable for interactive processing</description>
///   </item>
/// </list>
/// </remarks>
public sealed class Program
{
    #region Constants

    /// <summary>
    /// Supported DPI values for PDF rendering operations.
    /// </summary>
    private static readonly int[] SupportedDpiValues = { 300, 600, 1200 };

    /// <summary>
    /// Default DPI setting for balanced performance and quality.
    /// </summary>
    private const int DefaultDpi = 600;

    /// <summary>
    /// Default log level for normal operation.
    /// </summary>
    private const string DefaultLogLevel = "Information";

    /// <summary>
    /// Exit code for successful operation.
    /// </summary>
    private const int ExitCodeSuccess = 0;

    /// <summary>
    /// Exit code when compliance failure is detected and --fail-on-noncompliant is enabled.
    /// </summary>
    private const int ExitCodeComplianceFailure = 1;

    /// <summary>
    /// Exit code for general errors (invalid inputs, processing failures, etc.).
    /// </summary>
    private const int ExitCodeError = 2;

    /// <summary>
    /// Number of top compliance issues to display in the summary.
    /// </summary>
    private const int MaxDisplayedIssues = 5;

    #endregion

    #region Main Entry Point

    /// <summary>
    /// Main entry point for the AT QR Code Extractor application.
    /// </summary>
    /// <param name="args">Command-line arguments provided by the user.</param>
    /// <returns>Exit code indicating success (0), compliance failure (1), or general error (2).</returns>
    /// <remarks>
    /// This method automatically detects whether the application was invoked via command-line
    /// or drag-and-drop, and routes to the appropriate handler.
    /// </remarks>
    /// <exception cref="Exception">Thrown when an unexpected fatal error occurs during application startup.</exception>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            ConfigureApplication();

            // Detect drag-and-drop scenario (no command specified, just file/folder paths)
            if (IsDragAndDropInvocation(args))
            {
                return await HandleDragAndDropMode(args);
            }

            // Standard command-line mode
            return await HandleCommandLineMode(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return ExitCodeError;
        }
    }

    #endregion

    #region Command Line Mode

    /// <summary>
    /// Handles standard command-line invocation with explicit commands and options.
    /// </summary>
    /// <param name="args">Command-line arguments to parse and execute.</param>
    /// <returns>Exit code from command execution (0 for success, non-zero for errors).</returns>
    private static async Task<int> HandleCommandLineMode(string[] args)
    {
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Creates and configures the root command with all available options and subcommands.
    /// </summary>
    /// <returns>Configured <see cref="RootCommand"/> for the CLI with all available commands.</returns>
    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("AT QR Code Extractor - Fiscal Invoice Processing Tool");

        var extractCommand = CreateExtractCommand();
        rootCommand.AddCommand(extractCommand);

        return rootCommand;
    }

    /// <summary>
    /// Creates and configures the extract command with all its options.
    /// </summary>
    /// <returns>Configured extract command for processing invoice documents.</returns>
    private static Command CreateExtractCommand()
    {
        var extractCommand = new Command("extract", "Extract and process QR codes from invoice documents");

        // Configure command options
        var inputOption = CreateInputOption();
        var outputOption = CreateOutputOption();
        var dpiOption = CreateDpiOption();
        var logLevelOption = CreateLogLevelOption();
        var failOnNonCompliantOption = CreateFailOnNonCompliantOption();
        var multiScaleOption = CreateMultiScaleOption();
        var enhancementsOption = CreateEnhancementsOption();

        // Add options to command
        extractCommand.AddOption(inputOption);
        extractCommand.AddOption(outputOption);
        extractCommand.AddOption(dpiOption);
        extractCommand.AddOption(logLevelOption);
        extractCommand.AddOption(failOnNonCompliantOption);
        extractCommand.AddOption(multiScaleOption);
        extractCommand.AddOption(enhancementsOption);

        // Configure command handler
        extractCommand.SetHandler(async (input, output, dpi, logLevel, failOnNonCompliant, multiScale, enhancements) =>
        {
            var exitCode = await ExecuteExtraction(input, output, dpi, logLevel, failOnNonCompliant, multiScale, enhancements);
            Environment.ExitCode = exitCode;
        }, inputOption, outputOption, dpiOption, logLevelOption, failOnNonCompliantOption, multiScaleOption, enhancementsOption);

        return extractCommand;
    }

    #endregion

    #region Command Options Factory Methods

    /// <summary>
    /// Creates the input option for specifying source files and directories.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> for input paths accepting multiple file or directory paths.</returns>
    private static Option<string[]> CreateInputOption()
    {
        return new Option<string[]>(
            "--input",
            "Paths to files or folders containing invoices (PDF or Images). Can be specified multiple times.")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };
    }

    /// <summary>
    /// Creates the output option for specifying the destination directory.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> for output directory where reports and logs will be saved.</returns>
    private static Option<DirectoryInfo> CreateOutputOption()
    {
        return new Option<DirectoryInfo>(
            "--output",
            "Output directory for Excel report and logs")
        {
            IsRequired = true
        };
    }

    /// <summary>
    /// Creates the DPI option for PDF rendering quality.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> for DPI setting with default value of 600.</returns>
    private static Option<int> CreateDpiOption()
    {
        return new Option<int>(
            "--dpi",
            getDefaultValue: () => DefaultDpi,
            "DPI for PDF image extraction (300, 600, or 1200). Higher DPI improves detection of small/dense QR codes.");
    }

    /// <summary>
    /// Creates the log level option for controlling logging verbosity.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> for log level with default value of "Information".</returns>
    private static Option<string> CreateLogLevelOption()
    {
        return new Option<string>(
            "--log-level",
            getDefaultValue: () => DefaultLogLevel,
            "Log level (Debug, Information, Warning, Error)");
    }

    /// <summary>
    /// Creates the fail-on-noncompliant option for compliance enforcement.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> that determines whether non-compliant QR codes trigger an error exit code.</returns>
    private static Option<bool> CreateFailOnNonCompliantOption()
    {
        return new Option<bool>(
            "--fail-on-noncompliant",
            getDefaultValue: () => false,
            "Exit with code 1 if non-compliant QR codes detected");
    }

    /// <summary>
    /// Creates the multi-scale detection option.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> that enables multi-scale detection for better QR code recognition.</returns>
    private static Option<bool> CreateMultiScaleOption()
    {
        return new Option<bool>(
            "--multi-scale",
            getDefaultValue: () => true,
            "Enable multi-scale detection (tries different image sizes for better detection)");
    }

    /// <summary>
    /// Creates the image enhancements option.
    /// </summary>
    /// <returns>Configured <see cref="Option{T}"/> that enables image preprocessing enhancements for improved QR detection.</returns>
    private static Option<bool> CreateEnhancementsOption()
    {
        return new Option<bool>(
            "--enhancements",
            getDefaultValue: () => true,
            "Enable image enhancements (contrast, sharpening) for better QR detection");
    }

    #endregion

    #region Drag and Drop Mode

    /// <summary>
    /// Determines whether the application was invoked via drag-and-drop.
    /// </summary>
    /// <param name="args">Command-line arguments to analyze for drag-and-drop patterns.</param>
    /// <returns><c>true</c> if this appears to be a drag-and-drop invocation; otherwise <c>false</c>.</returns>
    private static bool IsDragAndDropInvocation(string[] args)
    {
        return args.Length > 0 &&
               !args[0].StartsWith("-") &&
               args[0] != "extract";
    }

    /// <summary>
    /// Handles drag-and-drop invocation with interactive user prompts.
    /// </summary>
    /// <param name="droppedPaths">File and directory paths dropped onto the executable.</param>
    /// <returns>Exit code indicating success (0), compliance failure (1), or general error (2).</returns>
    /// <remarks>
    /// This mode provides a user-friendly interface for non-technical users who prefer
    /// to drag files onto the executable rather than use command-line arguments.
    /// </remarks>
    private static async Task<int> HandleDragAndDropMode(string[] droppedPaths)
    {
        DisplayDragDropHeader();

        // Validate dropped paths
        var validPaths = ValidateDroppedPaths(droppedPaths);
        if (validPaths.Length == 0)
        {
            DisplayErrorAndWaitForKey("Error: No valid files or folders were dropped.");
            return ExitCodeError;
        }

        DisplayDroppedItems(validPaths);

        // Determine output directory using intelligent defaults
        var outputInfo = DetermineOutputDirectory(validPaths);
        Console.WriteLine($"Output directory: {outputInfo.FullName}");
        Console.WriteLine();

        // Interactive DPI selection
        int selectedDpi = PromptForDpiSelection();

        Console.WriteLine($"Using {selectedDpi} DPI");
        Console.WriteLine();
        Console.WriteLine("Processing started...");
        Console.WriteLine();

        // Execute with defaults optimized for drag-and-drop usage
        var exitCode = await ExecuteExtraction(
            validPaths,
            outputInfo,
            selectedDpi,
            DefaultLogLevel,
            failOnNonCompliant: false,
            multiScale: true,
            enhancements: true
        );

        DisplayDragDropResults(exitCode);
        return exitCode;
    }

    /// <summary>
    /// Displays the header for drag-and-drop mode.
    /// </summary>
    private static void DisplayDragDropHeader()
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  AT QR Extractor - Drag & Drop Mode");
        Console.WriteLine("===========================================");
        Console.WriteLine();
    }

    /// <summary>
    /// Validates and filters dropped file and directory paths.
    /// </summary>
    /// <param name="droppedPaths">Raw dropped paths from the system.</param>
    /// <returns>Array of valid, existing paths that point to accessible files or directories.</returns>
    private static string[] ValidateDroppedPaths(string[] droppedPaths)
    {
        return droppedPaths.Where(p => File.Exists(p) || Directory.Exists(p)).ToArray();
    }

    /// <summary>
    /// Displays information about successfully validated dropped items.
    /// </summary>
    /// <param name="validPaths">Valid file and directory paths to display.</param>
    private static void DisplayDroppedItems(string[] validPaths)
    {
        Console.WriteLine($"Received {validPaths.Length} item(s):");
        foreach (var path in validPaths)
        {
            var type = File.Exists(path) ? "File" : "Folder";
            Console.WriteLine($"  [{type}] {Path.GetFileName(path)}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Determines an appropriate output directory based on dropped paths.
    /// </summary>
    /// <param name="validPaths">Valid file and directory paths.</param>
    /// <returns><see cref="DirectoryInfo"/> for the determined output location.</returns>
    /// <remarks>
    /// Uses the parent directory of the first dropped item, or the current directory as fallback.
    /// </remarks>
    private static DirectoryInfo DetermineOutputDirectory(string[] validPaths)
    {
        var firstPath = validPaths[0];
        var outputDir = File.Exists(firstPath)
            ? Path.GetDirectoryName(firstPath)
            : firstPath;

        if (string.IsNullOrEmpty(outputDir))
        {
            outputDir = Environment.CurrentDirectory;
        }

        return new DirectoryInfo(outputDir);
    }

    /// <summary>
    /// Prompts the user to select a DPI setting for processing quality.
    /// </summary>
    /// <returns>Selected DPI value (300, 600, or 1200). Defaults to 600 if no valid selection is made.</returns>
    private static int PromptForDpiSelection()
    {
        Console.WriteLine("Select DPI quality (higher = better detection but slower):");
        Console.WriteLine("  [1] 300 DPI - Fast (recommended for clear, large QR codes)");
        Console.WriteLine("  [2] 600 DPI - Balanced (default, works for most cases)");
        Console.WriteLine("  [3] 1200 DPI - High quality (for small or poor-quality QR codes)");
        Console.Write("Enter choice [1-3] or press Enter for default (600): ");

        var dpiChoice = Console.ReadLine()?.Trim();
        return dpiChoice switch
        {
            "1" => 300,
            "3" => 1200,
            _ => DefaultDpi
        };
    }

    /// <summary>
    /// Displays the final results of drag-and-drop processing.
    /// </summary>
    /// <param name="exitCode">Exit code from the processing operation (0 for success, non-zero for errors).</param>
    private static void DisplayDragDropResults(int exitCode)
    {
        Console.WriteLine();
        Console.WriteLine("===========================================");
        if (exitCode == ExitCodeSuccess)
        {
            Console.WriteLine("  ✓ Processing completed successfully!");
        }
        else
        {
            Console.WriteLine("  ✗ Processing completed with errors.");
        }
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Displays an error message and waits for user input before returning.
    /// </summary>
    /// <param name="errorMessage">The error message to display to the user.</param>
    private static void DisplayErrorAndWaitForKey(string errorMessage)
    {
        Console.WriteLine(errorMessage);
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    #endregion

    #region Core Processing Logic

    /// <summary>
    /// Executes the main QR code extraction and analysis workflow.
    /// </summary>
    /// <param name="inputs">Array of input file and directory paths to process.</param>
    /// <param name="output">Output directory for reports and logs.</param>
    /// <param name="dpi">DPI setting for PDF rendering (300, 600, or 1200).</param>
    /// <param name="logLevel">Logging verbosity level (Debug, Information, Warning, Error).</param>
    /// <param name="failOnNonCompliant">When <c>true</c>, returns error code 1 if non-compliant QR codes are detected.</param>
    /// <param name="multiScale">When <c>true</c>, enables multi-scale QR detection for improved recognition.</param>
    /// <param name="enhancements">When <c>true</c>, applies image preprocessing enhancements for better QR detection.</param>
    /// <returns>Exit code indicating the result of processing (0 for success, 1 for compliance failure, 2 for errors).</returns>
    /// <remarks>
    /// This is the main processing pipeline that:
    /// <list type="number">
    ///   <item>Validates inputs and configures logging</item>
    ///   <item>Processes files to extract QR codes</item>
    ///   <item>Deduplicates and interprets QR payloads</item>
    ///   <item>Validates compliance against Portuguese fiscal regulations</item>
    ///   <item>Generates comprehensive Excel reports</item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown when an unexpected error occurs during processing.</exception>
    private static async Task<int> ExecuteExtraction(
        string[] inputs,
        DirectoryInfo output,
        int dpi,
        string logLevel,
        bool failOnNonCompliant,
        bool multiScale,
        bool enhancements)
    {
        ValidateAndWarnAboutDpi(dpi);

        // Configure comprehensive logging
        var logPath = ConfigureLogging(output, logLevel);

        try
        {
            LogProcessingStart(inputs, output, dpi, multiScale, enhancements);

            // Validate inputs
            var validationResult = ValidateInputs(inputs, output);
            if (!validationResult.IsValid)
            {
                return validationResult.ExitCode;
            }

            // Initialize processing services
            var services = InitializeServices(dpi, multiScale, enhancements);

            // Execute main processing pipeline
            var processingResult = await ExecuteProcessingPipeline(services, inputs);

            // Display processing summary
            LogProcessingSummary(processingResult);

            // Handle early exit for no QR codes found
            if (processingResult.UniquePayloads.Count == 0)
            {
                LogNoQrCodesFoundGuidance();
                return ExitCodeSuccess;
            }

            // Interpret QR codes and validate compliance
            var interpretedData = InterpretQrCodes(services.Interpreter, processingResult.UniquePayloads);
            LogComplianceSummary(interpretedData);

            // Generate comprehensive Excel report
            GenerateExcelReport(services.ExcelGenerator, interpretedData, output);

            Log.Information("Processing completed successfully");

            // Determine final exit code based on compliance results
            return DetermineExitCode(interpretedData, failOnNonCompliant);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fatal error during processing");
            return ExitCodeError;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    #endregion

    #region Processing Pipeline Components

    /// <summary>
    /// Container for initialized processing services.
    /// </summary>
    private sealed record ProcessingServices(
        QrProcessor Processor,
        PortariaInterpreter Interpreter,
        ExcelGenerator ExcelGenerator);

    /// <summary>
    /// Container for processing pipeline results.
    /// </summary>
    private sealed record ProcessingResult(
        List<FileProcessingResult> FileResults,
        List<QrPayload> UniquePayloads,
        int TotalFiles,
        int FilesWithQrCodes,
        int FilesWithErrors);

    /// <summary>
    /// Container for input validation results.
    /// </summary>
    private sealed record ValidationResult(bool IsValid, int ExitCode);

    /// <summary>
    /// Initializes all required processing services with the specified configuration.
    /// </summary>
    /// <param name="dpi">DPI setting for PDF processing (300, 600, or 1200).</param>
    /// <param name="multiScale">When <c>true</c>, enables multi-scale detection for better QR recognition.</param>
    /// <param name="enhancements">When <c>true</c>, enables image preprocessing enhancements.</param>
    /// <returns><see cref="ProcessingServices"/> containing initialized service instances.</returns>
    private static ProcessingServices InitializeServices(int dpi, bool multiScale, bool enhancements)
    {
        Log.Information("Initializing processing services...");

        return new ProcessingServices(
            Processor: new QrProcessor(dpi, multiScale, enhancements),
            Interpreter: new PortariaInterpreter(),
            ExcelGenerator: new ExcelGenerator()
        );
    }

    /// <summary>
    /// Executes the main file processing pipeline.
    /// </summary>
    /// <param name="services">Initialized processing services.</param>
    /// <param name="inputs">Input file and directory paths to process.</param>
    /// <returns><see cref="ProcessingResult"/> containing extracted QR codes, statistics, and processing outcomes.</returns>
    private static async Task<ProcessingResult> ExecuteProcessingPipeline(ProcessingServices services, string[] inputs)
    {
        Log.Information("Starting file processing pipeline...");

        var fileResults = await services.Processor.ProcessInputs(inputs);
        var totalFiles = fileResults.Count;
        var filesWithQr = fileResults.Count(r => r.QrPayloads.Any());
        var filesWithErrors = fileResults.Count(r => r.Errors.Any());

        // Deduplicate QR payloads across all files
        var uniquePayloads = DeduplicatePayloads(fileResults);

        return new ProcessingResult(
            FileResults: fileResults,
            UniquePayloads: uniquePayloads,
            TotalFiles: totalFiles,
            FilesWithQrCodes: filesWithQr,
            FilesWithErrors: filesWithErrors
        );
    }

    /// <summary>
    /// Interprets all unique QR payloads using the Portuguese fiscal specification.
    /// </summary>
    /// <param name="interpreter">Configured interpretation service for Portuguese fiscal regulations.</param>
    /// <param name="uniquePayloads">Deduplicated QR payloads to interpret.</param>
    /// <returns>List of <see cref="InterpretedQrData"/> containing interpreted QR data with compliance information.</returns>
    private static List<InterpretedQrData> InterpretQrCodes(PortariaInterpreter interpreter, List<QrPayload> uniquePayloads)
    {
        Log.Information("Interpreting QR codes according to Portaria n.º 195/2020...");

        var interpretedData = new List<InterpretedQrData>();
        foreach (var payload in uniquePayloads)
        {
            var interpreted = interpreter.Interpret(payload);
            interpretedData.Add(interpreted);
        }

        return interpretedData;
    }

    /// <summary>
    /// Generates the comprehensive Excel report with all analysis results.
    /// </summary>
    /// <param name="excelGenerator">Excel generation service for creating reports.</param>
    /// <param name="interpretedData">Interpreted QR data with compliance analysis.</param>
    /// <param name="output">Output directory where the Excel report will be saved.</param>
    private static void GenerateExcelReport(ExcelGenerator excelGenerator, List<InterpretedQrData> interpretedData, DirectoryInfo output)
    {
        var excelPath = Path.Combine(output.FullName, $"atqr_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        excelGenerator.Generate(interpretedData, excelPath);
        Log.Information("Excel report generated: {Path}", excelPath);
    }

    #endregion

    #region Application Configuration

    /// <summary>
    /// Performs one-time application startup configuration.
    /// </summary>
    /// <remarks>
    /// This includes third-party licensing, runtime configuration,
    /// and other global initialization required before program execution.
    /// </remarks>
    private static void ConfigureApplication()
    {
        ConfigureLicensing();
        // ConfigureCulture();
        // ConfigureTelemetry();
        // ConfigureThreading();
    }

    /// <summary>
    /// Configures third-party library licenses required at application startup.
    /// </summary>
    /// <remarks>
    /// Must be called once before any licensed components are used.
    /// Currently configures EPPlus for non-commercial personal use.
    /// </remarks>
    private static void ConfigureLicensing()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Dude");
    }

    /// <summary>
    /// Validates DPI setting and provides warnings if non-standard values are used.
    /// </summary>
    /// <param name="dpi">DPI value to validate against supported values (300, 600, 1200).</param>
    private static void ValidateAndWarnAboutDpi(int dpi)
    {
        if (!SupportedDpiValues.Contains(dpi))
        {
            Console.WriteLine($"Warning: DPI {dpi} is not a standard value. Recommended values are 300, 600, or 1200.");
            Console.WriteLine("Continuing with specified DPI value...");
        }
    }

    /// <summary>
    /// Configures the logging system for the current processing session.
    /// </summary>
    /// <param name="output">Output directory where log files will be saved.</param>
    /// <param name="logLevel">Desired logging verbosity level (Debug, Information, Warning, Error).</param>
    /// <returns>Path to the generated log file.</returns>
    private static string ConfigureLogging(DirectoryInfo output, string logLevel)
    {
        var logPath = Path.Combine(output.FullName, $"atqr_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(ParseLogLevel(logLevel))
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Infinite)
            .CreateLogger();

        return logPath;
    }

    /// <summary>
    /// Validates input paths and output directory, ensuring they exist and are accessible.
    /// </summary>
    /// <param name="inputs">Input file and directory paths to validate.</param>
    /// <param name="output">Output directory information to validate or create.</param>
    /// <returns><see cref="ValidationResult"/> indicating whether validation succeeded and the appropriate exit code if not.</returns>
    private static ValidationResult ValidateInputs(string[] inputs, DirectoryInfo output)
    {
        var validInputs = inputs.Where(i => File.Exists(i) || Directory.Exists(i)).ToList();
        if (validInputs.Count == 0)
        {
            Log.Error("None of the specified input paths exist.");
            return new ValidationResult(false, ExitCodeError);
        }

        if (!output.Exists)
        {
            output.Create();
            Log.Information("Created output directory: {Output}", output.FullName);
        }

        return new ValidationResult(true, ExitCodeSuccess);
    }

    /// <summary>
    /// Determines the appropriate exit code based on processing results and configuration.
    /// </summary>
    /// <param name="interpretedData">Interpreted QR data with compliance results.</param>
    /// <param name="failOnNonCompliant">When <c>true</c>, compliance failures trigger error exit code 1.</param>
    /// <returns>Appropriate exit code for the operation (0 for success, 1 for compliance failure).</returns>
    private static int DetermineExitCode(List<InterpretedQrData> interpretedData, bool failOnNonCompliant)
    {
        var nonCompliantCount = interpretedData.Count(d => !d.IsCompliant);

        if (nonCompliantCount > 0 && failOnNonCompliant)
        {
            Log.Warning("Exiting with code 1 due to non-compliant QR codes (--fail-on-noncompliant)");
            return ExitCodeComplianceFailure;
        }

        return ExitCodeSuccess;
    }

    #endregion

    #region Logging and Output Methods

    /// <summary>
    /// Logs the start of processing with all configuration parameters.
    /// </summary>
    /// <param name="inputs">Input file and directory paths being processed.</param>
    /// <param name="output">Output directory for reports and logs.</param>
    /// <param name="dpi">DPI setting for PDF rendering.</param>
    /// <param name="multiScale">Multi-scale detection enabled status.</param>
    /// <param name="enhancements">Image enhancements enabled status.</param>
    private static void LogProcessingStart(string[] inputs, DirectoryInfo output, int dpi, bool multiScale, bool enhancements)
    {
        Log.Information("AT QR Extractor started");
        Log.Information("Inputs: {Inputs}", string.Join(", ", inputs));
        Log.Information("Output: {Output}", output.FullName);
        Log.Information("DPI: {Dpi}", dpi);
        Log.Information("Multi-scale detection: {MultiScale}", multiScale);
        Log.Information("Image enhancements: {Enhancements}", enhancements);
    }

    /// <summary>
    /// Logs a summary of file processing results.
    /// </summary>
    /// <param name="result">Processing result containing statistics about files processed, QR codes found, and errors encountered.</param>
    private static void LogProcessingSummary(ProcessingResult result)
    {
        Log.Information("Processed {TotalFiles} files: {WithQr} with QR codes, {WithErrors} with errors",
            result.TotalFiles, result.FilesWithQrCodes, result.FilesWithErrors);

        if (result.FilesWithErrors > 0)
        {
            foreach (var fileResult in result.FileResults.Where(r => r.Errors.Any()))
            {
                Log.Warning("Errors in {File}: {Errors}",
                    Path.GetFileName(fileResult.FilePath),
                    string.Join("; ", fileResult.Errors));
            }
        }

        var totalQrCodes = result.FileResults.Sum(r => r.QrPayloads.Count);
        Log.Information("Found {Total} QR codes, {Unique} unique", totalQrCodes, result.UniquePayloads.Count);
    }

    /// <summary>
    /// Logs guidance when no QR codes are found in any files.
    /// </summary>
    private static void LogNoQrCodesFoundGuidance()
    {
        Log.Warning("No QR codes found in any files. Check that:");
        Log.Warning("  - Files contain visible QR codes");
        Log.Warning("  - QR codes are at least 2cm x 2cm (0.8in x 0.8in) in size");
        Log.Warning("  - QR codes have sufficient contrast");
        Log.Warning("  - Images are not overly compressed or degraded");
    }

    /// <summary>
    /// Logs a summary of compliance analysis results.
    /// </summary>
    /// <param name="interpretedData">Interpreted QR data with compliance information.</param>
    private static void LogComplianceSummary(List<InterpretedQrData> interpretedData)
    {
        var compliantCount = interpretedData.Count(d => d.IsCompliant);
        var nonCompliantCount = interpretedData.Count(d => !d.IsCompliant);

        Log.Information("Compliance check complete:");
        Log.Information("  ✓ Compliant: {Compliant}", compliantCount);
        Log.Information("  ✗ Non-compliant: {NonCompliant}", nonCompliantCount);

        if (nonCompliantCount > 0)
        {
            LogTopComplianceIssues(interpretedData);
        }
    }

    /// <summary>
    /// Logs the most common compliance issues found in non-compliant QR codes.
    /// </summary>
    /// <param name="interpretedData">Interpreted QR data with compliance notes.</param>
    private static void LogTopComplianceIssues(List<InterpretedQrData> interpretedData)
    {
        Log.Information("Common issues in non-compliant codes:");

        var topIssues = interpretedData
            .Where(d => !d.IsCompliant)
            .SelectMany(d => d.ComplianceNotes)
            .GroupBy(n => n)
            .OrderByDescending(g => g.Count())
            .Take(MaxDisplayedIssues);

        foreach (var issue in topIssues)
        {
            Log.Information("  • {Issue} ({Count} occurrences)", issue.Key, issue.Count());
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Deduplicates QR payloads across multiple file processing results.
    /// </summary>
    /// <param name="results">File processing results containing QR payloads.</param>
    /// <returns>Deduplicated list of unique QR payloads with consolidated source file lists.</returns>
    /// <remarks>
    /// QR payloads are considered duplicates if they have the same SHA-256 hash.
    /// When duplicates are found, their source file lists are merged.
    /// </remarks>
    private static List<QrPayload> DeduplicatePayloads(List<FileProcessingResult> results)
    {
        var uniquePayloads = new Dictionary<string, QrPayload>();

        foreach (var result in results)
        {
            foreach (var payload in result.QrPayloads)
            {
                if (!uniquePayloads.ContainsKey(payload.Hash))
                {
                    // First occurrence - add source file and store payload
                    payload.SourceFiles.Add(result.FilePath);
                    uniquePayloads[payload.Hash] = payload;
                }
                else
                {
                    // Duplicate found - merge source file if not already present
                    if (!uniquePayloads[payload.Hash].SourceFiles.Contains(result.FilePath))
                    {
                        uniquePayloads[payload.Hash].SourceFiles.Add(result.FilePath);
                    }
                }
            }
        }

        return uniquePayloads.Values.OrderBy(p => p.Hash).ToList();
    }

    /// <summary>
    /// Parses a string log level into the corresponding Serilog LogEventLevel.
    /// </summary>
    /// <param name="level">Log level string (case-insensitive).</param>
    /// <returns>Corresponding <see cref="Serilog.Events.LogEventLevel"/>, defaults to Information for invalid inputs.</returns>
    private static Serilog.Events.LogEventLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "debug" => Serilog.Events.LogEventLevel.Debug,
            "information" => Serilog.Events.LogEventLevel.Information,
            "warning" => Serilog.Events.LogEventLevel.Warning,
            "error" => Serilog.Events.LogEventLevel.Error,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }

    #endregion
}