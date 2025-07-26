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

    private async Task LoadAndDisplayData()
    {
        var allReadings = await _database.GetReadingsByUserAsync(_user.Id);
        var now = DateTime.Now;

        DateTime from = PeriodPicker.SelectedIndex switch
        {
            0 => now.AddDays(-7),
            1 => now.AddMonths(-1),
            2 => now.AddMonths(-3),
            3 => now.AddYears(-1),
            _ => DateTime.MinValue
        };

        var filtered = allReadings
            .Where(r => r.MeasurementTime >= from)
            .OrderBy(r => r.MeasurementTime)
            .ToList();

        if (!filtered.Any())
        {
            ChartView.Chart = null;
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

        // Sovrapporre i grafici è limitato in Microcharts.
        // Per ora mostriamo solo la pressione massima. (alternativa: switch o multi-grafico)

        ChartView.Chart = new LineChart
        {
            Entries = maxEntries,
            LineMode = LineMode.Straight,
            LineSize = 5,
            PointMode = PointMode.Circle,
            PointSize = 10
        };

        // Vuoi mostrare anche minima e battiti in modo alternato/switch? Posso aiutarti dopo.


    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

}
