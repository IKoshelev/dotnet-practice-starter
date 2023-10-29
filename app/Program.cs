// See https://aka.ms/new-console-template for more information
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Net;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var password = File.ReadAllText("/run/secrets/ms-sql-db-password");

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

        builder.DataSource = "db,1433"; 
        builder.UserID = "sa";            
        builder.Password = password;     
        builder.InitialCatalog = "master";
        builder.MultipleActiveResultSets = true;
        builder.Encrypt = true;
        builder.TrustServerCertificate = true;

        using IDbConnection db = new SqlConnection(builder.ConnectionString);

        var result = db.Query<int>("SELECT 1").ToList();

        Console.WriteLine(result[0]);

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
}