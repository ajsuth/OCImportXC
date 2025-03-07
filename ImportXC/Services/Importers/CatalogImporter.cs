﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC.Services.Importers
{
    public class CatalogImporter : IImporter
    {
        private readonly OrderCloudClient client;
        private readonly string importFolder;

        public CatalogImporter(OrderCloudClient client, string importFolder)
        {
            this.client = client;
            this.importFolder = importFolder;
        }

        public async Task Import()
        {
            Console.WriteLine("Importing catalogs");
            
            var catalogFileNames = Directory.EnumerateFiles(this.importFolder, "Catalog.*.json");
            var catalogs = new List<Catalog>();
            
            foreach (var catalogFileName in catalogFileNames)
            {
                JObject catalogsJson = JObject.Parse(File.ReadAllText(catalogFileName));
                foreach (var catalogJson in catalogsJson["$values"])
                {
                    var catalogId = catalogJson["FriendlyId"].ToString();
                    var catalog = await client.Catalogs.GetAsync(catalogId);

                    if (catalog == null)
                    {
                        Console.WriteLine($"Creating catalog {catalogId}...");
                        catalog = new Catalog
                        {
                            ID = catalogId,
                            Name = catalogJson["Name"].ToString(),
                            Active = true,
                            Description = catalogJson["DisplayName"].ToString()
                        };
                        catalogs.Add(catalog);
                    }
                }
            }
            
            await Throttler.RunAsync(catalogs, 100, 20, catalog => client.Catalogs.CreateAsync(catalog));
        }
    }
}