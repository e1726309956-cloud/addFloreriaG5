namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text.Json;
using System.Text;

public partial class vCatalogoArreglos : ContentPage
{
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

            // AQUÍ ES DONDE VA: Filtramos para traer solo los arreglos activos (estado = true)
            string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?estado=eq.true&select=*";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();

                var listaArreglos = JsonSerializer.Deserialize<List<ArregloModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                cvArreglos.ItemsSource = listaArreglos;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "Aceptar");
        }
    }


    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            // Navegamos a la vista de proceso de compra enviándole el objeto
            await Navigation.PushAsync(new vProcesoCompra(arreglo));
        }

    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            try
            {
                // Número de WhatsApp de la florería (ejemplo con código de país, cámbialo por el real)
                string numeroTelefono = "593959178595";
                string mensaje = Uri.EscapeDataString($"Hola, estoy interesado en el arreglo '{arreglo.nombre}' que cuesta ${arreglo.precio}. ¿Me pueden dar más información?");

                string urlWhatsApp = $"https://wa.me/{numeroTelefono}?text={mensaje}";

                // Abre el enlace en el navegador o la app de WhatsApp del teléfono
                await Launcher.OpenAsync(new Uri(urlWhatsApp));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir WhatsApp: " + ex.Message, "Aceptar");
            }
        }

    }

    private async void btnNuevoArreglo_Clicked(object sender, EventArgs e)
    {
        // Aquí abrirías la vista para registrar un nuevo producto en la base de datos
        await Navigation.PushAsync(new vRegistroArreglo());

    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            string detalleMensaje = $"Nombre: {arreglo.nombre}\n\n" +
                                    $"Descripción: {arreglo.descripcion}\n\n" +
                                    $"Precio: ${arreglo.precio:F2}\n" +
                                    $"Stock Disponible: {arreglo.stock} unidades";

            await DisplayAlert("Detalles del Arreglo", detalleMensaje, "Aceptar");
        }

    }

    private async void Button_Clicked_4(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            bool confirmar = await DisplayAlert("Confirmar Eliminación", $"¿Estás seguro de desactivar el arreglo '{arreglo.nombre}'?", "Sí", "No");

            if (confirmar)
            {
                try
                {
                    SupabaseService servicio = new SupabaseService();
                    var client = servicio.GetClient();

                    // Cambiamos el estado a false (Borrado lógico)
                    var actualizarEstado = new
                    {
                        estado = false
                    };

                    string json = JsonSerializer.Serialize(actualizarEstado);
                    StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

                    // Endpoint de Supabase haciendo un PATCH filtrando por id_arreglo
                    string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{arreglo.id_arreglo}";

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                    {
                        Content = contenido
                    };
                    request.Headers.Add("Prefer", "return=minimal");

                    var respuesta = await client.SendAsync(request);

                    if (respuesta.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Éxito", "El arreglo ha sido dado de baja correctamente", "Aceptar");
                        await CargarArreglos(); // Recargamos la lista para que se oculte
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

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            // Navegamos a la vista de edición enviándole el arreglo seleccionado
            await Navigation.PushAsync(new vEditarArreglo(arreglo));
        }
    }
}