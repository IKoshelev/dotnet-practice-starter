public static class SecretSource
{
    public static Task<string> Value = Task.Run(() => File.ReadAllTextAsync("./secret.txt"));
}