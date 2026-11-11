using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace InterfazZombieApocalipsis
{
    public partial class InterfazForm : Form
    {
        //Socket y Thread
        Socket server;
        Thread atender;

        // Encapsulará la función para hacer lo que yo quiero (la de PonRespuesta), debe tener mismos parámetros (L5.2 ejercicio guía)
        delegate void DelegadoParaEscribir(string mensaje);
        delegate void DelegadoParaConectados(string[] datosConectados);
        delegate void DelegadoParaPartidas(string codigoPartida, string jugadorInvitado);

        const int PUERTO = 9080; // El puerto del servidor C
        IPAddress ip = IPAddress.Parse("127.0.0.1");    //Si no va, poned la que asigna la VirtualBox por defecto (192.168.56.101 o mirad con ip a)
                
        int playerTurn = 1;
        bool partidaIniciada = false;
        Random randomNumber = new Random();
        List<List<int>> PosicionJugadores = new List<List<int>>();

        string NombreCliente = "";
        bool ClienteSeHaRegistrado = false;
        Stack<string> StackConsultas = new Stack<string>();
        //Lista de partidas para llevar el control de las partidas y los jugadores que hay en ellas
        private List<Partidas> listaPartidas = new List<Partidas>();

        public InterfazForm()
        {
            InitializeComponent();
        }

        private void InterfazForm_Load(object sender, EventArgs e)
        { 
            // Tablero
            ConfigurarTablero();

            //Cagada threads
            //CheckForIllegalCrossThreadCalls = false;
        }


        /// IMPORTANTE
        /// Función que se encarga de recibir TODAS las peticiones
        /// IMPORTANTE

        //FUNCIONES IMPORTANTES DE LOS DELEGADOS PARA ESCRIBIR EN LA INTERFAZ (L5.2 ejercicio guía)
        public void PonRespuesta(string mensaje)
        {
            RespuestasServidorLabel.Text = mensaje;
        }

        public void PonConectados(string[] partes)
        {
            ConectadosListBox.Items.Clear();
            for (int i = 2; i < partes.Length; i++)
            {
                string nombreLimpio = partes[i].Split('\0')[0];
                if (!string.IsNullOrWhiteSpace(nombreLimpio))
                    ConectadosListBox.Items.Add(nombreLimpio);
            }
        }

        public void PonPartidas(string codigoPartida, string jugadorInvitado)
        {
            ConfigurarTablaPartidas(codigoPartida, jugadorInvitado);
        }

        private void AtenderServidor()
        {
            while (true)
            {
                try
                {
                    //Recibimos la respuesta del servidor C
                    byte[] buffer = new byte[512];
                    int bytesRecibidos = server.Receive(buffer);
                    string respuesta = Encoding.ASCII.GetString(buffer, 0, bytesRecibidos);
                    
                    if (respuesta != null)
                    {
                        StackConsultas.Push(respuesta); // Guardamos la respuesta en la lista de consultas

                        string respuestaUsada = StackConsultas.Pop(); // Sacamos la respuesta de la lista de consultas para usarla

                        string[] partes = respuestaUsada.Split('/');
                        if (partes.Length == 0 || string.IsNullOrWhiteSpace(partes[0]))
                        {
                            continue; // Ignoramos tramas vacías o corruptas
                        }

                        int codigo;
                        if (!int.TryParse(partes[0], out codigo))
                        {
                            continue; // Si el código no es un número válido, saltamos al siguiente ciclo
                        }
                        string mensaje;
                        //Creamos delegado
                        DelegadoParaEscribir delegado = new DelegadoParaEscribir(PonRespuesta);

                        switch (codigo)
                        {
                            case 1: //Respuesta de registro  --  "1/Usuario registrado correctamente, pruebe a iniciar sesiÃ³n"
                                mensaje = partes[1].Split('\0')[0];
                                string resultadoRegistro = "Respuesta de registro: " + mensaje;

                                //Invocamos al delegado para que lo escriba él
                                RespuestasServidorLabel.Invoke(delegado, new object[] { resultadoRegistro });
                                ClienteSeHaRegistrado = true;
                                break;
                            case 2: //Respuesta de inicio de sesión  --  "2/Correcto/Inicio de sesiÃ³n correcto"
                                mensaje = partes[1].Split('\0')[0];
                                string resultadoInicioSesion = "Respuesta de inicio de sesión: " + mensaje;

                                //Invocamos al delegado para que lo escriba él
                                RespuestasServidorLabel.Invoke(delegado, new object[] { resultadoInicioSesion });
                                break;
                            case 3: //Respuesta de consulta de pistola -- "3/SI/Pistola encontrada."
                                mensaje = partes[2].Split('\0')[0];
                                string resultadoPistola = "Tiene pistola? " + mensaje + " Consulta Pistola";
                                //MessageBox.Show(mensaje, "Consulta Pistola", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                //Invocamos al delegado para que lo escriba él
                                RespuestasServidorLabel.Invoke(delegado, new object[] { resultadoPistola });
                                break;

                            case 4: //Respuesta de consulta de número de jugadores  --  "4/%s"
                                mensaje = partes[1].Split('\0')[0];
                                string resultadoJugadoresPartida = "Número de jugadores: " + mensaje + " Consulta Jugadores";
                                //MessageBox.Show("Número de jugadores: " + mensaje, "Consulta Jugadores", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                //Invocamos al delegado para que lo escriba él
                                RespuestasServidorLabel.Invoke(delegado, new object[] { resultadoJugadoresPartida });
                                break;

                            case 5: //Respuesta de consulta de jugador con más tiempo  --  "5/%s-%s"
                                mensaje = partes[1].Split('\0')[0];
                                string resultadoJugadorLongevo = "Jugador con más tiempo: " + mensaje + " Consulta Superviviente";
                                //MessageBox.Show("Jugador con más tiempo: " + mensaje, "Consulta Superviviente", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                //Invocamos al delegado para que lo escriba él
                                RespuestasServidorLabel.Invoke(delegado, new object[] { resultadoJugadorLongevo });
                                break;
                            case 6: //Respuesta de la lista de conectados (debe actualizarse SIEMPRE)  --  "6/%s"

                                DelegadoParaConectados delegadoConectados = new DelegadoParaConectados(PonConectados);
                                ConectadosListBox.Invoke(delegadoConectados, new object[] { partes });
                                break;
                            case 7: //Respuesta de invitación a la partida  --  "7/codigoPartida/usuarioInvitado/resultado" (resultado es "Aceptado" o "Rechazado")
                                string JugadorInvitado = partes[2].Split('\0')[0];
                                string codigoPartida = partes[1].Split('\0')[0];

                                if (partes.Length == 4)
                                {
                                    if (partes[3] == "Aceptado")
                                    {
                                        //Delegado para actualizar la lista de partidas en la interfaz
                                        DelegadoParaPartidas delegadoPartidas = new DelegadoParaPartidas(PonPartidas);
                                        this.Invoke(delegadoPartidas, new object[] { codigoPartida, JugadorInvitado });
                                    }
                                    else if (partes[3] == "Rechazado")
                                    {
                                        Console.WriteLine("El jugador " + partes[2] + " ha rechazado la invitación.");
                                    }
                                }
                                else
                                { Console.WriteLine("Respuesta inesperada del servidor: " + respuesta); }
                                break;
                            case 8: // El servidor nos avisa que hemos entrado a una partida como invitados
                                    // Formato esperado del servidor: "8/codigoPartida/nombreAnfitrion/nombreInvitado"

                                //ESTE CASO HAY QUE REVISARLO
                                string codigoPartidaInv = partes[1].Split('\0')[0];
                                string anfitrion = partes[2].Split('\0')[0];
                                string miNombreInvitado = partes[3].Split('\0')[0];

                                // Usamos el delegado para actualizar la UI de forma segura desde el hilo de red
                                if (this.InvokeRequired)
                                {
                                    DelegadoParaPartidas delegadoPartidas = new DelegadoParaPartidas(PonPartidas);
                                    this.Invoke(delegadoPartidas, new object[] { codigoPartidaInv, miNombreInvitado });
                                }
                                else
                                {
                                    ConfigurarTablaPartidas(codigoPartidaInv, miNombreInvitado);
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al recibir datos del servidor: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }


        // BOTÓN INICIAR SESIÓN (Se conecta solo y envía el login)
        private void IniciarSesionButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text) || string.IsNullOrWhiteSpace(ConTextBox.Text))
            {
                MessageBox.Show("Por favor, introduce usuario y contraseña.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                
                // Si ya se ha registrado el cliente, no es necesario volver a conectarse al servidor C, ya que la conexión se mantiene abierta.
                if (!ClienteSeHaRegistrado)
                {
                    // Abrimos el socket con la IP del servidor (127.0.0.1 para pruebas locales)
                    IPEndPoint ipep = new IPEndPoint(ip, PUERTO);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server.Connect(ipep);
                    // Ver que no se haya creado ya el thread de atender al servidor, si no, lo creamos
                    if (atender == null || !atender.IsAlive)
                    {
                        try
                        {
                            //Poner en marcha el thread que atenderá los mensajes del servidor
                            ThreadStart ts = delegate { AtenderServidor(); };
                            atender = new Thread(ts);
                            atender.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("No se pudo crear el Thread: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                

                // Enviamos los datos en el formato del protocolo: 2/usuario-contraseña
                string mensaje = "2/" + NombreTextBox.Text + "-" + ConTextBox.Text;
                byte[] msg = Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo conectar con el servidor: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // BOTÓN REGISTRARSE (Se conecta solo y envía el registro)
        private void RegistraButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text) || string.IsNullOrWhiteSpace(ConTextBox.Text))
            {
                MessageBox.Show("Por favor, introduce usuario y contraseña.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IPEndPoint ipep = new IPEndPoint(ip, PUERTO);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Connect(ipep);

                // Enviamos formato de registro: 1/usuario-contraseña
                string mensaje = "1/" + NombreTextBox.Text + "-" + ConTextBox.Text;
                byte[] msg = Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo conectar con el servidor: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (atender == null || !atender.IsAlive)
            {
               try
               {
                   //Poner en marcha el thread que atenderá los mensajes del servidor
                   ThreadStart ts = delegate { AtenderServidor(); };
                   atender = new Thread(ts);
                   atender.Start();
               }
               catch (Exception ex)
               {
                   MessageBox.Show("No se pudo crear el Thread: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
            }
        }

        // DESCONECTAR AL CERRAR
        private void InterfazForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null && server.Connected)
            {
                byte[] msg = Encoding.ASCII.GetBytes("0/"+ NombreCliente); //Después del 0/ va el nombre del jugador que se desconecta
                server.Send(msg);

                //Cerramos tanto Servidor como Thread
                server.Close();
                atender.Abort();
            }
        }

        // MÉTODOS DE TABLERO Y JUEGO
        private void ConfigurarTablero()
        {
            TableroDataGridView.ReadOnly = true;
            TableroDataGridView.ColumnCount = 30;
            TableroDataGridView.RowCount = 34;

            TableroDataGridView.ScrollBars = ScrollBars.None;
            TableroDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            TableroDataGridView.RowHeadersVisible = false;
            TableroDataGridView.AllowUserToResizeColumns = false;
            TableroDataGridView.AllowUserToResizeRows = false;
            TableroDataGridView.ColumnHeadersVisible = false;

            int alturaDisponible = TableroDataGridView.ClientSize.Height;
            int numeroFilas = TableroDataGridView.RowCount;
            int alturaPorFila = alturaDisponible / numeroFilas;
            int sobrante = alturaDisponible % numeroFilas;

            TableroDataGridView.AllowUserToResizeRows = false;
            for (int i = 0; i < numeroFilas; i++)
            {
                TableroDataGridView.Rows[i].Height = alturaPorFila;
                if (i < sobrante)
                {
                    TableroDataGridView.Rows[i].Height++;
                }
            }
        }

        private void InivitarButton_Click(object sender, EventArgs e)
        {
            string JugadorInvitado = Convert.ToString(ConectadosListBox.SelectedItem);

            if (server == null || !server.Connected)
            {
                MessageBox.Show("Recuerda no ser pendejo e iniciar sesión antes de invitar a alguien.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            try
            {
                //Petición de invitación al servidor C, con el formato 7/usuario-invitado
                string peticion = "7/" + JugadorInvitado;
                server.Send(Encoding.ASCII.GetBytes(peticion));

                // La respuesta del servidor C se recibirá en el hilo de atenderServidor y se procesará allí, actualizando la lista de partidas si es necesario.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al invitar: " + ex.Message);
            }
        }

        private void ConfigurarTablaPartidas(string codigoPartida, string JugadorInvitado)
        {
            //int indicePartida = -1; //Inútil porque ya sabemos el código con la clase Partidas, pero lo dejo por si acaso
            bool encontrado = false;
            
            Partidas partidaActual = null;
            

            if (PartidasListBox.Items.Count == 0)
            {
                partidaActual = new Partidas(codigoPartida); //Establecemos el código de la partida
                partidaActual.AgregarJugador(NombreCliente, codigoPartida); //Añadimos al jugador que invita a la partida
                //Si no hay ninguna partida, añadimos la primera con el jugador que invita y el invitado
                if (!string.IsNullOrWhiteSpace(JugadorInvitado))
                {
                    partidaActual.AgregarJugador(JugadorInvitado, codigoPartida); //Añadimos al jugador invitado a la partida
                }
                listaPartidas.Add(partidaActual); // Añadimos la partida a la lista de partidas
            }
            else
            {
                //Si sí hay partidas, buscamos si ya existe la partida con el código dado
                int i = 0;
                while (!encontrado && listaPartidas.Count > i)
                {
                    if (listaPartidas[i].GetCodigoPartida() == codigoPartida)
                    {
                        encontrado = true;
                        partidaActual = listaPartidas[i]; // Obtenemos la partida existente y añadimos jugador
                        partidaActual.AgregarJugador(JugadorInvitado, codigoPartida);
                    }
                    else
                    { i++; }
                }
                if (!encontrado)
                {
                    partidaActual = new Partidas(codigoPartida);
                    partidaActual.AgregarJugador(NombreCliente, codigoPartida);
                    //Si no se encontró la partida, la añadimos a la lista de partidas
                    if (!string.IsNullOrWhiteSpace(JugadorInvitado))
                    {
                        partidaActual.AgregarJugador(JugadorInvitado, codigoPartida);
                    }
                    listaPartidas.Add(partidaActual);
                }
            }
            ActualizarListaPartidas(); // Actualizamos la lista de partidas en la interfaz
        }
        private void ActualizarListaPartidas()
        {
            PartidasListBox.Items.Clear();
            foreach (Partidas partida in listaPartidas)
            {
                // Creamos un string con el código de la partida y los jugadores que hay en ella
                string jugadores = string.Join(", ", partida.GetJugadores());
                string textoPartida = $"Código: {partida.GetCodigoPartida()} | Jugadores: {jugadores}";

                PartidasListBox.Items.Add(textoPartida);
            }
        }

        // Pone a los jugadores de dentro de la partida en posiciones al azar del tablero --ESTO DEBE HACERLO EL SERVIDOR
        // ESTO DEBE HACERLO EL SERVIDOR
        private void IniciarPartidaButton_Click(object sender, EventArgs e)
        {
            string codigoPartida = PartidaTextBox.Text.Trim(); // Aquí deberías obtener el código de la partida que quieres iniciar con un textbox

            Partidas partidaActual = EncontrarPartida(codigoPartida);   //Obtener partida que se juega
            if(partidaActual == null)
            {
                MessageBox.Show("No se encontró la partida con el código proporcionado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                int playerNumber = partidaActual.GetNumJugadores(codigoPartida);
                List<string> Jugadores = partidaActual.GetJugadores();
                //Console.WriteLine("Número de jugadores en el lobby: " + playerNumber);
                if ((partidaIniciada == false && playerNumber > 1) && Jugadores[0] == NombreCliente)
                {
                    int zombieX = randomNumber.Next(0, TableroDataGridView.ColumnCount);
                    int zombieY = randomNumber.Next(0, TableroDataGridView.RowCount);

                    TableroDataGridView.Rows[zombieY].Cells[zombieX].Value = "Z";

                    for (int i = 0; i < playerNumber; i++)
                    {
                        int playerX = randomNumber.Next(0, TableroDataGridView.ColumnCount);
                        int playerY = randomNumber.Next(0, TableroDataGridView.RowCount);
                        while (TableroDataGridView.Rows[playerY].Cells[playerX].Value != null)
                        {
                            playerX = randomNumber.Next(0, TableroDataGridView.ColumnCount);
                            playerY = randomNumber.Next(0, TableroDataGridView.RowCount);
                        }
                        List<int> posicionJugador = new List<int> { playerX, playerY };
                        PosicionJugadores.Add(posicionJugador);
                        TableroDataGridView.Rows[playerY].Cells[playerX].Value = Jugadores[i][0];   //Los spawneamos escribiendo la primera letra de su nombre en la casilla
                    }
                    partidaIniciada = true;
                }
                else
                {
                    MessageBox.Show("No se puede iniciar la partida. Asegúrese de que haya al menos 2 jugadores en el lobby.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Método para contar el número de jugadores en una partida específica y saber si es posible iniciarla o no
        private Partidas EncontrarPartida(string codigoPartida)
        {
            bool encontrado = false;
            int i = 0;
            while (!encontrado && listaPartidas.Count > i)
            {
                if (listaPartidas[i].GetCodigoPartida() == codigoPartida)
                {
                    encontrado = true;
                }
                else
                { i++; }
            }
            if (encontrado)
            {
                return listaPartidas[i];
            }
            return null;
        }

        //Para mover el jugador
        private void MoverJugadorConsulta(int i, int j)
        {
            int Jugadores = PosicionJugadores.Count;
            for (int k = 0; k < Jugadores; k++)
            {
                int AntiguaPosicionXJugador = PosicionJugadores[k][0];
                int AntiguaPosicionYJugador = PosicionJugadores[k][1];
                if (Math.Abs(AntiguaPosicionXJugador - j) + Math.Abs(AntiguaPosicionYJugador - i) != 1)
                {
                    MessageBox.Show("Solo puedes moverte a una casilla adyacente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //else if (ComprobarQueNoHaPulsadoDondeOtroJugador(i,j))
                //{
                //    MessageBox.Show("No puedes moverte a una casilla ocupada por otro jugador.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}
                else
                {
                    //Enviar al servidor la nueva posición del jugador y actualizar la posición en PosicionJugadores
                }
            }
        }

        private void MoverZombie()
        {
            // Lógica para mover el zombie
        }

        private void TableroDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int i = e.RowIndex;
            int j = e.ColumnIndex;

            if (playerTurn == 1)
            {
                MoverJugadorConsulta(i, j);
                playerTurn = 0;
            }
            else
            {
                MoverZombie();
                playerTurn = 1;
            }
        }

        private void RegistraButton_Click_1(object sender, EventArgs e)
        {
            RegistraButton_Click(sender, e);
        }

        //No hace nada
        private void label13_Click(object sender, EventArgs e)
        {
        }

        private void ConsultarButton_Click_1(object sender, EventArgs e)
        {
            string parametro = "";
            int codigoConsulta = 0;

            if (server == null || !server.Connected)
            {
                MessageBox.Show("Primero inicia sesión.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (PistolaRadioButton.Checked)
            {
                codigoConsulta = 3;
                parametro = JugadorTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(parametro))
                {
                    MessageBox.Show("Introduce el nombre del jugador.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (NumJugadoresRadioButton.Checked)
            {
                codigoConsulta = 4;
                parametro = PartidaTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(parametro))
                {
                    MessageBox.Show("Introduce el código de la partida.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (MasTiempoRadioButton.Checked)
            {
                codigoConsulta = 5;
                parametro = PartidaTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(parametro))
                {
                    MessageBox.Show("Introduce el código de la partida.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Selecciona una de las opciones de consulta.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IPEndPoint ipep = new IPEndPoint(ip, PUERTO);
                
                string mensaje = codigoConsulta + "/" + parametro;
                byte[] msg = Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message, "Error de red");
            }
        }
    }
    
    //Clase de partidas para llevar el control de las partidas y los jugadores que hay en ellas
    public class Partidas
    {
        //Atributos de la clase
        public string CodigoPartida;
        public List<string> Jugadores;
        public int numJugadores;

        //Constructor de la clase
        public Partidas(string codigoPartida)
        {
            CodigoPartida = codigoPartida;
            Jugadores = new List<string>();
            numJugadores = 0;
        }

        //Métodos de la clase
        public void AgregarJugador(string jugador, string codigo)
        {
            if ((jugador != null && !Jugadores.Contains(jugador)) && codigo == this.CodigoPartida)
            {
                this.Jugadores.Add(jugador);
                this.numJugadores++;  // Incrementamos el número de jugadores al agregar uno nuevo
            }
        }

        public void EliminarJugador(string jugador, string codigo)
        {
            if ((jugador != null && Jugadores.Contains(jugador)) && codigo == this.CodigoPartida)
            {
                this.Jugadores.Remove(jugador);
                this.numJugadores--;  // Decrementamos el número de jugadores al eliminar uno
            }
        }

        //Getters
        public List<string> GetJugadores()
        {
            return this.Jugadores;
        }

        public int GetNumJugadores(string code)
        {
            if (code == this.CodigoPartida)
            {
                return this.numJugadores;
            }
            else
            {
                return -1; // Si el código no coincide, devolvemos -1
            }
        }

        public string GetCodigoPartida()
        {
            return this.CodigoPartida;
        }
        //Setters
        public void SetCodigoPartida(string codigo)
        {
            this.CodigoPartida = codigo;
        }
    }
}