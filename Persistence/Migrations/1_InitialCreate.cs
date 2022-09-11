using System.Data;

namespace MiniApp.Persistence.Migrations;

public class InitialCreate : IMigration
{
    public const string MigrationName = nameof(InitialCreate);

    public static readonly DateTime MigrationDate = new(2020, 9, 11, 0, 0, 0, DateTimeKind.Utc);

    public string Name => MigrationName;

    public DateTime Date => MigrationDate;

    public void Up(IDbCommand command)
    {
        command.CommandText = @"
        CREATE TABLE RequestLogs (
            Id BIGINT PRIMARY KEY IDENTITY(1, 1),
            QueryString VARCHAR(MAX) NULL,
            Body VARCHAR(MAX) NULL,
            Method VARCHAR(16) NOT NULL,
            Path VARCHAR(128) NOT NULL,
            Response VARCHAR(MAX) NULL,
            Exception VARCHAR(MAX) NULL,
            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        )

        CREATE TABLE Pairs (
            Id BIGINT PRIMARY KEY IDENTITY(1, 1),
            Code INT NOT NULL,
            Value VARCHAR(MAX) NOT NULL
        )
        ";
    }

    public void Down(IDbCommand command)
    {
        command.CommandText = @"
        DROP TABLE RequestLogs
        
        DROP TABLE Pairs
        ";
    }
}