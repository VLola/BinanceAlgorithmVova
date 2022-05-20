using System;
using System.ComponentModel.DataAnnotations;

namespace BinanceAlgorithmVova.Objects
{
    public class Candle
    {
        [Dapper.Contrib.Extensions.ExplicitKey]
        [Key]
        public DateTime DateTime { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }
}
