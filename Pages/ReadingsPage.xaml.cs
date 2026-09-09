using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
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
                //ReadingsList.ItemsSource = null;
                //ReadingsList.ItemsSource = (List<ReadingDisplayItem>)ReadingsList.ItemsSource;
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

    private static readonly FilePickerFileType CsvFileType = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "text/plain", "application/csv" } },
            { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.plain-text", "public.text" } },
            { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text", "public.plain-text", "public.text" } },
            { DevicePlatform.WinUI, new[] { ".csv", ".txt" } },
        });

    private async void OnImportClicked(object sender, EventArgs e)
    {
        FileResult? file;
        try
        {
            file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona il file CSV da importare",
                FileTypes = CsvFileType
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Impossibile aprire il selettore file: {ex.Message}", "OK");
            return;
        }

        if (file is null)
            return; // annullato dall'utente

        string content;
        try
        {
            using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            content = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Impossibile leggere il file: {ex.Message}", "OK");
            return;
        }

        var parsed = CsvService.Parse(content, _user.Id);

        if (parsed.Readings.Count == 0)
        {
            var detail = parsed.Errors.Count > 0 ? "\n\n" + string.Join("\n", parsed.Errors) : "";
            await DisplayAlert("Nessun dato importabile",
                $"Il file non contiene misurazioni valide.{detail}", "OK");
            return;
        }

        // Il file sembra di un altro utente?
        var guessedName = CsvService.GuessUserNameFromFileName(file.FileName);
        if (!string.IsNullOrWhiteSpace(guessedName) &&
            !string.Equals(guessedName, _user.Name, StringComparison.OrdinalIgnoreCase))
        {
            var proceed = await DisplayAlert("Attenzione",
                $"Il file sembra appartenere a «{guessedName}», ma verrà importato sull'utente «{_user.Name}».",
                "Continua", "Annulla");
            if (!proceed)
                return;
        }

        var existing = await _database.GetReadingsByUserAsync(_user.Id);

        var notes = new List<string> { $"{parsed.Readings.Count} misurazioni valide nel file." };
        notes.Add($"Utente «{_user.Name}»: {existing.Count} misurazioni attuali.");
        if (parsed.InvalidRows > 0)
            notes.Add($"Righe ignorate per errori: {parsed.InvalidRows}");
        if (parsed.OutOfRangeRows > 0)
            notes.Add($"Valori fuori scala (importati comunque): {parsed.OutOfRangeRows}");

        var choice = await DisplayActionSheet(
            string.Join("\n", notes),
            "Annulla", null,
            "Aggiungi (salta i duplicati)",
            "Sostituisci tutto");

        if (choice == "Aggiungi (salta i duplicati)")
        {
            var toAdd = CsvService.Deduplicate(parsed.Readings, existing, out var skipped);
            if (toAdd.Count > 0)
                await _database.AddReadingsAsync(toAdd);
            await LoadReadings();
            await DisplayAlert("Import completato",
                $"Importate: {toAdd.Count}\n" +
                $"Duplicate saltate: {skipped}\n" +
                $"Righe con errori: {parsed.InvalidRows}", "OK");
        }
        else if (choice == "Sostituisci tutto")
        {
            var confirm = await DisplayAlert("Conferma sostituzione",
                $"Verranno eliminate le {existing.Count} misurazioni esistenti di «{_user.Name}» " +
                $"e sostituite con le {parsed.Readings.Count} del file.\n\nOperazione irreversibile.",
                "Elimina e importa", "Annulla");
            if (!confirm)
                return;

            await _database.ReplaceUserReadingsAsync(_user.Id, parsed.Readings);
            await LoadReadings();
            await DisplayAlert("Import completato",
                $"Tutte le misurazioni di «{_user.Name}» sono state sostituite.\n" +
                $"Importate: {parsed.Readings.Count}\n" +
                $"Righe con errori: {parsed.InvalidRows}", "OK");
        }
    }

}


