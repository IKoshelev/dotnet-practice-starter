// See https://aka.ms/new-console-template for more information
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var password = File.ReadAllText("/run/secrets/db-password");

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

        builder.DataSource = "db,1433"; 
        builder.UserID = "sa";            
        builder.Password = password;     
        builder.InitialCatalog = "master";
        builder.MultipleActiveResultSets = true;
        builder.Encrypt = true;
        builder.TrustServerCertificate = true;

        using IDbConnection db = new SqlConnection(builder.ConnectionString);

        var result = db.Query<int>("Select 1").ToList();

        Console.WriteLine(result);
    }
}