# MyPressureRecorder

Cross-platform .NET MAUI app for recording and tracking blood-pressure and
heart-rate measurements. All data stays **on the device**: there is no server,
account, or cloud sync.

Developed by **Pydo's Soft** – 2025.

## Features

- Multiple user profiles (add / delete)
- Record measurements: systolic, diastolic, heart rate, date and time
- Per-user list of measurements, with long-press to delete
- Statistics by period (1 week / 1 month / 3 months / 1 year / all):
  line charts for systolic, diastolic and heart rate, plus averages
- Export to CSV: current period or a full backup, shared via the system share sheet
- Import measurements from a CSV file into the selected user (append with
  de-duplication, or full replace)
- Daily local reminders (08:00 and 20:00), toggleable from Settings

## Supported platforms

| Platform | Minimum version |
|----------|-----------------|
| Android  | 5.0 (API 21)    |
| iOS      | 15.0            |
| macOS (Mac Catalyst) | 15.0 |
| Windows  | 10.0.17763.0    |

## Tech stack

- .NET 9 / .NET MAUI (`Microsoft.Maui.Controls` 9.0.90)
- Application ID: `com.pydosoft.mypressurerecorder`
- `sqlite-net-pcl` for local persistence (`pressure.db3` in `FileSystem.AppDataDirectory`)
- `Microcharts.Maui` for charts
- `Plugin.LocalNotification` for reminders
- `CommunityToolkit.Maui`

## Building

Prerequisites: .NET 9 SDK and the MAUI workload.

```bash
dotnet workload install maui
dotnet restore
dotnet build -f net9.0-android      # or net9.0-ios / net9.0-maccatalyst / net9.0-windows10.0.19041.0
```

To run on a specific target:

```bash
dotnet build -t:Run -f net9.0-android
```

The build produces an APK (`AndroidPackageFormat=apk`), suitable for testing and
sideloading. For Play Store publishing, switch to `aab` and configure signing.

## Project layout

```
Data/            SQLite database access (AppDatabase)
Models/          Entities: UserProfile, PressureReading, ReadingDisplayItem
Pages/           Pages: Readings, Statistics, Settings, BaseContentPage
Platforms/       Platform-specific code for Android / iOS / MacCatalyst / Windows / Tizen
Resources/       Icons, images, fonts, styles
MainPage.xaml    User selection / creation
```

## Privacy and data handling

- Measurements are **personal health data** and are stored locally in an
  unencrypted SQLite database.
- On Android `allowBackup` is disabled (`false`): the data is not included in
  system backups and never leaves the device. Migrating history to a new device
  is done with the "full backup" CSV export and the CSV import.
- The exported CSV is written to the app cache and shared only on an explicit
  user action, via the system share sheet (mail, messaging, cloud…). It contains
  the user name and the measurements.
- The app makes no network calls and requests no network permissions.

## License

Released under the MIT License. See the [`LICENSE`](LICENSE) file.
