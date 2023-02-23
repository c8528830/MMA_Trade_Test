using Newtonsoft.Json;
using Sinopac.Shioaji;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tick歷史合併訂閱 {
	/// <summary>
	/// MainWindow.xaml 的互動邏輯
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged {
		private static Shioaji ShioajiAPI;
		private static string Username = "-----";
		private static string Password = "-----";
		private static string CA_File_Path = "-----";
		private static bool IsLogin = false;
		private static bool IsStop = false;
		private static bool MorningOrNight = true; //早盤或夜盤
		private ObservableCollection<ContractsDataObject> 歷史TickList = new ObservableCollection<ContractsDataObject>();
		private ObservableCollection<ContractsDataObject> 實時TickList = new ObservableCollection<ContractsDataObject>();
		private ObservableCollection<ContractsDataObject> TradeList = new ObservableCollection<ContractsDataObject>();

		public ConcurrentQueue<ContractsDataObject> 實時TickQueuet = new ConcurrentQueue<ContractsDataObject>();

		private int _歷史總量 = 0;
		private int _歷史真實總量 = 0;
		private int _實時總量 = 0;
		private int _歷史筆數 = 0;
		private int _實時筆數 = 0;
		public int 歷史總量 { get { return _歷史總量; } set { _歷史總量 = value; NotifyPropertyChanged(nameof(歷史總量)); } }
		public int 歷史真實總量 { get { return _歷史真實總量; } set { _歷史真實總量 = value; NotifyPropertyChanged(nameof(歷史真實總量)); } }
		public int 實時總量 { get { return _實時總量; } set { _實時總量 = value; NotifyPropertyChanged(nameof(實時總量)); } }
		public int 歷史筆數 { get { return _歷史筆數; } set { _歷史筆數 = value; NotifyPropertyChanged(nameof(歷史筆數)); } }
		public int 實時筆數 { get { return _實時筆數; } set { _實時筆數 = value; NotifyPropertyChanged(nameof(實時筆數)); } }


		private Task WorkTask { get; set; }

		public MainWindow() {
			InitializeComponent();
			Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			DataGrid_History.ItemsSource = 歷史TickList;
			DataGrid_Live.ItemsSource = 實時TickList;
			var columeDic = DataGrid_History.Columns.ToDictionary(k => k.Header);
			((DataGridTextColumn)columeDic["時間"]).Binding.StringFormat = "yyyy/MM/dd HH:mm:ss:fff";
			columeDic = DataGrid_Live.Columns.ToDictionary(k => k.Header);
			((DataGridTextColumn)columeDic["時間"]).Binding.StringFormat = "yyyy/MM/dd HH:mm:ss:fff";

			Label_History總量.DataContext = this;
			Label_History真實總量.DataContext = this;
			Label_History筆數.DataContext = this;
			Label_Live總量.DataContext = this;
			Label_Live筆數.DataContext = this;
		}

		private async void UpdateStatus() {
			await Task.Run(() => {
				while (true) {
					try {
						if (IsLogin) {
							DateTime startDt = DateTime.Now;
							ShioajiAPI.UpdateStatus();
							Debug.WriteLine($"UpdateStatus: {(DateTime.Now - startDt).TotalMilliseconds} ms.");
						}
					} catch (Exception ex) { }
					System.Threading.Thread.Sleep(200);
				}
			});
		}

		private void Button_Login_Click(object sender, RoutedEventArgs e) {
			if (WorkTask != null) return;
			ShioajiAPI = new Shioaji();
			ShioajiAPI.SetQuoteCallback_v1(QuoteCallBack);
			WorkTask = Work();
		}
		private async Task Work() {
			await Task.Run(async () => {
				bool isSubscribe = false;
				bool isTickHistoryRun = false;
				while (IsStop == false) {

					try {
						//執行登入
						if (IsLogin == false) {
							isSubscribe = false;
							isTickHistoryRun = false;
							ShioajiAPI.Login(Username, Password);
							//ShioajiApi.ca_activate(CA_File_Path, Username, Username);
						}
						IsLogin = true;
					} catch (Exception ex) {
						isSubscribe = false;
						Debug.WriteLine($"[Error] [Login] {ex.Message}.");
						System.Threading.Thread.Sleep(10000);
						continue;
					}

					try {
						//訂閱台指期2023/03
						//先訂閱後取歷史 中間間隔數據才不會遺失
						if (isSubscribe == false) {
							IContract contract = ShioajiAPI.Contracts.Futures["TXF"]["TXF202303"];
							ShioajiAPI.Subscribe(
								contract,
								QuoteType.tick,
								version: QuoteVersion.v1
							);
							isSubscribe = true;
						}
					} catch (Exception ex) {
						IsLogin = false;
						Debug.WriteLine($"[Error] [Subscribe] {ex.Message}.");
						System.Threading.Thread.Sleep(10000);
						continue;
					}

					while (實時TickQueuet.Count == 0) {
						Debug.WriteLine("等待報價回傳");
						await Task.Delay(1000);
					}
					await Task.Delay(5000);

					try {
						//取得歷史 Ticks
						if (isTickHistoryRun == false) {
							IContract contract = ShioajiAPI.Contracts.Futures["TXF"]["TXF202303"];
							Dispatcher.Invoke(歷史TickList.Clear);
							var historyTickList = 取得歷史Tick(contract, DateTime.Now.AddHours(-6).ToString("yyyy-MM-dd"), MorningOrNight);
							Dispatcher.Invoke(() => historyTickList.ForEach(v => 歷史TickList.Add(v)));
							isTickHistoryRun = true;
						}
					} catch (Exception ex) {
						IsLogin = false;
						Debug.WriteLine($"[Error] [History] {ex.Message}.");
						System.Threading.Thread.Sleep(10000);
						continue;
					}

					var historyLastDt = 歷史TickList.Last().時間;
					while (實時TickQueuet.TryPeek(out var 實時Tick)) {
						Dispatcher.Invoke(() => 實時TickList.Add(實時Tick));
						實時TickQueuet.TryDequeue(out 實時Tick);
					}
					歷史總量 = 歷史TickList.Last().總量;
					歷史真實總量 = 歷史TickList.Sum(v => v.單量);
					歷史筆數 = 歷史TickList.Count();
					實時總量 = 實時TickList.Last().總量;
					實時筆數 = 實時TickList.Count();
					System.Threading.Thread.Sleep(1000);
				}
			});
			WorkTask = null;
			IsLogin = false;
		}
		private List<ContractsDataObject> 取得歷史Tick(IContract contract, string todayStr, bool morningOrNight) {
			List<ContractsDataObject> result = new List<ContractsDataObject>();
			var sn = (Sinopac.Shioaji.Ticks)ShioajiAPI.Ticks(contract, todayStr);
			if (sn.ts.Count > 0) {
				var ary = sn.ts.Select((item, index) => new ContractsDataObject() {
					時間 = ConvertToDateTime(((int)(((decimal)item) * 0.000000001M))).AddHours(-8),
					成交價 = (decimal)sn.close[index],
					單量 = (int)sn.volume[index],
				}).ToArray();
				ary = morningOrNight ? ary.Where(v => v.時間.Hour >= 8 && v.時間.Hour <= 13).ToArray() : ary.Where(v => v.時間.Hour >= 15 || v.時間.Hour <= 5).ToArray();
				var 總量 = 0;
				foreach (var item in ary) {
					總量 += item.單量;
					item.總量 = 總量;
					result.Add(item);
				}
			}
			return result;
		}

		private void QuoteCallBack(Exchange arg1, dynamic quote) {
			if (quote is TickFOPv1) {
				if (((TickFOPv1)quote).simtrade == true) return;
				{
					var data = (TickFOPv1)quote;
					實時TickQueuet.Enqueue(new ContractsDataObject() {
						時間 = ConvertToDateTime(data.datetime),
						成交價 = data.close,
						單量 = (int)(data.volume),
						總量 = (int)(data.total_volume),
					});
				}
			}
		}

		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		public DateTime ConvertToDateTime(int unixTimeStamp) {
			return ConvertToDateTime((uint)unixTimeStamp);
		}
		public DateTime ConvertToDateTime(uint unixTimeStamp) {
			DateTime dt = UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
			return dt;
		}
		public DateTime ConvertToDateTime(string dateString, string dateTimeFormat = null) {
			DateTime dt;
			if (String.IsNullOrEmpty(dateTimeFormat) == false) {
				if (DateTime.TryParseExact(dateString, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) == false) {
					dt = Convert.ToDateTime(dateString);
				}
			} else {
				dt = Convert.ToDateTime(dateString);
			}
			return dt;
		}
		Trade _trade;

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			var startDt = DateTime.Now;
			ShioajiAPI.CancelOrder(_trade);
			Debug.WriteLine($"CancelOrder: {(DateTime.Now - startDt).TotalMilliseconds} ms.");
		}

		private void Button_Click_2(object sender, RoutedEventArgs e) {
			var startDt = DateTime.Now;
			IContract contract = ShioajiAPI.Contracts.Options["TXO"]["TXO20230310600P"];
			var futOptOrder = new FutOptOrder() {
				price = 0.1,
				quantity = 1,
				action = Sinopac.Shioaji.Action.Buy,
				price_type = FuturePriceType.LMT,
				order_type = FutureOrderType.ROD,
				octype = OCType.Auto,
			};
			_trade = ShioajiAPI.PlaceOrder(contract, futOptOrder);
			if (_trade != null) {
				//TradeList.Add(_trade);
			}
			Debug.WriteLine($"PlaceOrder: {(DateTime.Now - startDt).TotalMilliseconds} ms.");
		}

		private void Button_Click_3(object sender, RoutedEventArgs e) {
			var startDt = DateTime.Now;
			var trades = ShioajiAPI.ListTrades();
			Debug.WriteLine($"GetTrades: {(DateTime.Now - startDt).TotalMilliseconds} ms.");
			Debug.WriteLine(JsonConvert.SerializeObject(trades.Where(v => v.status.status != "Cancelled" && v.status.status != "Inactive").Select(v => v.status)));
		}

		private async void Button_Click_4(object sender, RoutedEventArgs e) {
			await Task.Run(() => {
				TradeList[0].成交價 = 99;
				BindingOperations.EnableCollectionSynchronization(TradeList, TradeList[0]);
				TradeList.Add(TradeList[0]);
			});
			//BindingOperations.EnableCollectionSynchronization(TradeList, TradeList[0]);
		}


		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyPropertyChanged(string propertyName) {
			if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
		}
	}

	
}
