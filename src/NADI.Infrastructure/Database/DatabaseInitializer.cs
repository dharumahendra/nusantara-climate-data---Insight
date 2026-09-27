using Microsoft.Data.Sqlite;

namespace NADI.Infrastructure.Database;

public static class DatabaseInitializer
{
    private static readonly string ScriptDirectory = Path.Combine(AppContext.BaseDirectory, "Database");

    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        foreach (var script in new[] { "schema.sql", "seed.sql" })
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = File.ReadAllText(Path.Combine(ScriptDirectory, script));
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}
