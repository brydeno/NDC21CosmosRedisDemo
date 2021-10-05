using AzureGems.Repository.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain
{
    public class City : BaseEntity
    {
        public int ZombieCount { get; set; }
        public int KangarooCount { get; set; }
        public int HumanCount { get; set; }
        [Key]
        public string Name { get { return Id; } set { Id = value; } }
        public string State { get; set; }

        // This is where the magic happens
        public void Calculate()
        {
            var initialK = KangarooCount;
            var initialH = HumanCount;
            var initialZ = ZombieCount;

            // Kangaroos eat zombies and are eaten by humans and so on round the circle
            KangarooCount = KangarooCount + (int) (1.1 * initialZ) - (int) (1.1 * initialH);
            HumanCount += HumanCount + (int)(1.1 * initialK) - (int)(1.1 * initialZ);
            ZombieCount += ZombieCount + (int)(1.1 * initialH) - (int)(1.1 * initialK);
        }

    }
}
