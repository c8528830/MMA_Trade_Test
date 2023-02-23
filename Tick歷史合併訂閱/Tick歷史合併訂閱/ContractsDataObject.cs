using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tick歷史合併訂閱 {
	public class ContractsDataObject : INotifyPropertyChanged {
		private DateTime _時間;
		private decimal _成交價;
		private int _單量;
		private int _總量;

		public DateTime 時間 { get { return _時間; } set { _時間 = value; NotifyPropertyChanged(nameof(時間)); } }
		public decimal 成交價 { get { return _成交價; } set { _成交價 = value; NotifyPropertyChanged(nameof(成交價)); } }
		public int 單量 { get { return _單量; } set { _單量 = value; NotifyPropertyChanged(nameof(單量)); } }
		public int 總量 { get { return _總量; } set { _總量 = value; NotifyPropertyChanged(nameof(總量)); } }

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
