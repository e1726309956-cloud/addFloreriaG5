namespace addFloreriaG5.Views;

public partial class vMenuAdministrador : ContentPage
{
    private string nombreAdministrador;
    public vMenuAdministrador(string nombre)
	{
		InitializeComponent();
        nombreAdministrador = nombre;
        lblTituloAdmin.Text = $"Bienvenido, {nombreAdministrador}";
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        // Abre el catálogo que creamos
        await Navigation.PushAsync(new vCatalogoArreglos());

    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vVerPedidos());
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vControlEntregas());
    }

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        await DisplayAlert("Usuarios", "Módulo de gestión de personal y roles.", "Aceptar");

    }

    private async void Button_Clicked_4(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cerrar Sesión", "¿Estás seguro de salir?", "Sí", "No");
        if (confirmar)
        {
            Application.Current.MainPage = new NavigationPage(new vLogin());
        }

    }

    private async void Button_Clicked_5(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vArreglosInactivos());
    }
}