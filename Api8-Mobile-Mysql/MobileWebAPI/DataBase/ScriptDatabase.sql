Create Database myfirstapi;
Use myfirstapi;

/********************************************************
    Criação da tabela de pessoas
/********************************************************/


CREATE TABLE pessoas (
  IdPessoa int NOT NULL AUTO_INCREMENT,
  RazaoSocial varchar(80) NOT NULL,
  CnpjCpf varchar(18) NOT NULL,
  Email varchar(120) DEFAULT NULL,
  Telefone varchar(15) DEFAULT NULL,
  Usuario varchar(100) DEFAULT NULL,
  Senha varchar(12) DEFAULT NULL,
  PRIMARY KEY (IdPessoa)
);

/********************************************************