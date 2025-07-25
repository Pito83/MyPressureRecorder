using MyPressureRecorder.Models;
using MyPressureRecorder.Data;
using MyPressureRecorder.Pages;

namespace MyPressureRecorder;

public partial class MainPage : ContentPage
{
    private readonly AppDatabase _database;
    private List<UserProfile> _users = new();

    public MainPage(AppDatabase database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        _users = await _database.GetUsersAsync();
        UserPicker.ItemsSource = _users;
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        var name = NewUserEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Errore", "Inserisci un nome valido.", "OK");
            return;
        }

        var user = new UserProfile { Name = name };
        await _database.SaveUserAsync(user);
        NewUserEntry.Text = string.Empty;
        await LoadUsers();
    }

    private async void OnDeleteUserClicked(object sender, EventArgs e)
    {
        if (UserPicker.SelectedItem is UserProfile selectedUser)
        {
            var confirm = await DisplayAlert("Conferma", $"Eliminare l'utente {selectedUser.Name}?", "Sì", "No");
            if (confirm)
            {
                await _database.DeleteUserAsync(selectedUser);
                await LoadUsers();
            }
        }
        else
        {
            await DisplayAlert("Attenzione", "Seleziona un utente da eliminare.", "OK");
        }
    }

    private async void OnEnterClicked(object sender, EventArgs e)
    {
        if (UserPicker.SelectedItem is not UserProfile selectedUser)
        {
            await DisplayAlert("Errore", "Seleziona un utente per continuare.", "OK");
            return;
        }

        // Prossimo passo: vai a ReadingsPage, passando l'utente
        await Navigation.PushAsync(new ReadingsPage(selectedUser, _database));
    }
}
