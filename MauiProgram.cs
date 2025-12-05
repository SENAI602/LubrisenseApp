using CommunityToolkit.Maui;
using Lubrisense.Services;
using Lubrisense.ViewModels;
using Lubrisense.Views;
using Microsoft.Extensions.Logging;
using Shiny;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Lubrisense
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .ConfigureSyncfusionToolkit()
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
            builder.Services.AddTransient<DeviceMenuViewModel>();
            builder.Services.AddTransient<DeviceConfigViewModel>();
            builder.Services.AddTransient<DeviceHistoryView>();

            builder.Services.AddSingleton<DeviceView>();
            builder.Services.AddTransient<DeviceMenuView>();
            builder.Services.AddTransient<DeviceConfigView>();
            builder.Services.AddTransient<DeviceHistoryViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
