using BinanceAlgorithmVova.Binance;
using BinanceAlgorithmVova.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Binance.Net.Enums;
using ScottPlot;
using System.Drawing;
using Binance.Net.Objects.Models.Spot;
using ScottPlot.Plottable;
using BinanceAlgorithmVova.ConnectDB;
using System.Windows.Threading;
using Binance.Net.Objects.Models.Futures;
using BinanceAlgorithmVova.Interval;
using BinanceAlgorithmVova.Model;
using BinanceAlgorithmVova.Objects;
using Binance.Net.Objects.Models.Futures.Socket;

namespace BinanceAlgorithmVova
{
    public partial class MainWindow : Window
    {
        public Socket socket;
        public IntervalCandles IntervalCandles = new IntervalCandles();
        public List<string> interval = new List<string>();
        public List<string> list_sumbols_name = new List<string>();
        public FinancePlot candlePlot;
        public ScatterPlot sma_long_plot;
        public ScatterPlot bolinger2;
        public ScatterPlot bolinger3;
        public ScatterPlot order_long_open_plot;
        public ScatterPlot order_long_close_plot;
        public ScatterPlot order_short_open_plot;
        public ScatterPlot order_short_close_plot;
        public List<ScatterPlot> order_long_lines_vertical = new List<ScatterPlot>();
        public List<ScatterPlot> order_long_lines_horisontal = new List<ScatterPlot>();
        public List<ScatterPlot> order_short_lines_vertical = new List<ScatterPlot>();
        public List<ScatterPlot> order_short_lines_horisontal = new List<ScatterPlot>();
        public List<BinanceFuturesUsdtTrade> history;
        public KlineInterval interval_time = KlineInterval.OneMinute;
        public TimeSpan timeSpan = new TimeSpan(TimeSpan.TicksPerMinute);
        public List<HistoryOrder> history_order = new List<HistoryOrder>();
        public MainWindow()
        {
            InitializeComponent();
            ErrorWatcher();
            Chart();
            Clients();
            HISTORY_ORDER.ItemsSource = history_order;
            INTERVAL_TIME.ItemsSource = IntervalCandles.Intervals;
            INTERVAL_TIME.SelectedIndex = 0;
            LIST_SYMBOLS.ItemsSource = list_sumbols_name;
            EXIT_GRID.Visibility = Visibility.Hidden;
            LOGIN_GRID.Visibility = Visibility.Visible;
            SMA_LONG.TextChanged += SMA_LONG_TextChanged;
            COUNT_CANDLES.TextChanged += COUNT_CANDLES_TextChanged;
            LINE.TextChanged += LINE_TextChanged;
            TAB_CONTROL.MouseLeftButtonUp += TAB_CONTROL_MouseLeftButtonUp;

            //Create Table BinanceFuturesOrders
            using (ModelBinanceFuturesOrder context = new ModelBinanceFuturesOrder())
            {
                context.BinanceFuturesOrders.Create();
            }
            //Create Table HistoryOrders
            using (ModelHistoryOrder context = new ModelHistoryOrder())
            {
                context.HistoryOrders.Create();
            }
            //Create Table Candles
            using (ModelCandle context = new ModelCandle())
            {
                context.Candles.Create();
            }
        }

        private void LINE_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewLine();
        }
        public ScatterPlot line_scatter;
        double[] line_x = new double[2];
        double[] line_y = new double[2];
        private void NewLine()
        {
            Array.Clear(line_x, 0, 2);
            Array.Clear(line_y, 0, 2);
            if (list_candle_ohlc.Count > 0)
            {
                string text = LINE.Text;
                int count = Int32.Parse(COUNT_CANDLES.Text);
                if (text != "" && text != "-" && count > 0)
                {
                    int percent = Int32.Parse(text);
                    double line = list_candle_ohlc[list_candle_ohlc.Count - 1].Close;
                    double price = line + (line / 1000 * percent);
                    line_x[0] = list_candle_ohlc[list_candle_ohlc.Count - 1].DateTime.AddMinutes(-count).ToOADate();
                    line_x[1] = list_candle_ohlc[list_candle_ohlc.Count - 1].DateTime.ToOADate();
                    line_y[0] = price;
                    line_y[1] = line_y[0];
                    line_scatter = plt.Plot.AddScatterLines(line_x, line_y, Color.LightGreen, lineStyle: LineStyle.Dash);
                    line_scatter.YAxisIndex = 1;
                }
            }
        }
        private void LoadLine()
        {
            line_x[1] = list_candle_ohlc[list_candle_ohlc.Count - 1].DateTime.ToOADate();
        }

