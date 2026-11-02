namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using addFloreriaG5.Views;


public partial class vEditarArreglo : ContentPage
{
    private int idArregloActual;
    private string rutaImagenLocal = string.Empty;

    public vEditarArreglo(ArregloModel arregloAEditar)
	{
		InitializeComponent();
        idArregloActual = arregloAEditar.id_arreglo;

        // Rellenamos los campos con la información actual del producto
        txtNombre.Text = arregloAEditar.nombre;
        txtDescripcion.Text = arregloAEditar.descripcion;
        txtPrecio.Text = arregloAEditar.precio.ToString();
        txtStock.Text = arregloAEditar.stock.ToString();
        txtImagenUrl.Text = arregloAEditar.imagen;

        if (!string.IsNullOrEmpty(arregloAEditar.imagen))
        {
            imgPrevia.Source = arregloAEditar.imagen;
        }

        // Cargamos las categorías para que el administrador pueda seleccionarla
        _ = CargarCategoriasYSeleccionarActual(arregloAEditar.id_categoria);
    }

    private async Task CargarCategoriasYSeleccionarActual(int idCategoriaActual)
    {
        try
        {
            SupabaseService servicio = new SupabaseService();
            string endpoint = $"{servicio.GetUrl()}/rest/v1/categorias?select=*";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();
                var listaCategorias = JsonSerializer.Deserialize<List<CategoriaModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                pickerCategorias.ItemsSource = listaCategorias;

                // Buscamos y preseleccionamos la categoría que ya tenía el arreglo
                var categoriaEncontrada = listaCategorias.FirstOrDefault(c => c.id_categoria == idCategoriaActual);
                if (categoriaEncontrada != null)
                {
                    pickerCategorias.SelectedItem = categoriaEncontrada;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Aviso", "No se pudieron cargar las categorías: " + ex.Message, "Aceptar");
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una nueva imagen",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado != null)
            {
                rutaImagenLocal = resultado.FullPath;
                imgPrevia.Source = ImageSource.FromFile(rutaImagenLocal);
                txtImagenUrl.Text = string.Empty; // Limpiamos la URL si eligió local
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }

    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        // Validaciones básicas y numéricas
        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtPrecio.Text) ||
            string.IsNullOrWhiteSpace(txtStock.Text) ||
            pickerCategorias.SelectedItem == null)
        {
            await DisplayAlert("Error", "Complete todos los campos obligatorios.", "Aceptar");
            return;
        }

        if (!decimal.TryParse(txtPrecio.Text, out decimal precioValidado) || precioValidado <= 0)
        {
            await DisplayAlert("Error", "El precio debe ser un número válido mayor a 0.", "Aceptar");
            return;
        }

        if (!int.TryParse(txtStock.Text, out int stockValidado) || stockValidado < 0)
        {
            await DisplayAlert("Error", "El stock debe ser un número entero válido.", "Aceptar");
            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();
            var categoriaSeleccionada = (CategoriaModel)pickerCategorias.SelectedItem;

            string urlOImagenFinal = string.IsNullOrWhiteSpace(txtImagenUrl.Text) ? rutaImagenLocal : txtImagenUrl.Text;

            // Objeto con los datos actualizados
            var arregloActualizado = new
            {
                nombre = txtNombre.Text,
                descripcion = txtDescripcion.Text ?? string.Empty,
                precio = precioValidado,
                stock = stockValidado,
                imagen = urlOImagenFinal,
                id_categoria = categoriaSeleccionada.id_categoria
            };

            string json = JsonSerializer.Serialize(arregloActualizado);
            StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

            // En Supabase, para actualizar se usa una petición PATCH filtrando por id
            string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{idArregloActual}";

            // HttpClient de .NET no tiene un método PatchAsync nativo estándar directo en todas las versiones antiguas, 
            // pero podemos usar HttpRequestMessage con HttpMethod.Patch:
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
            {
                Content = contenido
            };

            var respuesta = await servicio.GetClient().SendAsync(request);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Arreglo actualizado correctamente", "Aceptar");
                await Navigation.PopAsync(); // Regresa al catálogo y este se refrescará al volver
            }
            else
            {
                string error = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error de Base de Datos", error, "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }
    }

    
}
