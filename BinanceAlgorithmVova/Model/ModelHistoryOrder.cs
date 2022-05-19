using BinanceAlgorithmVova.Objects;
using System;
using System.Data.Entity;
using System.Linq;

namespace BinanceAlgorithmVova.Model
{
    public class ModelHistoryOrder : DbContext
    {
        public ModelHistoryOrder()
            : base("name=ModelHistoryOrder")
        { }
        public DbSet<HistoryOrder> HistoryOrders { get; set; }
    }

}