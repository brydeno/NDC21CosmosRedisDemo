using AzureGems.Repository.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class CosmosCity : BaseEntity
    {
        public int ZombieCount { get; set; }
        public int KangarooCount { get; set; }
        public int HumanCount { get; set; }
        public string Name { get { return Id; } set { Id = value; } }
        public string State { get; set; }

    }
}
