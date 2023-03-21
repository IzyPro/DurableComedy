using System;

namespace DurableComedy.Helpers
{
    public static class EnvironmentVariables
    {
        public static string Key { get; } = Environment.GetEnvironmentVariable("Key");
        public static string Client { get; } = Environment.GetEnvironmentVariable("Client");
        public static string SubscriptionId { get; } = Environment.GetEnvironmentVariable("SubscriptionId");
        public static string Tenant { get; } = Environment.GetEnvironmentVariable("Tenant");
        public static string ResourceGroupName { get; } = Environment.GetEnvironmentVariable("ResourceGroupName"); 

        public static string Server { get; } = Environment.GetEnvironmentVariable("Server");
        public static string Username { get; } = Environment.GetEnvironmentVariable("RegistryUsername");
        public static string Password { get; } = Environment.GetEnvironmentVariable("RegistryPassword");
    }
}
