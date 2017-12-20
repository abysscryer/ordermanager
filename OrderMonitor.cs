using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OrderManager
{
    public partial class OrderMonitor : Form
    {
        private OrderChangeNotifier notifier;
        delegate void SetGridCallback(DataTable dt);
        private DataTable dt;

        private void SetGrid(DataTable dt)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.dgvOrders.InvokeRequired)
            {
                SetGridCallback callback = new SetGridCallback(SetGrid);
                this.Invoke(callback, new object[] { dt });
            }
            else
            {
                this.dgvOrders.DataSource = dt;
            }
        }

        public OrderMonitor()
        {
            InitializeComponent();
            InitDataGridView();
            InitNotifier();
        }

        private void InitDataGridView()
        {
            this.dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("seq", typeof(Int64)),
                new DataColumn("price", typeof(Int64)),
                new DataColumn("amount", typeof(Int64)),
                new DataColumn("classify", typeof(string))
            });
        }

        private void InitNotifier()
        {
            this.notifier = NotifierFactory.Create(CoinType.btc);
            this.notifier.NewMessage += new EventHandler<SqlNotificationEventArgs>(OnNotifierChanged);
            this.notifier.TimeStamp = DateTime.Now;
            this.notifier.RegisterDependency();
        }

        /// <summary>
        /// event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNotifierChanged(object sender, SqlNotificationEventArgs e)
        {
            var dt = notifier.RegisterDependency();
            if (dt?.Rows?.Count > 0)
            {
                this.notifier.TimeStamp = (DateTime)dt.Rows[dt.Rows.Count - 1]["up_date"];
                SetGrid(dt);
            }

            

            //this.OnOrderChanged(LoadMessage(dt));
            
        }

        private void LoadOrder(DataTable dt)
        {
            var format = GetTableName(CoinType.btc) + ".[seq] {0} is {1} at {2}";
            if (dt?.Rows?.Count > 0)
            {
                foreach (DataRow drow in dt.Rows)
                {
                    switch (drow["order_status"].ToString())
                    {
                        case "traded":
                            break;
                        case "canceled":
                            break;
                        default:
                            dgvOrders.DataSource = dt; 
                            //Console.WriteLine(string.Format(format, drow["seq"], "created", drow["up_date"]));
                            break;
                    }
                }
            }
        }

        private string GetTableName(CoinType coinType)
        {
            var format = "[dbo].[ORDERBOOK_{0}_ALL]";
            var arg = Enum.GetName(typeof(CoinType), coinType).ToUpper();
            return string.Format(format, arg);
        }
    }
}
