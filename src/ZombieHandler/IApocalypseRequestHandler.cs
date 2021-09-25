﻿using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZombieHandler
{
    public interface IApocalypseRequestHandler
    {
        public Task<IEnumerable<City>> GetCurrentInformation();
        public Task UpdateCurrentInformation(string cityName, int kangarooChange, int humanChange, int zombieChange);
        public Task SetInformation(string cityName, int kangarooCount, int humanCount, int zombieCount);
        public Task Calculate();
    }
}
