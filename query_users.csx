using Npgsql;

var connStr = "Host=dpg-d71t3g0gjchc739p22n0-a.oregon-postgres.render.com;Port=5432;Database=musically;Username=musically_user;Password=qbNRQSh3yQsWEFFdD89hZxqrX8m6mHeR;SSL Mode=Require;Trust Server Certificate=true";

using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();

Console.WriteLine("=== USERS ===");
using var cmd = new NpgsqlCommand("SELECT \"Id\", \"Username\", \"Email\", \"Role\" FROM \"Users\" ORDER BY \"Id\"", conn);
using var reader = await cmd.ExecuteReaderAsync();
Console.WriteLine($"{"Id",-5} {"Username",-15} {"Email",-30} {"Role",-10}");
Console.WriteLine(new string('-', 60));
while (await reader.ReadAsync())
{
    Console.WriteLine($"{reader.GetInt32(0),-5} {reader.GetString(1),-15} {reader.GetString(2),-30} {reader.GetString(3),-10}");
}
