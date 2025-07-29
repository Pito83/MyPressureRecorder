using Plugin.LocalNotification;
using Microsoft.Maui.Storage;

namespace MyPressureRecorder.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NotifSwitch.IsToggled = Preferences.Get("notifications_enabled", false);
        StatusLabel.Text = NotifSwitch.IsToggled ? "Notifiche attive" : "Notifiche disattivate";
    }

    private async void OnNotifToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("notifications_enabled", e.Value);
        StatusLabel.Text = e.Value ? "Notifiche attive" : "Notifiche disattivate";

        if (e.Value)
        {
            await ScheduleReminders();
        }
        else
        {
            LocalNotificationCenter.Current.CancelAll();
        }
    }

    private Task ScheduleReminders()
    {
        LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = 1001,
            Title = "Promemoria pressione",
            Description = "Ti sei ricordato di misurare la pressione?",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Today.AddHours(8),
                RepeatType = NotificationRepeat.Daily
            }
        });

        LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = 1002,
            Title = "Promemoria pressione",
            Description = "Ti sei ricordato di misurare la pressione?",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Today.AddHours(20),
                RepeatType = NotificationRepeat.Daily
            }
        });

        return Task.CompletedTask;
    }
}
