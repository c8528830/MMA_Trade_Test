using Sinopac.Shioaji;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrDLL
{
    public class Class1 {
        private static Shioaji ShioajiApi = new Shioaji();
        private static string Username = "N125337804";
        private static string Password = "a0923160649";
        private static string CA_File_Path = "H:\\Project\\StockAutoCrawler\\BillionFutures\\bin\\Debug\\N125337804.pfx";
        public void Run() {
			ShioajiApi.SetEventCallback(CallBack);
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
					order_cond = Sinopac.Shioaji.StockOrderCond.Cash,
					account = ShioajiApi.FutureAccount,
				};
				var tt = futOptOrder.EnumPropToString();
				var _trade = ShioajiApi.PlaceOrder(contract, futOptOrder);
			} catch (Exception ex) { }
		}

		private void CallBack(int arg1, int arg2, string arg3, string arg4) {
			Debug.WriteLine(arg3);
			Debug.WriteLine(arg4);
		}
	}
	internal class FutOptOrder : Sinopac.Shioaji.FutOptOrder { 
	
	}
}
