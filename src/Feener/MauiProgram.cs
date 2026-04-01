using Microsoft.Extensions.Logging;

namespace Feener
{[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                    fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("Inter-Bold.ttf", "InterBold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
