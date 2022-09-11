using System.Data;

namespace MiniApp.Persistence.Migrations;

public class CreateStoredProcedureForInsertingPairs : IMigration
{
    public const string MigrationName = nameof(CreateStoredProcedureForInsertingPairs);

    public static readonly DateTime MigrationDate = new(2022, 9, 11, 2, 0, 0, DateTimeKind.Utc);

    public string Name => MigrationName;

    public DateTime Date => MigrationDate;
    
    public void Up(IDbCommand command)
    {
        command.CommandText = @"
        CREATE PROCEDURE usp_InsertPairs(@TablePairs Pair readonly)
        AS
        BEGIN
            INSERT INTO Pairs SELECT Code, Value FROM @TablePairs
        END
        ";
    }

    public void Down(IDbCommand command)
    {
        command.CommandText = @"
        DROP PROCEDURE usp_InsertPairs
        ";
    }
}