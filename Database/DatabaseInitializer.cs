using Microsoft.Data.SqlClient;
using System.IO; // Добавьте эту строку

namespace ElectronicComponentStore.Database;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Создание базы данных, если она не существует
        var createDbCommand = new SqlCommand(
            @"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'ElectronicComponentsDB')
            BEGIN
                CREATE DATABASE ElectronicComponentsDB;
            END", connection);
        await createDbCommand.ExecuteNonQueryAsync();

        // Использование созданной базы данных
        var useDbCommand = new SqlCommand("USE ElectronicComponentsDB", connection);
        await useDbCommand.ExecuteNonQueryAsync();

        // Создание таблицы Components
        var createTableCommand = new SqlCommand(
            @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Components' and xtype='U')
            BEGIN
                CREATE TABLE Components (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Type NVARCHAR(50) NOT NULL,
                    CellNumber NVARCHAR(20),
                    Quantity INT NOT NULL DEFAULT 0,
                    DateOfChanges DATETIME2 NOT NULL,
                    Datasheet NVARCHAR(255)
                )
            END", connection);
        await createTableCommand.ExecuteNonQueryAsync();

        // Добавление тестовых данных
        var insertDataCommand = new SqlCommand(
            @"IF NOT EXISTS (SELECT 1 FROM Components)
            BEGIN
                INSERT INTO Components (Name, Type, CellNumber, Quantity, DateOfChanges, Datasheet) VALUES
                ('ATmega328P', 'Microcontrollers', 'A1', 50, GETDATE(), 'atmega328p.pdf'),
                ('ESP32', 'Microcontrollers', 'A2', 30, GETDATE(), 'esp32.pdf'),
                ('2N2222', 'Transistors', 'B1', 100, GETDATE(), '2n2222.pdf'),
                ('IRF3205', 'Mosfets', 'B2', 75, GETDATE(), 'irf3205.pdf'),
                ('1N4148', 'Diodes', 'C1', 200, GETDATE(), '1n4148.pdf'),
                ('1N4733A', 'Zener diodes', 'C2', 150, GETDATE(), '1n4733a.pdf'),
                ('100nF', 'Capacitors', 'D1', 300, GETDATE(), '100nf.pdf'),
                ('100uF', 'Electrolytic capacitors', 'D2', 120, GETDATE(), '100uf.pdf')
            END", connection);
        await insertDataCommand.ExecuteNonQueryAsync();
    }
}