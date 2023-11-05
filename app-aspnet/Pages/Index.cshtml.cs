using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebClient.Pages;

public class IndexModel(
        ILogger<IndexModel> logger,
        SqlConnection dbConnection,
        IMongoDatabase mongoDatabase) : PageModel
{

    //public bool dbConnectionIsOk = false;

    public async Task OnGetAsync()
    {
        //dbConnectionIsOk = await TestDbConnection();
    }


    public async Task<bool> TestMsSqlDbConnection()
    {
        var result = (await dbConnection.QueryAsync<int>("SELECT 789")).ToArray();

        return result[0] == 789;
    }

    public async Task<bool> TestMongoDocumentDbConnection()
    {
        var pingResult = await mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

        return pingResult["ok"] == 1;
    }
}
