using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Algoritmos : MonoBehaviour {
    public static int ms = 0; // indica el tiempo de ejecucion durante todo el proceso del algoritmo
    public int cpu = 0; // indica el indice del proceso que ejecute actualmente
    public static int quantum = 0, auxQuantum = 0; // indica el quantum... y su auxiliar

    // Listas para almacenar las colas y los procesos
    public List<Proceso> listaProcesos = new List<Proceso> (); // Lista de todos los procesos
    public List<int> colaListos = new List<int> (); // Procesos que actualmente se encuentre en LISTOS
    public List<int> colaES = new List<int> (); // Procesos que actualmente se encuentren en E/S

    // Lista las N operaciones que tenga un proceso (Solo para el formulario de Nuevo Proceso)
    public List<Operacion> operaciones = new List<Operacion> ();

    // Cadenas para presentar las colas
    public string txtColaListos = "";
    public string txtColaCPU = "";
    public string txtColaES = "";

    // privadas
    private int auxIndice = 0; // Toma en cuenta el indice del proceso actual para el ingreso de procesos

    // Banderas
    private bool banEjecutando = false;
    private bool banCPUinactiva = false;
    private bool banEsperar = false;

    // Referencia a otros componentes
    ControlUI controlUI;

    private void Awake () {
        controlUI = GetComponent<ControlUI> (); // Obtiene el componente y sus atributos publicos
    }

    void Start () {
        valoresPorDefecto ();

        // borrar esto
        llenarTabla ();
    }

    void Update () {
        // Mientras el algoritmo elegido sea Round Robin
        if (controlUI.algoritmoRR) {
            // Controla que este ingresado almenos 1 proceso y definido el Quantum para poder iniciar
            if (quantum > 0 && listaProcesos.Count > 0) {
                controlUI.activarBtnVisualizarAlgoritmo ();
            } else {
                controlUI.desactivarBtnVisualizarAlgoritmo ();
            }
        }
    }

    public void btnEjecutar () {
        // Inicia la ejecucion y animacion del algoritmo, segun el elegido
        if (controlUI.algoritmoSJF) {
            StartCoroutine ("ejecutarAlgoritmoSJF");
        } else if (controlUI.algoritmoRR) {
            StartCoroutine ("ejecutarAlgoritmoRR");
        } else if (controlUI.algoritmoPrioridad) {
            StartCoroutine ("ejecutarAlgoritmoPrioridades");
        }
    }

    /************************************** ALGORITMO SJF **************************************/
    IEnumerator ejecutarAlgoritmoSJF () {
        while (true) {
            // Se verifica cada milisegundo los procesos que podrian 'entrar' a la cola de listos
            verificarProcesosListos ();

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.75f);
            }

            // Verificar y 'procesar' operaciones de E/S
            procesarOperacionesES ();

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.5f);
            }

            // Verifica si un proceso puede ocupar la CPU (si esta vacia)
            // Verifica si un proceso puede 'desalojar' a otro proceso de la cola 
            verificarApropiarCPU ();

            // 'Ejecutar' el ultimo proceso que se encuentre actualmente en la cola CPU
            if (banEjecutando) {
                ejecutarColaCPU ();
                banCPUinactiva = true;
            } else {
                if (banCPUinactiva) {
                    banCPUinactiva = false;
                    txtColaCPU += " |<size=10>(" + ms + "ms)</size>///";

                    controlUI.eliminarUltimoCPU (); // eliminar ultimo proceso por UI
                    controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES); // mostrar por UI

                    if (verificarFinalProcesos ()) {
                        controlUI.borrarTodosProcesos ();
                        break; // Terminar ejecución
                    }
                }
            }

            yield return new WaitForSeconds (1.25f);

            ms++;

            calcularTiemposPromedio ();
        }

        // Calcular y presentar los promedios
        calcularTiemposPromedio ();
        // final por UI
        controlUI.PlanificacionTerminada ();
    }

    /************************************** ALGORITMO ROUND ROBIN **************************************/
    IEnumerator ejecutarAlgoritmoRR () {
        while (true) {

            // Se verifica cada milisegundo los procesos que podrian 'entrar' a la cola de listos
            verificarProcesosListos ();

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.75f);
            }

            // procesar operaciones de E/S
            procesarOperacionesES_RR ();

            // Verificar operaciones de E/S
            verificarProcesosES ();

            if (auxQuantum == 0) {
                //auxQuantum = quantum;
                if (cpu != -1) {
                    banEjecutando = false; // deja de 'ejecutar'
                    colaListos.Add (cpu); //
                    txtColaListos += " |<size=10>(" + (ms) + "ms)</size> " + listaProcesos[cpu].nombre;
                    print ("holi" + listaProcesos[cpu].nombre + " ms:" + ms);

                    //mostrar por UI
                    controlUI.agregarProcesoColaListos (listaProcesos[cpu].nombre, ms, 1);
                    controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);

                    cpu = -1;
                }
            }

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.5f);
            }

            // Si el CPU no esta 'ejecutando' ningun proceso...
            // Verifica si un proceso puede ocupar la CPU (si esta vacia)
            if (colaListos.Count > 0 && cpu == -1) {
                int p = colaListos[0]; // Escoge el primer proceso de la cola de listos
                int auxListo = 0; // Almacena ka posicion del proceso que se encuentra en la cola de listo

                asignarProcesoCPU (p, auxListo);
                auxQuantum = quantum;
            }

            // 'Ejecutar' el ultimo proceso que se encuentre actualmente en la cola CPU
            if (banEjecutando) {
                ejecutarColaCPU_RR ();
                banCPUinactiva = true;
            } else {
                if (banCPUinactiva) {
                    banCPUinactiva = false;
                    txtColaCPU += " |<size=10>(" + ms + "ms)</size>///";

                    controlUI.eliminarUltimoCPU (); // eliminar ultimo proceso por UI
                    controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES); // mostrar por UI

                    if (verificarFinalProcesos ()) {
                        controlUI.borrarTodosProcesos ();
                        break; // Terminar ejecución
                    }
                }
            }

            yield return new WaitForSeconds (1.25f);

            ms++;

            calcularTiemposPromedio ();
        }

        // Calcular y presentar los promedios
        calcularTiemposPromedio ();
        // final por UI
        controlUI.PlanificacionTerminada ();
    }

    /************************************** ALGORITMO POR PRIORIDADES **************************************/
    IEnumerator ejecutarAlgoritmoPrioridades () {
        while (true) {
            // Se verifica cada milisegundo los procesos que podrian 'entrar' a la cola de listos
            verificarProcesosListos ();

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.75f);
            }

            // Verificar y 'procesar' operaciones de E/S
            procesarOperacionesES ();

            if (banEsperar) {
                banEsperar = false;
                yield return new WaitForSeconds (0.5f);
            }

            // Verifica si un proceso puede ocupar la CPU (si esta vacia)
            // Verifica si un proceso puede 'desalojar' a otro proceso de la cola 
            verificarApropiarCPU_Prioridades ();

            // 'Ejecutar' el ultimo proceso que se encuentre actualmente en la cola CPU
            if (banEjecutando) {
                ejecutarColaCPU ();
                banCPUinactiva = true;
            } else {
                if (banCPUinactiva) {
                    banCPUinactiva = false;
                    txtColaCPU += " |<size=10>(" + ms + "ms)</size>///";

                    controlUI.eliminarUltimoCPU (); // eliminar ultimo proceso por UI
                    controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES); // mostrar por UI

                    if (verificarFinalProcesos ()) {
                        controlUI.borrarTodosProcesos ();
                        break; // Terminar ejecución
                    }
                }
            }

            yield return new WaitForSeconds (1.0f);

            ms++;

            calcularTiemposPromedio ();
        }

        // Calcular y presentar los promedios
        calcularTiemposPromedio ();
        // final por UI
        controlUI.PlanificacionTerminada ();
    }

    public void verificarProcesosListos () {
        banEsperar = false;
        foreach (var i in listaProcesos) {
            // si el tiempo de llegada del proceso es igual al milisegundo actual entonces 
            // es almacenado en la cola de listos (Por si acaso verifica que la rafaga no sea 0)
            if ((ms == i.llegada) && (i.auxRafaga > 0)) {
                colaListos.Add (i.index);
                txtColaListos += " |<size=10>(" + ms + "ms)</size> " + i.nombre;
                banEsperar = true;

                //mostrar por UI
                controlUI.agregarProcesoColaListos (i.nombre, ms, 0);
                controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);
            }
        }
    }

    public void procesarOperacionesES () {
        banEsperar = false;
        // Verifica si existen procesos en la cola de E/S
        if (colaES.Count > 0) {
            bool banDevolverP = false; // porque no se puede devolver el proceso mientras aun recorre el FOR...
            List<int> listDevolver = new List<int> (); // lista de los procesos que volveran a LISTOS
            List<int> listSalirES = new List<int> (); // lista de los procesos que salen de ES
            for (int i = 0; i < colaES.Count; i++) {
                // si el proceso tiene una o mas operaciones E/S, las recorre
                if (listaProcesos[colaES[i]].auxDuracion.Count > 0) {
                    for (int j = 0; j < listaProcesos[colaES[i]].auxDuracion.Count; j++) {
                        int auxDuracion = listaProcesos[colaES[i]].auxDuracion[j];
                        // Realmente necesario comparar cada una hasta encontrar la operacion 'actual'
                        if (auxDuracion > 0) {
                            // Decrementa 1 la duracion de la operacion cada ms que pasa
                            listaProcesos[colaES[i]].auxDuracion[j] = auxDuracion - 1;
                            // Si el proceso ya termino de realizar la operacion de E/S entonces lo elimina de la cola de E/S
                            if (auxDuracion == 1) { // esto quiere decir que ya termino su duracion de la operacion actual
                                banDevolverP = true;

                                listSalirES.Add (i);
                                listDevolver.Add (listaProcesos[colaES[i]].index);
                            }
                            break; // Debe romperse el bucle debido a que solo debe restarle duracion a una de las operaciones
                        }
                    }
                }
            }
            // Es necesario devolver al final del recorrido el proceso porque de hacerlo antes
            // se dañaria el recorrido de las siguientes operaciones de otros precesos (De haberlos)
            if (banDevolverP) {
                foreach (int i in listDevolver) {
                    if (listaProcesos[i].auxRafaga > 0) {
                        colaListos.Add (i); //
                        txtColaListos += " |<size=10>(" + ms + "ms)</size> " + listaProcesos[i].nombre;

                        // Eliminar proceso en UI
                        controlUI.eliminarProcesoES (listaProcesos[i].nombre);
                        print ("salio de ES: " + listaProcesos[i].nombre + " en ms: " + ms);
                        //colaES.RemoveAt (j);
                        colaES.Remove (listaProcesos[i].index);

                        //mostrar por UI
                        controlUI.agregarProcesoColaListos (listaProcesos[i].nombre, ms, 2);
                        controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);
                        banEsperar = true;
                    }
                }
                listDevolver.Clear ();
            }
        }

        if (cpu != -1) {
            // Verificar si el proceso tiene operaciones de E/S
            if (listaProcesos[cpu].operacion.Count > 0) {
                for (int i = 0; i < listaProcesos[cpu].auxDuracion.Count; i++) {
                    // si la operacion actual ya fue ejecutado
                    if (listaProcesos[cpu].auxDuracion[i] == 0) {
                        continue;
                    }
                    // sino comprueba si tiene que realizar alguna operacion en el milisegundo actual
                    int operacion = listaProcesos[cpu].operacion[i];
                    int rafagaActual = listaProcesos[cpu].auxRafaga;
                    int rafaga = listaProcesos[cpu].rafaga;
                    int comp = rafaga - operacion;
                    if (comp == rafagaActual) {
                        // El proceso debe hacer una operacion de E/S
                        banEjecutando = false; // deja de 'ejecutar' el proceso actual

                        colaES.Add (listaProcesos[cpu].index); // envia el proceso de CPU a la cola de E/S
                        txtColaES += " |<size=10>(" + ms + "ms)</size> " + listaProcesos[cpu].nombre;

                        // solo si es el algoritmo de prioridad...
                        //if (controlUI.algoritmoPrioridad) {
                        for (int j = 0; j < colaListos.Count; j++) {
                            if (colaListos[j] == listaProcesos[cpu].index) {
                                colaListos.RemoveAt (j);
                                break;
                            }
                        }
                        //}

                        //mostrar por UI
                        controlUI.agregarProcesoColaES (listaProcesos[cpu].nombre, ms);
                        controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);

                        cpu = -1;
                        break;
                    }
                }
            }
        }
    }

    public void procesarOperacionesES_RR () {
        banEsperar = false;
        // Verifica si existen procesos en la cola de E/S
        if (colaES.Count > 0) {
            bool banDevolverP = false; // porque no se puede devolver el proceso mientras aun recorre el FOR...
            List<int> listDevolver = new List<int> (); // lista de los procesos que volveran a LISTOS
            List<int> listSalirES = new List<int> (); // lista de los procesos que salen de ES
            for (int i = 0; i < colaES.Count; i++) {
                // si el proceso tiene una o mas operaciones E/S, las recorre
                if (listaProcesos[colaES[i]].auxDuracion.Count > 0) {
                    for (int j = 0; j < listaProcesos[colaES[i]].auxDuracion.Count; j++) {
                        int auxDuracion = listaProcesos[colaES[i]].auxDuracion[j];
                        // Realmente necesario comparar cada una hasta encontrar la operacion 'actual'
                        if (auxDuracion > 0) {
                            // Decrementa 1 la duracion de la operacion cada ms que pasa
                            listaProcesos[colaES[i]].auxDuracion[j] = auxDuracion - 1;
                            // Si el proceso ya termino de realizar la operacion de E/S entonces lo elimina de la cola de E/S
                            if (auxDuracion == 1) { // esto quiere decir que ya termino su duracion de la operacion actual
                                banDevolverP = true;

                                listSalirES.Add (i);
                                listDevolver.Add (listaProcesos[colaES[i]].index);
                            }
                            break; // Debe romperse el bucle debido a que solo debe restarle duracion a una de las operaciones
                        }
                    }
                }
            }
            // Es necesario devolver al final del recorrido el proceso porque de hacerlo antes
            // se dañaria el recorrido de las siguientes operaciones de otros precesos (De haberlos)
            if (banDevolverP) {
                foreach (int i in listDevolver) {
                    if (listaProcesos[i].auxRafaga > 0) {
                        colaListos.Add (i); //
                        txtColaListos += " |<size=10>(" + ms + "ms)</size> " + listaProcesos[i].nombre;

                        // Eliminar proceso en UI
                        controlUI.eliminarProcesoES (listaProcesos[i].nombre);
                        print ("salio de ES: " + listaProcesos[i].nombre + " en ms: " + ms);
                        //colaES.RemoveAt (j);
                        colaES.Remove (listaProcesos[i].index);

                        //mostrar por UI
                        controlUI.agregarProcesoColaListos (listaProcesos[i].nombre, ms, 2);
                        controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);
                        banEsperar = true;
                    }
                }
                listDevolver.Clear ();
            }
        }
    }

    public void verificarProcesosES () {
        // Ejecuta este bloque solo si existe un proceso en CPU
        if (cpu != -1) {
            // Verificar si el proceso tiene operaciones de E/S
            if (listaProcesos[cpu].operacion.Count > 0) {
                for (int i = 0; i < listaProcesos[cpu].auxDuracion.Count; i++) {
                    // si la operacion actual ya fue ejecutada, continua con la siguiente (de tener otra)
                    if (listaProcesos[cpu].auxDuracion[i] == 0) {
                        continue;
                    }
                    // sino comprueba si tiene que realizar alguna operacion en el milisegundo actual
                    int operacion = listaProcesos[cpu].operacion[i]; // obtiene la operacion actual
                    int rafagaActual = listaProcesos[cpu].auxRafaga; // obtiene la rafaga auxiliar
                    int rafaga = listaProcesos[cpu].rafaga; // obtiene la rafaga original
                    int comp = rafaga - operacion; // para comparar

                    // comprueba si es el milisegundo en que debe hacer la operacion actual
                    if (comp == rafagaActual) {
                        // El proceso debe hacer una operacion de E/S
                        banEjecutando = false; // deja de 'ejecutar' el proceso actual

                        colaES.Add (listaProcesos[cpu].index); // envia el proceso de CPU a la cola de E/S
                        txtColaES += " |<size=10>(" + (ms) + "ms)</size> " + listaProcesos[cpu].nombre;

                        // solo si es el algoritmo de prioridad...
                        //if (controlUI.algoritmoPrioridad) {
                        // busca el proceso en la cola de listos
                        for (int j = 0; j < colaListos.Count; j++) {
                            if (colaListos[j] == listaProcesos[cpu].index) {
                                colaListos.RemoveAt (j); // quita el proceso de la cola de listos
                                break;
                            }
                        }
                        //}

                        //mostrar por UI
                        controlUI.agregarProcesoColaES (listaProcesos[cpu].nombre, ms);
                        controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);

                        cpu = -1; // el CPU no ejecuta ningun proceso.
                        break;
                    }
                }
            }
        }
    }

    public void verificarApropiarCPU () {
        // Si existen procesos en la cola de listos verifica...
        if (colaListos.Count > 0) {
            int p = colaListos[0]; // Escoge el primer proceso de la cola de listos
            int auxListo = 0; // Almacena ka posicion del proceso que se encuentra en la cola de listo
            // Si existe mas de un proceso en la cola de LISTOS
            // Entonces compara cual es el de menor rafaga y segun FIFO
            if (colaListos.Count > 1) {
                for (int i = 1; i < colaListos.Count; i++) {
                    if (listaProcesos[colaListos[i]].auxRafaga < listaProcesos[p].auxRafaga) {
                        p = listaProcesos[colaListos[i]].index; // obtiene el objeto del que tenga menor rafaga
                        auxListo = i;
                    }
                }
            }
            // Solo si esta ejecutando verifica que el proceso con menor rafaga 
            // de la cola de LISTOS sea menor al de la cola de CPU
            if (cpu != -1) {
                Proceso pCPU = listaProcesos[cpu]; // ultimo proceso en CPU
                if (listaProcesos[p].auxRafaga < pCPU.auxRafaga) {
                    if (pCPU.auxRafaga > 0) {
                        colaListos.Add (pCPU.index); // El proceso desalojado vuelve a la cola de LISTOS
                        // UI
                        //controlUI.agregarProcesoColaListos(listaProcesos[p].nombre, ms, 1);
                    }
                    print (listaProcesos[p].nombre + " desalojo a: " + listaProcesos[cpu].nombre);
                    asignarProcesoCPU (p, auxListo); // El nuevo proceso pasa a ejecutarse en CPU
                }
            }
            // Si el CPU no esta 'ejecutando' ningun proceso...
            if (cpu == -1) {
                asignarProcesoCPU (p, auxListo);
            }
        }
    }

    public void verificarApropiarCPU_Prioridades () {
        quitarListosSinRafaga ();
        // Si existen procesos en la cola de listos verifica...
        if (colaListos.Count > 0) {
            int p = colaListos[0]; // Escoge el primer proceso de la cola de listos
            int auxListo = 0; // Almacena la posicion del proceso que se encuentra en la cola de listo
            // Si existe mas de un proceso en la cola de LISTOS
            // Entonces compara cual es el de menor prioridad y segun FIFO
            if (colaListos.Count > 1) {
                for (int i = 1; i < colaListos.Count; i++) {
                    if (listaProcesos[colaListos[i]].prioridad < listaProcesos[p].prioridad) {
                        p = listaProcesos[colaListos[i]].index; // obtiene el objeto del que tenga menor rafaga
                        auxListo = i;
                    }
                }
            }
            // Solo si esta ejecutando verifica que el proceso con menor rafaga 
            // de la cola de LISTOS sea menor al de la cola de CPU
            if (cpu != -1) {
                Proceso pCPU = listaProcesos[cpu]; // ultimo proceso en CPU
                if (listaProcesos[p].prioridad < pCPU.prioridad) {
                    if (pCPU.auxRafaga > 0) {
                        colaListos.Add (pCPU.index); // El proceso desalojado vuelve a la cola de LISTOS
                        // UI
                        //controlUI.agregarProcesoColaListos(listaProcesos[p].nombre, ms, 1);
                    }
                    print (listaProcesos[p].nombre + " desalojo a: " + listaProcesos[cpu].nombre);
                    asignarProcesoCPU (p, auxListo); // El nuevo proceso pasa a ejecutarse en CPU
                }
            }
            // Si el CPU no esta 'ejecutando' ningun proceso...
            if (cpu == -1) {
                asignarProcesoCPU (p, auxListo);
            }
        }
    }

    private void quitarListosSinRafaga () {
        // Verificar que no haya procesos sin rafaga en LISTOS
        for (int k = 0; k < colaListos.Count; k++) {
            if (listaProcesos[colaListos[k]].auxRafaga == 0) {
                colaListos.RemoveAt (k);
                break;
            }
        }
    }

    public void asignarProcesoCPU (int p, int remove) {
        banEjecutando = true;
        txtColaCPU += " |<size=10>(" + ms + "ms)</size> " + listaProcesos[p].nombre;

        //mostrar por UI
        controlUI.agregarProcesoColaCPU (listaProcesos[p].nombre, ms);
        controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);

        cpu = p;

        // Si NO es el algoritmo de prioridad entonces quita el proceso de LISTOS
        if (!controlUI.algoritmoPrioridad) {
            colaListos.RemoveAt (remove);
        }
    }

    public void ejecutarColaCPU () {
        if (listaProcesos[cpu].auxRafaga > 0) {
            int n = listaProcesos[cpu].auxRafaga - 1;
            listaProcesos[cpu].auxRafaga = n; // decrementa en 1 la rafaga (Auxiliar)
        }
        listaProcesos[cpu].ultCPU = ms + 1; // almacena el ultimo ms de ejecucion del proceso
        // Cuando un proceso termina su rafaga de milisegundos
        if (listaProcesos[cpu].auxRafaga == 0) {
            banEjecutando = false; // deja de 'ejecutar'
            cpu = -1;

            // Buscar y eliminar de la cola de listos
            quitarListosSinRafaga ();
        } else {
            banEjecutando = true;
        }
    }

    public void ejecutarColaCPU_RR () {
        if (listaProcesos[cpu].auxRafaga > 0) {
            int n = listaProcesos[cpu].auxRafaga - 1;
            listaProcesos[cpu].auxRafaga = n; // decrementa en 1 la rafaga (Auxiliar)
            auxQuantum--;
        }
        listaProcesos[cpu].ultCPU = ms + 1; // almacena el ultimo ms de ejecucion del proceso
        // Cuando un proceso termina su rafaga de milisegundos
        if (listaProcesos[cpu].auxRafaga == 0) {
            banEjecutando = false; // deja de 'ejecutar'
            cpu = -1;
        }
        /*else if (auxQuantum == 0) {
                   verificarProcesosES ();
                   if (cpu != -1) {
                       banEjecutando = false; // deja de 'ejecutar'
                       colaListos.Add (cpu); //
                       txtColaListos += " |<size=10>(" + (ms + 1) + "ms)</size> " + listaProcesos[cpu].nombre;

                       //mostrar por UI
                       controlUI.agregarProcesoColaListos (listaProcesos[cpu].nombre, ms, 1);
                       controlUI.refrescarTextosColas (txtColaListos, txtColaCPU, txtColaES);

                       cpu = -1;
                   }
               } */
        else {
            banEjecutando = true;
        }

    }

    public bool verificarFinalProcesos () {
        foreach (var i in listaProcesos) {
            if (i.auxRafaga != 0) {
                return false; // si almenos 1 proceso tiene rafaga entonces sigue 'ejecutando' el CPU
            }
        }
        return true; // si todos los procesos terminaron su rafaga entonces termina el bucle principal
    }

    public void calcularTiemposPromedio () {
        float rEje = 0f, rEsp = 0f, sum1 = 0f, sum2 = 0f;

        for (int i = 0; i < listaProcesos.Count; i++) {
            if (listaProcesos[i].rafaga != listaProcesos[i].auxRafaga) {
                sum1 += (listaProcesos[i].ultCPU - listaProcesos[i].llegada); // ejecucion

                // si el proceso tiene operaciones de entrada y salida, suma sus duraciones
                int sumDuracion = 0;
                for (int j = 0; j < listaProcesos[i].operacion.Count; j++) {
                    sumDuracion += listaProcesos[i].duracion[j];
                }
                sum2 += (listaProcesos[i].ultCPU - listaProcesos[i].llegada - sumDuracion - listaProcesos[i].rafaga); // espera
            }
        }

        rEje = sum1 / listaProcesos.Count; // promedio tiempo de ejecucion
        rEsp = sum2 / listaProcesos.Count; // promedio tiempo de espera 

        if (rEje < 0f)
            rEje = rEje * -1;
        if (rEsp < 0f)
            rEsp = rEsp * -1;

        // Presentar por UI
        controlUI.refrescarTextosPromedios (rEje, rEsp);
    }

    public void btnReiniciarAlgoritmo () {
        StopAllCoroutines ();
        // Establece los valores de texto y variables por default
        valoresPorDefecto ();
        // Restablece los valores auxiliares de los objetos
        restablecerValoresProcesos ();
        // Elimina los objetos creados en UI...
        controlUI.borrarTodosProcesos ();

        btnEjecutar ();
    }

    private void valoresPorDefecto () {
        txtColaCPU = "CPU: ";
        txtColaES = "E/S: ";
        txtColaListos = "LISTOS: ";

        cpu = -1;
        ms = 0;
        auxQuantum = quantum;
        banEjecutando = false;
        colaListos.Clear ();
        colaES.Clear ();
    }

    public void btnPararEjecucionAlgoritmo () {
        // Detiene todas las corutinas
        StopAllCoroutines ();
        // Restablece los valores auxiliares de los objetos
        restablecerValoresProcesos ();
        // Establece los valores de texto y variables por default
        valoresPorDefecto ();
        // borra los objetos de la interfaz
        controlUI.BtnParar ();
    }

    private void restablecerValoresProcesos () {
        // Restablece los valores auxiliares de los objetos
        foreach (var i in listaProcesos) {
            i.auxRafaga = i.rafaga;
            for (int j = 0; j < i.duracion.Count; j++) {
                i.auxDuracion[j] = i.duracion[j];
            }
        }
    }

    public void btnBorrarListaProcesos () {
        auxIndice = 0;
        listaProcesos.Clear ();
        operaciones.Clear ();
        controlUI.defaultTabla ();
        controlUI.dibujarFilas (-1);
        controlUI.btnLimpiar.SetActive (false);
        controlUI.desactivarBtnBorrarUtlProceso ();
        controlUI.desactivarBtnVisualizarAlgoritmo ();
    }

    public void btnBorrarUltimoProceso () {
        auxIndice--;
        listaProcesos.RemoveAt (listaProcesos.Count - 1);
        operaciones.Clear ();
        llenarTabla ();
        if (listaProcesos.Count == 0) {
            controlUI.dibujarFilas (-1);
            controlUI.desactivarBtnBorrarUtlProceso ();
            controlUI.desactivarBtnVisualizarAlgoritmo ();
        } else {
            controlUI.dibujarFilas (listaProcesos.Count);
        }
    }

    public void llenarTabla () {
        controlUI.defaultTabla ();
        controlUI.btnLimpiar.SetActive (true);
        for (int j = 0; j < listaProcesos.Count; j++) {
            controlUI.txtNombreP.text += "\n" + listaProcesos[j].nombre;
            controlUI.txtTRafaga.text += "\n" + listaProcesos[j].rafaga;
            controlUI.txtTLlegada.text += "\n" + listaProcesos[j].llegada;
            string opers = "", duras = "";
            for (int i = 0; i < listaProcesos[j].operacion.Count; i++) {
                if (i == 0) {
                    opers = "" + listaProcesos[j].operacion[i];
                    duras = "" + listaProcesos[j].duracion[i];
                } else {
                    opers += " - " + listaProcesos[j].operacion[i];
                    duras += " - " + listaProcesos[j].duracion[i];
                }
            }
            if (controlUI.algoritmoPrioridad)
                controlUI.txtPriorQuantum.text += "\n" + listaProcesos[j].prioridad;

            controlUI.txtOperES.text += "\n" + opers;
            controlUI.txtDuraES.text += "\n" + duras;
        }

        // dibujar filas
        controlUI.dibujarFilas (listaProcesos.Count);
    }

    public void btnAgregarProceso () {
        if (controlUI.inputLlegada.text != "" && controlUI.inputRafaga.text != "") {
            Proceso p = new Proceso ();
            p.index = auxIndice;
            p.nombre = "P" + (auxIndice + 1);
            p.llegada = Convert.ToInt16 (controlUI.inputLlegada.text);
            p.auxRafaga = p.rafaga = Convert.ToInt16 (controlUI.inputRafaga.text);

            if (controlUI.algoritmoPrioridad) {
                p.prioridad = Convert.ToInt16 (controlUI.cbxPrioridad.options[controlUI.cbxPrioridad.value].text); // obtiene la prioridad elegida
            }

            // Agregar operaciones de E/S (si exiten)
            foreach (var i in operaciones) {
                p.operacion.Add (i.operacion);
                p.duracion.Add (i.duracion);
                p.auxDuracion.Add (i.duracion);
            }

            listaProcesos.Add (p);
            operaciones.Clear ();
            controlUI.limpiarInputsFormulario ();
            llenarTabla ();
            auxIndice++;
            controlUI.btnCerrarFormNuevoProceso ();
            controlUI.activarBtnBorrarUtlProceso ();
            controlUI.activarBtnVisualizarAlgoritmo ();
        } else {
            controlUI.txtMensaje.text = "<color=red>Faltan datos.";
        }
    }

    public void btnAgregarOperacionES () {
        if (controlUI.inputLlegada.text != "" && controlUI.inputRafaga.text != "") {
            if (controlUI.inputOperacion.text != "" && controlUI.inputDuracion.text != "") {
                int op = Convert.ToInt16 (controlUI.inputOperacion.text);
                int du = Convert.ToInt16 (controlUI.inputDuracion.text);

                // Validar si la operacion ingresada es correcta...
                int ra = Convert.ToInt16 (controlUI.inputRafaga.text);
                if (op < ra) {
                    foreach (var i in operaciones) {
                        if (op == i.operacion) { // no puede haber operaciones en el mismo milisegundo (ms)
                            controlUI.txtMensaje.text = "<color=red>YA existe una operación en ese milisegundo.";
                            return;
                        }
                    }
                } else { // la operacion no debe ser mayor o igual a la rafaga del proceso
                    controlUI.txtMensaje.text = "<color=red>La operación debe ser menor a la ráfaga del proceso.";
                    return;
                }
                Operacion oper = new Operacion ();
                oper.operacion = op;
                oper.duracion = du;
                operaciones.Add (oper);
                // Ordena las operaciones de ES de menor a mayor de acuerdo al ms de entrada
                operaciones.Sort ((x, y) => x.operacion.CompareTo (y.operacion));
                // Se limpia las cajas
                controlUI.inputOperacion.text = "";
                controlUI.inputDuracion.text = "";
                // Se muestra en interfaz
                controlUI.txtMensaje.text = "";
                string str1 = "O. E/S:", str2 = "\nD. E/S:";
                foreach (var i in operaciones) {
                    str1 += " \t" + i.operacion;
                    str2 += " \t" + i.duracion;
                }
                controlUI.txtMensaje.text += str1 + str2;
                controlUI.inputRafaga.interactable = false;
            } else {
                controlUI.txtMensaje.text = "<color=red>Faltan datos para agregar una operación de E/S.";
            }
        } else {
            controlUI.txtMensaje.text = "<color=red>Faltan datos.";
        }
    }
}