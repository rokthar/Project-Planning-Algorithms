using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ControlUI : MonoBehaviour {
    public TextMeshProUGUI txtTituloAlgoritmo; 
    public TextMeshProUGUI txtMilisegundos;
    public TextMeshProUGUI txtColaListos;
    public TextMeshProUGUI txtColaCPU;
    public TextMeshProUGUI txtColaES;

    public TextMeshProUGUI txtTEjePromedio; // tiempo ejecucion promedio
    public TextMeshProUGUI txtTEspPromedio; // tiempo espera promedio

    public Transform parentColaListos;
    public Transform parentColaES;
    public Transform parentColaCPU;

    public GameObject prefabProceso;
    public GameObject btnIniciar;
    public GameObject btnParar;

    public ItemProceso proccessToCPU;
    public ItemProceso proccessToES;
    public ItemProceso proccessToLISTOS;
    public ItemProceso proccessCPUToLISTOS;
    public ItemProceso proccessESToLISTOS;

    public Animator anim;
    public Animator animProToES;
    public Animator animProToLISTOS;
    public Animator animProCPUToLISTOS;
    public Animator animProESToLISTOS;

    [Header ("Información para la tabla")]
    public GameObject panelTablaDatos;
    public GameObject btnVisualizarAlg;
    public TextMeshProUGUI txtNombreP;
    public TextMeshProUGUI txtTRafaga;
    public TextMeshProUGUI txtTLlegada;
    public TextMeshProUGUI txtOperES;
    public TextMeshProUGUI txtDuraES;
    public TextMeshProUGUI txtPriorQuantum;
    public TMP_InputField inputQuantum;
    public GameObject objQuantum;
    public GameObject btnLimpiar;
    public GameObject[] rows;

    [Header ("Formulario Nuevo Proceso")]
    public GameObject formNuevoProceso;
    public TMP_InputField inputLlegada;
    public TMP_InputField inputRafaga;
    public TMP_InputField inputOperacion;
    public TMP_InputField inputDuracion;
    public TMP_Dropdown cbxPrioridad;
    public TextMeshProUGUI txtMensaje;
    public GameObject btnBorrarProceso;
    public GameObject panelNuevaPrioridad;

    [Header ("Formulario Cambiar Algoritmo")]
    public GameObject formCambiarAlgoritmo;

    // banderas
    [HideInInspector] public bool algoritmoSJF = false;
    [HideInInspector] public bool algoritmoRR = false;
    [HideInInspector] public bool algoritmoPrioridad = false;

    // 
    private List<GameObject> listaColaListos = new List<GameObject> ();
    private List<GameObject> listaColaCPU = new List<GameObject> ();
    private List<GameObject> listaColaES = new List<GameObject> ();

    private int auxMs = 0; 
    private bool banTerminoEjecucion = false;

    // inicializa variables por primera vez (Al darle play en el Editor)
    void Start () {
        auxMs = 0;

        txtTEjePromedio.text = "";
        txtTEspPromedio.text = "";
        activarFormCambiarAlgoritmo (); // indica que este panel se mostrara al inicio
        banTerminoEjecucion = false;
    }

    void Update () {
        if (!banTerminoEjecucion) {
            if (auxMs != Algoritmos.ms) { 
                auxMs = Algoritmos.ms;
                txtMilisegundos.text = auxMs + " ms"; // actualiza el texto de los milisegundos actuales
            }   
        } else {
            txtMilisegundos.text = "<color=green>"+Algoritmos.ms + " ms</color>";
        }
        if (algoritmoRR) {
            if (inputQuantum.text != "" && (inputQuantum.text != Algoritmos.quantum.ToString ())) {
                Algoritmos.quantum = Convert.ToInt16 (inputQuantum.text);
            }
        }
    }

    public void BtnEjecutar () {
        btnIniciar.SetActive (false);
        btnParar.SetActive (true);
        banTerminoEjecucion = false;
    }

    public void PlanificacionTerminada () {
        btnIniciar.SetActive (true);
        btnParar.SetActive (false);
        banTerminoEjecucion = true;
    }

    // El origen solo es para identificar desde donde llega el proceso, asi las animaciones del
    // proceso que llega a la cola listos tendran una posicion de origen distinta...
    // 0 = nuevo, 1 = CPU, y 2 = E/S
    public void agregarProcesoColaListos (string nombre, int ms, int origen) {
        switch (origen) {
            case 0: // proceso nuevo
                animProToLISTOS.SetBool ("toLISTOS", true);
                proccessToLISTOS.setInfoProceso (nombre, ms);
                break;
                // case 1: // proviene desde CPU
                //     animProCPUToLISTOS.SetBool("toLISTOS", true);
                //     proccessCPUToLISTOS.setInfoProceso(nombre, ms);
                // break;
            case 2: // proviene desde E/S
                animProESToLISTOS.SetBool ("toLISTOS", true);
                proccessESToLISTOS.setInfoProceso (nombre, ms);
                break;
            default: // proceso nuevo
                animProToLISTOS.SetBool ("toLISTOS", true);
                proccessToLISTOS.setInfoProceso (nombre, ms);
                break;
        }

        GameObject obj = (GameObject) Instantiate (prefabProceso);
        obj.SetActive (false); // necesario para este metodo...
        obj.name = nombre;
        obj.transform.SetParent (parentColaListos.parent);
        obj.transform.localScale = new Vector3 (1f, 1f, 1f);
        listaColaListos.Add (obj);

        // cambiar parametros del proceso
        ItemProceso proceso = obj.GetComponent<ItemProceso> ();
        proceso.setInfoProceso (nombre, ms);
        proceso.colorEstadoNormal ();

        StartCoroutine (agregarProcesoA_LISTOS (nombre));
    }

    IEnumerator agregarProcesoA_LISTOS (string nombre) {
        yield return new WaitForSeconds (0.05f);
        animProToLISTOS.SetBool ("toLISTOS", false);
        animProCPUToLISTOS.SetBool ("toLISTOS", false);
        animProESToLISTOS.SetBool ("toLISTOS", false);

        yield return new WaitForSeconds (0.4f);
        for (int i = 0; i < listaColaListos.Count; i++) {
            if (listaColaListos[i].name == nombre) {
                listaColaListos[i].SetActive (true);
                break;
            }
        }
    }

    public void agregarProcesoColaES (string nombre, int ms) {
        animProToES.SetBool ("toES", true);

        eliminarUltimoCPU ();

        proccessToES.setInfoProceso ("", ms);

        StartCoroutine (agregarProcesoA_ES (nombre, ms));
    }

    IEnumerator agregarProcesoA_ES (string nombre, int ms) {
        yield return new WaitForSeconds (0.1f);

        animProToES.SetBool ("toES", false);

        yield return new WaitForSeconds (0.9f);

        GameObject obj = (GameObject) Instantiate (prefabProceso);
        obj.name = nombre;
        obj.transform.SetParent (parentColaES.parent);
        obj.transform.localScale = new Vector3 (1f, 1f, 1f);
        listaColaES.Add (obj);

        // cambiar parametros del proceso
        ItemProceso proceso = obj.GetComponent<ItemProceso> ();
        proceso.setInfoProceso (nombre, ms);
        proceso.colorEstadoOperacionES ();
    }

    public void agregarProcesoColaCPU (string nombre, int ms) {
        anim.SetBool ("toCPU", true);
        for (int i = 0; i < listaColaListos.Count; i++) {
            if (listaColaListos[i].name == nombre) {
                Destroy (listaColaListos[i]);
                listaColaListos.RemoveAt (i);
                break;
            }
        }
        proccessToCPU.setInfoProceso (nombre, ms);

        StartCoroutine (agregarProcesoAcpu (nombre, ms));
    }

    IEnumerator agregarProcesoAcpu (string nombre, int ms) {
        yield return new WaitForSeconds (0.1f);

        anim.SetBool ("toCPU", false);
        eliminarUltimoCPU ();

        yield return new WaitForSeconds (0.85f);

        GameObject obj = (GameObject) Instantiate (prefabProceso);
        obj.name = nombre;
        obj.transform.SetParent (parentColaCPU.parent);
        obj.transform.localScale = new Vector3 (1f, 1f, 1f);
        listaColaCPU.Add (obj);

        // cambiar parametros del proceso
        ItemProceso proceso = obj.GetComponent<ItemProceso> ();
        proceso.setInfoProceso (nombre, ms);
        proceso.colorEstadoEjecutando ();
    }

    public void eliminarUltimoCPU () {
        if (listaColaCPU.Count > 0) {
            Destroy (listaColaCPU[0]); // destruye el objeto, para que desaparesca de la interfaz
            listaColaCPU.RemoveAt (0); // borra el primer item de la lista
        }
    }

    public void eliminarProcesoES (string nombre) {
        for (int i = 0; i < listaColaES.Count; i++) {
            if (listaColaES[i].name == nombre) {
                Destroy (listaColaES[i]);
                listaColaES.RemoveAt (i);
                break;
            }
        }
    }

    public void refrescarTextosColas (string listos, string cpu, string es) {
        txtColaListos.text = listos;
        txtColaCPU.text = cpu;
        txtColaES.text = es;
    }

    public void refrescarTextosPromedios (float tEjeP, float tEspP) {
        txtTEjePromedio.text = "TEjeP = <color=#0FF023><size=22>" + tEjeP + "ms";
        txtTEspPromedio.text = "TEP = <color=#0FF023><size=22>" + tEspP + "ms";
    }

    public void dibujarFilas (int filas) {
        for (int i = 0; i < rows.Length; i++) {
            if (filas > i) {
                rows[i].SetActive (true);
            } else {
                rows[i].SetActive (false);
            }
        }
    }

    public void borrarTodosProcesos () {
        for (int i = 0; i < listaColaCPU.Count; i++) {
            Destroy (listaColaCPU[i]);
        }
        for (int j = 0; j < listaColaES.Count; j++) {
            Destroy (listaColaES[j]);
        }
        for (int k = 0; k < listaColaListos.Count; k++) {
            Destroy (listaColaListos[k]);
        }
        listaColaCPU.Clear ();
        listaColaES.Clear ();
        listaColaListos.Clear ();
    }

    public void btnAbrirFormNuevoProceso () {
        formNuevoProceso.SetActive (true);
        defaultFormNuevoProceso ();
    }

    public void btnCerrarFormNuevoProceso () {
        formNuevoProceso.SetActive (false);
    }

    public void limpiarInputsFormulario () {
        inputLlegada.text = "";
        inputRafaga.text = "";
        inputOperacion.text = "";
        inputDuracion.text = "";
        txtMensaje.text = "";
    }

    private void defaultFormNuevoProceso () {
        inputLlegada.text = "";
        inputRafaga.text = "";
        inputOperacion.text = "";
        inputDuracion.text = "";
        inputRafaga.interactable = true;
        if (algoritmoPrioridad) {
            panelNuevaPrioridad.SetActive (true);
        } else {
            panelNuevaPrioridad.SetActive (false);
        }
    }

    public void BtnParar () {
        // Detiene todas las corutinas
        StopAllCoroutines ();
        //
        btnIniciar.SetActive (true);
        btnParar.SetActive (false);
        // Elimina los objetos creados en UI...
        borrarTodosProcesos ();
        // textos por defecto
        txtMilisegundos.text = "0 ms";
        txtColaListos.text = "";
        txtColaCPU.text = "";
        txtColaES.text = "";
        txtTEjePromedio.text = "";
        txtTEspPromedio.text = "";
        banTerminoEjecucion = false;
    }

    public void activarBtnBorrarUtlProceso () {
        btnBorrarProceso.SetActive (true);
    }

    public void desactivarBtnBorrarUtlProceso () {
        btnBorrarProceso.SetActive (false);
    }

    public void activarBtnVisualizarAlgoritmo () {
        btnVisualizarAlg.SetActive (true);
    }

    public void desactivarBtnVisualizarAlgoritmo () {
        btnVisualizarAlg.SetActive (false);
    }

    public void btnAbrirPanelTablaDatos () {
        panelTablaDatos.SetActive (true);
    }

    public void btnCerrarPanelTablaDatos () {
        panelTablaDatos.SetActive (false);
    }

    public void activarFormCambiarAlgoritmo () {
        formCambiarAlgoritmo.SetActive (true);
    }

    public void desactivarFormCambiarAlgoritmo () {
        formCambiarAlgoritmo.SetActive (false);
    }

    public void BtnAlgoritmoSJF () {
        resetBanderasAlgoritmo ();
        algoritmoSJF = true;
        desactivarFormCambiarAlgoritmo ();
        defaultTabla ();
        txtTituloAlgoritmo.text = "Algoritmo SJF Apropiativo";
    }

    public void BtnAlgoritmoRR () {
        resetBanderasAlgoritmo ();
        algoritmoRR = true;
        objQuantum.SetActive (true);
        desactivarFormCambiarAlgoritmo ();
        defaultTabla ();
        txtTituloAlgoritmo.text = "Algoritmo Round Robin";
    }

    public void BtnAlgoritmoPrioridades () {
        resetBanderasAlgoritmo ();
        algoritmoPrioridad = true;
        desactivarFormCambiarAlgoritmo ();
        defaultTabla ();
        txtTituloAlgoritmo.text = "Algoritmo por Prioridades";
    }

    private void resetBanderasAlgoritmo () {
        algoritmoSJF = false;
        algoritmoPrioridad = false;
        algoritmoRR = false;
        objQuantum.SetActive (false);
    }

    public void defaultTabla () {
        txtNombreP.text = "<size=22>PROCESO</size>";
        txtTRafaga.text = "<size=22>T. Rafaga (ms)</size>";
        txtTLlegada.text = "<size=22>T. Llegada (ms)</size>";
        txtOperES.text = "<size=22>Oper. E/S</size>";
        txtDuraES.text = "<size=22>Duracion E/S</size>";

        if (algoritmoSJF) {
            txtPriorQuantum.text = "<size=22></size>"; // este depende del tipo de algoritmo
        } else if (algoritmoRR) {
            txtPriorQuantum.text = "<size=22>QUANTUM</size>"; // este depende del tipo de algoritmo
        } else if (algoritmoPrioridad) {
            txtPriorQuantum.text = "<size=22>PRIORIDAD</size>"; // este depende del tipo de algoritmo
        }
    }
}