using Frugt_Grønt_Scanner.Services;
using Microsoft.Extensions.Logging;

namespace Frugt_Grønt_Scanner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<ProduktService>();
            builder.Services.AddSingleton<OnnxModelProviderService>();
            builder.Services.AddSingleton<OnnxDetectorService>();
            builder.Services.AddSingleton<CameraService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
