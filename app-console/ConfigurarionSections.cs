using Microsoft.Extensions.Configuration;

namespace Practice.Csharp;

public sealed class MsSqlDbConfig
{
    public const string SectionPath = "infrastructure:ms-sql-db";
    //[ConfigurationKeyName("address")] // for alternative names
    public required string Address { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}

public sealed class MongoDocumentDbConfig
{
    public const string SectionPath = "infrastructure:mongo-document-db";
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}

public sealed class AzuriteStorageConfig
{
    public const string SectionPath = "infrastructure:azurite-storage";
    public required string Host { get; set; }
    public required int BlobPort { get; set; }
    public required int QueuePort { get; set; }
    public required int TablePort { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}