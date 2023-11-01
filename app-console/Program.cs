// See https://aka.ms/new-console-template for more information
using Azure;
using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Text;

using static Practice.Csharp.ConsoleHelper;

namespace Practice.Csharp;

internal partial class Program
{
    public static bool IsDocker = Environment.GetEnvironmentVariable("IS_DOCKER")
                                    ?.Equals("TRUE", StringComparison.InvariantCultureIgnoreCase) ?? false;

    private static async Task Main(string[] args)
    {
        Console.WriteLine($"Starting console app. IsDocker={IsDocker}");

        var rootConfiguration = WithConsoleNotification("Configuration read", ReadConfiguration);

        await WithConsoleNotification("Infrastructure checks",
            async () => await Task.WhenAll(
                CheckMsSqlDbConnection(
                    rootConfiguration.GetRequiredSection(MsSqlDbConfig.SectionPath).Get<MsSqlDbConfig>()!),
                CheckMongoConnection(
                    rootConfiguration.GetRequiredSection(MongoConfig.SectionPath).Get<MongoConfig>()!),
                CheckAzuriteStorageConnection(
                    rootConfiguration.GetRequiredSection(AzuriteStorageConfig.SectionPath).Get<AzuriteStorageConfig>()!)
                )
        );

        TestHttpListener();
    }

    private static void TestHttpListener()
    {
        Console.WriteLine($"Starting HttpListener test");

        var listener = new HttpListener();
        listener.Prefixes.Add("http://+:8080/");

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Color(RED, "HttpListener.Start")} failed. You may need to run as admin");
            return;
        }

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

    private static IConfigurationRoot ReadConfiguration()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder();

        builder.AddJsonFile("appsettings.json", false, true);

        if (IsDocker)
        {
            builder.AddJsonFile("appsettings.docker.json", false, true);
        }

        IConfigurationRoot root = builder.Build();

        return root;
    }

    private static async Task CheckMsSqlDbConnection(MsSqlDbConfig config)
    {
        await RunCheck("ms-sql-db", async () =>
        {
            var password = await File.ReadAllTextAsync(config.PasswordFile);

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = config.Address;
            builder.UserID = config.UserName;
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

    public class MongoDocumentExample
    {
        public ObjectId Id { get; set; }
        public int Value { get; set; }
    }

    private static async Task CheckMongoConnection(MongoConfig config)
    {
        await RunCheck("mongo", async () =>
        {
            var password = await File.ReadAllTextAsync(config.PasswordFile);

            var clientSetting = new MongoClientSettings()
            {
                Credential = MongoCredential.CreateCredential("admin", config.UserName, password),
                Server = new MongoServerAddress(config.Host, config.Port),
            };

            var client = new MongoClient(clientSetting);

            var database = client.GetDatabase("development");

            var collection = database.GetCollection<MongoDocumentExample>("connectionTest");

            await collection.InsertOneAsync(new MongoDocumentExample { Value = 789 });

            var list = await collection.Find(x => x.Value == 789).ToListAsync();

            await collection.DeleteManyAsync(x => true);

            return list[0].Value == 789;
        });
    }

    public class AzureStorageTableExample : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int Value { get; set; }
    }
    
    private static async Task CheckAzuriteStorageConnection(AzuriteStorageConfig config)
    {
        await RunCheck("azurite-storage", async () =>
        {
            var password = await File.ReadAllTextAsync(config.PasswordFile);

            return (await Task.WhenAll(
                RunCheck("azurite-storage-blob", async () => await CheckBlobStorage(config, password)),
                RunCheck("azurite-storage-queue", async () => await CheckQueueStorage(config, password)),
                RunCheck("azurite-storage-table", async () => await CheckTableStorage(config, password))
                )).All(x => x);

            static async Task<bool> CheckBlobStorage(
                AzuriteStorageConfig config, 
                string password)
            {
                string blobUri = $"http://{config.Host}:{config.BlobPort}/{config.UserName}";

                var blobServiceClient = new BlobServiceClient(
                    new Uri(blobUri),
                    new StorageSharedKeyCredential(config.UserName, password));

                string containerName = "container-test";

                BlobContainerClient containerClient = null;
                try
                {
                    containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();
                    await containerClient.UploadBlobAsync("test-blob", new BinaryData("789"));
                    var res = await containerClient.GetBlobClient("test-blob").DownloadContentAsync();
                    return res.HasValue && res.Value.Content.ToString() == "789";
                }
                finally
                {
                    if (containerClient != null)
                    {
                        await containerClient.DeleteAsync();
                    }
                }
            }

            static async Task<bool> CheckQueueStorage(
                AzuriteStorageConfig config, 
                string password)
            {
                string queueUri = $"http://{config.Host}:{config.QueuePort}/{config.UserName}";

                var queueServiceClient = new QueueServiceClient(
                    new Uri(queueUri),
                    new StorageSharedKeyCredential(config.UserName, password));

                string queueName = "queue-est";

                QueueClient queueClient = null;
                try
                {
                    queueClient = queueServiceClient.GetQueueClient(queueName);
                    await queueClient.CreateIfNotExistsAsync();
                    await queueClient.SendMessageAsync("789");
                    var res = await queueClient.ReceiveMessagesAsync(maxMessages: 1);
                    return res.HasValue && res.Value.FirstOrDefault()?.Body.ToString() == "789";
                }
                finally
                {
                    if (queueClient != null)
                    {
                        await queueClient.DeleteAsync();
                    }
                }
            }

            static async Task<bool> CheckTableStorage(
                AzuriteStorageConfig config,
                string password)
            {
                string tableUri = $"http://{config.Host}:{config.TablePort}/{config.UserName}";

                //TODO this throws in Docker
                var serviceClient = new TableServiceClient(
                    new Uri(tableUri), 
                    new TableSharedKeyCredential(config.UserName, password));

                string tableName = "tableTest";

                TableClient tableClient = null; 
                try
                {
                    tableClient = serviceClient.GetTableClient(tableName);
                    await tableClient.CreateIfNotExistsAsync();
                    await tableClient.AddEntityAsync(new AzureStorageTableExample()
                    {
                        PartitionKey = "part-key-1",
                        RowKey = "row-key-1",
                        Value = 789
                    });

                    var res = await tableClient.GetEntityAsync<AzureStorageTableExample>("part-key-1", "row-key-1");
                    return res.HasValue && res.Value.Value == 789;
                }
                finally
                {
                    if (tableClient != null)
                    {
                        await tableClient.DeleteAsync();
                    }
                }
            }
        });
    }
}