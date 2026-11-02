namespace addFloreriaG5.Views;
using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text.Json;

public partial class vLogin : ContentPage
{
	public vLogin()
	{
		InitializeComponent();
	}

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCorreo.Text))
        {
            await DisplayAlert(
                "Error",
                "Ingrese el correo",
                "Aceptar");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert(
                "Error",
                "Ingrese la contraseña",
                "Aceptar");
            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();

            string endpoint =
                $"{servicio.GetUrl()}/rest/v1/usuarios?correo=eq.{txtCorreo.Text}&password=eq.{txtPassword.Text}";

            var respuesta =
                await servicio.GetClient().GetAsync(endpoint);

            string contenido =
                await respuesta.Content.ReadAsStringAsync();

            if (contenido != "[]")
            {
                using JsonDocument doc = JsonDocument.Parse(contenido);

                int idRol = doc.RootElement[0]
                    .GetProperty("id_rol")
                    .GetInt32();

                string nombre = doc.RootElement[0]
                    .GetProperty("nombres")
                    .GetString();

                // Intentamos capturar el teléfono si la columna existe en tu tabla 'usuarios'
                string telefono = "";
                if (doc.RootElement[0].TryGetProperty("telefono", out var telProp))
                {
                    telefono = telProp.GetString() ?? "";
                }

                // >>> GUARDAMOS LOS DATOS EN LA SESIÓN GLOBAL <<<
                SesionUsuario.Nombre = nombre;
                SesionUsuario.Telefono = telefono;

                string mensaje = "";

                if (idRol == 1)
                {
                    await DisplayAlert("Bienvenido", $"Hola Administrador, {nombre}", "Aceptar");
                    await Navigation.PushAsync(new vMenuAdministrador(nombre));
                }
                else if (idRol == 2)
                {
                    await DisplayAlert("Bienvenido", $"Hola Vendedor, {nombre}", "Aceptar");
                    await Navigation.PushAsync(new vMenuVendedor(nombre));
                }
                else
                {
                    await DisplayAlert("Bienvenido", $"Hola, {nombre}", "Aceptar");
                    await Navigation.PushAsync(new vMenuCliente(nombre));
                }

                await DisplayAlert(
                    "Éxito",
                    mensaje,
                    "Aceptar");

                // Aquí puedes abrir la página principal
                // await Navigation.PushAsync(new vMenu());
            }
            else
            {
                await DisplayAlert(
                    "Error",
                    "Correo o contraseña incorrectos",
                    "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                ex.Message,
                "Aceptar");
        }

    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}