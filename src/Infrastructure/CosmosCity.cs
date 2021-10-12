using AzureGems.Repository.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class CosmosCity : BaseEntity
    {
        public Int64 ZombieCount { get; set; }
        public Int64 KangarooCount { get; set; }
        public Int64 HumanCount { get; set; }
        public string Name { get { return Id; } set { Id = value; } }
        public string State { get; set; }

    }
}
