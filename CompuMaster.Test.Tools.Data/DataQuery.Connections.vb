Imports NUnit.Framework

Namespace CompuMaster.Test.Data.DataQuery

    <TestFixture(Category:="DB Connections")> Public Class Connections

#If Not NET_1_1 Then
        <Test(), Obsolete> Public Sub DataException()
            Dim ex As New CompuMaster.Data.DataQuery.AnyIDataProvider.DataException(Nothing, Nothing)
            Assert.Pass("No exception thrown - all is perfect :-)")
        End Sub

        <Test()> Public Sub CloseAndDisposeConnectionNpgSql()
            Dim conn As System.Data.IDbConnection
            conn = New Npgsql.NpgsqlConnection
            conn.Dispose()
            CompuMaster.Data.DataQuery.AnyIDataProvider.CloseConnection(conn) 'should not throw an exception
            CompuMaster.Data.DataQuery.AnyIDataProvider.CloseAndDisposeConnection(conn) 'should not throw an exception
            Assert.AreEqual(System.Data.ConnectionState.Closed, conn.State)
            Assert.Pass("No exception thrown - all is perfect :-)")
        End Sub
        <Test()> Public Sub CloseAndDisposeConnectionMsSql()
            Dim conn As System.Data.IDbConnection
            conn = New System.Data.SqlClient.SqlConnection
            conn.Dispose()
            CompuMaster.Data.DataQuery.AnyIDataProvider.CloseConnection(conn) 'should not throw an exception
            CompuMaster.Data.DataQuery.AnyIDataProvider.CloseAndDisposeConnection(conn) 'should not throw an exception
            Assert.AreEqual(System.Data.ConnectionState.Closed, conn.State)
            Assert.Pass("No exception thrown - all is perfect :-)")
        End Sub
#End If

        <Test()> Public Sub LoadAndUseConnectionFromExternalAssembly()
            'TODO: Unabh�ngigkeit von spezifischer Workstation mit Lw. G:
            'TODO: Sinnvolles Testing
        End Sub

        <Test()> Public Sub ReadMsAccessDatabase()
            Dim TestFile As String = AssemblyTestEnvironment.TestFileAbsolutePath("testfiles\test_for_msaccess.mdb")
            Dim MyConn As IDbConnection = CompuMaster.Data.DataQuery.Connections.MicrosoftAccessConnection(TestFile)
            Dim MyCmd As IDbCommand = MyConn.CreateCommand()
            MyCmd.CommandType = CommandType.Text
            MyCmd.CommandText = "SELECT * FROM TestData"
            Dim table As DataTable = CompuMaster.Data.DataQuery.FillDataTable(MyCmd, CompuMaster.Data.DataQuery.Automations.AutoOpenAndCloseAndDisposeConnection, "testdata")
            Assert.AreEqual(3, table.Rows.Count, "Row count for table TestData")
        End Sub

        <Test()> Public Sub EnumerateTablesAndViewsInOleDbDataSource()
            Dim TestFile As String = AssemblyTestEnvironment.TestFileAbsolutePath("testfiles\test_for_msaccess.mdb")
            Dim conn As IDbConnection = CompuMaster.Data.DataQuery.Connections.MicrosoftAccessConnection(TestFile)
            'If CType(conn, Object).GetType Is GetType(System.Data.OleDb.OleDbConnection) Then
            Try
                conn.Open()
                Dim tables As CompuMaster.Data.DataQuery.Connections.OleDbTableDescriptor() = CompuMaster.Data.DataQuery.Connections.EnumerateTablesAndViewsInOleDbDataSource(CType(conn, System.Data.OleDb.OleDbConnection))
                Dim tableNames As New System.Collections.Generic.List(Of String)
                Dim TestDataTable As CompuMaster.Data.DataQuery.Connections.OleDbTableDescriptor = Nothing
                For Each table As CompuMaster.Data.DataQuery.Connections.OleDbTableDescriptor In tables
                    Console.WriteLine(table.ToString)
                    tableNames.Add(table.ToString)
                    If table.ToString = "[TestData]" Then
                        TestDataTable = table
                    End If
                Next
                Assert.AreNotEqual(0, tables.Length)
                Assert.IsNotNull(TestDataTable, "Table TestData not found")
                Assert.AreEqual("TestData", TestDataTable.TableName)
                Assert.AreEqual(Nothing, TestDataTable.SchemaName)
                Assert.AreEqual("[TestData]", TestDataTable.ToString)
            Finally
                CompuMaster.Data.DataQuery.CloseAndDisposeConnection(conn)
            End Try
            'End If
        End Sub

        <Test()> Public Sub ReadMsAccessDatabaseEnumeratedTable()
            Dim TestFile As String = AssemblyTestEnvironment.TestFileAbsolutePath("testfiles\test_for_msaccess.mdb")
            Dim MyConn As IDbConnection = CompuMaster.Data.DataQuery.Connections.MicrosoftAccessConnection(TestFile)
            Dim table As DataTable
            Try
                MyConn.Open()
                Dim MyCmd As IDbCommand = MyConn.CreateCommand()
                Dim tableIdentifier As String = CompuMaster.Data.DataQuery.Connections.EnumerateTablesAndViewsInOleDbDataSource(MyConn)(0).ToString
                MyCmd.CommandType = CommandType.Text
                MyCmd.CommandText = "SELECT * FROM " & tableIdentifier
                table = CompuMaster.Data.DataQuery.FillDataTable(MyCmd, CompuMaster.Data.DataQuery.Automations.AutoOpenAndCloseAndDisposeConnection, tableIdentifier)
                Console.WriteLine(CompuMaster.Data.DataTables.ConvertToPlainTextTable(table))
            Finally
                CompuMaster.Data.DataQuery.CloseAndDisposeConnection(MyConn)
            End Try
            Assert.AreNotEqual(0, table.Columns.Count, "Column count for random, enumerated table")
        End Sub

    End Class

End Namespace