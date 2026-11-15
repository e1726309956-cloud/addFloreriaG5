using System;
using System.Collections.Generic;
using System.Text;

namespace addFloreriaG5.Configuration
{
    public static class SupabaseSettings
    {
        public static string Url { get; private set; }
        = string.Empty;

        public static string PublishableKey { get; private set; }
            = string.Empty;

        public static void Configure(
            string url,
            string publishableKey)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "La URL de Supabase debe utilizar HTTPS.");
            }

            if (!publishableKey.StartsWith(
                "sb_publishable_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Se requiere una clave pública de Supabase.");
            }

            Url = url.TrimEnd('/');
            PublishableKey = publishableKey;
        }
    }
}
