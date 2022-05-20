using BinanceAlgorithmVova.Objects;
using System.Data.Entity;

namespace BinanceAlgorithmVova.Model
{
    public class ModelCandle : DbContext
    {
        public ModelCandle()
            : base("name=ModelCandle")
        {
        }
        public virtual DbSet<Candle> Candles { get; set; }
    }
}