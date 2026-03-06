using VegaFileConstructor.Models;

namespace VegaFileConstructor.Services;

public record PdfTextReplacementInput(int Order, string OldValue, string NewValue);
public record PdfTextReplacementResult(int Order, string OldValue, string NewValue, int FoundCount, int AppliedCount);
public record PdfTextReplaceSummary(IReadOnlyList<PdfTextReplacementResult> Rows, int TotalFoundOccurrences, int TotalAppliedReplacements);

public interface IPdfTextReplaceService
{
    Task<PdfTextReplaceSummary> ReplaceTextAsync(string sourcePath, string outputPath, IReadOnlyList<PdfTextReplacementInput> replacements);
}
