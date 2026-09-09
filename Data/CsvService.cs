using System.Globalization;
using System.Text;
using MyPressureRecorder.Models;

namespace MyPressureRecorder.Data;

/// <summary>
/// Serializzazione/parsing del CSV delle misurazioni.
/// Formato: <c>Data/Ora;Massima;Minima;Battiti</c> con data <c>dd/MM/yyyy HH:mm</c>.
/// </summary>
public static class CsvService
{
    public const string Header = "Data/Ora;Massima;Minima;Battiti";
    private const string DateFormat = "dd/MM/yyyy HH:mm";

    // Limiti di plausibilità: fuori range la riga viene segnalata ma NON scartata.
    private const int MinPlausible = 20;
    private const int MaxPlausible = 320;

    public static string Export(IEnumerable<PressureReading> readings)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var r in readings.OrderBy(r => r.MeasurementTime))
        {
            sb.AppendLine(string.Join(';',
                r.MeasurementTime.ToString(DateFormat, CultureInfo.InvariantCulture),
                r.MaxPressure,
                r.MinPressure,
                r.HeartRate));
        }
        return sb.ToString();
    }

    public sealed class ImportResult
    {
        public List<PressureReading> Readings { get; } = new();
        public int InvalidRows { get; set; }
        public int OutOfRangeRows { get; set; }
        public List<string> Errors { get; } = new();
    }

    public static ImportResult Parse(string content, Guid userId)
    {
        var result = new ImportResult();
        if (string.IsNullOrWhiteSpace(content))
            return result;

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        char separator = ';';
        bool separatorDetected = false;
        bool firstContentLine = true;
        int lineNo = 0;

        foreach (var rawLine in lines)
        {
            lineNo++;
            var line = rawLine.Trim().TrimStart('﻿').Trim();
            if (line.Length == 0)
                continue;

            if (!separatorDetected)
            {
                separator = line.Contains(';') ? ';' : ',';
                separatorDetected = true;
            }

            var parts = line.Split(separator);
            var dateText = parts[0].Trim();

            var hasDate = DateTime.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out var measurementTime)
                          || DateTime.TryParse(dateText, CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out measurementTime);

            if (!hasDate)
            {
                // La prima riga di contenuto senza data è considerata intestazione.
                if (firstContentLine)
                {
                    firstContentLine = false;
                    continue;
                }
                result.InvalidRows++;
                AddError(result, $"Riga {lineNo}: data non valida (\"{dateText}\")");
                continue;
            }

            firstContentLine = false;

            if (parts.Length < 4)
            {
                result.InvalidRows++;
                AddError(result, $"Riga {lineNo}: numero di colonne insufficiente");
                continue;
            }

            if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var max) ||
                !int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) ||
                !int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var heart))
            {
                result.InvalidRows++;
                AddError(result, $"Riga {lineNo}: valori non numerici");
                continue;
            }

            if (IsOutOfRange(max) || IsOutOfRange(min) || IsOutOfRange(heart))
                result.OutOfRangeRows++;

            result.Readings.Add(new PressureReading
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MeasurementTime = measurementTime,
                MaxPressure = max,
                MinPressure = min,
                HeartRate = heart
            });
        }

        return result;
    }

    /// <summary>
    /// Rimuove dalla lista <paramref name="incoming"/> le misurazioni già presenti in
    /// <paramref name="existing"/> (stessa data/ora e stessi valori).
    /// </summary>
    public static List<PressureReading> Deduplicate(
        IEnumerable<PressureReading> incoming,
        IEnumerable<PressureReading> existing,
        out int skipped)
    {
        var keys = new HashSet<string>(existing.Select(KeyOf));
        var kept = new List<PressureReading>();
        skipped = 0;

        foreach (var reading in incoming)
        {
            if (keys.Add(KeyOf(reading)))
                kept.Add(reading);
            else
                skipped++;
        }

        return kept;
    }

    /// <summary>
    /// Prova a ricavare il nome utente dal nome file prodotto dall'export
    /// (<c>Pressione_{Nome}_{timestamp}.csv</c> o <c>Pressione-backup_{Nome}_{timestamp}.csv</c>).
    /// Restituisce <c>null</c> se il pattern non combacia.
    /// </summary>
    public static string? GuessUserNameFromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var name = Path.GetFileNameWithoutExtension(fileName);

        foreach (var prefix in new[] { "Pressione-backup_", "Pressione_" })
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[prefix.Length..];
                break;
            }
        }

        var lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore <= 0)
            return null;

        var candidate = name[..lastUnderscore].Trim();
        return candidate.Length == 0 ? null : candidate;
    }

    /// <summary>Ripulisce una stringa perché sia usabile in un nome file.</summary>
    public static string SafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "utente";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Trim('_', '.').Length == 0 ? "utente" : cleaned;
    }

    private static bool IsOutOfRange(int value) => value < MinPlausible || value > MaxPlausible;

    private static void AddError(ImportResult result, string message)
    {
        if (result.Errors.Count < 5)
            result.Errors.Add(message);
    }

    private static string KeyOf(PressureReading r) =>
        $"{r.MeasurementTime:yyyyMMddHHmm}|{r.MaxPressure}|{r.MinPressure}|{r.HeartRate}";
}
