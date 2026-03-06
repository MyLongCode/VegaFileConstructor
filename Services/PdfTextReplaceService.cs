using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace VegaFileConstructor.Services;

public class PdfTextReplaceService : IPdfTextReplaceService
{
    private const float MinFontSize = 8f;

    public async Task<PdfTextReplaceSummary> ReplaceTextAsync(string sourcePath, string outputPath, IReadOnlyList<PdfTextReplacementInput> replacements)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        await using var input = File.OpenRead(sourcePath);
        await using var output = File.Create(outputPath);
        using var pdfReader = new PdfReader(input);
        using var pdfWriter = new PdfWriter(output);
        using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

        var workingText = new Dictionary<int, string>();
        var results = new List<PdfTextReplacementResult>();

        for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
            workingText[i] = text;
        }

        foreach (var replacement in replacements.OrderBy(x => x.Order))
        {
            var found = 0;
            foreach (var pageNum in workingText.Keys.ToList())
            {
                found += CountOccurrences(workingText[pageNum], replacement.OldValue);
                workingText[pageNum] = workingText[pageNum].Replace(replacement.OldValue, replacement.NewValue, StringComparison.Ordinal);
            }

            results.Add(new PdfTextReplacementResult(replacement.Order, replacement.OldValue, replacement.NewValue, found, found));
        }

        for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

            canvas.SaveState();
            canvas.SetFillColorRgb(1, 1, 1);
            canvas.Rectangle(pageSize.GetLeft(), pageSize.GetBottom(), pageSize.GetWidth(), pageSize.GetHeight());
            canvas.Fill();
            canvas.RestoreState();

            using var document = new Document(pdfDoc);
            document.SetMargins(36, 36, 36, 36);
            document.ShowTextAligned(
                new Paragraph(FitText(workingText[i]))
                    .SetFontSize(MinFontSize)
                    .SetMultipliedLeading(1.1f),
                pageSize.GetLeft() + 36,
                pageSize.GetTop() - 36,
                i,
                TextAlignment.LEFT,
                VerticalAlignment.TOP,
                0);
        }

        var totalFound = results.Sum(x => x.FoundCount);
        var totalApplied = results.Sum(x => x.AppliedCount);
        return new PdfTextReplaceSummary(results, totalFound, totalApplied);
    }

    private static int CountOccurrences(string input, string search)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(search)) return 0;
        var count = 0;
        var index = 0;
        while ((index = input.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static string FitText(string value)
    {
        return value.Length <= 16000 ? value : value[..15997] + "...";
    }
}
