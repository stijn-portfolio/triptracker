using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics;
using TripTracker.App.Services;
using TripTracker.App.ViewModels;
using TripTracker.App.Views;

namespace TripTracker.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp() // Vereist voor Mapsui
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Debug exception handlers (zoals SafariSnap)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Debug.WriteLine($"[AppDomain] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Debug.WriteLine($"[TaskScheduler] {e.Exception}");
            e.SetObserved();
        };


        // ===== Services registreren =====

        // Navigation service (Transient = nieuwe instantie per request)
        builder.Services.AddTransient<INavigationService, NavigationService>();

        // Data Services (API communicatie)
        // Singleton = hergebruik dezelfde instantie (HttpClient wordt hergebruikt)
        builder.Services.AddSingleton<ITripDataService, TripDataService>();
        builder.Services.AddSingleton<ITripStopDataService, TripStopDataService>();

        // Fase 6+7: Smart Stop Capture services
        // Singleton = hergebruik dezelfde instantie
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
        builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();

        // ===== Pages en ViewModels registreren =====
        // Singleton = dezelfde instantie hergebruiken (voor hoofdpagina's)
        // Transient = nieuwe instantie per navigatie (voor detail pagina's)

        // Trips pagina (hoofdpagina - Singleton)
        builder.Services.AddSingleton<TripsPage>();
        builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

        // Trip detail pagina (Transient - nieuwe instantie bij elke navigatie)
        builder.Services.AddTransient<TripDetailPage>();
        builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();

        // Add stop pagina (Transient)
        builder.Services.AddTransient<AddStopPage>();
        builder.Services.AddTransient<IAddStopViewModel, AddStopViewModel>();

        // Add trip pagina (Transient) - Fase 10
        builder.Services.AddTransient<AddTripPage>();
        builder.Services.AddTransient<IAddTripViewModel, AddTripViewModel>();

        // Edit trip pagina (Transient) - Fase 11
        builder.Services.AddTransient<EditTripPage>();
        builder.Services.AddTransient<IEditTripViewModel, EditTripViewModel>();

        // Stop detail pagina (Transient)
        builder.Services.AddTransient<StopDetailPage>();
        builder.Services.AddTransient<IStopDetailViewModel, StopDetailViewModel>();

        // Edit stop pagina (Transient) - Fase 11
        builder.Services.AddTransient<EditStopPage>();
        builder.Services.AddTransient<IEditStopViewModel, EditStopViewModel>();

        // Map pagina (Transient) - Fase 13
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<IMapViewModel, MapViewModel>();

        return builder.Build();
    }
}
