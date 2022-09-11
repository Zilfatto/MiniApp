using System.Data;

namespace MiniApp.Persistence.Migrations;

public interface IMigration
{
    const string DatabaseTableName = "__Migrations";
    
    string Name { get; }
    
    DateTime Date { get; }
    
    void Up(IDbCommand command);

    void Down(IDbCommand command);
}