        #region - Trede History -
        private void TAB_CONTROL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            History();
            double sum_total = 0;
            int count_orders = 0;
            foreach (var it in ConnectHistoryOrder.Get())
            {
                history_order.Insert(0, it);
                sum_total += it.total;
                count_orders++;
            }
            COUNT_ORDERS.Content = count_orders;
            SUM_TOTAL.Content = sum_total;
            if (sum_total > 0) SUM_TOTAL.Foreground = System.Windows.Media.Brushes.Green;
            else if (sum_total < 0) SUM_TOTAL.Foreground = System.Windows.Media.Brushes.Red;
            HISTORY_ORDER.Items.Refresh();
        }

        private void History()
        {
            history_order.Clear();
            ConnectHistoryOrder.DeleteAll();
            List<BinanceFuturesOrder> orders = ConnectOrder.Get();
            int i = 0;
            foreach (var it in ConnectOrder.Get())
            {
                if (it.PositionSide == PositionSide.Long && it.Side == OrderSide.Sell)
                {
                    ConnectHistoryOrder.Insert(new HistoryOrder(it.CreateTime, it.Symbol, Convert.ToDouble(orders[i - 1].AvgPrice), Convert.ToDouble(it.AvgPrice), Convert.ToDouble(orders[i - 1].QuoteQuantityFilled), Convert.ToDouble(it.QuoteQuantityFilled), it.PositionSide));
                }
                else if (it.PositionSide == PositionSide.Short && it.Side == OrderSide.Buy)
                {
                    ConnectHistoryOrder.Insert(new HistoryOrder(it.CreateTime, it.Symbol, Convert.ToDouble(orders[i - 1].AvgPrice), Convert.ToDouble(it.AvgPrice), Convert.ToDouble(orders[i - 1].QuoteQuantityFilled), Convert.ToDouble(it.QuoteQuantityFilled), it.PositionSide));
                }
                i++;
            }
        }
        #endregion

        #region - Coordinate Orders -
        List<double> long_open_order_x = new List<double>();
        List<double> long_open_order_y = new List<double>();
        List<double> long_close_order_x = new List<double>();
        List<double> long_close_order_y = new List<double>();
        List<double> short_open_order_x = new List<double>();
        List<double> short_open_order_y = new List<double>();
        List<double> short_close_order_x = new List<double>();
        List<double> short_close_order_y = new List<double>();
        private void InfoOrderAsunc(DateTime start_time)
        {
            long_open_order_x.Clear();
            long_open_order_y.Clear();
            long_close_order_x.Clear();
            long_close_order_y.Clear();
            short_open_order_x.Clear();
            short_open_order_y.Clear();
            short_close_order_x.Clear();
            short_close_order_y.Clear();
            bool check_one = false;
            string symbol = LIST_SYMBOLS.Text;
            if (symbol != "")
            {
                ConnectOrder.DeleteAll();
                foreach (var it in Algorithm.AlgorithmBet.InfoOrder(socket, symbol, start_time))
                {

                    if (it.PositionSide == PositionSide.Long && it.Side == OrderSide.Buy)
                    {
                        long_open_order_x.Add(it.CreateTime.ToOADate());
                        long_open_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                        check_one = true;
                        ConnectOrder.Insert(it);
                    }
                    else if (it.PositionSide == PositionSide.Long && it.Side == OrderSide.Sell && check_one)
                    {
                        long_close_order_x.Add(it.CreateTime.ToOADate());
                        long_close_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                        ConnectOrder.Insert(it);
                    }
                    else if (it.PositionSide == PositionSide.Short && it.Side == OrderSide.Sell)
                    {
                        short_open_order_x.Add(it.CreateTime.ToOADate());
                        short_open_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                        check_one = true;
                        ConnectOrder.Insert(it);
                    }
                    else if (it.PositionSide == PositionSide.Short && it.Side == OrderSide.Buy && check_one)
                    {
                        short_close_order_x.Add(it.CreateTime.ToOADate());
                        short_close_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                        ConnectOrder.Insert(it);
                    }
                }
            }
        }
        #endregion

        #region - Event Text Changed -
        private void COUNT_CANDLES_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text_long = SMA_LONG.Text;
            string count_candles = COUNT_CANDLES.Text;
            if (text_long != "" && count_candles != "")
            {
                if (Convert.ToInt32(count_candles) > Convert.ToInt32(text_long) && Convert.ToInt32(count_candles) < 500)
                {
                    Reload();
                }
            }
        }

        private void INTERVAL_TIME_DropDownClosed(object sender, EventArgs e)
        {
            int index = INTERVAL_TIME.SelectedIndex;
            interval_time = IntervalCandles.Intervals[index].interval;
            timeSpan = new TimeSpan(IntervalCandles.Intervals[index].timespan);
            Reload();
        }
        #endregion

        #region - Event SMA -

        private void SMA_LONG_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadChart();
        }
        #endregion

        #region - Load Chart -

        public List<ScottPlot.OHLC> list_candle_ohlc = new List<ScottPlot.OHLC>();
        private void LIST_SYMBOLS_DropDownClosed(object sender, EventArgs e)
        {
            Reload();
        }
        private void Reload()
        {
            //StopAsync();
            ConnectCandle.DeleteAll();
            LoadCandles();
            if (ONLINE_CHART.IsChecked == true)
            {
                StartKlineAsync();
            }

            startLoadChart();
            ReloadChart();
            //NewLine();
            LoadChart();
        }
        public OHLC[] olhc_data;
        private void startLoadChart()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (symbol != "")
                {
                    List<OHLC> ohlc_data_list = new List<OHLC>();
                    foreach(Candle it in ConnectCandle.Get())
                    {
                        ohlc_data_list.Add(new OHLC(it.Open, it.High, it.Low, it.Close, it.DateTime, it.TimeSpan));
                    }
                    olhc_data = ohlc_data_list.ToArray();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"ComboBox_SelectionChanged {c.Message}");
            }
        }
        private void ReloadChart()
        {

            plt.Plot.Remove(candlePlot);
            //plt.Plot.Remove(line_scatter);
            //plt.Plot.Remove(candlePlot);
            //plt.Plot.Remove(sma_long_plot);
            //plt.Plot.Remove(order_long_open_plot);
            //plt.Plot.Remove(order_long_close_plot);
            //plt.Plot.Remove(order_short_open_plot);
            //plt.Plot.Remove(order_short_close_plot);
            //if (order_long_lines_vertical.Count > 0) foreach (var it in order_long_lines_vertical) plt.Plot.Remove(it);
            //if (order_long_lines_horisontal.Count > 0) foreach (var it in order_long_lines_horisontal) plt.Plot.Remove(it);
            //if (order_short_lines_vertical.Count > 0) foreach (var it in order_short_lines_vertical) plt.Plot.Remove(it);
            //if (order_short_lines_horisontal.Count > 0) foreach (var it in order_short_lines_horisontal) plt.Plot.Remove(it);
            //plt.Plot.Remove(bolinger2);
            //plt.Plot.Remove(bolinger3);
        }
        public (double[] xs, double[] ys, double[] lower, double[] upper) sma_long;
        private void LoadChart()
        {
            string text_long = SMA_LONG.Text;
            string count_candles = COUNT_CANDLES.Text;
            if (text_long != "" && count_candles != "")
            {
                int sma_indi_long = Convert.ToInt32(text_long);
                if (sma_indi_long > 1 && sma_indi_long < Convert.ToInt32(count_candles))
                {
                    candlePlot = plt.Plot.AddCandlesticks(olhc_data);
                    candlePlot.YAxisIndex = 1;
                    //LoadLine();

                    //candlePlot = plt.Plot.AddCandlesticks(list_candle_ohlc.ToArray());
                    //candlePlot.YAxisIndex = 1;
                    //sma_long = candlePlot.GetBollingerBands(sma_indi_long);
                    //sma_long_plot = plt.Plot.AddScatterLines(sma_long.xs, sma_long.ys, Color.Cyan, 2, label: text_long + " minute SMA");
                    //sma_long_plot.YAxisIndex = 1;
                    //bolinger2 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.lower, Color.Blue, lineStyle: LineStyle.Dash);
                    //bolinger2.YAxisIndex = 1;
                    //bolinger3 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.upper, Color.Blue, lineStyle: LineStyle.Dash);
                    //bolinger3.YAxisIndex = 1;
                    //if (long_close_order_x.Count != 0 && long_close_order_y.Count != 0)
                    //{
                    //    order_long_close_plot = plt.Plot.AddScatter(long_close_order_x.ToArray(), long_close_order_y.ToArray(), color: Color.Orange, lineWidth: 0, markerSize: 10, markerShape: MarkerShape.eks);
                    //    order_long_close_plot.YAxisIndex = 1;
                    //    order_long_lines_vertical.Clear();
                    //    for (int i = 0; i < long_close_order_x.Count; i++)
                    //    {
                    //        double[] x = { long_open_order_x[i], long_open_order_x[i] };
                    //        double[] y = { long_open_order_y[i], long_close_order_y[i] };
                    //        ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                    //        scatter.YAxisIndex = 1;
                    //        order_long_lines_vertical.Add(scatter);
                    //    }
                    //    order_long_lines_horisontal.Clear();
                    //    for (int i = 0; i < long_close_order_x.Count; i++)
                    //    {
                    //        double[] x = { long_open_order_x[i], long_close_order_x[i] };
                    //        double[] y = { long_close_order_y[i], long_close_order_y[i] };
                    //        ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                    //        scatter.YAxisIndex = 1;
                    //        order_long_lines_horisontal.Add(scatter);
                    //    }
                    //}
                    //if (long_open_order_x.Count != 0 && long_open_order_y.Count != 0)
                    //{
                    //    order_long_open_plot = plt.Plot.AddScatter(long_open_order_x.ToArray(), long_open_order_y.ToArray(), color: Color.Green, lineWidth: 0, markerSize: 8);
                    //    order_long_open_plot.YAxisIndex = 1;
                    //}
                    //if (short_close_order_x.Count != 0 && short_close_order_y.Count != 0)
                    //{
                    //    order_short_close_plot = plt.Plot.AddScatter(short_close_order_x.ToArray(), short_close_order_y.ToArray(), color: Color.Orange, lineWidth: 0, markerSize: 10, markerShape: MarkerShape.eks);
                    //    order_short_close_plot.YAxisIndex = 1;
                    //    order_short_lines_vertical.Clear();
                    //    for (int i = 0; i < short_close_order_x.Count; i++)
                    //    {
                    //        double[] x = { short_close_order_x[i], short_close_order_x[i] };
                    //        double[] y = { short_open_order_y[i], short_close_order_y[i] };
                    //        ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                    //        scatter.YAxisIndex = 1;
                    //        order_short_lines_vertical.Add(scatter);
                    //    }
                    //    order_short_lines_horisontal.Clear();
                    //    for (int i = 0; i < short_close_order_x.Count; i++)
                    //    {
                    //        double[] x = { short_open_order_x[i], short_close_order_x[i] };
                    //        double[] y = { short_open_order_y[i], short_open_order_y[i] };
                    //        ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                    //        scatter.YAxisIndex = 1;
                    //        order_short_lines_horisontal.Add(scatter);
                    //    }
                    //}
                    //if (short_open_order_x.Count != 0 && short_open_order_y.Count != 0)
                    //{
                    //    order_short_open_plot = plt.Plot.AddScatter(short_open_order_x.ToArray(), short_open_order_y.ToArray(), color: Color.DarkRed, lineWidth: 0, markerSize: 8);
                    //    order_short_open_plot.YAxisIndex = 1;
                    //}


                    //StartAlgorithm();
                    plt.Refresh();
                    //plt.Render();
                }
            }
        }

        #endregion

        #region - Algorithm -
        public decimal quantity_bet;
        public long bet_order_id = 0;
        private void LongBet_Click(object sender, RoutedEventArgs e)
        {
            string symbol = LIST_SYMBOLS.Text;
            string usdt_bet = USDT_BET.Text;
            string price_symbol = PRICE_SYMBOL.Text;
            if (usdt_bet != "" && price_symbol != "" && bet_order_id == 0)
            {
                decimal usdt = Decimal.Parse(usdt_bet);
                decimal price = Decimal.Parse(price_symbol);
                if (usdt > 0m && price > 0m)
                {
                    quantity_bet = Math.Round(usdt / price, 1);
                    bet_order_id = Algorithm.Algorithm.OpenOrder(socket, symbol, quantity_bet, PositionSide.Long);
                }
            }
        }
        private void ShortBet_Click(object sender, RoutedEventArgs e)
        {
            string symbol = LIST_SYMBOLS.Text;
            string usdt_bet = USDT_BET.Text;
            string price_symbol = PRICE_SYMBOL.Text;
            if (usdt_bet != "" && price_symbol != "" && bet_order_id == 0)
            {
                decimal usdt = Decimal.Parse(usdt_bet);
                decimal price = Decimal.Parse(price_symbol);
                if (usdt > 0m && price > 0m)
                {
                    quantity_bet = Math.Round(usdt / price, 1);
                    bet_order_id = Algorithm.Algorithm.OpenOrder(socket, symbol, quantity_bet, PositionSide.Short);
                }
            }
        }
        private void CloseOrder_Click(object sender, RoutedEventArgs e)
        {
            string symbol = LIST_SYMBOLS.Text;
            if (bet_order_id != 0) bet_order_id = Algorithm.Algorithm.CloseOrder(socket, symbol, bet_order_id, quantity_bet);
        }
        private void StartAlgorithm()
        {
            try
            {

            }
            catch (Exception c)
            {
                ErrorText.Add($"StartAlgorithm {c.Message}");
            }
        }
        #endregion

        #region - Load Candles -
        private void LoadCandles()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                int count = Convert.ToInt32(COUNT_CANDLES.Text);
                if (count > 0 && count < 500 && symbol != "")
                {
                    Klines(symbol, klines_count: count);
                }
                else MessageBox.Show("Button_Refile: Не верные условия!");
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadCandles {c.Message}");
            }
        }

        #endregion

        #region - List Sumbols -
        private void GetSumbolName()
        {
            foreach (var it in ListSymbols())
            {
                list_sumbols_name.Add(it.Symbol);
            }
            list_sumbols_name.Sort();
            LIST_SYMBOLS.Items.Refresh();
            LIST_SYMBOLS.SelectedIndex = 0;
        }
        public List<BinancePrice> ListSymbols()
        {
            try
            {
                var result = socket.futures.ExchangeData.GetPricesAsync().Result;
                if (!result.Success) ErrorText.Add("Error GetKlinesAsync");
                return result.Data.ToList();
            }
            catch (Exception e)
            {
                ErrorText.Add($"ListSymbols {e.Message}");
                return ListSymbols();
            }
        }

        #endregion

        #region - Chart -
        private void Chart()
        {
            plt.Plot.Layout(padding: 12);
            plt.Plot.Style(figureBackground: Color.Black, dataBackground: Color.Black);
            plt.Plot.Frameless();
            plt.Plot.XAxis.TickLabelStyle(color: Color.White);
            plt.Plot.XAxis.TickMarkColor(ColorTranslator.FromHtml("#333333"));
            plt.Plot.XAxis.MajorGrid(color: ColorTranslator.FromHtml("#333333"));

            plt.Plot.YAxis.Ticks(false);
            plt.Plot.YAxis.Grid(false);
            plt.Plot.YAxis2.Ticks(true);
            plt.Plot.YAxis2.Grid(true);
            plt.Plot.YAxis2.TickLabelStyle(color: ColorTranslator.FromHtml("#00FF00"));
            plt.Plot.YAxis2.TickMarkColor(ColorTranslator.FromHtml("#333333"));
            plt.Plot.YAxis2.MajorGrid(color: ColorTranslator.FromHtml("#333333"));

            var legend = plt.Plot.Legend();
            legend.FillColor = Color.Transparent;
            legend.OutlineColor = Color.Transparent;
            legend.Font.Color = Color.White;
            legend.Font.Bold = true;
        }
        #endregion

        #region - Async klines -

        private void STOP_ASYNC_Click(object sender, RoutedEventArgs e)
        {
            StopAsync();
        }
        private void StopAsync()
        {
            try
            {
                socket.socketClient.UnsubscribeAllAsync();
            }
            catch (Exception c)
            {
                ErrorText.Add($"STOP_ASYNC_Click {c.Message}");
            }
        }
        public OHLC_NEW ohlc_item = new OHLC_NEW();
        public int Id;
        public void StartKlineAsync()
        {
            StartPriceAsync();
            socket.socketClient.UsdFuturesStreams.SubscribeToKlineUpdatesAsync(LIST_SYMBOLS.Text, interval_time, Message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Candle candle = new Candle();
                    candle.DateTime = Message.Data.Data.OpenTime;
                    candle.Open = Decimal.ToDouble(Message.Data.Data.OpenPrice);
                    candle.Close = Decimal.ToDouble(Message.Data.Data.ClosePrice);
                    candle.High = Decimal.ToDouble(Message.Data.Data.HighPrice);
                    candle.Low = Decimal.ToDouble(Message.Data.Data.LowPrice);
                    candle.TimeSpan = timeSpan;
                    if (!ConnectCandle.Update(candle)) ConnectCandle.Insert(candle);
                    startLoadChart();
                    //if (ohlc_item.DateTime == Message.Data.Data.OpenTime) ConnectOHLC_NEW.Delete(Id);
                    //ohlc_item.DateTime = Message.Data.Data.OpenTime;
                    //ohlc_item.Open = Message.Data.Data.OpenPrice;
                    //ohlc_item.High = Message.Data.Data.HighPrice;
                    //ohlc_item.Low = Message.Data.Data.LowPrice;
                    //ohlc_item.Close = Message.Data.Data.ClosePrice;
                    //Id = Convert.ToInt32(ConnectOHLC_NEW.Insert(ohlc_item));
                    //startLoadChart();
                }));
            });

        }

        private void StartPriceAsync()
        {
            socket.socketClient.UsdFuturesStreams.SubscribeToMarkPriceUpdatesAsync(symbol: LIST_SYMBOLS.Text, updateInterval: 1000, Message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    PRICE_SYMBOL.Text = Message.Data.MarkPrice.ToString();
                }));
            });
        }
        #endregion

        #region - Candles Save -
        public void Klines(string Symbol, DateTime? start_time = null, DateTime? end_time = null, int? klines_count = null)
        {
            try
            {
                var result = socket.futures.ExchangeData.GetKlinesAsync(symbol: Symbol, interval: interval_time, startTime: start_time, endTime: end_time, limit: klines_count).Result;
                if (!result.Success) ErrorText.Add("Error GetKlinesAsync");
                else
                {
                    foreach (var it in result.Data.ToList())
                    {
                        Id = Convert.ToInt32(ConnectCandle.Insert(new Candle() { 
                            Open = Decimal.ToDouble(it.OpenPrice),
                            Close = Decimal.ToDouble(it.ClosePrice),
                            High = Decimal.ToDouble(it.HighPrice),
                            Low = Decimal.ToDouble(it.LowPrice),
                            DateTime = it.OpenTime,
                            TimeSpan = timeSpan
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                ErrorText.Add($"Klines {e.Message}");
            }
        }

        #endregion

        #region - Login -
        private void Button_Save(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = CLIENT_NAME.Text;
                string api = API_KEY.Text;
                string key = SECRET_KEY.Text;
                if (name != "" && api != "" && key != "")
                {
                    string path = System.IO.Path.Combine(Environment.CurrentDirectory, "clients");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    if (!File.Exists(path + "/" + CLIENT_NAME.Text))
                    {
                        CLIENT_NAME.Text = "";
                        API_KEY.Text = "";
                        SECRET_KEY.Text = "";
                        Client client = new Client(name, api, key);
                        string json = JsonConvert.SerializeObject(client);
                        File.WriteAllText(path + "/" + name, json);
                        Clients();
                    }
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"Button_Save {c.Message}");
            }
        }
        private void Clients()
        {
            try
            {
                string path = System.IO.Path.Combine(Environment.CurrentDirectory, "clients");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                List<string> filesDir = (from a in Directory.GetFiles(path) select System.IO.Path.GetFileNameWithoutExtension(a)).ToList();
                if (filesDir.Count > 0)
                {
                    ClientList file_list = new ClientList(filesDir);
                    BOX_NAME.ItemsSource = file_list.BoxNameContent;
                    BOX_NAME.SelectedItem = file_list.BoxNameContent[0];
                }
            }
            catch (Exception e)
            {
                ErrorText.Add($"Clients {e.Message}");
            }
        }
        private void Button_Login(object sender, RoutedEventArgs e)
        {
            try
            {
                string api = API_KEY.Text;
                string key = SECRET_KEY.Text;
                if (api != "" && key != "")
                {
                    CLIENT_NAME.Text = "";
                    API_KEY.Text = "";
                    SECRET_KEY.Text = "";
                    socket = new Socket(api, key);
                    Login_Click();
                }
                else if (BOX_NAME.Text != "")
                {
                    string path = System.IO.Path.Combine(Environment.CurrentDirectory, "clients");
                    string json = File.ReadAllText(path + "\\" + BOX_NAME.Text);
                    Client client = JsonConvert.DeserializeObject<Client>(json);
                    socket = new Socket(client.ApiKey, client.SecretKey);
                    Login_Click();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"Button_Login {c.Message}");
            }
        }
        private void Login_Click()
        {
            LOGIN_GRID.Visibility = Visibility.Hidden;
            EXIT_GRID.Visibility = Visibility.Visible;
            GetSumbolName();
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            EXIT_GRID.Visibility = Visibility.Hidden;
            LOGIN_GRID.Visibility = Visibility.Visible;
            socket = null;
            list_sumbols_name.Clear();
        }
        #endregion

        #region - Error -
        // ------------------------------------------------------- Start Error Text Block --------------------------------------
        private void ErrorWatcher()
        {
            try
            {
                FileSystemWatcher error_watcher = new FileSystemWatcher();
                error_watcher.Path = ErrorText.Directory();
                error_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                error_watcher.Changed += new FileSystemEventHandler(OnChanged);
                error_watcher.Filter = ErrorText.Patch();
                error_watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                ErrorText.Add($"ErrorWatcher {e.Message}");
            }
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(new Action(() => { ERROR_LOG.Text = File.ReadAllText(ErrorText.FullPatch()); }));
        }
        private void Button_ClearErrors(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(ErrorText.FullPatch(), "");
        }
        // ------------------------------------------------------- End Error Text Block ----------------------------------------
        #endregion

    }
}
