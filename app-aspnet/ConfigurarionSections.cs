using Microsoft.Extensions.Configuration;

namespace Practice.Aspnet;

public sealed class MsSqlDbConfig
{
    public const string SectionPath = "Infrastructure:ms-sql-db";
    public required string Address { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}

public sealed class MongoDocumentDbConfig
{
    public const string SectionPath = "Infrastructure:mongo-document-db";
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}

public sealed class AzuriteStorageConfig
{
    public const string SectionPath = "Infrastructure:azurite-storage";
    public required string Host { get; set; }
    public required int BlobPort { get; set; }
    public required int QueuePort { get; set; }
    public required int TablePort { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}

public sealed class SeqIdentityServerConfig
{
    public const string SectionPath = "Infrastructure:seq-open-telemetry-logs";
    public required string Address { get; set; }
}

public sealed class DuendeIdentityServerConfig
{
    public const string SectionPath = "Infrastructure:duende-identity-server";
    public required string Address { get; set; }
    public required string ClientSecretFile { get; set; }
}

public sealed class GeneralConfig
{
    public const string SectionPath = "General";
    [ConfigurationKeyName("AppName")]
    public required string AppName { get; set;}
}