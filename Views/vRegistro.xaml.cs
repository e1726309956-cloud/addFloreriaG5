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
        // 1. Validar que los campos no estén vacíos
        if (string.IsNullOrWhiteSpace(txtNombres.Text) ||
            string.IsNullOrWhiteSpace(txtApellidos.Text) ||
            string.IsNullOrWhiteSpace(txtCorreo.Text) ||
            string.IsNullOrWhiteSpace(txtTelefono.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert(
                "Error",
                "Complete todos los campos",
                "Aceptar");

            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();

            // 2. NUEVO: Verificar si el correo ya existe en la base de datos
            string verificar = $"{servicio.GetUrl()}/rest/v1/usuarios?correo=eq.{txtCorreo.Text}";
            var respuestaVerificar = await servicio.GetClient().GetAsync(verificar);
            string existe = await respuestaVerificar.Content.ReadAsStringAsync();

            // Si Postgrest (Supabase) devuelve algo diferente a un arreglo vacío "[]", significa que el correo ya está registrado
            if (existe != "[]")
            {
                await DisplayAlert(
                    "Error",
                    "El correo ya se encuentra registrado",
                    "Aceptar");
                return;
            }

            // 3. Si el correo no existe, procedemos a armar el objeto y registrar al usuario
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
                await DisplayAlert(
                    "Éxito",
                    "Usuario registrado correctamente",
                    "Aceptar");

                await Navigation.PopAsync();
            }
            else
            {
                string error = await respuesta.Content.ReadAsStringAsync();

                await DisplayAlert(
                    "Error",
                    error,
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

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PopAsync();

    }
}