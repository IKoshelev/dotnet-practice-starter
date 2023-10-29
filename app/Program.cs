// See https://aka.ms/new-console-template for more information
using Microsoft.Data.SqlClient;
using Dapper;
using System.Net;
using System.Text;
using static ConsoleHelper;


internal partial class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Starting console app.");

        await Task.WhenAll(
            CheckMsSqlDbConnection()
        );

        Console.WriteLine("Checks section finished.");

        using var listener = new HttpListener();
        listener.Prefixes.Add("http://+:8080/");

        listener.Start();

        Console.WriteLine("Listening on port 8080...");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest req = context.Request;

            Console.WriteLine($"Received request for {req.Url}");

            using HttpListenerResponse resp = context.Response;
            resp.Headers.Set("Content-Type", "text/plain");

            string data = "Hello there!";
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            resp.ContentLength64 = buffer.Length;

            using Stream ros = resp.OutputStream;
            ros.Write(buffer, 0, buffer.Length);
        }
    }

    private static async Task CheckMsSqlDbConnection()
    {
        await Check("ms-sql-db", async () => 
        {
            var password = File.ReadAllText("/run/secrets/ms-sql-db-password");

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = "ms-sql-db,1433";
            builder.UserID = "sa";
            builder.Password = password;
            builder.InitialCatalog = "master";
            builder.MultipleActiveResultSets = true;
            builder.Encrypt = true;
            builder.TrustServerCertificate = true;

            using var db = new SqlConnection(builder.ConnectionString);
            var result = (await db.QueryAsync<int>("SELECT 789")).ToArray();

            return result[0] == 789;
        });
    }
}