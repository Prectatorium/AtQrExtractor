namespace AtQrExtractor.Services;

using OfficeOpenXml;
using OfficeOpenXml.Style;
using AtQrExtractor.Models;
using Serilog;
using System.Drawing;

/// <summary>
/// Generates comprehensive Excel reports from validated AT QR code data.
/// </summary>
/// <remarks>
/// <para>
/// Produces multi-sheet workbooks containing summary statistics, detailed field analysis,
/// and non-compliance issue tracking. Each report includes visual formatting with
/// color-coded compliance indicators and organized field groupings.
/// </para>
/// <para><b>Report Structure:</b></para>
/// <list type="bullet">
///   <item><description><b>Summary Sheet:</b> Aggregate statistics, document type breakdown, and common validation issues</description></item>
///   <item><description><b>Detailed Analysis Sheet:</b> Complete field-by-field breakdown with validation status for each QR code</description></item>
///   <item><description><b>Issues Only Sheet:</b> Filtered view of non-compliant QR codes for quick remediation</description></item>
/// </list>
/// <para><b>Dependencies:</b></para>
/// <para>
/// Requires EPPlus library. For commercial use, ensure proper EPPlus licensing.
/// Set <see cref="ExcelPackage.LicenseContext"/> before first use.
/// </para>
/// </remarks>
public sealed class ExcelGenerator
{
    #region Constants

    private const string SummarySheetName = "Summary";
    private const string DetailedSheetName = "Detailed Analysis";
    private const string IssuesSheetName = "Issues Only";
    private const int DefaultFirstColumnWidth = 50;
    private const int DetailedValueColumnWidth = 60;
    private const int ValidationMessageColumnWidth = 40;
    private const int SourceFileColumnWidth = 30;
    private const int IssuesColumnWidth = 60;
    private const int MaxCommonIssuesCount = 10;

    #endregion

    /// <summary>
    /// Generates a comprehensive Excel report from interpreted QR code data.
    /// </summary>
    /// <param name="data">The collection of validated and interpreted QR codes to include in the report.</param>
    /// <param name="outputPath">The absolute file path where the Excel workbook will be saved.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> or <paramref name="outputPath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when the output file cannot be written (e.g., file is locked, insufficient permissions).
    /// </exception>
    /// <remarks>
    /// Creates a new Excel workbook with three worksheets providing different analytical views
    /// of the QR code data. The file is created or overwritten at the specified path.
    /// </remarks>
    public void Generate(List<InterpretedQrData> data, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ArgumentNullException.ThrowIfNull(outputPath, nameof(outputPath));

        Log.Information("Generating Excel report: {Path}", outputPath);

        using var package = new ExcelPackage();

        GenerateSummarySheet(package, data);
        GenerateDetailedSheet(package, data);
        GenerateIssuesSheet(package, data);

        var fileInfo = new FileInfo(outputPath);
        package.SaveAs(fileInfo);

        Log.Information("Excel report generated successfully at {Path}", outputPath);
    }

    #region Summary Sheet Generation

    /// <summary>
    /// Generates the summary worksheet with aggregate statistics and trends.
    /// </summary>
    /// <param name="package">The Excel package to add the worksheet to.</param>
    /// <param name="data">The interpreted QR data for analysis.</param>
    /// <remarks>
    /// Creates a summary view including total counts, compliance statistics,
    /// document type breakdown, and most common validation issues.
    /// </remarks>
    private void GenerateSummarySheet(ExcelPackage package, List<InterpretedQrData> data)
    {
        var worksheet = package.Workbook.Worksheets.Add(SummarySheetName);
        int row = 1;

        worksheet.Cells[row, 1].Value = "AT QR Code Analysis Report - Summary";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 16;
        row += 2;

        AddMetadataRow(worksheet, ref row, "Generated:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddMetadataRow(worksheet, ref row, "Total QR Codes:", data.Count.ToString());
        AddMetadataRow(worksheet, ref row, "Compliant:", data.Count(d => d.IsCompliant).ToString(), Color.DarkGreen);
        AddMetadataRow(worksheet, ref row, "Non-Compliant:", data.Count(d => !d.IsCompliant).ToString(), Color.DarkRed);
        row += 2;

        var byDocType = data
            .GroupBy(d => d.Fields.FirstOrDefault(f => f.Code == "D")?.Value ?? "Unknown")
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count(),
                Compliant = g.Count(d => d.IsCompliant)
            })
            .OrderByDescending(x => x.Count);

