using addFloreriaG5.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace addFloreriaG5.Models
{
    public static class CarritoCompras
    {
        // Lista global que acumula los arreglos elegidos
        public static List<ItemCarrito> Items { get; set; } = new List<ItemCarrito>();

        public static void AgregarItem(ArregloModel arreglo, int cantidad)
        {
            // Si el usuario ya añadió el mismo arreglo antes, solo sumamos la cantidad
            var existente = Items.FirstOrDefault(i => i.Arreglo.id_arreglo == arreglo.id_arreglo);
            if (existente != null)
            {
                existente.Cantidad += cantidad;
            }
            else
            {
                Items.Add(new ItemCarrito { Arreglo = arreglo, Cantidad = cantidad });
            }
        }

        public static void Limpiar()
        {
            Items.Clear();
        }
    }

    public class ItemCarrito
    {
        public ArregloModel Arreglo { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal => Arreglo.precio * Cantidad;
    }

}
