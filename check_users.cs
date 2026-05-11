using Npgsql;

var connStr = "Host=localhost;Port=5432;Database=Musically;Username=postgres;Password=Hundiba123";
using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
using var cmd = new NpgsqlCommand("SELECT \"Id\", \"Username\", \"Role\" FROM \"Users\" ORDER BY \"Id\"", conn);
using var reader = await cmd.ExecuteReaderAsync();
Console.WriteLine("Id | Username | Role");
Console.WriteLine("---|----------|-----");
while (await reader.ReadAsync())
{
    Console.WriteLine($"{reader[0]} | {reader[1]} | {reader[2]}");
}
