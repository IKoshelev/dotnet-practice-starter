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

public sealed class MongoConfig
{
    public const string SectionPath = "infrastructure:mongo";
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string UserName { get; set; }
    public required string PasswordFile { get; set; }
}