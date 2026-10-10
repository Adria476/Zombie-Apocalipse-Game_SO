using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InterfazZombieApocalipsis
{
    public partial class InterfazForm : Form
    {
        string nombrePrueba = "Pepitopalotes";
        string conPrueba = "123";
        int playerTurn = 1; // 1 = jugador, 0 = zombie
        bool partidaIniciada = false;
        Random randomNumber = new Random();
        List<List<int>> PosicionJugadores = new List<List<int>>();


        public InterfazForm()
        {
            InitializeComponent();
        }

        private void InterfazForm_Load(object sender, EventArgs e)
        {
            //Lobby
            LobbyDataGridView.ColumnCount = 1;
            LobbyDataGridView.RowCount = 5;
            LobbyDataGridView.ScrollBars = ScrollBars.None;
            LobbyDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            LobbyDataGridView.RowHeadersVisible = false;
            LobbyDataGridView.AllowUserToResizeColumns = false;
            LobbyDataGridView.AllowUserToResizeRows = false;
            LobbyDataGridView.ColumnHeadersVisible = false;
            //Tablero
            ConfigurarTablero();
        }

        private void ConfigurarTablero()
        {
            TableroDataGridView.ReadOnly = true;
            //Tamaño tablero
            TableroDataGridView.ColumnCount = 30;
            TableroDataGridView.RowCount = 34;

            //Ajustar filas y columnas para que llenen todo el espacio disponible
            TableroDataGridView.ScrollBars = ScrollBars.None;
            TableroDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            TableroDataGridView.RowHeadersVisible = false;
            TableroDataGridView.AllowUserToResizeColumns = false;
            TableroDataGridView.AllowUserToResizeRows = false;
            TableroDataGridView.ColumnHeadersVisible = false;

            //Ajustar altura de las rows
            int alturaDisponible = TableroDataGridView.ClientSize.Height;
            int numeroFilas = TableroDataGridView.RowCount;
            int alturaPorFila = alturaDisponible / numeroFilas;
            int sobrante = alturaDisponible % numeroFilas;

            TableroDataGridView.AllowUserToResizeRows = false;
            for (int i = 0; i < numeroFilas; i++)
            {
                TableroDataGridView.Rows[i].Height = alturaPorFila;

                // Repartimos los píxeles que sobran
                if (i < sobrante)
                {
                    TableroDataGridView.Rows[i].Height++;
                }
            }
        }

        private int ConfigurarLobby()
        {
            int numJugadores = 0;
            //Esto se tendrá que cambiar cuando se pueda invitar a amigos, pero por ahora solo se puede iniciar sesión y aparecer en el lobby
            for (int i = 0; i < LobbyDataGridView.RowCount; i++)
            {
                DataGridViewCell celda = LobbyDataGridView.Rows[i].Cells[0];
                if (celda.Value != null && !string.IsNullOrWhiteSpace(celda.Value.ToString()))
                    numJugadores++;
                /// Descomentar esto cuando se pueda invitar a amigos, por ahora solo se puede iniciar sesión y aparecer en el lobby
                //if (celda.Value != null && celda.Value.ToString() == NombreTextBox.Text)
                //{
                //    MessageBox.Show("El nombre de usuario ya está en el lobby.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    NombreTextBox.Clear();
                //    return;
                //}
                if (celda.Value == null)
                {
                    celda.Value = NombreTextBox.Text;
                    break;
                }
            }

            

            return numJugadores;
        }

        private bool IniciarSesion()
        {
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                MessageBox.Show("Por favor, ingrese un nombre de usuario válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else if (string.IsNullOrWhiteSpace(ConTextBox.Text))
            {
                MessageBox.Show("Por favor, ingrese un nombre de usuario válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else if (NombreTextBox.Text == nombrePrueba && ConTextBox.Text == conPrueba)
                return true;
            else
                return false;
        }
        
        private void IniciarSesionButton_Click(object sender, EventArgs e)
        {
            if (IniciarSesion())
            {
                MessageBox.Show("Inicio de sesión exitoso.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ConfigurarLobby();
            }
            else
            {
                MessageBox.Show("Nombre de usuario o contraseña incorrectos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                
                for(int i = 0; i < playerNumber; i++)
                {
                    int playerX = randomNumber.Next(0, TableroDataGridView.ColumnCount);
                    int playerY = randomNumber.Next(0, TableroDataGridView.RowCount);
                    // Asegurarse de que el jugador no se coloque en la misma posición que el zombie u otro jugador
                    // Asegurarse en el futuro también de dejar margen entre zombie y jugador
                    while (TableroDataGridView.Rows[playerY].Cells[playerX].Value != null)
                    {
                        playerX = randomNumber.Next(0, TableroDataGridView.ColumnCount);
                        playerY = randomNumber.Next(0, TableroDataGridView.RowCount);
                    }
                    List<int> posicionJugador = new List<int>{playerX, playerY};
                    PosicionJugadores.Add(posicionJugador);
                    TableroDataGridView.Rows[playerY].Cells[playerX].Value = NombreTextBox.Text[0];
                }
                partidaIniciada = true;
            }
            else
            {
                MessageBox.Show("No se puede iniciar la partida. Asegúrese de que haya al menos 2 jugadores en el lobby y que otra partida no haya comenzado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void MoverJugador(int i, int j)
        {
            int Jugadores = PosicionJugadores.Count;
            for(int k = 0; k < Jugadores; k++)
            {
                int AntiguaPosicionXJugador = PosicionJugadores[k][0];
                int AntiguaPosicionYJugador = PosicionJugadores[k][1];
                //Jugadores solo se mueven a casillas adyacentes y no pueden moverse a casillas ocupadas por otros jugadores
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
            // Lógica para mover el zombie de manera aleatoria a una casilla adyacente
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
    }
}
