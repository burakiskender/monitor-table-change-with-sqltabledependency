﻿using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Base;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    [TestClass]
    public class TransactionTestSqlServer2 : SqlTableDependencyBaseTest
    {
        private const string TableName = "AfModel";
        private int _counter;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText =
                        $"CREATE TABLE [{TableName}]( " +
                        "[Id][int] IDENTITY(1, 1) NOT NULL, " +
                        "[First Name] [nvarchar](50) NOT NULL, " +
                        "[Second Name] [nvarchar](50) NOT NULL, " +
                        "[Born] [datetime] NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestCategory("SqlServer")]
        [TestMethod]
        public void EventForAllColumnsTest()
        {
            SqlTableDependency<EventForAllColumnsTestSqlServerModel> tableDependency = null;
            string naming;

            try
            {
                var mapper = new ModelToTableMapper<EventForAllColumnsTestSqlServerModel>();
                mapper.AddMapping(c => c.Name, "FIRST name").AddMapping(c => c.Surname, "Second Name");

                tableDependency = new SqlTableDependency<EventForAllColumnsTestSqlServerModel>(ConnectionStringForTestUser, TableName, mapper);
                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                var t = new Task(ModifyTableContent);
                t.Start();

                Thread.Sleep(500 * 60 * 1);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 2);
            Assert.IsTrue(base.AreAllDbObjectDisposed(naming));
            Assert.IsTrue(base.CountConversationEndpoints(naming) == 0);
        }

        private void TableDependency_Changed(object sender, RecordChangedEventArgs<EventForAllColumnsTestSqlServerModel> e)
        {
            _counter++;
        }

        private static void ModifyTableContent()
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var sqlConnection = new SqlConnection(ConnectionStringForTestUser))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('AAAA', 'aaaa'); DELETE FROM [{TableName}];";
                        sqlCommand.ExecuteNonQuery();

                        transactionScope.Complete();
                    }
                }
            }
        }
    }
}