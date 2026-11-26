
using addFloreriaG5.Models;
using addFloreriaG5.Services;

namespace addFloreriaG5.Views;

public partial class vMenuCliente : ContentPage
{
    public vMenuCliente(string nombre)
    {
        InitializeComponent();
      
    }

    // CATÁLOGO
    private async void Catalogo_Tapped(
        object sender,
        TappedEventArgs e)
    {
        // Abrir el catálogo en modo Cliente
        await Navigation.PushAsync(
            new vCatalogoArreglos(false));
    }

    // MIS PEDIDOS
    private async void MisPedidos_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
        new vVerPedidos(false));
    }

    // MI PERFIL
    private async void MiPerfil_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await MensajeModal.MostrarAsync(
            this,
            "Mi perfil",
            "Aquí podrás consultar y actualizar tus datos personales.",
            "advertencia");
    }

    // FAVORITOS
    private async void Favoritos_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await MensajeModal.MostrarAsync(
            this,
            "Favoritos",
            "Aquí podrás consultar tus arreglos favoritos.",
            "advertencia");
    }

    // CHATBOT
    private async void ChatBot_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new vChatBot(false));
    }

    // CERRAR SESIÓN
    private async void CerrarSesion_Tapped(
        object sender,
        TappedEventArgs e)
    {
        bool confirmar =
            await MensajeModal.ConfirmarAsync(
                this,
                "Cerrar sesión",
                "¿Deseas cerrar tu sesión?");

        if (!confirmar)
            return;

        // Limpieza de la sesión local temporal
        SesionAuth.CerrarSesionLocal();

        SesionUsuario.Nombre = string.Empty;
        SesionUsuario.Telefono = string.Empty;

        // Regresar al login y limpiar la navegación
        var ventana =
            Application.Current?.Windows.FirstOrDefault();

        if (ventana != null)
        {
            ventana.Page =
                new NavigationPage(new vLogin());
        }
    }
}
