using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace InterfazZombieApocalipsis
{
    public partial class InterfazForm : Form
    {
        Socket server;
        const int PUERTO = 9050; // El puerto del servidor C
        IPAddress ip = IPAddress.Parse("127.0.0.1");    //Si no va, poned la que asigna la VirtualBox por defecto (192.168.56.101 o mirad con ip a)

        int playerTurn = 1;
        bool partidaIniciada = false;
        Random randomNumber = new Random();
        List<List<int>> PosicionJugadores = new List<List<int>>();

        public InterfazForm()
        {
            InitializeComponent();
        }

        private void InterfazForm_Load(object sender, EventArgs e)
        {
            // Lobby
            LobbyDataGridView.ColumnCount = 1;
            LobbyDataGridView.RowCount = 5;
            LobbyDataGridView.ScrollBars = ScrollBars.None;
            LobbyDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            LobbyDataGridView.RowHeadersVisible = false;
            LobbyDataGridView.AllowUserToResizeColumns = false;
            LobbyDataGridView.AllowUserToResizeRows = false;
            LobbyDataGridView.ColumnHeadersVisible = false;

            // Tablero
            ConfigurarTablero();
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
                // 1. Abrimos el socket con la IP del servidor (127.0.0.1 para pruebas locales)
                IPEndPoint ipep = new IPEndPoint(ip, PUERTO);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Connect(ipep);

                // 2. Enviamos los datos en el formato del protocolo: 2/usuario-contraseña
                string mensaje = "2/" + NombreTextBox.Text + "-" + ConTextBox.Text;
                byte[] msg = Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);

                // 3. Recibimos la respuesta del servidor C
                byte[] buffer = new byte[512];
                int bytesRecibidos = server.Receive(buffer);
                string respuesta = Encoding.ASCII.GetString(buffer, 0, bytesRecibidos);

                // 4. Si la respuesta contiene "Correcto", entramos al lobby
                if (respuesta.Contains("Correcto"))
                {
                    MessageBox.Show("Inicio de sesión exitoso.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ConfigurarLobby();
                }
                else
                {
                    MessageBox.Show("Respuesta del servidor: " + respuesta, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

                byte[] buffer = new byte[512];
                int bytesRecibidos = server.Receive(buffer);
                string respuesta = Encoding.ASCII.GetString(buffer, 0, bytesRecibidos);

                MessageBox.Show("Respuesta del servidor: " + respuesta, "Registro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo conectar con el servidor: " + ex.Message, "Error de red", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // DESCONECTAR AL CERRAR
        private void InterfazForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null && server.Connected)
            {
                byte[] msg = Encoding.ASCII.GetBytes("0/");
                server.Send(msg);
                server.Close();
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

        private int ConfigurarLobby()
        {
            int numJugadores = 0;
            for (int i = 0; i < LobbyDataGridView.RowCount; i++)
            {
                DataGridViewCell celda = LobbyDataGridView.Rows[i].Cells[0];
                if (celda.Value != null && !string.IsNullOrWhiteSpace(celda.Value.ToString()))
                    numJugadores++;

                if (celda.Value == null)
                {
                    celda.Value = NombreTextBox.Text;
                    break;
                }
            }
            return numJugadores;
        }

        private void IniciarPartidaButton_Click(object sender, EventArgs e)
        {
            int playerNumber = ConfigurarLobby();
            Console.WriteLine("Número de jugadores en el lobby: " + playerNumber);
            if (partidaIniciada == false && playerNumber > 1)
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
                    TableroDataGridView.Rows[playerY].Cells[playerX].Value = NombreTextBox.Text[0];
                }
                partidaIniciada = true;
            }
            else
            {
                MessageBox.Show("No se puede iniciar la partida. Asegúrese de que haya al menos 2 jugadores en el lobby.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MoverJugador(int i, int j)
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
                else if (PosicionJugadores[k][0] == j && PosicionJugadores[k][1] == i)
                {
                    MessageBox.Show("No puedes moverte a una casilla ocupada por otro jugador.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
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
                MoverJugador(i, j);
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
    }
}