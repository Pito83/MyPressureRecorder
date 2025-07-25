using MyPressureRecorder.Models;
using MyPressureRecorder.Data;

namespace MyPressureRecorder.Pages;

public partial class ReadingsPage : ContentPage
{
    private readonly AppDatabase _database;
    private readonly UserProfile _user;

    public ReadingsPage(UserProfile user, AppDatabase database)
    {
        InitializeComponent();
        _database = database;
        _user = user;
        Title = $"Misurazioni: {_user.Name}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DatePicker.Date = DateTime.Now.Date;
        TimePicker.Time = DateTime.Now.TimeOfDay;
        await LoadReadings();
    }

    private async Task LoadReadings()
    {
        var readings = await _database.GetReadingsByUserAsync(_user.Id);
        var displayList = readings.Select(r => new
        {
            r.MeasurementTime,
            DisplayString = $"{r.MaxPressure}/{r.MinPressure} mmHg - {r.HeartRate} bpm"
        });
        ReadingsList.ItemsSource = displayList;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(MaxEntry.Text, out var max) ||
            !int.TryParse(MinEntry.Text, out var min) ||
            !int.TryParse(HeartEntry.Text, out var heart))
        {
            await DisplayAlert("Errore", "Inserisci tutti i valori numerici validi.", "OK");
            return;
        }

        var dateTime = DatePicker.Date + TimePicker.Time;

        var reading = new PressureReading
        {
            UserId = _user.Id,
            MaxPressure = max,
            MinPressure = min,
            HeartRate = heart,
            MeasurementTime = dateTime
        };

        await _database.SaveReadingAsync(reading);

        MaxEntry.Text = MinEntry.Text = HeartEntry.Text = string.Empty;

        await LoadReadings();
    }
}
