using CommunityToolkit.Maui;
using Lubrisense.Services;
using Lubrisense.ViewModels;
using Lubrisense.Views;
using Shiny;
using Microsoft.Extensions.Logging;

namespace Lubrisense
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseShiny()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddFont("Fluent-Regular.ttf", "FluentRegular");
                });

            builder.Services.AddBluetoothLE();

            builder.Services.AddSingleton<BluetoothService>();

            builder.Services.AddSingleton<DeviceViewModel>();
            builder.Services.AddTransient<DeviceDetailViewModel>();

            builder.Services.AddSingleton<DeviceView>();
            builder.Services.AddTransient<DeviceDetailView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
