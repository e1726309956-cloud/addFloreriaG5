namespace addFloreriaG5.Views;

using System.Text;
using addFloreriaG5.Services;

public partial class vRegistro : ContentPage
{
    public vRegistro()
    {
        InitializeComponent();
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        // Validar campos vacíos
        if (string.IsNullOrWhiteSpace(txtNombres.Text) ||
            string.IsNullOrWhiteSpace(txtApellidos.Text) ||
            string.IsNullOrWhiteSpace(txtCorreo.Text) ||
            string.IsNullOrWhiteSpace(txtTelefono.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text) ||
            string.IsNullOrWhiteSpace(txtConfirmPassword.Text))
        {
            await DisplayAlert("Error", "Complete todos los campos", "Aceptar");
            return;
        }

        // Validar coincidencia de contraseñas
        if (txtPassword.Text != txtConfirmPassword.Text)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden", "Aceptar");
            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();

            // Verificar si el correo ya existe
            string verificar = $"{servicio.GetUrl()}/rest/v1/usuarios?correo=eq.{txtCorreo.Text}";
            var respuestaVerificar = await servicio.GetClient().GetAsync(verificar);
            string existe = await respuestaVerificar.Content.ReadAsStringAsync();

            if (existe != "[]")
            {
                await DisplayAlert("Error", "El correo ya se encuentra registrado", "Aceptar");
                return;
            }

            // Crear usuario
            var nuevoUsuario = new
            {
                nombres = txtNombres.Text,
                apellidos = txtApellidos.Text,
                correo = txtCorreo.Text,
                password = txtPassword.Text,
                telefono = txtTelefono.Text,
                id_rol = 3,
                estado = true,
                fecha_registro = DateTime.Now
            };

            string json = System.Text.Json.JsonSerializer.Serialize(nuevoUsuario);

            StringContent contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            string endpoint = $"{servicio.GetUrl()}/rest/v1/usuarios";

            var respuesta = await servicio.GetClient().PostAsync(endpoint, contenido);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Usuario registrado correctamente", "Aceptar");
                await Navigation.PopAsync();
            }
            else
            {
                string error = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error", error, "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        txtPassword.IsPassword = !txtPassword.IsPassword;
    }

    private void ToggleConfirmPassword_Clicked(object sender, EventArgs e)
    {
        txtConfirmPassword.IsPassword = !txtConfirmPassword.IsPassword;
    }

    private void txtNombres_Completed(object sender, EventArgs e)
    {
        txtApellidos.Focus();
    }

    private void txtApellidos_Completed(object sender, EventArgs e)
    {
        txtCorreo.Focus();
    }

    private void txtCorreo_Completed(object sender, EventArgs e)
    {
        txtTelefono.Focus();
    }

    private void txtTelefono_Completed(object sender, EventArgs e)
    {
        txtPassword.Focus();
    }

    private void txtPassword_Completed(object sender, EventArgs e)
    {
        txtConfirmPassword.Focus();
    }
}