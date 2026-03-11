using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace VegaFileConstructor.Services;

public class PdfTextReplaceService(IWebHostEnvironment env) : IPdfTextReplaceService
{
    private const string DefaultFontPath = @"C:\Users\Пользователь\AppData\Local\Microsoft\Windows\Fonts\GOST.ttf";
    private const float MinFontSize = 4f;
    private const float MaxFontScale = 0.98f;
    private const float LineGroupTolerance = 2.5f;
    private const float WhiteoutPadding = 0f;
    private const float TextHorizontalInset = 2.2f;
    private const string ImageMarker = "img:";
    private const float SyntheticItalicAngleDegrees = 15f;

    private static readonly Color ReplacementBackgroundColor = new DeviceRgb(230, 230, 230);

    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Cp1251Encoding;

    static PdfTextReplaceService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp1251Encoding = Encoding.GetEncoding(1251);
    }

    public async Task<PdfTextReplaceSummary> ReplaceTextAsync(
        string sourcePath,
        string outputPath,
        IReadOnlyList<PdfTextReplacementInput> replacements)
    {
        if (replacements.Count == 0)
        {
            return new PdfTextReplaceSummary(Array.Empty<PdfTextReplacementResult>(), 0, 0);
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath)!);

        await using var input = File.OpenRead(sourcePath);
        await using var output = File.Create(outputPath);

        using var pdfReader = new PdfReader(input);
        using var pdfWriter = new PdfWriter(output);
        using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

        var replacementFont = CreateUnicodeFont();
        var pageModels = ExtractPageModels(pdfDoc);
        var orderedReplacements = replacements.OrderBy(x => x.Order).ToList();
        var results = new List<PdfTextReplacementResult>(orderedReplacements.Count);

        foreach (var replacement in orderedReplacements)
        {
            var foundCount = 0;
            var appliedCount = 0;

            foreach (var pageModel in pageModels)
            {
                var page = pdfDoc.GetPage(pageModel.PageNumber);
                var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

                foreach (var group in pageModel.Groups)
                {
                    var matches = FindMatches(group.NormalizedText, replacement.OldValue);
                    foundCount += matches.Count;

                    foreach (var match in matches)
                    {
                        var fragments = group.GetFragmentsInRange(match.Start, match.Length);
                        if (fragments.Count == 0)
                        {
                            continue;
                        }

                        var area = UnionBounds(fragments.Select(x => x.Bounds));
                        if (area.GetWidth() <= 0 || area.GetHeight() <= 0)
                        {
                            continue;
                        }

                        PaintBackgroundRectangle(canvas, area);
                        if (!TryDrawReplacementImage(canvas, replacement.NewValue, area))
                        {
                            DrawReplacementText(canvas, replacementFont, replacement.NewValue, area, fragments);
                        }
                        appliedCount++;
                    }
                }
            }

            results.Add(new PdfTextReplacementResult(
                replacement.Order,
                replacement.OldValue,
                replacement.NewValue,
                foundCount,
                appliedCount));
        }

        return new PdfTextReplaceSummary(
            results,
            results.Sum(x => x.FoundCount),
            results.Sum(x => x.AppliedCount));
    }
    private static void PaintBackgroundRectangle(PdfCanvas canvas, Rectangle area)
    {
        canvas.SaveState();
        canvas.SetFillColor(ReplacementBackgroundColor);
        canvas.Rectangle(
            area.GetX() - WhiteoutPadding,
            area.GetY() - WhiteoutPadding,
            area.GetWidth() + WhiteoutPadding * 2,
            area.GetHeight() + WhiteoutPadding * 2);
        canvas.Fill();
        canvas.RestoreState();
    }
    private bool TryDrawReplacementImage(PdfCanvas canvas, string replacementValue, Rectangle area)
    {
        if (!replacementValue.StartsWith(ImageMarker, StringComparison.Ordinal))
        {
            return false;
        }

        var relativeImagePath = replacementValue[ImageMarker.Length..];
        if (string.IsNullOrWhiteSpace(relativeImagePath) || !relativeImagePath.StartsWith('/'))
        {
            return false;
        }

        var imageAbsolutePath = System.IO.Path.Combine(env.WebRootPath, relativeImagePath.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
        if (!File.Exists(imageAbsolutePath))
        {
            return false;
        }

        var imageData = ImageDataFactory.Create(imageAbsolutePath);
        var imageWidth = imageData.GetWidth();
        var imageHeight = imageData.GetHeight();
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        var scale = Math.Min(area.GetWidth() / imageWidth, area.GetHeight() / imageHeight);
        var targetWidth = imageWidth * scale;
        var targetHeight = imageHeight * scale;
        var offsetX = area.GetX() + (area.GetWidth() - targetWidth) / 2f;
        var offsetY = area.GetY() + (area.GetHeight() - targetHeight) / 2f;

        var rect = new Rectangle(offsetX, offsetY, targetWidth, targetHeight);
        canvas.AddImageFittedIntoRectangle(imageData, rect, false);
        return true;
    }

    private static PdfFont CreateUnicodeFont()
    {
        if (File.Exists(DefaultFontPath))
        {
            return PdfFontFactory.CreateFont(DefaultFontPath, PdfEncodings.IDENTITY_H);
        }

        return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    }

    private static List<PageTextModel> ExtractPageModels(PdfDocument pdfDoc)
    {
        var pages = new List<PageTextModel>();
        for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var listener = new TextFragmentListener();
            var processor = new PdfCanvasProcessor(listener);
            processor.ProcessPageContent(pdfDoc.GetPage(i));

            var groups = BuildGroups(listener.Fragments);
            pages.Add(new PageTextModel(i, groups));
        }

        return pages;
    }

    private static List<TextGroup> BuildGroups(List<TextFragment> fragments)
    {
        var lines = new List<List<TextFragment>>();

        foreach (var fragment in fragments.OrderByDescending(x => x.BaselineY).ThenBy(x => x.Bounds.GetX()))
        {
            var line = lines.FirstOrDefault(x => Math.Abs(x[0].BaselineY - fragment.BaselineY) <= LineGroupTolerance);
            if (line is null)
            {
                lines.Add(new List<TextFragment> { fragment });
                continue;
            }

            line.Add(fragment);
        }

        var groups = new List<TextGroup>(lines.Count);
        foreach (var line in lines)
        {
            var ordered = line.OrderBy(x => x.Bounds.GetX()).ToList();
            groups.Add(TextGroup.Create(ordered));
        }

        return groups;
    }

    private static void PaintWhiteRectangle(PdfCanvas canvas, Rectangle area)
    {
        canvas.SaveState();
        canvas.SetFillColor(ColorConstants.WHITE);
        canvas.Rectangle(
            area.GetX() - WhiteoutPadding,
            area.GetY() - WhiteoutPadding,
            area.GetWidth() + WhiteoutPadding * 2,
            area.GetHeight() + WhiteoutPadding * 2);
        canvas.Fill();
        canvas.RestoreState();
    }

    private static void DrawReplacementText(
    PdfCanvas canvas,
    PdfFont font,
    string text,
    Rectangle area,
    List<TextFragment> originalFragments)
    {
        var estimatedSize = originalFragments.Average(x => x.FontSize);
        var targetSize = Math.Max(MinFontSize, estimatedSize * MaxFontScale);

        var skewX = (float)Math.Tan(SyntheticItalicAngleDegrees * Math.PI / 180d);

        var leftInset = TextHorizontalInset;
        var rightInset = TextHorizontalInset;

        var availableWidth = Math.Max(1f, area.GetWidth() - leftInset - rightInset);
        targetSize = FitText(font, text, targetSize, availableWidth, skewX);

        var baseline = originalFragments.Average(x => x.BaselineY);
        var textX = area.GetX() + leftInset;

        canvas.BeginText();
        canvas.SetFillColor(ColorConstants.BLACK);
        canvas.SetFontAndSize(font, targetSize);
        canvas.SetTextMatrix(1, 0, skewX, 1, textX, baseline);
        canvas.ShowText(text);
        canvas.EndText();
    }

    private static float FitText(
        PdfFont font,
        string value,
        float initialSize,
        float maxWidth,
        float skewX = 0f)
    {
        var size = initialSize;

        while (size > MinFontSize)
        {
            var plainWidth = font.GetWidth(value, size);

            var skewExtraWidth = Math.Abs(skewX) * size;

            if (plainWidth + skewExtraWidth <= maxWidth)
            {
                break;
            }

            size -= 0.2f;
        }

        return Math.Max(MinFontSize, size);
    }

    private static List<TextMatch> FindMatches(string text, string search)
    {
        var matches = new List<TextMatch>();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
        {
            return matches;
        }

        var index = 0;
        while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            matches.Add(new TextMatch(index, search.Length));
            index += search.Length;
        }

        return matches;
    }

    private static int CountOccurrences(string input, string search) => FindMatches(input, search).Count;

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
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return value.Count(ch => ch is >= '\u0400' and <= '\u04FF');
    }

    private static Rectangle UnionBounds(IEnumerable<Rectangle> rectangles)
    {
        var rects = rectangles.ToList();
        var left = rects.Min(x => x.GetX());
        var bottom = rects.Min(x => x.GetY());
        var right = rects.Max(x => x.GetX() + x.GetWidth());
        var top = rects.Max(x => x.GetY() + x.GetHeight());

        return new Rectangle(left, bottom, right - left, top - bottom);
    }

    private sealed class TextFragmentListener : IEventListener
    {
        public List<TextFragment> Fragments { get; } = new();

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT || data is not TextRenderInfo tri)
            {
                return;
            }

            var text = tri.GetText();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var ascent = tri.GetAscentLine().GetBoundingRectangle();
            var descent = tri.GetDescentLine().GetBoundingRectangle();
            var bounds = UnionBounds(new[] { ascent, descent });
            var normalized = NormalizeExtractedText(text);

            Fragments.Add(new TextFragment(
                text,
                normalized,
                bounds,
                tri.GetBaseline().GetStartPoint().Get(1),
                tri.GetFontSize()));
        }

        public ICollection<EventType> GetSupportedEvents() => new HashSet<EventType> { EventType.RENDER_TEXT };
    }

    private sealed record TextFragment(string RawText, string NormalizedText, Rectangle Bounds, float BaselineY, float FontSize)
    {
        public int StartIndex { get; init; }
    }

    private sealed class TextGroup
    {
        public string NormalizedText { get; }
        public IReadOnlyList<TextFragment> Fragments { get; }

        private TextGroup(string normalizedText, IReadOnlyList<TextFragment> fragments)
        {
            NormalizedText = normalizedText;
            Fragments = fragments;
        }

        public static TextGroup Create(List<TextFragment> fragments)
        {
            var list = new List<TextFragment>(fragments.Count);
            var sb = new StringBuilder();
            var index = 0;

            foreach (var fragment in fragments)
            {
                list.Add(fragment with { StartIndex = index });
                sb.Append(fragment.NormalizedText);
                index += fragment.NormalizedText.Length;
            }

            return new TextGroup(sb.ToString(), list);
        }

        public List<TextFragment> GetFragmentsInRange(int start, int length)
        {
            var end = start + length;
            return Fragments
                .Where(fragment =>
                {
                    var fragmentStart = fragment.StartIndex;
                    var fragmentEnd = fragment.StartIndex + fragment.NormalizedText.Length;
                    return fragmentEnd > start && fragmentStart < end;
                })
                .ToList();
        }
    }

    private sealed record PageTextModel(int PageNumber, List<TextGroup> Groups);
    private sealed record TextMatch(int Start, int Length);
}
