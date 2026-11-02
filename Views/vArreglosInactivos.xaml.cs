namespace addFloreriaG5.Views;

using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vArreglosInactivos : ContentPage
{
	public vArreglosInactivos()
	{
		InitializeComponent();
	}


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarArreglosInactivos();
    }

    private async Task CargarArreglosInactivos()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();
            // Traemos únicamente los arreglos con estado = false
            string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?estado=eq.false&select=*";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();
                var listaInactivos = JsonSerializer.Deserialize<List<ArregloModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                cvArreglosInactivos.ItemsSource = listaInactivos;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudieron cargar los inactivos: " + ex.Message, "Aceptar");
        }
    }



    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ArregloModel arreglo)
        {
            bool confirmar = await DisplayAlert("Reactivar", $"¿Deseas volver a activar '{arreglo.nombre}' en el catálogo?", "Sí", "No");

            if (confirmar)
            {
                try
                {
                    SupabaseService servicio = new SupabaseService();
                    var client = servicio.GetClient();

                    // Preparamos los datos para poner estado en true (y opcionalmente stock en 10 si estaba en 0)
                    var datosReactivacion = new
                    {
                        estado = true,
                        stock = arreglo.stock <= 0 ? 10 : arreglo.stock
                    };

                    string json = JsonSerializer.Serialize(datosReactivacion);
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
                        await DisplayAlert("Éxito", "El arreglo ha sido reactivado correctamente.", "Aceptar");
                        await CargarArreglosInactivos(); // Recargamos la lista para que desaparezca de inactivos
                    }
                    else
                    {
                        string error = await respuesta.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", "No se pudo reactivar: " + error, "Aceptar");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "Aceptar");
                }
            }
        }

    }
}