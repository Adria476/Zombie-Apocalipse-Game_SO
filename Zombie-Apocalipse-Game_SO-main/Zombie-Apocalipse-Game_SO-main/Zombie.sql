-- Eliminamos database y la creamos para que no se raye
DROP DATABASE IF EXISTS Zombie;
CREATE DATABASE Zombie;
USE Zombie;

-- Creamos las tablas
CREATE TABLE PlayerTable (
	ID INTEGER AUTO_INCREMENT PRIMARY KEY NOT NULL,
	Name TEXT NOT NULL,
	Contra TEXT NOT NULL,

	GlobalPoints INTEGER NOT NULL
) ENGINE = InnoDB;
-- El ID se irá a la tabla de relaciones igual que el code de la partida, se pueden añadir nuevos campos en el -- futuro según se necesite

CREATE TABLE PartidaTable (
	CODE VARCHAR(10) PRIMARY KEY NOT NULL,
	Fecha TEXT NOT NULL,
	Duration INTEGER NOT NULL,
	
	PistolaFound BOOLEAN NOT NULL
) ENGINE = InnoDB;
-- El campo PistolaFound indica si la pistola se ha encontrado en esa partida o no

-- Relación
CREATE TABLE Relation (
	PlayerID INTEGER NOT NULL,
	PartidaCode VARCHAR(10) NOT NULL,
	PointsPartida INTEGER NOT NULL,
	TiempoSobrevivido INTEGER NOT NULL,
	ZombieKilled BOOLEAN NOT NULL,
	FOREIGN KEY (PlayerID) REFERENCES PlayerTable(ID),
	FOREIGN KEY (PartidaCode) REFERENCES PartidaTable(Code)
) ENGINE = InnoDB;
-- El campo ZombieKilled indica si el jugador que ha jugado esa partida ha matado el zombie, si lo ha matado -- -- significa que tiene la pistola porque solo lo puedes matar con la pistola

-- INSERTAMOS UNOS DATOS PARA PROBAR
INSERT INTO PlayerTable VALUES(1, 'Pepitopalotes', 'No', 10000);
INSERT INTO PlayerTable VALUES(2, 'LoDesconozco', 'Banana', 20);
INSERT INTO PlayerTable VALUES(3, 'FanUPC', 'UniversitatPolitecnicaCatalunya', 500);
INSERT INTO PartidaTable VALUES('LSZH', 'Ayer', 300, 1);
INSERT INTO PartidaTable VALUES('LEBL', 'Hoy', 347, 0);
INSERT INTO PartidaTable VALUES('LEMD', 'Mañana', 107, 1);

-- La table relación
INSERT INTO Relation VALUES(1, 'LSZH', 2, 300, 1);
INSERT INTO Relation VALUES(2, 'LSZH', 2, 120, 0);
INSERT INTO Relation VALUES(3, 'LSZH', 2, 35, 0); 
INSERT INTO Relation VALUES(1, 'LEBL', 2, 190, 0); 
INSERT INTO Relation VALUES(2, 'LEBL', 2, 347, 0); 
INSERT INTO Relation VALUES(3, 'LEBL', 2, 200, 0); 
INSERT INTO Relation VALUES(1, 'LEMD', 2, 81, 0); 
INSERT INTO Relation VALUES(2, 'LEMD', 2, 88, 1); 
INSERT INTO Relation VALUES(3, 'LEMD', 2, 107, 0); 




-- A hacer el file .c
