#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <stdio.h>
#include <pthread.h>

#include <mysql.h>	//MySQL

typedef struct {
	int sock_conn;
	MYSQL *conn;
} Datos;

Datos datos[100];	//Vector para los datos de la conexión de 100 clientes

//Estructura necesaria para acceso excluyente
pthread_mutex_t mutex = PTHREAD_MUTEX_INITIALIZER;

int i;
int sockets[100];

void *AtenderCliente (void *arguments)
{
	Datos *d;
	d = (Datos *) arguments;
	int sock_conn = d->sock_conn;
	MYSQL *conn = d->conn;
	
	/*int sock_conn;
	int *s;
	s= (int *) socket;
	sock_conn= *s;*/
	
	//int socket_conn = * (int *) socket;
	
	char peticion[512];
	char respuesta[512];
	int ret;
	
	
	int terminar =0;
	while (terminar == 0)
	{
	    ret = read(sock_conn, peticion, sizeof(peticion));

	    if (ret <= 0)
	    {
	        terminar = 1;
	        continue;
	    }
	
	    printf ("Recibido\n");

	    peticion[ret] = '\0';
	    printf ("Peticion: %s\n", peticion);

	    char *p = strtok(peticion, "/");

	    if (p == NULL)
	        continue;
	
	    int codigo = atoi(p);
	    
	    char data[100];
	    
	    if (codigo != 0)
	    {
	        p = strtok(NULL, "/");
	        if (p != NULL)
	            strcpy (data, p);
	        
	        printf ("Codigo: %d, Quiere: %s\n", codigo, data);
	    }
		
		if (codigo ==0) //peticion de desconexion
		{ terminar=1; }
		else if (codigo == 1) //Registro de usuario (vendran como usuario-contraseña en data)
		{
			char usuario[30];
			char *pAyuda = strtok(data, "-");
			strcpy(usuario, pAyuda);
			char con[30];
			pAyuda = strtok(NULL, "-");
			strcpy(con, pAyuda);
			
			char consulta[200];
			sprintf(consulta, "SELECT PlayerTable.Name FROM PlayerTable WHERE PlayerTable.Name = '%s'", usuario);		//Antes de añadir comprobamos que el nombre sea distinto y no haya duplicados
			int err = mysql_query (conn, consulta); //Comprobamos que no haya duplicados
			
			if (err != 0)
			{
				sprintf(respuesta, "1/Error al consultar: %u %s\n", mysql_errno(conn), mysql_error(conn));
			}
			else
			{
				MYSQL_RES *resultado;
				int rows;
				
				resultado = mysql_store_result (conn);	//Nos devuelve una tabla con todas las coincidencias
				
				rows = mysql_num_rows(resultado); //Comprobamos el número de filas que ha seleccionado (deberían ser cero)
				
				if (rows > 0)	//Sigifica que alguien tiene el mismo nombre
				{
					sprintf(respuesta, "1/Error, nombre de usuario ya en uso");
				}
				else
				{
					char orden[200];
					sprintf(orden, "INSERT INTO PlayerTable (Name, Contra, GlobalPoints) VALUES ('%s','%s',0)", usuario,con);	//El ID se ha automatizado en la base de datos, sube solo por cada uno que añadas
					
					int err = mysql_query (conn, orden);	//Hacemos la orden de añadir el cliente
					if (err != 0)
					{
						sprintf(respuesta, "1/Error al consultar: %u %s\n", mysql_errno(conn), mysql_error(conn));
					}
					else
					{ sprintf(respuesta,"1/Usuario registrado correctamente, pruebe a iniciar sesión"); }
				}
				mysql_free_result(resultado);	//Liberamos el resultado
			}
		}
		else if (codigo == 2)	//Inicio de sesión
		{
			char usuario[30];					//Nos quedamos con el usuario y la contraseña
			char *pAyuda = strtok(data, "-");
			strcpy(usuario, pAyuda);
			char con[30];
			pAyuda = strtok(NULL, "-");
			strcpy(con, pAyuda);
			
			char consulta[300];
			strcpy(consulta, "SELECT PlayerTable.Name, PlayerTable.Contra FROM PlayerTable WHERE PlayerTable.Name = '");
			strcat(consulta,usuario);
			strcat(consulta, "' AND PlayerTable.Contra = '");
			strcat(consulta,con);
			strcat(consulta,"'");
			
			int err = mysql_query (conn, consulta);
			if (err != 0)
			{ sprintf(respuesta, "2/Usuario no encontrado: %u %s\n", mysql_errno(conn), mysql_error(conn)); }
			else
			{ 
				MYSQL_RES *resultado;
				int row;
				
				resultado = mysql_store_result (conn);	//Nos devuelve una tabla con todas las coincidencias
				
				row = mysql_num_rows(resultado); //Comprobamos el número de filas que ha seleccionado (debería ser una)
				if (row == 0)	//Si devuelve 0 filas, hubo algún error
				{ sprintf(respuesta, "2/NO/Usuario o contraseña incorrectos"); }
				else 	//Una fila
				{ sprintf(respuesta, "2/Correcto/Inicio de sesión correcto"); }
				
				mysql_free_result(resultado);	//Liberamos el resultado
			}
		}
		else if (codigo == 3) // consulta adria: el jugador encontro pistola?
		{
		    char usuario[30];
		    strcpy(usuario, data);

		    char consulta[400];
		    sprintf(consulta, 
		        "SELECT PartidaTable.Code "
		        "FROM PlayerTable, PartidaTable, Relation "
		        "WHERE PlayerTable.Name = '%s' "
		        "AND PlayerTable.ID = Relation.PlayerID "
		        "AND Relation.PartidaCode = PartidaTable.Code "
		        "AND Relation.ZombieKilled = 1 "
		        "AND PartidaTable.PistolaFound = 1", usuario);

		    int err = mysql_query(conn, consulta);
		    if (err == 0)
		    {
		        MYSQL_RES *resultado = mysql_store_result(conn);

		        if (mysql_num_rows(resultado) == 0)
		            sprintf(respuesta, "3/NO/El jugador %s no ha encontrado o usado pistola.", usuario);
		        else
		            sprintf(respuesta, "3/SI/Pistola encontrada.");
		
		        mysql_free_result(resultado);
		    }
		}
		else if (codigo == 4) // consulta tomas: cuantos jugadores hay en la partida?
		{
		    char codigoPartida[10];
		    strcpy(codigoPartida, data);
		
		    char consulta[300];
		    sprintf(consulta, "SELECT COUNT(*) FROM Relation WHERE PartidaCode = '%s'", codigoPartida);
		
		    int err = mysql_query(conn, consulta);
		    if (err != 0)
		    {
		        sprintf(respuesta, "4/Error al consultar: %u %s\n", mysql_errno(conn), mysql_error(conn));
		    }
		    else
		    {
		        MYSQL_RES *resultado = mysql_store_result(conn);
		        MYSQL_ROW row = mysql_fetch_row(resultado);
		
		        if (row != NULL)
		        {
		            sprintf(respuesta, "4/%s", row[0]); 
		        }
		        else
		        {
		            sprintf(respuesta, "4/0");
		        }
		        mysql_free_result(resultado);
		    }
		}
		else if (codigo == 5) // consulta maroua: que jugador vivio mas tiempo en la partida X?
		{
		    char codigoPartida[10];
		    strcpy(codigoPartida, data);
		
		    char consulta[400];
		    sprintf(consulta, 
		        "SELECT PlayerTable.Name, Relation.TiempoSobrevivido "
		        "FROM PlayerTable, Relation "
		        "WHERE PlayerTable.ID = Relation.PlayerID "
		        "AND Relation.PartidaCode = '%s' "
		        "ORDER BY Relation.TiempoSobrevivido DESC LIMIT 1;", 
		        codigoPartida);
		
		    int err = mysql_query(conn, consulta);
		    if (err != 0)
		    {
		        sprintf(respuesta, "5/Error al consultar: %u %s\n", mysql_errno(conn), mysql_error(conn));
		    }
		    else
		    {
		        MYSQL_RES *resultado = mysql_store_result(conn);
		        MYSQL_ROW row = mysql_fetch_row(resultado);
		
		        if (row == NULL)
		        {
		            sprintf(respuesta, "5/No existe la partida %s o no tiene jugadores registrados", codigoPartida);
		        }
		        else
		        {
		            sprintf(respuesta, "5/%s-%s", row[0], row[1]);
		        }
		        mysql_free_result(resultado);
		    }
		}
		//Escribimos la respuesta para el socket
		if (codigo !=0)
		{
			printf ("Respuesta: %s\n", respuesta);	//Comprobar que todo chill
			// Enviamos respuesta
			write (sock_conn,respuesta,strlen(respuesta));
		}			
	}
	// Se acabo el servicio para este cliente
	close(sock_conn); 
	
}


