# MyPressureRecorder

App multipiattaforma (.NET MAUI) per registrare e monitorare le misurazioni della
pressione arteriosa e della frequenza cardiaca. Tutti i dati restano **sul
dispositivo**: non c'è nessun server, account o sincronizzazione cloud.

Sviluppata da **Pydo's Soft** – 2025.

## Funzionalità

- Gestione di più profili utente (aggiunta / eliminazione)
- Inserimento di misurazioni: pressione massima, minima, battiti, data e ora
- Elenco delle misurazioni per utente con eliminazione tramite pressione prolungata
- Statistiche per periodo (1 settimana / 1 mese / 3 mesi / 1 anno / tutti):
  grafici a linee per massima, minima e battiti + valori medi
- Esportazione dei dati in CSV e condivisione tramite il menu di sistema
- Promemoria locali giornalieri (ore 8:00 e 20:00), attivabili dalle Opzioni

## Piattaforme supportate

| Piattaforma | Versione minima |
|-------------|-----------------|
| Android     | 5.0 (API 21)    |
| iOS         | 15.0            |
| macOS (Mac Catalyst) | 15.0   |
| Windows     | 10.0.17763.0    |

## Stack tecnico

- .NET 9 / .NET MAUI (`Microsoft.Maui.Controls` 9.0.90)
- Application ID: `com.pydosoft.mypressurerecorder`
- `sqlite-net-pcl` per la persistenza locale (`pressure.db3` in `FileSystem.AppDataDirectory`)
- `Microcharts.Maui` per i grafici
- `Plugin.LocalNotification` per i promemoria
- `CommunityToolkit.Maui`

## Come compilare

Prerequisiti: .NET 9 SDK e il workload MAUI.

```bash
dotnet workload install maui
dotnet restore
dotnet build -f net9.0-android      # oppure net9.0-ios / net9.0-maccatalyst / net9.0-windows10.0.19041.0
```

Per eseguire su un target specifico:

```bash
dotnet build -t:Run -f net9.0-android
```

L'app genera un APK (`AndroidPackageFormat=apk`), adatto a test e sideload. Per la
pubblicazione sul Play Store va impostato `aab` e configurata la firma.

## Struttura del progetto

```
Data/            Accesso al database SQLite (AppDatabase)
Models/          Entità: UserProfile, PressureReading, ReadingDisplayItem
Pages/           Pagine: Readings, Statistics, Settings, BaseContentPage
Platforms/       Codice specifico per Android / iOS / MacCatalyst / Windows / Tizen
Resources/       Icone, immagini, font, stili
MainPage.xaml    Selezione / creazione utente
```

## Privacy e gestione dei dati

- Le misurazioni sono **dati sanitari personali** e vengono salvate localmente in
  un database SQLite non cifrato.
- Su Android `allowBackup` è disattivato (`false`): i dati non vengono inclusi nei
  backup automatici del sistema e non lasciano il dispositivo. La migrazione dello
  storico su un nuovo dispositivo va fatta tramite l'export CSV.
- Il CSV esportato viene scritto nella cache dell'app e condiviso solo su azione
  esplicita dell'utente, tramite il menù di condivisione di sistema (mail,
  messaggistica, cloud…). Contiene nome utente e misurazioni.
- L'app non effettua alcuna chiamata di rete e non richiede permessi di rete.

## Licenza

Distribuito con licenza MIT. Vedi il file [`LICENSE`](LICENSE).
