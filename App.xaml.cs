using Microsoft.Extensions.DependencyInjection;

namespace addFloreriaG5
{
    public partial class App : Application
    {
        private bool avisoSinConexionMostrado;
        private bool mostrandoAviso;

        public App()
        {
            InitializeComponent();
            Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged += AlCambiarConexion;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var pagina = new NavigationPage(new Views.vLogin());
            pagina.Loaded += (_, _) => ComprobarConexion();
            var ventana = new Window(pagina);
            ventana.Resumed += (_, _) => ComprobarConexion();
            return ventana;
        }

        private void AlCambiarConexion(object? sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(ComprobarConexion);
        }

        private async void ComprobarConexion()
        {
            if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
            {
                avisoSinConexionMostrado = false;
                return;
            }

            if (avisoSinConexionMostrado || mostrandoAviso)
                return;

            var pagina = Windows.FirstOrDefault()?.Page;
            if (pagina == null)
                return;

            avisoSinConexionMostrado = true;
            mostrandoAviso = true;
            try
            {
                await pagina.DisplayAlertAsync(
                    "Sin conexión a internet",
                    "No tienes conexión a internet. Revisa tu conexión e inténtalo nuevamente.",
                    "Aceptar");
            }
            finally
            {
                mostrandoAviso = false;
            }
        }
    }
}