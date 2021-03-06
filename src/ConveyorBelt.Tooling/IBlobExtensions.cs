﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeeHive.DataStructures;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace ConveyorBelt.Tooling
{
    public static class IBlobExtensions
    {

        public static IEnumerable<DynamicTableEntity> FromIisLogsToEntities(this IBlob blob)
        {

            var reader = new StreamReader(blob.Body);
            string line = null;
            string[] fields = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#Fields: "))
                    fields = BuildFields(line);

                if (line.StartsWith("#"))
                    continue;

                var idSegments = blob.Id.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                var entries = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var datetime = string.Join(" ", entries.Take(2));
                var rest = entries.Skip(2).ToArray();
                var entity = new DynamicTableEntity();
                entity.Timestamp = DateTimeOffset.Parse(datetime);
                entity.PartitionKey = string.Join("_", idSegments.Take(idSegments.Length-1));
                entity.RowKey = string.Format("{0}_{1:yyyyMMddHHmmss}_{2}",                    
                    Path.GetFileNameWithoutExtension(idSegments.Last()),
                    entity.Timestamp,
                    Guid.NewGuid().ToString("N"));

                if (fields.Length != rest.Length)
                    throw new InvalidOperationException("fields not equal");

                for (int i = 0; i < fields.Length; i++)
                {
                    string name = fields[i];
                    string value = rest[i];

                    if (Regex.IsMatch(value, @"^[1-9]\d*$")) // numeric
                        entity.Properties.Add(name, new EntityProperty(Convert.ToInt64(value)));
                    else
                        entity.Properties.Add(name, new EntityProperty(value));
                }

                yield return entity;
            }
        }

        private static string[] BuildFields(string line)
        {
            line = line.Replace("#Fields: ", "");
            if (!line.StartsWith("date time "))
                throw new InvalidDataException("Does not contain date time as the first fields.");

            line = line.Replace("date time ", "");
            line = line.Replace(")", "");
            line = line.Replace("(", "_");
            return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

    }
}
