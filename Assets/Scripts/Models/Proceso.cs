using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class Proceso
{
    public int index = 0;
    public string nombre = "";
    public int llegada = 0;
    public int rafaga = 0;
    public int auxRafaga = 0;
    public List<int> operacion = new List<int>();
    public List<int> duracion = new List<int>();
    public List<int> auxDuracion = new List<int>();

    // Auxiliares para calculos de tiempo
    public int ultCPU = 0;

    // Para el algoritmo de Prioridades
    public int prioridad = 0;
}
