namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vCatalogoArreglos : ContentPage
{
    private List<ArregloModel> listaCompleta = new();
    private string filtroActual = "Todos";

    public vCatalogoArreglos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarArreglos();
    }

    private async Task CargarArreglos()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();

            // Ahora cargamos TODOS para poder filtrar activos e inactivos
            string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?select=*";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);

            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();

                listaCompleta = JsonSerializer.Deserialize<List<ArregloModel>>(
                    contenido,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<ArregloModel>();

                ActualizarContadores();
                ActualizarEstiloFiltros();
                AplicarFiltros();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo cargar el catálogo.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "Aceptar");
        }
    }

    private void AplicarFiltros()
    {
        IEnumerable<ArregloModel> consulta = listaCompleta;

        // Filtro por estado
        if (filtroActual == "Activos")
        {
            consulta = consulta.Where(x => x.estado == true);
        }
        else if (filtroActual == "Inactivos")
        {
            consulta = consulta.Where(x => x.estado == false);
        }

        // Filtro por búsqueda
        string texto = txtBuscar.Text?.Trim().ToLower() ?? "";

        if (!string.IsNullOrWhiteSpace(texto))
        {
            consulta = consulta.Where(x =>
                (x.nombre?.ToLower().Contains(texto) ?? false) ||
                (x.descripcion?.ToLower().Contains(texto) ?? false));
        }

        cvArreglos.ItemsSource = consulta.ToList();
    }

    private void ActualizarContadores()
    {
        int total = listaCompleta.Count;
        int activos = listaCompleta.Count(x => x.estado == true);
        int inactivos = listaCompleta.Count(x => x.estado == false);

        btnTodos.Text = $"Todos ({total})";
        btnActivos.Text = $"Activos ({activos})";
        btnInactivos.Text = $"Inactivos ({inactivos})";
    }

    private void ActualizarEstiloFiltros()
    {
        ResetearBotonFiltro(btnTodos);
        ResetearBotonFiltro(btnActivos);
        ResetearBotonFiltro(btnInactivos);

        switch (filtroActual)
        {
            case "Todos":
                ActivarBotonFiltro(btnTodos);
                break;
            case "Activos":
                ActivarBotonFiltro(btnActivos);
                break;
            case "Inactivos":
                ActivarBotonFiltro(btnInactivos);
                break;
        }
    }

    private void ResetearBotonFiltro(Button boton)
    {
        boton.BackgroundColor = Colors.White;
        boton.TextColor = Color.FromArgb("#7D273E");
        boton.BorderColor = Color.FromArgb("#CDB6BD");
        boton.BorderWidth = 1.2;
    }

    private void ActivarBotonFiltro(Button boton)
    {
        boton.BackgroundColor = Color.FromArgb("#9B3E5C");
        boton.TextColor = Colors.White;
        boton.BorderColor = Color.FromArgb("#9B3E5C");
        boton.BorderWidth = 1.2;
    }

    private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void btnTodos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Todos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private void btnActivos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Activos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private void btnInactivos_Clicked(object sender, EventArgs e)
    {
        filtroActual = "Inactivos";
        ActualizarEstiloFiltros();
        AplicarFiltros();
    }

    private async void Volver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void btnNuevoArreglo_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistroArreglo());
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            string detalleMensaje =
                $"Nombre: {arreglo.nombre}\n\n" +
                $"Descripción: {arreglo.descripcion}\n\n" +
                $"Precio: ${arreglo.precio:F2}\n" +
                $"Stock Disponible: {arreglo.stock} unidades\n" +
                $"Estado: {(arreglo.estado ? "Activo" : "Inactivo")}";

            await DisplayAlert("Detalles del Arreglo", detalleMensaje, "Aceptar");
        }
    }

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            await Navigation.PushAsync(new vEditarArreglo(arreglo));
        }
    }

    private async void Button_Clicked_4(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            bool nuevoEstado = !arreglo.estado;

            string accion = nuevoEstado ? "activar" : "desactivar";
            bool confirmar = await DisplayAlert(
                "Confirmar",
                $"¿Estás seguro de {accion} el arreglo '{arreglo.nombre}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            try
            {
                SupabaseService servicio = new SupabaseService();
                var client = servicio.GetClient();

                var actualizarEstado = new
                {
                    estado = nuevoEstado
                };

                string json = JsonSerializer.Serialize(actualizarEstado);
                StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

                string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{arreglo.id_arreglo}";

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = contenido
                };

                request.Headers.Add("Prefer", "return=minimal");

                var respuesta = await client.SendAsync(request);

                if (respuesta.IsSuccessStatusCode)
                {
                    string mensaje = nuevoEstado
                        ? "El arreglo ha sido activado correctamente."
                        : "El arreglo ha sido desactivado correctamente.";

                    await DisplayAlert("Éxito", mensaje, "Aceptar");
                    await CargarArreglos();
                }
                else
                {
                    string error = await respuesta.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "No se pudo actualizar el estado: " + error, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }
    }
}