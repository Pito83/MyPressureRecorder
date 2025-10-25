using MyPressureRecorder.Models;
using MyPressureRecorder.Data;

namespace MyPressureRecorder.Pages;

public partial class ReadingsPage : ContentPage
{
    private readonly AppDatabase _database;
    private readonly UserProfile _user;

    public Command<object> DeleteReadingCommand { get; }

    public ReadingsPage(UserProfile user, AppDatabase database)
    {
        InitializeComponent();
        _database = database;
        _user = user;
        Title = $"Misurazioni: {_user.Name}";


        DeleteReadingCommand = new Command<object>(async (parameter) =>
        {
            if (parameter is not ReadingDisplayItem item) return;

            // Deseleziona tutti
            if (ReadingsList.ItemsSource is List<ReadingDisplayItem> currentItems)
            {
                foreach (var i in currentItems)
                    i.IsSelected = false;

                item.IsSelected = true;
                ReadingsList.ItemsSource = null;
                ReadingsList.ItemsSource = currentItems;
            }

            var answer = await DisplayAlert("Conferma eliminazione",
                                            "Vuoi eliminare questa misurazione?",
                                            "Elimina", "Annulla");

            if (!answer)
            {
                item.IsSelected = false;
                ReadingsList.ItemsSource = null;
                ReadingsList.ItemsSource = (List<ReadingDisplayItem>)ReadingsList.ItemsSource;
                return;
            }

            await _database.DeleteReadingAsync(item.Reading.Id);
            await LoadReadings();
        });


        BindingContext = this; // <-- IMPORTANTE: serve per collegare il comando al XAML
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
        var displayList = readings.Select(r => new ReadingDisplayItem
        {
            Reading = r,
            IsSelected = false
        }).ToList();

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
    private async void OnStatisticsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new StatisticsPage(_user, _database));
    }

}


