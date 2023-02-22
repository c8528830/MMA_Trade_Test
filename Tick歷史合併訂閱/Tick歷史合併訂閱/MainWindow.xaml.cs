using Sinopac.Shioaji;
using System;
using System.Collections.Generic;
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
	public partial class MainWindow : Window {
		private static Shioaji ShioajiApi = new Shioaji();
		private static string Username = "--------";
		private static string Password = "--------";
		private static string CA_File_Path = "---------";

		public MainWindow() {
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			ShioajiApi.Login(Username, Password);
			ShioajiApi.ca_activate(CA_File_Path, Username, Username);

			try {
				IContract contract = ShioajiApi.Contracts.Options["TXO"]["TXO20230310600P"];
				var futOptOrder = new Sinopac.Shioaji.FutOptOrder() {
					action = Sinopac.Shioaji.Action.Buy,
					price = 0.1,
					quantity = 1,
					price_type = Sinopac.Shioaji.FuturePriceType.LMT,
					order_type = Sinopac.Shioaji.FutureOrderType.ROD,
					order_cond = Sinopac.Shioaji.OCType.Auto,
					account = ShioajiApi.FutureAccount,
				};
				var _trade = ShioajiApi.PlaceOrder(contract, futOptOrder);
				MessageBox.Show(_trade.status.ToString());
			} catch (Exception ex) { }
		}
	}
}
