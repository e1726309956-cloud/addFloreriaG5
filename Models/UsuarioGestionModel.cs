
using System;
using System.Text.Json.Serialization;

namespace addFloreriaG5.Models;

public class UsuarioGestionModel
{
    public int id_usuario { get; set; }

    public string nombres { get; set; } = "";
    public string apellidos { get; set; } = "";
    public string correo { get; set; } = "";
    public string telefono { get; set; } = "";

    public int id_rol { get; set; }
    public bool estado { get; set; }

    [JsonIgnore]
    public string NombreCompleto =>
        $"{nombres} {apellidos}".Trim();

    [JsonIgnore]
    public string NombreRol =>
        id_rol == 1 ? "Administrador" : "Cliente";

    [JsonIgnore]
    public string EstadoTexto =>
        estado ? "Activo" : "Inactivo";

    [JsonIgnore]
    public string TextoBoton =>
        estado ? "Desactivar" : "Activar";

    [JsonIgnore]
    public Color ColorEstado =>
        estado
            ? Color.FromArgb("#63754D")
            : Color.FromArgb("#9B3E5C");
}
