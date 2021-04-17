using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemProceso : MonoBehaviour
{
    public TextMeshProUGUI txtNombre;
    public int ms; // milisegundo de llegada del proceso (a cualquier cola)
    public Image img;

    public void setInfoProceso(string nom, int ms){
        txtNombre.text = nom;
        this.ms = ms;
    }
    public void colorEstadoNormal(){
        img.color = new Color (0.4f, 1f, 1f);
    }

    public void colorEstadoEjecutando(){
        img.color = new Color (0.4402813f, 0.9622642f, 0.4910671f);
    }

    public void colorEstadoOperacionES(){
        img.color = new Color (1f, 1f, 0.3f);
    }
}
