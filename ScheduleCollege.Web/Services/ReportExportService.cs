using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScheduleCollege.Web.Models;
using SkiaSharp;

namespace ScheduleCollege.Web.Services;

public class ReportExportService
{
    public byte[] BuildPdf(
        List<ScheduleEntry> entries,
        List<StudentGroup> groups,
        List<TimeSlot> timeSlots,
        DateTime fromDate,
        DateTime toDate,
        string groupText,
        string roomText)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        entries ??= new List<ScheduleEntry>();
        groups ??= new List<StudentGroup>();
        timeSlots ??= new List<TimeSlot>();

        var dates = BuildDateList(fromDate, toDate);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text($"{fromDate:dd.MM} - {toDate:dd.MM}").FontSize(18).Bold();
                    column.Item().AlignCenter().Text("Расписание учебных занятий").FontSize(10);
                    column.Item().Text($"Группа: {groupText} | Аудитория: {roomText} | Всего занятий: {entries.Count}").FontSize(8);
                });

                page.Content().PaddingTop(8).Element(content => BuildPdfGrid(content, entries, groups, timeSlots, dates));

                page.Footer()
                    .AlignCenter()
                    .Text($"Сформировано {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .FontSize(7);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] BuildPng(
        List<ScheduleEntry> entries,
        List<StudentGroup> groups,
        List<TimeSlot> timeSlots,
        DateTime fromDate,
        DateTime toDate,
        string groupText,
        string roomText)
    {
        entries ??= new List<ScheduleEntry>();
        groups ??= new List<StudentGroup>();
        timeSlots ??= new List<TimeSlot>();

        var dates = BuildDateList(fromDate, toDate);

        const int margin = 28;
        const int titleHeight = 120;
        const int headerHeight = 66;
        const int dayWidth = 90;
        const int timeWidth = 95;
        const int pairWidth = 95;
        const int subjectWidth = 260;
        const int roomWidth = 85;
        const int rowHeight = 54;
        const int weekendHeight = 150;
        const int footerHeight = 40;

        int tableWidth = dayWidth + timeWidth + pairWidth + groups.Count * (subjectWidth + roomWidth);
        int width = Math.Max(tableWidth + margin * 2, 1200);

        int bodyHeight = 0;
        foreach (var date in dates)
        {
            bodyHeight += IsWeekend(date) ? weekendHeight : timeSlots.Count * rowHeight;
        }

        int height = margin * 2 + titleHeight + headerHeight + bodyHeight + footerHeight;

        using var bitmap = new SKBitmap(width, Math.Max(height, 500));
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 38,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold)
        };

        using var subTitlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 22,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Times New Roman")
        };

        using var headerPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold)
        };

        using var cellPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Times New Roman")
        };

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        using var fillPaint = new SKPaint
        {
            Color = new SKColor(245, 245, 245),
            Style = SKPaintStyle.Fill
        };

        int x = margin;
        int y = margin + 38;

        DrawCenteredText(canvas, $"{fromDate:dd.MM} - {toDate:dd.MM}", margin, y, tableWidth, titlePaint);
        y += 34;
        DrawCenteredText(canvas, "Расписание учебных занятий", margin, y, tableWidth, subTitlePaint);
        y += 32;
        canvas.DrawText($"Группа: {groupText} | Аудитория: {roomText} | Всего занятий: {entries.Count}", margin, y, subTitlePaint);

        int tableTop = margin + titleHeight;

        DrawHeader(canvas, groups, margin, tableTop, dayWidth, timeWidth, pairWidth, subjectWidth, roomWidth, headerHeight, fillPaint, borderPaint, headerPaint);

        int currentY = tableTop + headerHeight;

        foreach (var date in dates)
        {
            if (IsWeekend(date))
            {
                DrawCell(canvas, FormatDay(date), margin, currentY, dayWidth, weekendHeight, borderPaint, cellPaint, true);
                DrawCell(canvas, "ВЫХОДНОЙ", margin + dayWidth, currentY, tableWidth - dayWidth, weekendHeight, borderPaint, titlePaint, false);
                currentY += weekendHeight;
                continue;
            }

            bool firstSlot = true;
            foreach (var slot in timeSlots)
            {
                x = margin;

                if (firstSlot)
                {
                    DrawCell(canvas, FormatDay(date), x, currentY, dayWidth, timeSlots.Count * rowHeight, borderPaint, cellPaint, true);
                    firstSlot = false;
                }

                x += dayWidth;

                DrawCell(canvas, slot.StartTime + "\n" + slot.EndTime, x, currentY, timeWidth, rowHeight, borderPaint, cellPaint, false);
                x += timeWidth;

                DrawCell(canvas, FormatPair(slot.PairNumber), x, currentY, pairWidth, rowHeight, borderPaint, cellPaint, false);
                x += pairWidth;

                foreach (var group in groups)
                {
                    var entry = FindEntry(entries, date, slot.PairNumber, group.Id);
                    DrawCell(canvas, entry?.Subject?.Title ?? "", x, currentY, subjectWidth, rowHeight, borderPaint, cellPaint, false);
                    x += subjectWidth;
                    DrawCell(canvas, GetRoomText(entry), x, currentY, roomWidth, rowHeight, borderPaint, cellPaint, false);
                    x += roomWidth;
                }

                currentY += rowHeight;
            }
        }

        canvas.DrawText($"Сформировано {DateTime.Now:dd.MM.yyyy HH:mm}", margin, bitmap.Height - 12, subTitlePaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private static void BuildPdfGrid(
        IContainer container,
        List<ScheduleEntry> entries,
        List<StudentGroup> groups,
        List<TimeSlot> timeSlots,
        List<DateTime> dates)
    {
        if (groups.Count == 0 || timeSlots.Count == 0)
        {
            container.Text("Нет данных для формирования расписания.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(38);
                columns.ConstantColumn(42);
                columns.ConstantColumn(45);

                foreach (var group in groups)
                {
                    columns.RelativeColumn(1.4f);
                    columns.ConstantColumn(34);
                }
            });

            table.Header(header =>
            {
                HeaderCell(header, "День");
                HeaderCell(header, "Время");
                HeaderCell(header, "№ пары");

                foreach (var group in groups)
                {
                    HeaderCell(header, group.Code + "\nпредмет");
                    HeaderCell(header, "аудит");
                }
            });

            foreach (var date in dates)
            {
                if (IsWeekend(date))
                {
                    Cell(table, FormatDay(date));
                    Cell(table, "");
                    Cell(table, "ВЫХОДНОЙ");

                    foreach (var group in groups)
                    {
                        Cell(table, "");
                        Cell(table, "");
                    }

                    continue;
                }

                foreach (var slot in timeSlots)
                {
                    Cell(table, FormatDay(date));
                    Cell(table, slot.StartTime + "\n" + slot.EndTime);
                    Cell(table, FormatPair(slot.PairNumber));

                    foreach (var group in groups)
                    {
                        var entry = FindEntry(entries, date, slot.PairNumber, group.Id);
                        Cell(table, entry?.Subject?.Title ?? "");
                        Cell(table, GetRoomText(entry));
                    }
                }
            }
        });
    }

    private static void HeaderCell(QuestPDF.Fluent.TableCellDescriptor header, string text)
    {
        header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(text).SemiBold().FontSize(6);
    }

    private static void Cell(QuestPDF.Fluent.TableDescriptor table, string text)
    {
        table.Cell().Border(1).Padding(2).AlignCenter().AlignMiddle().Text(text ?? "").FontSize(5.5f);
    }

    private static ScheduleEntry? FindEntry(List<ScheduleEntry> entries, DateTime date, int pairNumber, int groupId)
    {
        return entries.FirstOrDefault(x =>
            x.StudyDate.Date == date.Date
            && x.TimeSlot?.PairNumber == pairNumber
            && x.GroupId == groupId);
    }

    private static string GetRoomText(ScheduleEntry? entry)
    {
        if (entry?.Room == null)
        {
            return "";
        }

        if (entry.Room.Format == "Онлайн")
        {
            return "онлайн";
        }

        return entry.Room.Number;
    }

    private static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    private static string FormatDay(DateTime date)
    {
        return GetRussianDayName(date.DayOfWeek) + " " + date.ToString("dd.MM");
    }

    private static string FormatPair(int pairNumber)
    {
        return pairNumber + " пара";
    }

    private static string GetRussianDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Понедельник",
            DayOfWeek.Tuesday => "Вторник",
            DayOfWeek.Wednesday => "Среда",
            DayOfWeek.Thursday => "Четверг",
            DayOfWeek.Friday => "Пятница",
            DayOfWeek.Saturday => "Суббота",
            DayOfWeek.Sunday => "Воскресенье",
            _ => ""
        };
    }

    private static List<DateTime> BuildDateList(DateTime fromDate, DateTime toDate)
    {
        var dates = new List<DateTime>();
        var current = fromDate.Date;
        var last = toDate.Date;

        while (current <= last)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }

        return dates;
    }

    private static void DrawHeader(
        SKCanvas canvas,
        List<StudentGroup> groups,
        int x,
        int y,
        int dayWidth,
        int timeWidth,
        int pairWidth,
        int subjectWidth,
        int roomWidth,
        int height,
        SKPaint fillPaint,
        SKPaint borderPaint,
        SKPaint paint)
    {
        DrawCell(canvas, "День\nнедели", x, y, dayWidth, height, borderPaint, paint, false, fillPaint);
        x += dayWidth;
        DrawCell(canvas, "Время", x, y, timeWidth, height, borderPaint, paint, false, fillPaint);
        x += timeWidth;
        DrawCell(canvas, "№\nпары", x, y, pairWidth, height, borderPaint, paint, false, fillPaint);
        x += pairWidth;

        foreach (var group in groups)
        {
            DrawCell(canvas, group.Code + "\nУчебный предмет", x, y, subjectWidth, height, borderPaint, paint, false, fillPaint);
            x += subjectWidth;
            DrawCell(canvas, "аудит", x, y, roomWidth, height, borderPaint, paint, false, fillPaint);
            x += roomWidth;
        }
    }

    private static void DrawCell(
        SKCanvas canvas,
        string text,
        int x,
        int y,
        int width,
        int height,
        SKPaint borderPaint,
        SKPaint textPaint,
        bool vertical,
        SKPaint? fillPaint = null)
    {
        if (fillPaint != null)
        {
            canvas.DrawRect(new SKRect(x, y, x + width, y + height), fillPaint);
        }

        canvas.DrawRect(new SKRect(x, y, x + width, y + height), borderPaint);

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.ClipRect(new SKRect(x + 4, y + 3, x + width - 4, y + height - 3));

        text ??= "";
        text = text.Replace("\r", "");

        if (vertical)
        {
            canvas.Translate(x + width / 2f, y + height / 2f);
            canvas.RotateDegrees(-90);
            DrawMultilineCenteredText(canvas, text, 0, 0, height - 8, textPaint);
            return;
        }

        DrawMultilineCenteredText(canvas, text, x + width / 2f, y + height / 2f, width - 8, textPaint);
    }

    private static void DrawCenteredText(SKCanvas canvas, string text, int x, int y, int width, SKPaint paint)
    {
        var textWidth = paint.MeasureText(text);
        canvas.DrawText(text, x + (width - textWidth) / 2f, y, paint);
    }

    private static void DrawMultilineCenteredText(SKCanvas canvas, string text, float centerX, float centerY, int maxWidth, SKPaint paint)
    {
        var lines = WrapText(text, maxWidth, paint);
        var lineHeight = paint.TextSize + 3;
        var startY = centerY - (lines.Count - 1) * lineHeight / 2f + paint.TextSize / 3f;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineWidth = paint.MeasureText(line);
            canvas.DrawText(line, centerX - lineWidth / 2f, startY + i * lineHeight, paint);
        }
    }

    private static List<string> WrapText(string text, int maxWidth, SKPaint paint)
    {
        var result = new List<string>();

        foreach (var originalLine in text.Split('\n'))
        {
            var words = originalLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                result.Add("");
                continue;
            }

            var current = "";

            foreach (var word in words)
            {
                var candidate = string.IsNullOrWhiteSpace(current) ? word : current + " " + word;

                if (paint.MeasureText(candidate) <= maxWidth)
                {
                    current = candidate;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        result.Add(current);
                    }

                    current = word;
                }
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                result.Add(current);
            }
        }

        return result.Count == 0 ? new List<string> { "" } : result;
    }
}
