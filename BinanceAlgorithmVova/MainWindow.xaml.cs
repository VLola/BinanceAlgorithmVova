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
using System.Media;

namespace BinanceAlgorithmVova
{
    public partial class MainWindow : Window
    {
        public bool SOUND { get; set; } = false;
        public bool LONG { get; set; } = false;
        public bool SHORT { get; set; } = true;
        public int LINE_OPEN { get; set; } = 1;
        public double LINE_SL { get; set; } = 0.43;
        public int LINE_TP { get; set; } = 50;
        public string API_KEY { get; set; } = "";
        public string SECRET_KEY { get; set; } = "";
        public string CLIENT_NAME { get; set; } = "";
        public int COUNT_CANDLES { get; set; } = 100;
        public int SMA_LONG { get; set; } = 20;
        public decimal USDT_BET { get; set; } = 20m;
        public bool ONLINE_CHART { get; set; } = false;
        public bool START_BET { get; set; } = false;
        public double BOLINGER_TP { get; set; } = 100;
        public double BOLINGER_SL { get; set; } = 100;
        public decimal PRICE_SYMBOL { get; set; }
        public Socket socket;
        public List<string> list_sumbols_name = new List<string>();
        public FinancePlot candlePlot;
        public ScatterPlot sma_long_plot;
        public ScatterPlot bolinger_lower;
        public ScatterPlot bolinger_upper;
        public ScatterPlot line_open_scatter;
        public ScatterPlot line_open_1_scatter;
        public ScatterPlot line_open_2_scatter;
        public ScatterPlot line_open_3_scatter;
        public ScatterPlot line_sl_1_scatter;
        public ScatterPlot line_tp_1_scatter;
        public ScatterPlot line_tp_2_scatter;
        public ScatterPlot order_long_open_plot;
        public ScatterPlot order_long_close_plot;
        public ScatterPlot order_short_open_plot;
        public ScatterPlot order_short_close_plot;
        public List<ScatterPlot> order_long_lines_vertical = new List<ScatterPlot>();
        public List<ScatterPlot> order_long_lines_horisontal = new List<ScatterPlot>();
        public List<ScatterPlot> order_short_lines_vertical = new List<ScatterPlot>();
        public List<ScatterPlot> order_short_lines_horisontal = new List<ScatterPlot>();
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
            INTERVAL_TIME.ItemsSource = IntervalCandles.Intervals();
            INTERVAL_TIME.SelectedIndex = 0;
            LIST_SYMBOLS.ItemsSource = list_sumbols_name;
            EXIT_GRID.Visibility = Visibility.Hidden;
            LOGIN_GRID.Visibility = Visibility.Visible;
            this.DataContext = this;

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

        #region - Sound Order-
        private void SOUND_Click(object sender, RoutedEventArgs e)
        {
            CheckBox box = e.Source as CheckBox;
            SOUND = (bool)box.IsChecked;
        }
        private void SoundOpenOrder()
        {
            try
            {
                if (SOUND) new SoundPlayer(Properties.Resources.wav_2).Play();
            }
            catch (Exception c)
            {
                ErrorText.Add($"SoundOpenOrder {c.Message}");
            }
        }
        private void SoundCloseOrder()
        {
            try
            {
                if (SOUND) new SoundPlayer(Properties.Resources.wav_1).Play();
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadingCandlesToChart {c.Message}");
            }
        }
        #endregion