int main(int argc, char *argv[])
{

	
	/*MySQL*/
	
	MYSQL *conn;
	
	//Crear conexión al servidor MySQL
	conn = mysql_init(NULL);
	if (conn == NULL)
	{
		printf("Error al crear conexión: %u %s\n", mysql_errno(conn), mysql_error(conn));
		exit(1);
	}
	
	//Conectampos con lo creado									Tabla
	conn = mysql_real_connect(conn, "localhost","root","mysql","Zombie",0,NULL,0);
	if (conn == NULL)	//Protección
	{
		printf("Error al iniciar la conexión: %u %s\n", mysql_errno(conn), mysql_error(conn));
		exit(1);
	}
	
	/*Conexion servidor*/
	
	int sock_conn, sock_listen;
	struct sockaddr_in serv_adr;
	
	// INICIALIZACIONES
	// Abrimos el socket
	if ((sock_listen = socket(AF_INET, SOCK_STREAM, 0)) < 0)
		printf("Error creant socket");
	// Hacemos el bind al port
	
	
	memset(&serv_adr, 0, sizeof(serv_adr));// inicialitza a zero serv_addr
	serv_adr.sin_family = AF_INET;
	
	// asocia el socket a cualquiera de las IP de la maquina. 
	//htonl formatea el numero que recibe al formato necesario
	serv_adr.sin_addr.s_addr = htonl(INADDR_ANY);
	// establecemos el puerto de escucha
	serv_adr.sin_port = htons(9050);
	if (bind(sock_listen, (struct sockaddr *) &serv_adr, sizeof(serv_adr)) < 0)
		printf ("Error al bind\n");
	
	if (listen(sock_listen, 3) < 0)
		printf("Error en el Listen\n");
		
	pthread_t thread;
	i=0;
	for (;;){
		printf ("Escuchando\n");
		
		sock_conn = accept(sock_listen, NULL, NULL);
		printf ("He recibido conexion\n");
		
		datos[i].sock_conn = sock_conn;
		datos[i].conn = conn;
		//sock_conn es el socket que usaremos para este cliente y conn es la conexión a la BBDD
		
		// Crear thead y decirle lo que tiene que hacer
		pthread_create (&thread, NULL, AtenderCliente,&datos[i]);
		i=i+1;
	}
	
	mysql_close(conn);		//Cerrar conexin
	
}
