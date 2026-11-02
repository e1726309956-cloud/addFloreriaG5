using System.Text;
using System.Text.Json;

namespace addFloreriaG5.Services
{
    public class SupabaseService
    {
        private readonly string url =
            "https://ncomuzviwluyoszgossv.supabase.co";

        private readonly string apiKey =
            "sb_publishable_9H2lEm5VxWOLya8rFyAjVg_d5vCAb4W";

        private readonly HttpClient client;

        public SupabaseService()
        {
            client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "apikey",
                apiKey);

            client.DefaultRequestHeaders.Add(
                "Authorization",
                "Bearer " + apiKey);
        }

        public HttpClient GetClient()
        {
            return client;
        }

        public string GetUrl()
        {
            return url;
        }
    }
}