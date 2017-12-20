using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Permissions;

namespace OrderManager
{
    public class OrderChangeNotifier : IDisposable
    {
        #region private variables

        /// <summary>
        /// time stamp
        /// </summary>
        private DateTime timeStamp;
        
        /// <summary>
        /// connection object 
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// event variable
        /// </summary>
        private event EventHandler<SqlNotificationEventArgs> newMessage;

        #endregion

        #region public properties
        
        /// <summary>
        /// connection string property
        /// </summary>
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["youbit"].ConnectionString;
            }
        }

        /// <summary>
        /// connection object property
        /// </summary>
        public SqlConnection Connection
        {
            get
            {
                this.connection = this.connection ?? new SqlConnection(this.ConnectionString);

                return this.connection;
            }
        }

        /// <summary>
        /// command object property
        /// </summary>
        private SqlCommand Command { get; set; }

        /// <summary>
        /// event property
        /// </summary>
        public event EventHandler<SqlNotificationEventArgs> NewMessage
        {
            add { this.newMessage += value; }
            remove { this.newMessage -= value; }
        }

        /// <summary>
        /// lastest changed datetime
        /// </summary>
        public DateTime TimeStamp
        {
            get { return this.timeStamp; }
            set { this.timeStamp = value; }
        }

        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        public OrderChangeNotifier()
        {
            SqlDependency.Start(this.ConnectionString);
        }

        /// <summary>
        /// override constructor to strategy
        /// </summary>
        /// <param name="command"></param>
        public OrderChangeNotifier(SqlCommand command): this()
        {
            this.Command = command;
        }

        /// <summary>
        /// new message event callback
        /// </summary>
        /// <param name="notification"></param>
        public virtual void OnNewMessage(SqlNotificationEventArgs notification)
         {
            if (this.newMessage != null)
                this.newMessage(this, notification);
        }

        /// <summary>
        /// regist dependency
        /// </summary>
        /// <returns></returns>
        public DataTable RegisterDependency()
        {
            if (!DoesUserHavePermission())
                return null;
            
            this.Command.Connection = this.Connection;
            this.Command.Parameters[0].Value = this.TimeStamp;
            
            this.Command.Notification = null;

            SqlDependency dependency = new SqlDependency(this.Command);
            dependency.OnChange += this.dependency_OnChange;

            if (this.Connection.State == ConnectionState.Closed)
                this.Connection.Open();
            try
            {

                DataTable dt = new DataTable();
                dt.Load(this.Command.ExecuteReader(CommandBehavior.CloseConnection));
                return dt;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// dependency change event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {

            SqlDependency dependency = sender as SqlDependency;
            dependency.OnChange -= new OnChangeEventHandler(dependency_OnChange);
            var isChanged = dependency.HasChanges;
            this.OnNewMessage(e);
        }

        /// <summary>
        /// insert new message to destination
        /// </summary>
        /// <param name="msgTitle"></param>
        /// <param name="description"></param>
        public void Insert(string msgTitle, string description)
        {
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand("usp_CreateMessage", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@title", msgTitle);
                    command.Parameters.AddWithValue("@description", description);

                    connection.Open();

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// verify user permission
        /// </summary>
        /// <returns></returns>
        private bool DoesUserHavePermission()
        {
            try
            {
                SqlClientPermission clientPermission = new SqlClientPermission(PermissionState.Unrestricted);

                // will throw an error if user does not have permissions
                clientPermission.Demand();

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// dispose resource
        /// </summary>
        public void Dispose()
        {
            SqlDependency.Stop(this.ConnectionString);
        }

        #endregion
    }
}
