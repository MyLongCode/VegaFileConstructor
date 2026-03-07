using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using VegaFileConstructor.Models;

namespace VegaFileConstructor.Services;

public class PdfGeneratorService(IWebHostEnvironment env) : IPdfGeneratorService
{
    public Task<string> GenerateAsync(DocumentTemplate template, IEnumerable<TemplateFieldPlacement> placements, Dictionary<string, string> values, string outputPath)
    {
        var sourcePath = Path.Combine(env.WebRootPath, template.TemplateFilePath.Replace('/', Path.DirectorySeparatorChar));
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException($"Output path must include a directory: {outputPath}", nameof(outputPath));
        }

        Directory.CreateDirectory(outputDirectory);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Template PDF not found: {sourcePath}");
        }

        using var reader = new PdfReader(sourcePath);
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new PdfWriter(outputStream);
        using var pdf = new PdfDocument(reader, writer);
        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        foreach (var placement in placements)
        {
            if (!values.TryGetValue(placement.FieldKey, out var value)) continue;

            if (TryDrawImage(pdf, placement, value))
            {
                continue;
            }

            value = TrimValue(value, placement.MaxWidth);
            var page = pdf.GetPage(placement.Page);
            var canvas = new PdfCanvas(page);
            canvas.BeginText();
            canvas.SetFontAndSize(font, placement.FontSize);
            canvas.MoveText(placement.X, placement.Y);
            canvas.ShowText(value);
            canvas.EndText();
        }

        return Task.FromResult(outputPath);
    }

    private bool TryDrawImage(PdfDocument pdf, TemplateFieldPlacement placement, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith('/'))
        {
            return false;
        }

        var relativePath = value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var imagePath = Path.Combine(env.WebRootPath, relativePath);
        if (!File.Exists(imagePath))
        {
            return false;
        }

        var page = pdf.GetPage(placement.Page);
        var imageData = ImageDataFactory.Create(imagePath);

        var width = placement.MaxWidth ?? 120f;
        var ratio = imageData.GetHeight() / imageData.GetWidth();
        var height = width * ratio;
        var rect = new Rectangle(placement.X, placement.Y, width, height);
        new PdfCanvas(page).AddImageFittedIntoRectangle(imageData, rect, false);
        return true;
    }

    private static string TrimValue(string value, float? maxWidth)
    {
        var limit = maxWidth.HasValue ? Math.Clamp((int)(maxWidth.Value / 5f), 8, 120) : 120;
        if (value.Length <= limit) return value;
        return value[..(limit - 1)] + "…";
    }
}
