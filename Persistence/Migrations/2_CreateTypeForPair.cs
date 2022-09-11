using System.Data;

namespace MiniApp.Persistence.Migrations;

public class CreateTypeForPair : IMigration
{
    public const string MigrationName = nameof(CreateTypeForPair);

    public static readonly DateTime MigrationDate = new(2021, 9, 11, 1, 0, 0, DateTimeKind.Utc);

    public string Name => MigrationName;

    public DateTime Date => MigrationDate;
    
    public void Up(IDbCommand command)
    {
        command.CommandText = @"
        CREATE TYPE Pair AS TABLE(
            Code INT NOT NULL,
            Value VARCHAR(MAX) NOT NULL
        )
        ";
    }

    public void Down(IDbCommand command)
    {
        command.CommandText = @"
        DROP TYPE Pair
        ";
    }
}