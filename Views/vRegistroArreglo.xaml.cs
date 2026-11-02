namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vRegistroArreglo : ContentPage
{
    private string rutaImagenLocal = string.Empty;
    public vRegistroArreglo()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasEnPicker();
    }

    private async Task CargarCategoriasEnPicker()
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
                PickerTitle = "Selecciona una imagen",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado != null)
            {
                rutaImagenLocal = resultado.FullPath;
                lblRutaArchivo.Text = resultado.FileName;
                imgPrevia.Source = ImageSource.FromFile(rutaImagenLocal);
                txtImagenUrl.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }


    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        // 1. Validar campos vacíos generales
        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtPrecio.Text) ||
            string.IsNullOrWhiteSpace(txtStock.Text) ||
            pickerCategorias.SelectedItem == null)
        {
            await DisplayAlert("Error", "Complete todos los campos obligatorios y seleccione una categoría", "Aceptar");
            return;
        }

        // 2. VALIDACIÓN ESTRICTA: Comprobar que el PRECIO sea un número válido
        if (!decimal.TryParse(txtPrecio.Text, out decimal precioValidado) || precioValidado <= 0)
        {
            await DisplayAlert("Error de validación", "El precio debe ser un número válido mayor a 0 (use punto o coma según su región).", "Aceptar");
            txtPrecio.Focus();
            return;
        }

        // 3. VALIDACIÓN ESTRICTA: Comprobar que el STOCK sea un número entero válido
        if (!int.TryParse(txtStock.Text, out int stockValidado) || stockValidado < 0)
        {
            await DisplayAlert("Error de validación", "El stock debe ser un número entero válido (0 o mayor).", "Aceptar");
            txtStock.Focus();
            return;
        }

        try
        {
            SupabaseService servicio = new SupabaseService();

            // Obtenemos el ID de la categoría seleccionada en el Picker gracias al Binding
            var categoriaSeleccionada = (CategoriaModel)pickerCategorias.SelectedItem;
            int idCategoriaReal = categoriaSeleccionada.id_categoria;

            string urlOImagenFinal = string.IsNullOrWhiteSpace(txtImagenUrl.Text) ? rutaImagenLocal : txtImagenUrl.Text;

            var nuevoArreglo = new
            {
                nombre = txtNombre.Text,
                descripcion = txtDescripcion.Text ?? string.Empty,
                precio = precioValidado,          // Usamos el valor ya validado
                stock = stockValidado,            // Usamos el valor ya validado
                imagen = urlOImagenFinal,
                id_categoria = idCategoriaReal,   // ID capturado automáticamente del nombre seleccionado
                estado = true
            };

            string json = JsonSerializer.Serialize(nuevoArreglo);
            StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

            string endpoint = $"{servicio.GetUrl()}/rest/v1/arreglos";
            var respuesta = await servicio.GetClient().PostAsync(endpoint, contenido);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Arreglo floral registrado correctamente", "Aceptar");
                await Navigation.PopAsync();
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
