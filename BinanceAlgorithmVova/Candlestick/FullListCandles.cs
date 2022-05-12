using System.Collections.Generic;

namespace BinanceAlgorithmVova.Candlestick
{
    public class FullListCandles
    {
        public int number { get; set; }
        public List<Candle> list { get; set; }
        public FullListCandles(int number, List<Candle> list)
        {
            this.number = number;
            this.list = list;
        }
    }
}
