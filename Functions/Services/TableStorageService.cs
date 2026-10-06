using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions
{
    /// <summary>
    /// Helper service for Azure Table Storage operations.
    /// </summary>
    public class TableStorageService
    {
        private readonly TableServiceClient _serviceClient;

        public TableStorageService(string connectionString)
        {
            _serviceClient = new TableServiceClient(connectionString);
        }

        public TableClient GetTableClient(string tableName)
        {
            var client = _serviceClient.GetTableClient(tableName);
            client.CreateIfNotExists();
            return client;
        }

        public async Task InsertMenuItemAsync(MenuItem item)
        {
            var client = GetTableClient("MenuItems");
            var entity = new TableEntity(item.Category, item.Id)
            {
                { "Name", item.Name },
                { "Description", item.Description },
                { "Price", item.Price },
                { "IsAvailable", item.IsAvailable }
            };

            await client.AddEntityAsync(entity);
        }

        public async Task<IEnumerable<TableEntity>> GetAllMenuItemsAsync()
        {
            var client = GetTableClient("MenuItems");
            var items = client.QueryAsync<TableEntity>();
            var list = new List<TableEntity>();
            await foreach (var e in items)
            {
                list.Add(e);
            }

            return list;
        }

        public async Task<IEnumerable<TableEntity>> GetMenuItemsByCategoryAsync(string category)
        {
            var client = GetTableClient("MenuItems");
            var items = client.QueryAsync<TableEntity>(ent => ent.PartitionKey == category);
            var list = new List<TableEntity>();
            await foreach (var e in items)
            {
                list.Add(e);
            }

            return list;
        }

        public async Task<TableEntity?> GetMenuItemAsync(string category, string id)
        {
            var client = GetTableClient("MenuItems");
            try
            {
                var resp = await client.GetEntityAsync<TableEntity>(category, id);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateMenuItemAsync(string category, string id, double? price, bool? isAvailable)
        {
            var client = GetTableClient("MenuItems");
            var existing = await GetMenuItemAsync(category, id);
            if (existing == null) throw new InvalidOperationException("Menu item not found");

            if (price.HasValue) existing["Price"] = price.Value;
            if (isAvailable.HasValue) existing["IsAvailable"] = isAvailable.Value;

            await client.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteMenuItemAsync(string category, string id)
        {
            var client = GetTableClient("MenuItems");
            await client.DeleteEntityAsync(category, id);
        }

        // Orders table helpers
        public async Task EnsureOrdersTableAsync()
        {
            var client = GetTableClient("Orders");
            await Task.CompletedTask;
        }

        public async Task InsertOrMergeOrderEntityAsync(TableEntity entity)
        {
            var client = GetTableClient("Orders");
            await client.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        }
    }
}