        #region - Event RadioButton -
        private void Long_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radio = e.Source as RadioButton;
            LONG = (bool)radio.IsChecked;
            if (LONG) SHORT = false;
            if (LINE_OPEN > 0) LINE_OPEN = -LINE_OPEN;
            LINE_OPEN_TEXT.Text = LINE_OPEN.ToString();
            NewLines(0);
        }
        private void Short_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radio = e.Source as RadioButton;
            SHORT = (bool)radio.IsChecked;
            if (SHORT) LONG = false;
            if (LINE_OPEN < 0) LINE_OPEN = -LINE_OPEN;
            LINE_OPEN_TEXT.Text = LINE_OPEN.ToString();
            NewLines(0);
        }
        #endregion

        #region - Chart Line Take Profit -
        private void LINE_TP_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewLineTP();
            plt.Refresh();
        }
        double[] line_tp_1_y = new double[2];
        double[] line_tp_2_y = new double[2];
        private void NewLineTP()
        {
            try
            {
                if (list_candle_ohlc.Count > 0)
                {
                    Array.Clear(line_tp_1_y, 0, 2);
                    Array.Clear(line_tp_2_y, 0, 2);
                    plt.Plot.Remove(line_tp_1_scatter);
                    plt.Plot.Remove(line_tp_2_scatter);
                    if (LINE_TP == 0)
                    {
                        line_tp_1_y[0] = price;
                        line_tp_2_y[0] = price;
                    }
                    else
                    {
                        line_tp_1_y[0] = price + (price_percent * LINE_TP);
                        line_tp_2_y[0] = price + (price_percent * -LINE_TP);
                    }
                    line_tp_1_y[1] = line_tp_1_y[0];
                    line_tp_2_y[1] = line_tp_2_y[0];
                    line_tp_1_scatter = plt.Plot.AddScatterLines(line_x, line_tp_1_y, Color.Orange, lineStyle: LineStyle.Dash, label: line_tp_1_y[0] + " - 1 take profit price");
                    line_tp_1_scatter.YAxisIndex = 1;
                    line_tp_2_scatter = plt.Plot.AddScatterLines(line_x, line_tp_2_y, Color.Orange, lineStyle: LineStyle.Dash, label: line_tp_2_y[0] + " - 2 take profit price");
                    line_tp_2_scatter.YAxisIndex = 1;
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"NewLineTP {c.Message}");
            }
        }
        #endregion

        #region - Chart Line Stop Loss  -
        double[] line_sl_1_y = new double[2];
        private void NewLineSL(double price_sl)
        {
            if (LINE_SL != 0 && list_candle_ohlc.Count > 0)
            {
                try
                {
                    Array.Clear(line_sl_1_y, 0, 2);
                    plt.Plot.Remove(line_sl_1_scatter);
                    line_sl_1_y[0] = price_sl;
                    line_sl_1_y[1] = line_sl_1_y[0];
                    line_sl_1_scatter = plt.Plot.AddScatterLines(line_x, line_sl_1_y, Color.Red, lineStyle: LineStyle.Dash, label: line_sl_1_y[0] + " - stop loss price");
                    line_sl_1_scatter.YAxisIndex = 1;
                    plt.Refresh();
                }
                catch (Exception c)
                {
                    ErrorText.Add($"NewLineSL {c.Message}");
                }
            }
        }
        private void NewLineSLClear()
        {
            plt.Plot.Remove(line_sl_1_scatter);
            plt.Refresh();
        }
        //private void NewLineSL()
        //{
        //    if (LINE_SL != 0 && list_candle_ohlc.Count > 0)
        //    {
        //        Array.Clear(line_sl_1_y, 0, 2);
        //        Array.Clear(line_sl_2_y, 0, 2);
        //        Array.Clear(line_sl_3_y, 0, 2);
        //        plt.Plot.Remove(line_sl_1_scatter);
        //        plt.Plot.Remove(line_sl_2_scatter);
        //        plt.Plot.Remove(line_sl_3_scatter);
        //        line_sl_1_y[0] = price + (price_percent * LINE_SL);
        //        line_sl_1_y[1] = line_sl_1_y[0];
        //        line_sl_2_y[0] = price + (price_percent * 2 * LINE_SL);
        //        line_sl_2_y[1] = line_sl_2_y[0];
        //        line_sl_3_y[0] = price + (price_percent * 3 * LINE_SL);
        //        line_sl_3_y[1] = line_sl_3_y[0];
        //        line_sl_1_scatter = plt.Plot.AddScatterLines(line_x, line_sl_1_y, Color.Red, lineStyle: LineStyle.Dash, label: line_sl_1_y[0] + " - 1 stop loss price");
        //        line_sl_1_scatter.YAxisIndex = 1;
        //        line_sl_2_scatter = plt.Plot.AddScatterLines(line_x, line_sl_2_y, Color.Red, lineStyle: LineStyle.Dash, label: line_sl_2_y[0] + " - 2 stop loss price");
        //        line_sl_2_scatter.YAxisIndex = 1;
        //        line_sl_3_scatter = plt.Plot.AddScatterLines(line_x, line_sl_3_y, Color.Red, lineStyle: LineStyle.Dash, label: line_sl_3_y[0] + " - 3 stop loss price");
        //        line_sl_3_scatter.YAxisIndex = 1;
        //    }
        //}
        #endregion

        #region - Chart Line Open Order  -
        private void LINE_OPEN_TextChanged(object sender, TextChangedEventArgs e)
        {
            NewLines(0);
        }
        private void NewLines(double price_order)
        {
            NewLineOpen(price_order);
            NewLineTP();
            plt.Refresh();
        }
        double price;
        double price_percent;
        double[] line_x = new double[2];
        double[] line_open_y = new double[2];
        double[] line_open_1_y = new double[2];
        double[] line_open_2_y = new double[2];
        double[] line_open_3_y = new double[2];
        private void NewLineOpen(double price_order)
        {
            try
            {
                if (LINE_OPEN != 0 && list_candle_ohlc.Count > 0)
                {

                    Array.Clear(line_x, 0, 2);
                    Array.Clear(line_open_y, 0, 2);
                    Array.Clear(line_open_1_y, 0, 2);
                    Array.Clear(line_open_2_y, 0, 2);
                    Array.Clear(line_open_3_y, 0, 2);
                    plt.Plot.Remove(line_open_scatter);
                    plt.Plot.Remove(line_open_1_scatter);
                    plt.Plot.Remove(line_open_2_scatter);
                    plt.Plot.Remove(line_open_3_scatter);
                    if (price_order == 0) price = list_candle_ohlc[list_candle_ohlc.Count - 1].Close;
                    else price = price_order;
                    price_percent = price / 1000 * LINE_OPEN;
                    line_x[0] = list_candle_ohlc[0].DateTime.ToOADate();
                    line_x[1] = list_candle_ohlc[list_candle_ohlc.Count - 1].DateTime.ToOADate();
                    line_open_y[0] = price;
                    line_open_y[1] = price;
                    line_open_1_y[0] = price + price_percent;
                    line_open_1_y[1] = line_open_1_y[0];
                    line_open_2_y[0] = price + price_percent + price_percent;
                    line_open_2_y[1] = line_open_2_y[0];
                    line_open_3_y[0] = price + price_percent + price_percent + price_percent;
                    line_open_3_y[1] = line_open_3_y[0];
                    line_open_scatter = plt.Plot.AddScatterLines(line_x, line_open_y, Color.White, lineStyle: LineStyle.Dash, label: price + " - open order price");
                    line_open_scatter.YAxisIndex = 1;
                    line_open_1_scatter = plt.Plot.AddScatterLines(line_x, line_open_1_y, Color.LightGreen, lineStyle: LineStyle.Dash, label: line_open_1_y[0] + " - 1 open order price");
                    line_open_1_scatter.YAxisIndex = 1;
                    line_open_2_scatter = plt.Plot.AddScatterLines(line_x, line_open_2_y, Color.LightGreen, lineStyle: LineStyle.Dash, label: line_open_2_y[0] + " - 2 open order price");
                    line_open_2_scatter.YAxisIndex = 1;
                    line_open_3_scatter = plt.Plot.AddScatterLines(line_x, line_open_3_y, Color.LightGreen, lineStyle: LineStyle.Dash, label: line_open_3_y[0] + " - 3 open order price");
                    line_open_3_scatter.YAxisIndex = 1;
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"NewLineOpen {c.Message}");
            }
        }
        private void LoadLineOpen()
        {
            line_x[1] = list_candle_ohlc[list_candle_ohlc.Count - 1].DateTime.ToOADate();
        }
        #endregion

        #region - Event CheckBox -
        private void ONLINE_CHART_Click(object sender, RoutedEventArgs e)
        {
            CheckBox box = e.Source as CheckBox;
            ONLINE_CHART = (bool)box.IsChecked;
        }
        private void START_BET_Click(object sender, RoutedEventArgs e)
        {
            CheckBox box = e.Source as CheckBox;
            START_BET = (bool)box.IsChecked;
        }
        #endregion

        #region - Trede History -
        private void TAB_CONTROL_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
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
            catch (Exception c)
            {
                ErrorText.Add($"TAB_CONTROL_MouseLeftButtonUp {c.Message}");
            }
        }

        private void History()
        {
            try
            {
                history_order.Clear();
                ConnectHistoryOrder.DeleteAll();
                List<BinanceFuturesOrder> orders = ConnectOrder.Get();
                int i = 0;
                foreach (var it in ConnectOrder.Get())
                {
                    if (it.PositionSide == PositionSide.Long && it.Side == OrderSide.Sell)
                    {
                        ConnectHistoryOrder.Insert(new HistoryOrder(it.CreateTime, it.Symbol, Convert.ToDouble(orders[i - 1].AvgPrice), Convert.ToDouble(it.AvgPrice), Convert.ToDouble(it.Quantity), it.PositionSide));
                    }
                    else if (it.PositionSide == PositionSide.Short && it.Side == OrderSide.Buy)
                    {
                        ConnectHistoryOrder.Insert(new HistoryOrder(it.CreateTime, it.Symbol, Convert.ToDouble(orders[i - 1].AvgPrice), Convert.ToDouble(it.AvgPrice), Convert.ToDouble(it.Quantity), it.PositionSide));
                    }
                    i++;
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"History {c.Message}");
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
            try
            {
                long_open_order_x.Clear();
                long_open_order_y.Clear();
                long_close_order_x.Clear();
                long_close_order_y.Clear();
                short_open_order_x.Clear();
                short_open_order_y.Clear();
                short_close_order_x.Clear();
                short_close_order_y.Clear();
                string symbol = LIST_SYMBOLS.Text;
                if (symbol != "")
                {
                    ConnectOrder.DeleteAll();
                    List<BinanceFuturesOrder> list = Algorithm.Algorithm.InfoOrder(socket, symbol, start_time);
                    foreach (var it in list)
                    {
                        if (it.PositionSide == PositionSide.Long && it.Side == OrderSide.Buy)
                        {
                            long_open_order_x.Add(it.CreateTime.ToOADate());
                            long_open_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                            foreach (var iterator in list)
                            {
                                if (iterator.CreateTime > it.CreateTime && iterator.PositionSide == PositionSide.Long && iterator.Side == OrderSide.Sell && iterator.Quantity == it.Quantity)
                                {
                                    ConnectOrder.Insert(it);
                                    long_close_order_x.Add(iterator.CreateTime.ToOADate());
                                    long_close_order_y.Add(Double.Parse(iterator.AvgPrice.ToString()));
                                    ConnectOrder.Insert(iterator);
                                    break;
                                }
                            }
                        }
                        if (it.PositionSide == PositionSide.Short && it.Side == OrderSide.Sell)
                        {
                            short_open_order_x.Add(it.CreateTime.ToOADate());
                            short_open_order_y.Add(Double.Parse(it.AvgPrice.ToString()));
                            foreach (var iterator in list)
                            {
                                if (iterator.CreateTime > it.CreateTime && iterator.PositionSide == PositionSide.Short && iterator.Side == OrderSide.Buy && iterator.Quantity == it.Quantity)
                                {
                                    ConnectOrder.Insert(it);
                                    short_close_order_x.Add(iterator.CreateTime.ToOADate());
                                    short_close_order_y.Add(Double.Parse(iterator.AvgPrice.ToString()));
                                    ConnectOrder.Insert(iterator);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"InfoOrderAsunc {c.Message}");
            }
        }
        #endregion

        #region - Event Text Changed -
        private void COUNT_CANDLES_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReloadChart();
        }

        private void INTERVAL_TIME_DropDownClosed(object sender, EventArgs e)
        {
            int index = INTERVAL_TIME.SelectedIndex;
            interval_time = IntervalCandles.Intervals()[index].interval;
            timeSpan = new TimeSpan(IntervalCandles.Intervals()[index].timespan);
            ReloadChart();
        }
        #endregion

        #region - Event SMA -

        private void SMA_LONG_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReloadSmaChart();
        }
        #endregion

        #region - Load Chart -

        public List<OHLC> list_candle_ohlc = new List<OHLC>();
        private void LIST_SYMBOLS_DropDownClosed(object sender, EventArgs e)
        {
            ReloadChart();
        }
        private void LoadingCandlesToChart()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (symbol != "")
                {
                    list_candle_ohlc.Clear();
                    List<Candle> list_candles = new List<Candle>();
                    list_candles = ConnectCandle.Get();
                    foreach (Candle it in list_candles)
                    {
                        list_candle_ohlc.Add(new OHLC(it.Open, it.High, it.Low, it.Close, it.DateTime, it.TimeSpan));
                    }
                    InfoOrderAsunc(list_candle_ohlc[0].DateTime);
                    PRICE_SYMBOL = Decimal.Parse(list_candle_ohlc[list_candle_ohlc.Count - 1].Close.ToString());
                    PRICE.Text = PRICE_SYMBOL.ToString();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadingCandlesToChart {c.Message}");
            }
        }

        private void ReloadChart()
        {
            try
            {
                if (socket != null && SMA_LONG > 1 && COUNT_CANDLES > 0 && COUNT_CANDLES > SMA_LONG && COUNT_CANDLES < 500)
                {
                    StopAsync();
                    ConnectCandle.DeleteAll();
                    LoadingCandlesToDB();
                    if (ONLINE_CHART) StartKlineAsync();
                    LoadingCandlesToChart();
                    NewLines(0);
                    LoadingChart();
                    plt.Plot.AxisAuto();
                    plt.Refresh();
                    ReloadSettings();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"ReloadChart {c.Message}");
            }
        }
        private void ReloadSmaChart()
        {
            try
            {
                if (socket != null && SMA_LONG > 1 && COUNT_CANDLES > 0 && COUNT_CANDLES > SMA_LONG && COUNT_CANDLES < 500 && SMA_LONG < list_candle_ohlc.Count - 1)
                {
                    plt.Plot.Remove(sma_long_plot);
                    plt.Plot.Remove(bolinger_lower);
                    plt.Plot.Remove(bolinger_upper);
                    sma_long = candlePlot.GetBollingerBands(SMA_LONG);
                    sma_long_plot = plt.Plot.AddScatterLines(sma_long.xs, sma_long.ys, Color.Cyan, 2, label: SMA_LONG + " minute SMA");
                    sma_long_plot.YAxisIndex = 1;
                    bolinger_lower = plt.Plot.AddScatterLines(sma_long.xs, sma_long.lower, Color.Blue, lineStyle: LineStyle.Dash);
                    bolinger_lower.YAxisIndex = 1;
                    bolinger_upper = plt.Plot.AddScatterLines(sma_long.xs, sma_long.upper, Color.Blue, lineStyle: LineStyle.Dash);
                    bolinger_upper.YAxisIndex = 1;
                    plt.Refresh();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadingCandlesToChart {c.Message}");
            }
        }
        public (double[] xs, double[] ys, double[] lower, double[] upper) sma_long;
        private void LoadingChart()
        {
            try
            {
                if (COUNT_CANDLES > 0)
                {
                    if (SMA_LONG > 1 && SMA_LONG < list_candle_ohlc.Count - 1)
                    {

                        plt.Plot.Remove(candlePlot);
                        plt.Plot.Remove(sma_long_plot);
                        plt.Plot.Remove(bolinger_lower);
                        plt.Plot.Remove(bolinger_upper);
                        plt.Plot.Remove(order_long_open_plot);
                        plt.Plot.Remove(order_long_close_plot);
                        plt.Plot.Remove(order_short_open_plot);
                        plt.Plot.Remove(order_short_close_plot);
                        // Candles
                        candlePlot = plt.Plot.AddCandlesticks(list_candle_ohlc.ToArray());
                        candlePlot.YAxisIndex = 1;
                        // Line open order
                        LoadLineOpen();
                        //// Line stop loss
                        //LoadLineSL();
                        //// Line take profit
                        //LoadLineTP();
                        // Sma
                        sma_long = candlePlot.GetBollingerBands(SMA_LONG);
                        sma_long_plot = plt.Plot.AddScatterLines(sma_long.xs, sma_long.ys, Color.Cyan, 2, label: SMA_LONG + " minute SMA");
                        sma_long_plot.YAxisIndex = 1;
                        // Bolinger lower
                        bolinger_lower = plt.Plot.AddScatterLines(sma_long.xs, sma_long.lower, Color.Blue, lineStyle: LineStyle.Dash);
                        bolinger_lower.YAxisIndex = 1;
                        // Bolinger upper
                        bolinger_upper = plt.Plot.AddScatterLines(sma_long.xs, sma_long.upper, Color.Blue, lineStyle: LineStyle.Dash);
                        bolinger_upper.YAxisIndex = 1;
                        // Orders
                        if (order_long_lines_vertical.Count > 0) foreach (var it in order_long_lines_vertical) plt.Plot.Remove(it);
                        if (order_long_lines_horisontal.Count > 0) foreach (var it in order_long_lines_horisontal) plt.Plot.Remove(it);
                        if (order_short_lines_vertical.Count > 0) foreach (var it in order_short_lines_vertical) plt.Plot.Remove(it);
                        if (order_short_lines_horisontal.Count > 0) foreach (var it in order_short_lines_horisontal) plt.Plot.Remove(it);


                        if (long_close_order_x.Count != 0 && long_close_order_y.Count != 0)
                        {
                            order_long_close_plot = plt.Plot.AddScatter(long_close_order_x.ToArray(), long_close_order_y.ToArray(), color: Color.Orange, lineWidth: 0, markerSize: 10, markerShape: MarkerShape.eks);
                            order_long_close_plot.YAxisIndex = 1;
                            order_long_lines_vertical.Clear();
                            for (int i = 0; i < long_close_order_x.Count; i++)
                            {
                                double[] x = { long_open_order_x[i], long_open_order_x[i] };
                                double[] y = { long_open_order_y[i], long_close_order_y[i] };
                                ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                                scatter.YAxisIndex = 1;
                                order_long_lines_vertical.Add(scatter);
                            }
                            order_long_lines_horisontal.Clear();
                            for (int i = 0; i < long_close_order_x.Count; i++)
                            {
                                double[] x = { long_open_order_x[i], long_close_order_x[i] };
                                double[] y = { long_close_order_y[i], long_close_order_y[i] };
                                ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                                scatter.YAxisIndex = 1;
                                order_long_lines_horisontal.Add(scatter);
                            }
                        }
                        if (long_open_order_x.Count != 0 && long_open_order_y.Count != 0)
                        {
                            order_long_open_plot = plt.Plot.AddScatter(long_open_order_x.ToArray(), long_open_order_y.ToArray(), color: Color.Green, lineWidth: 0, markerSize: 8);
                            order_long_open_plot.YAxisIndex = 1;
                        }
                        if (short_close_order_x.Count != 0 && short_close_order_y.Count != 0)
                        {
                            order_short_close_plot = plt.Plot.AddScatter(short_close_order_x.ToArray(), short_close_order_y.ToArray(), color: Color.Orange, lineWidth: 0, markerSize: 10, markerShape: MarkerShape.eks);
                            order_short_close_plot.YAxisIndex = 1;
                            order_short_lines_vertical.Clear();
                            for (int i = 0; i < short_close_order_x.Count; i++)
                            {
                                double[] x = { short_close_order_x[i], short_close_order_x[i] };
                                double[] y = { short_open_order_y[i], short_close_order_y[i] };
                                ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                                scatter.YAxisIndex = 1;
                                order_short_lines_vertical.Add(scatter);
                            }
                            order_short_lines_horisontal.Clear();
                            for (int i = 0; i < short_close_order_x.Count; i++)
                            {
                                double[] x = { short_open_order_x[i], short_close_order_x[i] };
                                double[] y = { short_open_order_y[i], short_open_order_y[i] };
                                ScatterPlot scatter = plt.Plot.AddScatterLines(x, y, Color.Orange, lineStyle: LineStyle.Dash);
                                scatter.YAxisIndex = 1;
                                order_short_lines_horisontal.Add(scatter);
                            }
                        }
                        if (short_open_order_x.Count != 0 && short_open_order_y.Count != 0)
                        {
                            order_short_open_plot = plt.Plot.AddScatter(short_open_order_x.ToArray(), short_open_order_y.ToArray(), color: Color.DarkRed, lineWidth: 0, markerSize: 8);
                            order_short_open_plot.YAxisIndex = 1;
                        }

                        StartAlgorithm();

                    }
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadingChart {c.Message}");
            }
        }

        #endregion

        #region - Algorithm -

        #region - Open order, close order -

        public decimal open_quantity;
        public long open_order_id = 0;
        public decimal price_open_order;
        public decimal opposite_open_quantity;
        public long opposite_open_order_id = 0;
        private void Bet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (START_BET && USDT_BET >= 20m && PRICE_SYMBOL > 0 && LINE_OPEN < 0 && open_order_id == 0 && LONG)
                {

                    open_quantity = Math.Round(USDT_BET / PRICE_SYMBOL, 1);
                    open_order_id = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, open_quantity, PositionSide.Short);
                    opposite_open_quantity = Math.Round(open_quantity * 2, 1);
                    opposite_open_order_id = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Long);
                    if (open_order_id != 0 && opposite_open_order_id != 0)
                    {
                        start = true;
                        price_open_order = Algorithm.Algorithm.InfoOrderId(socket, symbol, open_order_id);
                        NewLines(Double.Parse(price_open_order.ToString()));
                    }
                    new SoundPlayer(Properties.Resources.wav_2).Play();
                }
                else if (START_BET && USDT_BET >= 20m && PRICE_SYMBOL > 0m && LINE_OPEN > 0 && open_order_id == 0 && SHORT)
                {
                    open_quantity = Math.Round(USDT_BET / PRICE_SYMBOL, 1);
                    open_order_id = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, open_quantity, PositionSide.Long);
                    opposite_open_quantity = Math.Round(open_quantity * 2, 1);
                    opposite_open_order_id = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Short);
                    if (open_order_id != 0 && opposite_open_order_id != 0)
                    {
                        start = true;
                        price_open_order = Algorithm.Algorithm.InfoOrderId(socket, symbol, open_order_id);
                        NewLines(Double.Parse(price_open_order.ToString()));
                    }
                    new SoundPlayer(Properties.Resources.wav_2).Play();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"Bet_Click {c.Message}");
            }
        }
        
        private void CloseOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (LONG && open_order_id != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, open_quantity, PositionSide.Short);
                    open_order_id = 0;
                    open_quantity = 0m;
                    SoundCloseOrder();
                }
                if (LONG && opposite_open_order_id != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Long);
                    opposite_open_order_id = 0;
                    opposite_open_quantity = 0m;
                    SoundCloseOrder();
                }
                if (LONG && order_id_1 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1, PositionSide.Short);
                    order_id_1 = 0;
                    quantity_1 = 0m;
                    SoundCloseOrder();
                }
                if (LONG && order_id_2 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_2, PositionSide.Short);
                    order_id_2 = 0;
                    quantity_2 = 0m;
                    SoundCloseOrder();
                }
                if (LONG && order_id_3 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_3, PositionSide.Short);
                    order_id_3 = 0;
                    quantity_3 = 0m;
                    SoundCloseOrder();
                }
                if (SHORT && open_order_id != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, open_quantity, PositionSide.Long);
                    open_order_id = 0;
                    open_quantity = 0m;
                    SoundCloseOrder();
                }
                if (SHORT && opposite_open_order_id != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Short);
                    opposite_open_order_id = 0;
                    opposite_open_quantity = 0m;
                    SoundCloseOrder();
                }
                if (SHORT && order_id_1 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1, PositionSide.Long);
                    order_id_1 = 0;
                    quantity_1 = 0m;
                    SoundCloseOrder();
                }
                if (SHORT && order_id_2 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_2, PositionSide.Long);
                    order_id_2 = 0;
                    quantity_2 = 0m;
                    SoundCloseOrder();
                }
                if (SHORT && order_id_3 != 0)
                {
                    Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_3, PositionSide.Long);
                    order_id_3 = 0;
                    quantity_3 = 0m;
                    SoundCloseOrder();
                }
                NewLineSLClear();
                start = false;
            }
            catch (Exception c)
            {
                ErrorText.Add($"CloseOrders_Click {c.Message}");
            }
        }
        #endregion
        private void ReloadSettings()
        {
            open_order_id = 0;
            opposite_open_order_id = 0;
            order_id_1 = 0;
            order_id_2 = 0;
            order_id_3 = 0;
            open_quantity = 0m;
            opposite_open_quantity = 0m;
            quantity_1 = 0m;
            quantity_2 = 0m;
            quantity_3 = 0m;
            start = false;
        }
        public decimal quantity_1 = 0m;
        public decimal quantity_2 = 0m;
        public decimal quantity_3 = 0m;
        public decimal price_order_1;
        public decimal price_order_2;
        public decimal price_order_3;
        public long order_id_1 = 0;
        public long order_id_2 = 0;
        public long order_id_3 = 0;
        public bool start = false;
        private void StartAlgorithm()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (ONLINE_CHART && START_BET && start)
                {
                    // Short
                    if (SHORT && order_id_1 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_open_1_y[0])
                    {
                        quantity_1 = Math.Round(open_quantity * 0.75m, 1);
                        order_id_1 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1, PositionSide.Long);
                        SoundOpenOrder();
                        price_order_1 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_1);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1)) / (quantity_1 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if (SHORT && order_id_2 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_open_2_y[0])
                    {
                        quantity_2 = Math.Round(open_quantity * 0.6m, 1);
                        order_id_2 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_2, PositionSide.Long);
                        SoundOpenOrder();
                        price_order_2 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_2);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1) + (quantity_2 * price_order_2)) / (quantity_1 + quantity_2 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if (SHORT && order_id_3 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_open_3_y[0])
                    {
                        quantity_3 = Math.Round(open_quantity * 0.5m, 1);
                        order_id_3 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_3, PositionSide.Long);
                        SoundOpenOrder();
                        price_order_3 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_3);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1) + (quantity_2 * price_order_2) + (quantity_3 * price_order_3)) / (quantity_1 + quantity_2 + quantity_3 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if(SHORT && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_sl_1_y[0])
                    {
                        if (order_id_1 != 0 && order_id_2 != 0 && order_id_3 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1 + quantity_2 + quantity_3, PositionSide.Long);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            quantity_2 = 0m;
                            order_id_2 = 0;
                            quantity_3 = 0m;
                            order_id_3 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                        else if (order_id_1 != 0 && order_id_2 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1 + quantity_2, PositionSide.Long);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            quantity_2 = 0m;
                            order_id_2 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                        else if (order_id_1 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1, PositionSide.Long);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                    }
                    
                    // Long
                    if(LONG && order_id_1 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_open_1_y[0])
                    {
                        quantity_1 = Math.Round(open_quantity * 0.75m, 1); 
                        order_id_1 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1, PositionSide.Short);
                        SoundOpenOrder();
                        price_order_1 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_1);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1)) / (quantity_1 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if (LONG && order_id_2 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_open_2_y[0])
                    {
                        quantity_2 = Math.Round(open_quantity * 0.6m, 1);
                        order_id_2 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_2, PositionSide.Short);
                        SoundOpenOrder();
                        price_order_2 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_2);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1) + (quantity_2 * price_order_2)) / (quantity_1 + quantity_2 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if (LONG && order_id_3 == 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_open_3_y[0])
                    {
                        quantity_3 = Math.Round(open_quantity * 0.5m, 1);
                        order_id_3 = Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_3, PositionSide.Short);
                        SoundOpenOrder();
                        price_order_3 = Algorithm.Algorithm.InfoOrderId(socket, symbol, order_id_3);
                        decimal average = Math.Round(((open_quantity * price_open_order) + (quantity_1 * price_order_1) + (quantity_2 * price_order_2) + (quantity_3 * price_order_3)) / (quantity_1 + quantity_2 + quantity_3 + open_quantity), 6);
                        NewLineSL(Decimal.ToDouble(average));
                    }
                    if (LONG && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_sl_1_y[0])
                    {
                        if (order_id_1 != 0 && order_id_2 != 0 && order_id_3 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1 + quantity_2 + quantity_3, PositionSide.Short);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            quantity_2 = 0m;
                            order_id_2 = 0;
                            quantity_3 = 0m;
                            order_id_3 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                        else if (order_id_1 != 0 && order_id_2 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1 + quantity_2, PositionSide.Short);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            quantity_2 = 0m;
                            order_id_2 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                        else if (order_id_1 != 0)
                        {
                            Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1, PositionSide.Short);
                            quantity_1 = 0m;
                            order_id_1 = 0;
                            SoundCloseOrder();
                            price_open_order = Decimal.Parse(line_sl_1_y[0].ToString());
                            NewLines(line_sl_1_y[0]);
                            NewLineSLClear();
                        }
                    }


                    // Take profit
                    if (SHORT && open_order_id != 0 && opposite_open_order_id != 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_tp_1_y[0])
                    {
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_1, PositionSide.Long);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_2, PositionSide.Long);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, quantity_3, PositionSide.Long);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, open_quantity, PositionSide.Long);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Short);
                        open_quantity = 0m;
                        open_order_id = 0;
                        opposite_open_order_id = 0;
                        opposite_open_quantity = 0m;
                        quantity_1 = 0m;
                        order_id_1 = 0;
                        quantity_2 = 0m;
                        order_id_2 = 0;
                        quantity_3 = 0m;
                        order_id_3 = 0;
                        SoundCloseOrder();
                        start = false;
                        NewLineSLClear();
                    }
                    if (SHORT && open_order_id != 0 && opposite_open_order_id != 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_tp_2_y[0])
                    {
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, open_quantity, PositionSide.Long);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Short);
                        open_quantity = 0m;
                        open_order_id = 0;
                        opposite_open_order_id = 0;
                        opposite_open_quantity = 0m;
                        SoundCloseOrder();
                        start = false;
                        NewLineSLClear();
                    }
                    if(LONG && open_order_id != 0 && opposite_open_order_id != 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close > line_tp_2_y[0])
                    {
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, open_quantity, PositionSide.Short);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Long);
                        open_quantity = 0m;
                        open_order_id = 0;
                        opposite_open_order_id = 0;
                        opposite_open_quantity = 0m;
                        SoundCloseOrder();
                        start = false;
                        NewLineSLClear();
                    }
                    if (LONG && open_order_id != 0 && opposite_open_order_id != 0 && list_candle_ohlc[list_candle_ohlc.Count - 1].Close < line_tp_1_y[0])
                    {
                        decimal quantity_sum = quantity_1 + quantity_2 + quantity_3;
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_1, PositionSide.Short);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_2, PositionSide.Short);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, quantity_3, PositionSide.Short);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Buy, FuturesOrderType.Market, open_quantity, PositionSide.Short);
                        Algorithm.Algorithm.Order(socket, symbol, OrderSide.Sell, FuturesOrderType.Market, opposite_open_quantity, PositionSide.Long);
                        open_quantity = 0m;
                        open_order_id = 0;
                        opposite_open_order_id = 0;
                        opposite_open_quantity = 0m;
                        quantity_1 = 0m;
                        order_id_1 = 0;
                        quantity_2 = 0m;
                        order_id_2 = 0;
                        quantity_3 = 0m;
                        order_id_3 = 0;
                        SoundCloseOrder();
                        start = false;
                        NewLineSLClear();
                    }
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"StartAlgorithm {c.Message}");
            }
        }

        #endregion

        #region - Load Candles -
        private void LoadingCandlesToDB()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (symbol != "")
                {
                    Klines(symbol, klines_count: COUNT_CANDLES);
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"LoadingCandlesToDB {c.Message}");
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
        public Candle candle = new Candle();
        public void StartKlineAsync()
        {
            //StartPriceAsync();
            socket.socketClient.UsdFuturesStreams.SubscribeToKlineUpdatesAsync(LIST_SYMBOLS.Text, interval_time, Message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    candle.DateTime = Message.Data.Data.OpenTime;
                    candle.Open = Decimal.ToDouble(Message.Data.Data.OpenPrice);
                    candle.High = Decimal.ToDouble(Message.Data.Data.HighPrice);
                    candle.Low = Decimal.ToDouble(Message.Data.Data.LowPrice);
                    candle.Close = Decimal.ToDouble(Message.Data.Data.ClosePrice);
                    candle.TimeSpan = timeSpan;
                    if (!ConnectCandle.Update(candle)) ConnectCandle.Insert(candle);
                    PRICE_SYMBOL = Message.Data.Data.ClosePrice;
                    PRICE.Text = PRICE_SYMBOL.ToString();
                    LoadingCandlesToChart();
                    LoadingChart();
                    plt.Refresh();
                }));
            });

        }

        //private void StartPriceAsync()
        //{
        //    socket.socketClient.UsdFuturesStreams.SubscribeToMarkPriceUpdatesAsync(symbol: LIST_SYMBOLS.Text, updateInterval: 1000, Message =>
        //    {
        //        Dispatcher.Invoke(new Action(() =>
        //        {
        //            PRICE_SYMBOL = Message.Data.MarkPrice;
        //        }));
        //    });
        //}
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
                        candle.DateTime = it.OpenTime;
                        candle.Open = Decimal.ToDouble(it.OpenPrice);
                        candle.High = Decimal.ToDouble(it.HighPrice);
                        candle.Low = Decimal.ToDouble(it.LowPrice);
                        candle.Close = Decimal.ToDouble(it.ClosePrice);
                        candle.TimeSpan = timeSpan;
                        ConnectCandle.Insert(candle);
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
                if (CLIENT_NAME != "" && API_KEY != "" && SECRET_KEY != "")
                {
                    if (ConnectTrial.Check(CLIENT_NAME))
                    {
                        string path = System.IO.Path.Combine(Environment.CurrentDirectory, "clients");
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        if (!File.Exists(path + "/" + CLIENT_NAME))
                        {

                            Client client = new Client(CLIENT_NAME, API_KEY, SECRET_KEY);
                            string json = JsonConvert.SerializeObject(client);
                            File.WriteAllText(path + "/" + CLIENT_NAME, json);
                            Clients();
                            CLIENT_NAME = "";
                            API_KEY = "";
                            SECRET_KEY = "";
                        }
                    }
                    else ErrorText.Add("Сlient name not found!");
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
                if (API_KEY != "" && SECRET_KEY != "" && CLIENT_NAME != "")
                {
                    if (ConnectTrial.Check(CLIENT_NAME))
                    {
                        socket = new Socket(API_KEY, SECRET_KEY);
                        Login_Click();
                        CLIENT_NAME = "";
                        API_KEY = "";
                        SECRET_KEY = "";
                    }
                    else ErrorText.Add("Сlient name not found!");
                }
                else if (BOX_NAME.Text != "")
                {
                    string path = System.IO.Path.Combine(Environment.CurrentDirectory, "clients");
                    string json = File.ReadAllText(path + "\\" + BOX_NAME.Text);
                    Client client = JsonConvert.DeserializeObject<Client>(json);
                    if (ConnectTrial.Check(client.ClientName))
                    {
                        socket = new Socket(client.ApiKey, client.SecretKey);
                        Login_Click();
                    }
                    else ErrorText.Add("Сlient name not found!");
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
