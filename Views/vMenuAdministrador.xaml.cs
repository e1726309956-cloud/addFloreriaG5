using addFloreriaG5.Models;
using addFloreriaG5.Services;

namespace addFloreriaG5.Views;

public partial class vMenuAdministrador : ContentPage
{
    private string nombreAdministrador;
    public vMenuAdministrador(string nombre)
	{
		InitializeComponent();
        nombreAdministrador = nombre;
        lblTituloAdmin.Text = $"¡Hola, {nombreAdministrador}!";
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        // Abre el catálogo que creamos
        await Navigation.PushAsync(new vCatalogoArreglos());

    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vVerPedidos(true));
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vControlEntregas());
    }

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        await DisplayAlert("Usuarios", "Módulo de gestión de personal y roles.", "Aceptar");

    }

    
    private async void CerrarSesion_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar = await MensajeModal.ConfirmarAsync(
            this,
            "Cerrar sesión",
            "¿Estás seguro de que deseas cerrar tu sesión?");

        if (!confirmar)
            return;

        // Limpiar la sesión de autenticación local
        SesionAuth.CerrarSesionLocal();

        // Limpiar los datos temporales del usuario
        SesionUsuario.Nombre = string.Empty;
        SesionUsuario.Telefono = string.Empty;

        // Regresar al login
        var ventana =
            Application.Current?.Windows.FirstOrDefault();

        if (ventana != null)
        {
            ventana.Page = new NavigationPage(
                new vLogin());
        }
    }


    private async void Button_Clicked_5(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vArreglosInactivos());
    }

    private async void Categorias_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new vGestionCategorias());
    }

    private async void Reporteria_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(new vReportes());
    }


}