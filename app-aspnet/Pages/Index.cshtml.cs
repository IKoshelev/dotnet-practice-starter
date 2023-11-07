using System.Data;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Practice.Aspnet;

public class IndexModel(
        ILogger<IndexModel> logger,
        IDbConnection dbConnection,
        IMongoDatabase mongoDatabase,
        BlobServiceClient blobServiceClient,
        QueueServiceClient queueServiceClient,
        TableServiceClient tableServiceClient) : PageModel
{
    public async Task OnGetAsync()
    {
        logger.LogInformation("Rendering Index page.");
    }

    public async Task<(string, bool)[]> CheckAllConnections() => new[]
    {
        ("ms-sql-db", await TestMsSqlDbConnection()),
        ("mongo-document-db", await TestMongoDocumentDbConnection()),
        ("azurite-storage-blobs", await TestAzuriteStorageBlobClient()),
        ("azurite-storage-queues", await TestAzuriteStorageQueueClient()),
        ("azurite-storage-tables", await TestAzuriteStorageTableClient())
    };

    public async Task<bool> TestMsSqlDbConnection() => await CheckWithTimeout(async () =>
    {
        var result = (await dbConnection.QueryAsync<int>("SELECT 789")).ToArray();

        return result[0] == 789;
    });

    public async Task<bool> TestMongoDocumentDbConnection() => await CheckWithTimeout(async () =>
    {
        var pingResult = await mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

        return pingResult["ok"] == 1;
    });

    public async Task<bool> TestAzuriteStorageBlobClient() => await CheckWithTimeout(async () =>
    {
        var res = await blobServiceClient.GetPropertiesAsync();

        return !res.GetRawResponse().IsError;
    });

    public async Task<bool> TestAzuriteStorageQueueClient() => await CheckWithTimeout(async () =>
    {
        var res = await queueServiceClient.GetPropertiesAsync();

        return !res.GetRawResponse().IsError;
    });

    public async Task<bool> TestAzuriteStorageTableClient() => await CheckWithTimeout(async () =>
    {
        var res = await tableServiceClient.GetPropertiesAsync();

        return !res.GetRawResponse().IsError;
    });

    private async Task<bool> CheckWithTimeout(
        Func<Task<bool>> check, 
        TimeSpan? timeout = null)
    {
        var resolvedTimeout = timeout ?? TimeSpan.FromSeconds(5);
        var result = await Task.WhenAny(
            Task.Run(async () => 
            {
                try 
                {
                    return await check();
                } 
                catch
                {
                    return false;
                }
            }),
            Task.Delay(resolvedTimeout).ContinueWith((_) => false));

        return await result;
    }
}
