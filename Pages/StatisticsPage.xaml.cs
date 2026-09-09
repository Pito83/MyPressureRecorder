using Microcharts;
using SkiaSharp;
using MyPressureRecorder.Models;
using MyPressureRecorder.Data;

namespace MyPressureRecorder.Pages;

public partial class StatisticsPage : ContentPage
{
    private readonly AppDatabase _database;
    private readonly UserProfile _user;

    public StatisticsPage(UserProfile user, AppDatabase database)
    {
        InitializeComponent();
        _database = database;
        _user = user;
        Title = $"Statistiche: {_user.Name}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PeriodPicker.SelectedIndex = 1; // default: 1 mese
        await LoadAndDisplayData();
    }

    private async void OnFilterChanged(object sender, EventArgs e)
    {
        await LoadAndDisplayData();
    }

    private DateTime PeriodFrom()
    {
        var now = DateTime.Now;
        return PeriodPicker.SelectedIndex switch
        {
            0 => now.AddDays(-7),
            1 => now.AddMonths(-1),
            2 => now.AddMonths(-3),
            3 => now.AddYears(-1),
            _ => DateTime.MinValue
        };
    }

    private async Task LoadAndDisplayData()
    {
        var allReadings = await _database.GetReadingsByUserAsync(_user.Id);
        var from = PeriodFrom();

        var filtered = allReadings
            .Where(r => r.MeasurementTime >= from)
            .OrderBy(r => r.MeasurementTime)
            .ToList();

        if (!filtered.Any())
        {
            MaxChart.Chart = null;
            MinChart.Chart = null;
            HeartChart.Chart = null;
            AvgMaxLabel.Text = "Nessun dato";
            AvgMinLabel.Text = "";
            AvgHeartLabel.Text = "";
            return;
        }

        // Calcola medie
        var avgMax = (int)filtered.Average(r => r.MaxPressure);
        var avgMin = (int)filtered.Average(r => r.MinPressure);
        var avgHeart = (int)filtered.Average(r => r.HeartRate);

        AvgMaxLabel.Text = $"Media massima: {avgMax} mmHg";
        AvgMinLabel.Text = $"Media minima: {avgMin} mmHg";
        AvgHeartLabel.Text = $"Media battiti: {avgHeart} bpm";

        // Costruisci grafico con 3 curve (una per tipo)
        var maxEntries = filtered.Select(r =>
            new ChartEntry(r.MaxPressure)
            {
                Label = r.MeasurementTime.ToString("dd/MM"),
                ValueLabel = r.MaxPressure.ToString(),
                Color = SKColor.Parse("#FF0000")
            }).ToList();

        var minEntries = filtered.Select(r =>
            new ChartEntry(r.MinPressure)
            {
                Label = r.MeasurementTime.ToString("dd/MM"),
                ValueLabel = r.MinPressure.ToString(),
                Color = SKColor.Parse("#0000FF")
            }).ToList();

        var heartEntries = filtered.Select(r =>
            new ChartEntry(r.HeartRate)
            {
                Label = r.MeasurementTime.ToString("dd/MM"),
                ValueLabel = r.HeartRate.ToString(),
                Color = SKColor.Parse("#00AA00")
            }).ToList();

        // Sovrapporre i grafici � limitato in Microcharts.
        // Per ora mostriamo solo la pressione massima. (alternativa: switch o multi-grafico)
        
        MaxChart.Chart = new LineChart { Entries = maxEntries, LineSize = 4, PointSize = 8, PointMode = PointMode.Square, LineMode = LineMode.Straight };
        MinChart.Chart = new LineChart { Entries = minEntries, LineSize = 4, PointSize = 8, PointMode = PointMode.Square, LineMode = LineMode.Straight };
        HeartChart.Chart = new LineChart { Entries = heartEntries, LineSize = 4, PointSize = 8, PointMode = PointMode.Square, LineMode = LineMode.Straight };


        // Vuoi mostrare anche minima e battiti in modo alternato/switch? Posso aiutarti dopo.


    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var allReadings = await _database.GetReadingsByUserAsync(_user.Id);
        var from = PeriodFrom();

        var filtered = allReadings
            .Where(r => r.MeasurementTime >= from)
            .OrderBy(r => r.MeasurementTime)
            .ToList();

        if (filtered.Count == 0)
        {
            await DisplayAlert("Attenzione", "Non ci sono dati nel periodo selezionato.", "OK");
            return;
        }

        var fileName = $"Pressione_{CsvService.SafeFileName(_user.Name)}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        await ShareCsvAsync(CsvService.Export(filtered), fileName, "Condividi il CSV del periodo");
    }

    private async void OnFullBackupClicked(object sender, EventArgs e)
    {
        var allReadings = (await _database.GetReadingsByUserAsync(_user.Id))
            .OrderBy(r => r.MeasurementTime)
            .ToList();

        if (allReadings.Count == 0)
        {
            await DisplayAlert("Attenzione", "Non ci sono misurazioni da salvare.", "OK");
            return;
        }

        var fileName = $"Pressione-backup_{CsvService.SafeFileName(_user.Name)}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        await ShareCsvAsync(CsvService.Export(allReadings), fileName,
            $"Backup completo di «{_user.Name}» ({allReadings.Count} misurazioni)");
    }

    private async Task ShareCsvAsync(string csv, string fileName, string shareTitle)
    {
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        try
        {
            File.WriteAllText(filePath, csv);
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = shareTitle,
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Errore nella condivisione: {ex.Message}", "OK");
        }
    }

}
