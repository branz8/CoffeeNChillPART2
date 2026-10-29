using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models
{
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // Role: "Admin", "Staff", "Customer"
        public string RowKey { get; set; } = string.Empty;       // Email
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}