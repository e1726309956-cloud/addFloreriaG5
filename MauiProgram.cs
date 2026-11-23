using Microsoft.Extensions.Logging;
using MauiIcons.Material;
using System.Text.Json;
using addFloreriaG5.Configuration;

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

            // CARGAR CONFIGURACIÓN DE SUPABASE
            using var stream = typeof(MauiProgram)
                .Assembly
                .GetManifestResourceStream("supabase.settings.json")
                ?? throw new InvalidOperationException(
                    "No se encontró supabase.settings.json");

            using var documento = JsonDocument.Parse(stream);

            var supabase = documento.RootElement
                .GetProperty("Supabase");

            SupabaseSettings.Configure(
                supabase.GetProperty("Url").GetString() ?? "",
                supabase.GetProperty("PublishableKey").GetString() ?? ""
            );

#if DEBUG
            builder.Logging.AddDebug();



#if ANDROID
            var transparente = Android.Content.Res.ColorStateList.ValueOf(
                Android.Graphics.Color.Transparent);

            // ENTRY
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
                "SinSubrayado",
                (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList = transparente;
                });

            // EDITOR - DIRECCIÓN
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping(
                "SinSubrayado",
                (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList = transparente;
                });

            // DATEPICKER - FECHA
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(
                "SinSubrayado",
                (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList = transparente;
                });

            // TIMEPICKER - HORA
            Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping(
                "SinSubrayado",
                (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList = transparente;
                });
#endif


#endif

            return builder.Build();
        }


    }

}