        AddSectionHeader(worksheet, ref row, "Statistics by Document Type");
        AddTableHeader(worksheet, row, new[] { "Document Type", "Total", "Compliant", "Non-Compliant", "Compliance %" });
        row++;

        foreach (var stat in byDocType)
        {
            worksheet.Cells[row, 1].Value = stat.Type;
            worksheet.Cells[row, 2].Value = stat.Count;
            worksheet.Cells[row, 3].Value = stat.Compliant;
            worksheet.Cells[row, 4].Value = stat.Count - stat.Compliant;
            worksheet.Cells[row, 5].Value = stat.Count > 0 ? (double)stat.Compliant / stat.Count : 0;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "0.0%";
            row++;
        }

        row += 2;

        AddSectionHeader(worksheet, ref row, "Most Common Validation Issues");
        var commonIssues = data
            .Where(d => !d.IsCompliant)
            .SelectMany(d => d.ComplianceNotes)
            .GroupBy(note => note)
            .Select(g => new { Issue = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxCommonIssuesCount);

        AddTableHeader(worksheet, row, new[] { "Issue", "Occurrences" });
        row++;

        foreach (var issue in commonIssues)
        {
            worksheet.Cells[row, 1].Value = issue.Issue;
            worksheet.Cells[row, 2].Value = issue.Count;
            row++;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Column(1).Width = DefaultFirstColumnWidth;
    }

    #endregion

    #region Detailed Sheet Generation

    /// <summary>
    /// Generates the detailed analysis worksheet with complete field-level data.
    /// </summary>
    /// <param name="package">The Excel package to add the worksheet to.</param>
    /// <param name="data">The interpreted QR data to analyze.</param>
    /// <remarks>
    /// Creates comprehensive sections for each QR code, organized by field categories
    /// (mandatory, optional, and tax region fields). Includes validation status and
    /// color-coded compliance indicators.
    /// </remarks>
    private void GenerateDetailedSheet(ExcelPackage package, List<InterpretedQrData> data)
    {
        var worksheet = package.Workbook.Worksheets.Add(DetailedSheetName);
        int currentRow = 1;

        AddSectionHeader(worksheet, ref currentRow, "AT QR Code Detailed Analysis");
        currentRow++;

        foreach (var qrData in data.OrderBy(d => d.IsCompliant).ThenBy(d => d.Hash))
        {
            currentRow = AddQrDataSection(worksheet, qrData, currentRow);
            currentRow += 2;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Column(2).Width = DetailedValueColumnWidth;
        worksheet.Column(8).Width = ValidationMessageColumnWidth;
    }

    /// <summary>
    /// Adds a complete detailed section for a single QR code entry.
    /// </summary>
    /// <param name="worksheet">The worksheet to modify.</param>
    /// <param name="qrData">The QR code data to display.</param>
    /// <param name="startRow">The starting row index for this section.</param>
    /// <returns>The row index immediately following the added section.</returns>
    /// <remarks>
    /// Creates a formatted section with header bar, metadata, and organized field tables
    /// grouped by category (mandatory, tax regions I/J/K, optional).
    /// </remarks>
    private int AddQrDataSection(ExcelWorksheet worksheet, InterpretedQrData qrData, int startRow)
    {
        int currentRow = startRow;

        worksheet.Cells[currentRow, 1].Value = "QR CODE ANALYSIS";
        worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
        worksheet.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(
            qrData.IsCompliant ? Color.LightGreen : Color.LightCoral);
        worksheet.Cells[currentRow, 1, currentRow, 10].Merge = true;
        currentRow++;

        AddMetadataRow(worksheet, ref currentRow, "Compliance Status:",
            qrData.IsCompliant ? "COMPLIANT" : "NON-COMPLIANT",
            qrData.IsCompliant ? Color.DarkGreen : Color.DarkRed);
        AddMetadataRow(worksheet, ref currentRow, "Hash (SHA-256):", qrData.Hash);
        AddMetadataRow(worksheet, ref currentRow, "Source Files:",
            string.Join("; ", qrData.SourceFiles.Select(Path.GetFileName)));

        if (qrData.ComplianceNotes.Any())
        {
            AddMetadataRow(worksheet, ref currentRow, "Compliance Notes:",
                string.Join(" | ", qrData.ComplianceNotes), Color.Red);
        }

        currentRow++;

        var mandatoryFields = qrData.Fields.Where(f => f.IsMandatory &&
            !f.Code.StartsWith("I") && !f.Code.StartsWith("J") && !f.Code.StartsWith("K")).ToList();
        var taxRegionI = qrData.Fields.Where(f => f.Code.StartsWith("I")).ToList();
        var taxRegionJ = qrData.Fields.Where(f => f.Code.StartsWith("J")).ToList();
        var taxRegionK = qrData.Fields.Where(f => f.Code.StartsWith("K")).ToList();
        var otherFields = qrData.Fields.Where(f => !f.IsMandatory &&
            !f.Code.StartsWith("I") && !f.Code.StartsWith("J") && !f.Code.StartsWith("K")).ToList();

        if (mandatoryFields.Any())
        {
            AddSubsectionHeader(worksheet, ref currentRow, "Mandatory Fields");
            currentRow = AddFieldTable(worksheet, currentRow, mandatoryFields);
            currentRow++;
        }

        if (taxRegionI.Any())
        {
            var regionCode = taxRegionI.FirstOrDefault(f => f.Code == "I1")?.Value ?? "Unknown";
            AddSubsectionHeader(worksheet, ref currentRow, $"Tax Region 1 ({regionCode})");
            currentRow = AddFieldTable(worksheet, currentRow, taxRegionI);
            currentRow++;
        }

        if (taxRegionJ.Any())
        {
            var regionCode = taxRegionJ.FirstOrDefault(f => f.Code == "J1")?.Value ?? "Unknown";
            AddSubsectionHeader(worksheet, ref currentRow, $"Tax Region 2 ({regionCode})");
            currentRow = AddFieldTable(worksheet, currentRow, taxRegionJ);
            currentRow++;
        }

        if (taxRegionK.Any())
        {
            var regionCode = taxRegionK.FirstOrDefault(f => f.Code == "K1")?.Value ?? "Unknown";
            AddSubsectionHeader(worksheet, ref currentRow, $"Tax Region 3 ({regionCode})");
            currentRow = AddFieldTable(worksheet, currentRow, taxRegionK);
            currentRow++;
        }

        if (otherFields.Any())
        {
            AddSubsectionHeader(worksheet, ref currentRow, "Optional Fields");
            currentRow = AddFieldTable(worksheet, currentRow, otherFields);
            currentRow++;
        }

        return currentRow;
    }

    /// <summary>
    /// Adds a formatted table of QR fields with validation status.
    /// </summary>
    /// <param name="worksheet">The worksheet to modify.</param>
    /// <param name="startRow">The starting row for the table.</param>
    /// <param name="fields">The fields to display in the table.</param>
    /// <returns>The row index immediately following the table.</returns>
    /// <remarks>
    /// Creates a table with headers and data rows. Invalid fields are highlighted
    /// with a yellow background for easy identification.
    /// </remarks>
    private int AddFieldTable(ExcelWorksheet worksheet, int startRow, List<QrField> fields)
    {
        int currentRow = startRow;

        string[] headers = { "Code", "Description", "Value", "Mandatory", "Max Len", "Actual Len", "Valid", "Validation Message" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cells[currentRow, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        currentRow++;

        foreach (var field in fields.OrderBy(f => f.SequenceIndex))
        {
            worksheet.Cells[currentRow, 1].Value = field.Code;
            worksheet.Cells[currentRow, 2].Value = field.Description;
            worksheet.Cells[currentRow, 3].Value = field.Value;
            worksheet.Cells[currentRow, 4].Value = field.IsMandatory ? "Yes" : "No";
            worksheet.Cells[currentRow, 5].Value = field.MaxLengthBytes < 999999 ? field.MaxLengthBytes : "N/A";
            worksheet.Cells[currentRow, 6].Value = field.ActualLengthBytes;
            worksheet.Cells[currentRow, 7].Value = field.IsValid ? "Yes" : "No";
            worksheet.Cells[currentRow, 8].Value = field.ValidationMessage;

            if (!field.IsValid)
            {
                worksheet.Cells[currentRow, 1, currentRow, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 1, currentRow, 8].Style.Fill.BackgroundColor.SetColor(Color.Khaki);
                worksheet.Cells[currentRow, 7].Style.Font.Color.SetColor(Color.DarkRed);
                worksheet.Cells[currentRow, 7].Style.Font.Bold = true;
            }

            currentRow++;
        }

        return currentRow;
    }

    #endregion

    #region Issues Sheet Generation

    /// <summary>
    /// Generates the issues worksheet showing only non-compliant QR codes.
    /// </summary>
    /// <param name="package">The Excel package to add the worksheet to.</param>
    /// <param name="data">The interpreted QR data to filter for issues.</param>
    /// <remarks>
    /// Creates a filtered view displaying only QR codes that failed compliance validation,
    /// with their issues highlighted for quick remediation. If all QR codes are compliant,
    /// displays a success message instead.
    /// </remarks>
    private void GenerateIssuesSheet(ExcelPackage package, List<InterpretedQrData> data)
    {
        var worksheet = package.Workbook.Worksheets.Add(IssuesSheetName);
        int row = 1;

        var nonCompliant = data.Where(d => !d.IsCompliant).ToList();

        if (!nonCompliant.Any())
        {
            worksheet.Cells[row, 1].Value = "No compliance issues found - all QR codes are valid!";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.DarkGreen);
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            return;
        }

        AddSectionHeader(worksheet, ref row, $"Non-Compliant QR Codes ({nonCompliant.Count})");
        row++;

        AddTableHeader(worksheet, row, new[] { "Hash", "Document Type", "Document ID", "Source Files", "Issues" });
        row++;

        foreach (var qr in nonCompliant)
        {
            var docType = qr.Fields.FirstOrDefault(f => f.Code == "D")?.Value ?? "N/A";
            var docId = qr.Fields.FirstOrDefault(f => f.Code == "G")?.Value ?? "N/A";
            var issues = string.Join(" | ", qr.ComplianceNotes);

            worksheet.Cells[row, 1].Value = qr.Hash.Substring(0, Math.Min(12, qr.Hash.Length));
            worksheet.Cells[row, 2].Value = docType;
            worksheet.Cells[row, 3].Value = docId;
            worksheet.Cells[row, 4].Value = string.Join("; ", qr.SourceFiles.Select(Path.GetFileName));
            worksheet.Cells[row, 5].Value = issues;
            worksheet.Cells[row, 5].Style.WrapText = true;

            worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(Color.MistyRose);

            row++;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Column(4).Width = SourceFileColumnWidth;
        worksheet.Column(5).Width = IssuesColumnWidth;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds a section header row with consistent formatting.
    /// </summary>
    /// <param name="ws">The worksheet to modify.</param>
    /// <param name="row">The current row index, incremented after adding the header.</param>
    /// <param name="title">The header text to display.</param>
    private void AddSectionHeader(ExcelWorksheet ws, ref int row, string title)
    {
        ws.Cells[row, 1].Value = title;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 1].Style.Font.Size = 14;
        ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
        ws.Cells[row, 1, row, 5].Merge = true;
        row++;
    }

    /// <summary>
    /// Adds a subsection header row with lighter formatting than main section headers.
    /// </summary>
    /// <param name="ws">The worksheet to modify.</param>
    /// <param name="row">The current row index, incremented after adding the header.</param>
    /// <param name="title">The header text to display.</param>
    private void AddSubsectionHeader(ExcelWorksheet ws, ref int row, string title)
    {
        ws.Cells[row, 1].Value = title;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 1].Style.Font.Size = 11;
        ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        ws.Cells[row, 1, row, 8].Merge = true;
        row++;
    }

    /// <summary>
    /// Adds a table header row with consistent styling.
    /// </summary>
    /// <param name="ws">The worksheet to modify.</param>
    /// <param name="row">The row index where the header should be placed.</param>
    /// <param name="headers">Array of column header texts.</param>
    private void AddTableHeader(ExcelWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cells[row, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    /// <summary>
    /// Adds a metadata row with label-value formatting.
    /// </summary>
    /// <param name="ws">The worksheet to modify.</param>
    /// <param name="row">The current row index, incremented after adding the metadata.</param>
    /// <param name="label">The label text (displayed in bold in column 1).</param>
    /// <param name="value">The value text (displayed in column 2).</param>
    /// <param name="valueColor">Optional color for the value text, typically used for status indicators.</param>
    private void AddMetadataRow(ExcelWorksheet ws, ref int row, string label, string value, Color? valueColor = null)
    {
        ws.Cells[row, 1].Value = label;
        ws.Cells[row, 1].Style.Font.Bold = true;

        var valueCell = ws.Cells[row, 2];
        valueCell.Value = value;
        valueCell.Style.WrapText = true;

        if (valueColor.HasValue)
        {
            valueCell.Style.Font.Color.SetColor(valueColor.Value);
            valueCell.Style.Font.Bold = true;
        }

        row++;
    }

    #endregion
}