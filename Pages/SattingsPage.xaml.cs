using Plugin.LocalNotification;
using Microsoft.Maui.Storage;

namespace MyPressureRecorder.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private const string ActiveText =
        "Notifiche attive: riceverai un promemoria ogni giorno alle 08:00 e alle 20:00.";
    private const string InactiveText = "Notifiche disattivate";
    private const string DeniedText =
        "Permesso notifiche negato. Abilitalo dalle impostazioni di sistema dell'app per ricevere i promemoria.";

    private bool _updatingSwitch;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool enabled = Preferences.Get("notifications_enabled", false);
        SetSwitch(enabled);

        if (enabled && !await LocalNotificationCenter.Current.AreNotificationsEnabled())
            StatusLabel.Text = DeniedText;
        else
            StatusLabel.Text = enabled ? ActiveText : InactiveText;
    }

    private void SetSwitch(bool value)
    {
        _updatingSwitch = true;
        NotifSwitch.IsToggled = value;
        _updatingSwitch = false;
    }

    private async void OnNotifToggled(object sender, ToggledEventArgs e)
    {
        if (_updatingSwitch)
            return;

        if (e.Value)
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                // Android 13+: asks the POST_NOTIFICATIONS runtime permission
                bool granted = await LocalNotificationCenter.Current.RequestNotificationPermission();
                if (!granted)
                {
                    Preferences.Set("notifications_enabled", false);
                    SetSwitch(false);
                    StatusLabel.Text = DeniedText;
                    return;
                }
            }

            Preferences.Set("notifications_enabled", true);
            StatusLabel.Text = ActiveText;
            await ScheduleReminders();
        }
        else
        {
            Preferences.Set("notifications_enabled", false);
            StatusLabel.Text = InactiveText;
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
