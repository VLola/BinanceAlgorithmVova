﻿using Binance.Net.Enums;
using BinanceAlgorithmVova.Binance;
using BinanceAlgorithmVova.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace BinanceAlgorithmVova.Algorithm
{
    public static class AlgorithmBet
    {
        public static long CloseOrder(Socket socket, string symbol, long order_id, decimal quantity)
        {
            OrderSide side;
            FuturesOrderType type = FuturesOrderType.Market;
            PositionSide position_side;

            if (order_id != 0)
            {
                position_side = InfoOrderPositionSide(socket, symbol, order_id);
                if (position_side == PositionSide.Long) side = OrderSide.Sell;
                else side = OrderSide.Buy;
                Order(socket, symbol, side, type, quantity, position_side);
                new SoundPlayer(Properties.Resources.wav_1).Play();
                return 0;
            }
            else return order_id;
        }
        public static long OpenOrder(Socket socket, string symbol, decimal quantity, double price_candle, double price_sma)
        {
            OrderSide side;
            FuturesOrderType type = FuturesOrderType.Market;
            PositionSide position_side;

            if (price_candle < price_sma) position_side = PositionSide.Short;
            else position_side = PositionSide.Long;

            if (position_side == PositionSide.Long) side = OrderSide.Buy;
            else side = OrderSide.Sell;

            long order_id = Order(socket, symbol, side, type, quantity, position_side);
            if(order_id != 0) new SoundPlayer(Properties.Resources.wav_2).Play();
            return order_id;
        }
        public static long Order(Socket socket, string symbol, OrderSide side, FuturesOrderType type, decimal quantity, PositionSide position_side)
        {
            var result = socket.futures.Trading.PlaceOrderAsync(symbol: symbol, side: side, type: type, quantity: quantity, positionSide: position_side).Result;
            if (!result.Success) ErrorText.Add($"Failed OpenOrder: {result.Error.Message}");
            return result.Data.Id;
        }
        public static PositionSide InfoOrderPositionSide(Socket socket, string symbol, long order_id)
        {
            var result = socket.futures.Trading.GetOrderAsync(symbol: symbol, orderId: order_id).Result;
            if (!result.Success)
            {
                ErrorText.Add($"InfoOrderPositionSide: {result.Error.Message}");
                return InfoOrderPositionSide(socket, symbol, order_id);
            }
            return result.Data.PositionSide;
        }
    }
}
