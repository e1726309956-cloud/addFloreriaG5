using Microsoft.Extensions.Logging;
using MauiIcons.Material;

namespace addFloreriaG5
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMaterialMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();


#if ANDROID
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
                "SinSubrayado",
                (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList =
                        Android.Content.Res.ColorStateList.ValueOf(
                            Android.Graphics.Color.Transparent);
                });
#endif

#endif

            return builder.Build();
        }


    }

}
