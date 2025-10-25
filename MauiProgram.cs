using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using MyPressureRecorder.Data;
using Plugin.LocalNotification;
using CommunityToolkit.Maui;

namespace MyPressureRecorder;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit() // 👈 aggiungi questa riga
			.UseMicrocharts() // ⬅️ aggiunto!
			.UseLocalNotification() // ⬅️ fondamentale!
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<AppDatabase>(s =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pressure.db3");
			return new AppDatabase(dbPath);
		});


#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
