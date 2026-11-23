
using System.Net.Http.Headers;
using addFloreriaG5.Services;

namespace addFloreriaG5.Services;

public class ImagenStorageService
{
    public async Task<string> SubirImagenAsync(FileResult archivo)
    {
        if (archivo == null)
            throw new ArgumentException("Selecciona una fotografía.");

        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        string tipoContenido = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => throw new InvalidOperationException(
                "Solo se permiten imágenes JPG, PNG o WEBP.")
        };

        const long tamañoMaximo = 5 * 1024 * 1024;

        await using var archivoStream = await archivo.OpenReadAsync();

        if (archivoStream.CanSeek && archivoStream.Length > tamañoMaximo)
            throw new InvalidOperationException(
                "La fotografía no puede superar los 5 MB.");

        string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

        var servicio = new SupabaseService();

        string url =
            $"{servicio.GetUrl().TrimEnd('/')}/storage/v1/object/arreglos/{nombreArchivo}";

        using var contenido = new StreamContent(archivoStream);
        contenido.Headers.ContentType =
            new MediaTypeHeaderValue(tipoContenido);

        using var respuesta = await servicio.GetClient()
            .PostAsync(url, contenido);

        if (!respuesta.IsSuccessStatusCode)
        {
            string detalle = await respuesta.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"No se pudo subir la fotografía: {detalle}");
        }

        // URL PÚBLICA QUE SE GUARDARÁ EN LA TABLA ARREGLOS
        return $"{servicio.GetUrl().TrimEnd('/')}/storage/v1/object/public/arreglos/{nombreArchivo}";
    }
}
