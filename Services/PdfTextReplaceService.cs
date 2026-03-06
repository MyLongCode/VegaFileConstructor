using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Text;

namespace VegaFileConstructor.Services;

public class PdfTextReplaceService : IPdfTextReplaceService
{
    private const float MinFontSize = 8f;
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Cp1251Encoding;

    static PdfTextReplaceService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp1251Encoding = Encoding.GetEncoding(1251);
    }

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
            workingText[i] = NormalizeExtractedText(text);
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

    private static string NormalizeExtractedText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var decoded = Cp1251Encoding.GetString(Latin1Encoding.GetBytes(value));

        var originalCyrillic = CountCyrillicLetters(value);
        var decodedCyrillic = CountCyrillicLetters(decoded);
        var originalLatin1Supplement = value.Count(ch => ch is >= '\u00C0' and <= '\u00FF');

        return decodedCyrillic > originalCyrillic && originalLatin1Supplement > 0
            ? decoded
            : value;
    }

    private static int CountCyrillicLetters(string value)
    {
        return value.Count(ch => ch is >= '\u0400' and <= '\u04FF');
    }
}
