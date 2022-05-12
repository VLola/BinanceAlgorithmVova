using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;
using BinanceAlgorithmVova.Binance;
using BinanceAlgorithmVova.Candlestick;
using BinanceAlgorithmVova.ConnectDB;
using BinanceAlgorithmVova.Errors;
using Newtonsoft.Json;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BinanceAlgorithmVova
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket socket;
        public List<string> interval = new List<string>();
        public List<string> list_sumbols_name = new List<string>();
        public List<ListCandles> list_listcandles = new List<ListCandles>();
        public List<FullListCandles> full_list_candles = new List<FullListCandles>();
        public FinancePlot candlePlot;
        public ScatterPlot sma_long_plot;
        public ScatterPlot bolinger2;
        public ScatterPlot bolinger3;
        public List<BinanceFuturesUsdtTrade> history;
        public KlineInterval interval_time;
        public TimeSpan timeSpan;
        public MainWindow()
        {
            InitializeComponent();
            ErrorWatcher();
            Chart();
            Clients();
            LIST_SYMBOLS.ItemsSource = list_sumbols_name;
            EXIT_GRID.Visibility = Visibility.Hidden;
            LOGIN_GRID.Visibility = Visibility.Visible;
            SMA_LONG.TextChanged += SMA_LONG_TextChanged;
            STOP_ASYNC.Click += STOP_ASYNC_Click;
            LIST_SYMBOLS.DropDownClosed += LIST_SYMBOLS_DropDownClosed;
            INTERVAL_TIME.DropDownClosed += INTERVAL_TIME_DropDownClosed;
        }

        private void IntervalTime()
        {
            interval.Add("OneMinute");
            interval.Add("ThreeMinutes");
            interval.Add("FiveMinutes");
            interval.Add("FifteenMinutes");
            interval.Add("ThirtyMinutes");
            interval.Add("OneHour");
            interval.Add("TwoHour");
            interval.Add("FourHour");
            interval.Add("SixHour");
            interval.Add("EightHour");
            interval.Add("TwelveHour");
            interval.Add("OneDay");
            interval.Add("ThreeDay");
            interval.Add("OneWeek");
            interval.Add("OneMonth");
        }
        private void INTERVAL_TIME_DropDownClosed(object sender, EventArgs e)
        {
            interval_time = 0;
        }

        #region - Event SMA -

        private void SMA_LONG_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SMA_LONG.Text != "")
            {
                string text_long = SMA_LONG.Text;
                int sma_indi_long = Convert.ToInt32(text_long);
                if (sma_indi_long > 1)
                {
                    plt.Plot.Remove(sma_long_plot);
                    plt.Plot.Remove(bolinger2);
                    plt.Plot.Remove(bolinger3);
                    sma_long = candlePlot.GetBollingerBands(sma_indi_long);
                    sma_long_plot = plt.Plot.AddScatterLines(sma_long.xs, sma_long.ys, Color.Cyan, 2, label: text_long + " minute SMA");
                    sma_long_plot.YAxisIndex = 1;
                    bolinger2 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.lower, Color.Blue, lineStyle: LineStyle.Dash);
                    bolinger2.YAxisIndex = 1;
                    bolinger3 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.upper, Color.Blue, lineStyle: LineStyle.Dash);
                    bolinger3.YAxisIndex = 1;
                    plt.Refresh();

                    StartAlgorithm();
                }
            }
        }
        #endregion

        #region - Load Chart -

        public List<OHLC> list_candle_ohlc = new List<OHLC>();
        private void LIST_SYMBOLS_DropDownClosed(object sender, EventArgs e)
        {
            StopAsync();
            Connect.DeleteAll();
            LoadCandles();
            if (ONLINE_CHART.IsChecked == true) StartKlineAsync();
            startLoadChart();
        }
        private void startLoadChart()
        {
            try
            {
                string symbol = LIST_SYMBOLS.Text;
                if (symbol != "")
                {
                    list_candle_ohlc.Clear();
                    List<OHLC_NEW> olhc_new = new List<OHLC_NEW>();
                    olhc_new = Connect.Get();
                    foreach (OHLC_NEW it in olhc_new)
                    {
                        list_candle_ohlc.Add(new OHLC(Decimal.ToDouble(it.Open), Decimal.ToDouble(it.High), Decimal.ToDouble(it.Low), Decimal.ToDouble(it.Close), it.DateTime, new TimeSpan(TimeSpan.TicksPerMinute)));
                    }
                    LoadChart();
                }
            }
            catch (Exception c)
            {
                ErrorText.Add($"ComboBox_SelectionChanged {c.Message}");
            }
        }

        public (double[] xs, double[] ys, double[] lower, double[] upper) sma_long;
        private void LoadChart()
        {
            try
            {
                plt.Plot.Remove(candlePlot);
                plt.Plot.Remove(sma_long_plot);
                plt.Plot.Remove(bolinger2);
                plt.Plot.Remove(bolinger3);

                candlePlot = plt.Plot.AddCandlesticks(list_candle_ohlc.ToArray());
                candlePlot.YAxisIndex = 1;

                if (SMA_LONG.Text != "" && SMA_SHORT.Text != "")
                {
                    string text_long = SMA_LONG.Text;
                    int sma_indi_long = Convert.ToInt32(text_long);
                    if (sma_indi_long > 1)
                    {
                        sma_long = candlePlot.GetBollingerBands(sma_indi_long);
                        sma_long_plot = plt.Plot.AddScatterLines(sma_long.xs, sma_long.ys, Color.Cyan, 2, label: text_long + " minute SMA");
                        sma_long_plot.YAxisIndex = 1;
                        bolinger2 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.lower, Color.Blue, lineStyle: LineStyle.Dash);
                        bolinger2.YAxisIndex = 1;
                        bolinger3 = plt.Plot.AddScatterLines(sma_long.xs, sma_long.upper, Color.Blue, lineStyle: LineStyle.Dash);
                        bolinger3.YAxisIndex = 1;
                        StartAlgorithm();
                    }
                }
                plt.Refresh();
            }
            catch (Exception e)
            {
                ErrorText.Add($"LoadChart {e.Message}");
            }
        }
        #endregion

        #region - Algorithm -
        public long order_id = 0;
        decimal quantity;
        public bool start;
        public bool position;
        public bool temp_position;
        public bool start_programm = true;

        #region - Check change position (Long, Short) -
        private void Position()
        {
            try
            {
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close < sma_long.ys[sma_long.ys.Length - 1]) position = false;
                else position = true;
            }
            catch (Exception c)
            {
                ErrorText.Add($"Position {c.Message}");
            }
        }
        private void TempPosition()
        {
            try
            {
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close < sma_long.ys[sma_long.ys.Length - 1]) temp_position = false;
                else temp_position = true;
            }
            catch (Exception c)
            {
                ErrorText.Add($"TempPosition {c.Message}");
            }
        }

        #endregion

        #region - Check SL TP -
        private bool PriceBolingerLongTP()
        {
            try
            {
                double price_bolinger = sma_long.upper[sma_long.upper.Length - 1];
                double price_sma = sma_long.ys[sma_long.ys.Length - 1];
                double price = (price_bolinger - price_sma) / 100 * Double.Parse(BOLINGER_TP.Text) + price_sma;
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close > price) return true;
                else return false;
            }
            catch (Exception c)
            {
                ErrorText.Add($"PriceBolingerLongTP {c.Message}");
                return false;
            }
        }
        private bool PriceBolingerLongSL()
        {
            try
            {
                double price_bolinger = sma_long.lower[sma_long.lower.Length - 1];
                double price_sma = sma_long.ys[sma_long.ys.Length - 1];
                double price = price_sma - (price_sma - price_bolinger) / 100 * Double.Parse(BOLINGER_SL.Text);
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close < price) return true;
                else return false;
            }
            catch (Exception c)
            {
                ErrorText.Add($"PriceBolingerLongSL {c.Message}");
                return false;
            }
        }
        private bool PriceBolingerShortTP()
        {
            try
            {
                double price_bolinger = sma_long.lower[sma_long.lower.Length - 1];
                double price_sma = sma_long.ys[sma_long.ys.Length - 1];
                double price = price_sma - (price_sma - price_bolinger) / 100 * Double.Parse(BOLINGER_TP.Text);
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close < price) return true;
                else return false;
            }
            catch (Exception c)
            {
                ErrorText.Add($"PriceBolingerShortTP {c.Message}");
                return false;
            }
        }
        private bool PriceBolingerShortSL()
        {
            try
            {
                double price_bolinger = sma_long.upper[sma_long.upper.Length - 1];
                double price_sma = sma_long.ys[sma_long.ys.Length - 1];
                double price = (price_bolinger - price_sma) / 100 * Double.Parse(BOLINGER_SL.Text) + price_sma;
                if (list_candle_ohlc[list_candle_ohlc.Count - 1].Close > price) return true;
                else return false;
            }
            catch (Exception c)
            {
                ErrorText.Add($"PriceBolingerShortSL {c.Message}");
                return false;
            }
        }
        #endregion

        private void StartAlgorithm()
        {
            try
            {
                if (START_BET.IsChecked == true && ONLINE_CHART.IsChecked == true && order_id == 0)
                {
                    Position();

                    if (start_programm)
                    {
                        TempPosition();
                        start_programm = false;
                    }

                    if (position == true && temp_position == false) start = true;
                    else if (position == false && temp_position == true) start = true;

                    TempPosition();
                }
                string symbol = LIST_SYMBOLS.Text;


                if (START_BET.IsChecked == true && ONLINE_CHART.IsChecked == true && order_id != 0)
                {
                    bool sl = false;
                    bool tp = false;
                    PositionSide position_side = Algorithm.AlgorithmBet.InfoOrderPositionSide(socket, symbol, order_id);
                    if (position_side == PositionSide.Long)
                    {
                        tp = PriceBolingerLongTP();
                        sl = PriceBolingerLongSL();
                    }
                    else if (position_side == PositionSide.Short)
                    {
                        tp = PriceBolingerShortTP();
                        sl = PriceBolingerShortSL();
                    }
                    if (tp || sl)
                    {
                        order_id = Algorithm.AlgorithmBet.CloseOrder(socket, symbol, order_id, quantity);
                        if (order_id == 0) start_programm = true;
                    }
                }
                if (START_BET.IsChecked == true && ONLINE_CHART.IsChecked == true && start && order_id == 0)
                {
                    quantity = Math.Round(Decimal.Parse(USDT_BET.Text) / Decimal.Parse(PRICE_SUMBOL.Text), 1);

                    order_id = Algorithm.AlgorithmBet.OpenOrder(socket, symbol, quantity, list_candle_ohlc[list_candle_ohlc.Count - 1].Close, sma_long.ys[sma_long.ys.Length - 1]);

                    if (order_id != 0) start = false;
                }
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

        #region - Server Time -
        private void Button_StartConnect(object sender, RoutedEventArgs e)
        {
            GetServerTime();
            GetSumbolName();
        }
        private void GetServerTime()
        {
            try
            {
                var result = socket.futures.ExchangeData.GetServerTimeAsync().Result;
                if (!result.Success) ErrorText.Add("Error GetServerTimeAsync");
                else
                {
                    SERVER_TIME.Text = result.Data.ToShortTimeString();
                }
            }

            catch (Exception e)
            {
                ErrorText.Add($"GetServerTime {e.Message}");
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
            socket.socketClient.UsdFuturesStreams.SubscribeToKlineUpdatesAsync(LIST_SYMBOLS.Text, KlineInterval.FiveMinutes, Message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (ohlc_item.DateTime == Message.Data.Data.OpenTime) Connect.Delete(Id);
                    ohlc_item.DateTime = Message.Data.Data.OpenTime;
                    ohlc_item.Open = Message.Data.Data.OpenPrice;
                    ohlc_item.High = Message.Data.Data.HighPrice;
                    ohlc_item.Low = Message.Data.Data.LowPrice;
                    ohlc_item.Close = Message.Data.Data.ClosePrice;
                    Id = Convert.ToInt32(Connect.Insert(ohlc_item));
                    startLoadChart();
                }));
            });

        }

        private void StartPriceAsync()
        {
            socket.socketClient.UsdFuturesStreams.SubscribeToMarkPriceUpdatesAsync(symbol: LIST_SYMBOLS.Text, updateInterval: 1000, Message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    PRICE_SUMBOL.Text = Message.Data.MarkPrice.ToString();
                }));
            });
        }
        #endregion

        #region - Candles Save -
        public void Klines(string Symbol, DateTime? start_time = null, DateTime? end_time = null, int? klines_count = null)
        {
            try
            {
                var result = socket.futures.ExchangeData.GetKlinesAsync(symbol: Symbol, interval: KlineInterval.FiveMinutes, startTime: start_time, endTime: end_time, limit: klines_count).Result;
                if (!result.Success) ErrorText.Add("Error GetKlinesAsync");
                else
                {
                    foreach (var it in result.Data.ToList())
                    {
                        ohlc_item.DateTime = it.OpenTime;
                        ohlc_item.Open = it.OpenPrice;
                        ohlc_item.High = it.HighPrice;
                        ohlc_item.Low = it.LowPrice;
                        ohlc_item.Close = it.ClosePrice;
                        Id = Convert.ToInt32(Connect.Insert(ohlc_item));
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
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            EXIT_GRID.Visibility = Visibility.Hidden;
            LOGIN_GRID.Visibility = Visibility.Visible;
            socket = null;
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
