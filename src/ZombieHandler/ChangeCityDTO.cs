using System;
using System.Collections.Generic;
using System.Text;

namespace ZombieHandler
{
    public class ChangeCityDTO
    {
        public string CityName { get; set; }
        public int KangarooChange { get; set; }
        public int HumanChange { get; set; }
        public int ZombieChange { get; set; }
    }
}
