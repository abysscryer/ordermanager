using System;
using System.Data;
using System.Data.SqlClient;

namespace OrderManager
{
    public class NotifierFactory
    {
        public static OrderChangeNotifier Create(CoinType coinType)
        {
            var command = new SqlCommand();
            var sql = @"SELECT [seq]
                            ,[transaction_srl]
                            ,[userid]
                            ,[krw_amount]
                            ,[krw_receive]
                            ,[krw_fees]
                            ,[btc_amount]
                            ,[btc_receive]
                            ,[btc_fees]
                            ,[btc_price]
                            ,[order_status]
                            ,[classify]
                            ,[in_date]
                            ,[up_date]
                        FROM [dbo].[ORDERBOOK_{0}_ALL] WHERE up_date > @up_date";

            command.CommandText = string.Format(sql, Enum.GetName(typeof(CoinType), coinType)).ToUpper();
            command.Parameters.Add("@up_date", SqlDbType.DateTime);

            return new OrderChangeNotifier(command);
        }
    }
}
