namespace addFloreriaG5.Views;

public partial class vMenuVendedor : ContentPage
{
	public vMenuVendedor(string nombre)
	{
		InitializeComponent();
        lblUsuario.Text = "Bienvenido: " + nombre;
    }
}