﻿Imports System.IO
Imports System.Xml.Serialization
Imports MySql.Data.MySqlClient

<XmlRoot("manager")> Public Class manager
    Implements IDisposable
    Public Const MySQLConfigFileExtension = ".mysql.config.xml"

    <XmlIgnore> Public Connection As New MySqlConnection
        Public Server As String, DatabaseName As String, UserID As String, Password As String, Port As String

        Public ReadOnly Property HasSelectedDatabase As Boolean
            Get
                Return Not DatabaseName = ""
            End Get
        End Property

        Public WriteOnly Property OpenConnection As Boolean
            Set(value As Boolean)
                If value Then
                    If Not Connection.State = ConnectionState.Open Then Connection.Open()
                Else
                    If Connection.State = ConnectionState.Open Then Connection.Close()
                End If
            End Set
        End Property

        Public Function CloneConnection() As MySqlConnection
            Dim con As MySqlConnection = Nothing
            con = Connection.Clone
            Return con
        End Function

        Public Function SetupConnection(Optional _connectionString As String = Nothing) As Boolean
            Try
                Dim connectionString As String = "Server={0};Uid={1};Pwd={2};port={3}{4};Convert Zero Datetime=True;command Timeout=20000;SslMode=None"
                If _connectionString IsNot Nothing And _connectionString IsNot "" Then
                    connectionString = _connectionString
                End If

                Connection = New MySqlConnection
                Connection.ConnectionString = String.Format(connectionString _
                                                             , Server, UserID, Password, Port, IIf(DatabaseName = "", "", ";database=" & DatabaseName))
                OpenConnection = True
                Return True
            Catch ex As Exception
                MsgBox(ex.Message)
                Return False
            End Try
        End Function

        Public Sub SetupConnection(ByRef con As MySqlConnection)
            Try
                con = New MySqlConnection
                con.ConnectionString = String.Format("Server={0};Uid={1};Pwd={2};port={3}{4};Convert Zero Datetime=True;command Timeout=20000;SslMode=None;" _
                                                     , Server, UserID, Password, Port, IIf(DatabaseName = "", "", ";database=" & DatabaseName))
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End Sub

        Public Sub SetupConnection(_databaseName As String, ByRef con As MySqlConnection)
            Try
                con = New MySqlConnection
                con.ConnectionString = String.Format("Server={0};Uid={1};Pwd={2};port={3};database={4};Convert Zero Datetime=True;command Timeout=20000;SslMode=None;" _
                                                     , Server, UserID, Password, Port, _databaseName)
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End Sub

        Public Sub Close()
            OpenConnection = False
            Connection.Dispose()
            GC.Collect()
            GC.WaitForPendingFinalizers()
        End Sub

        Public Function ExecuteDataReader(sql As String) As MySqlDataReader
            OpenConnection = True
            Return New MySqlCommand(sql, Connection).ExecuteReader
        End Function

        Public Shared Function ExecuteDataReader(sql As String, _con As MySqlConnection) As MySqlDataReader
            Return New MySqlCommand(sql, _con).ExecuteReader
        End Function

        Public Sub ExecuteQuery(ByVal sql As String)
            Try
                OpenConnection = True
                Dim cmd As MySqlCommand
                cmd = New MySqlCommand(sql, Connection)
                cmd.ExecuteNonQuery()
                OpenConnection = False
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End Sub

        Public Shared Sub ExecuteQuery(ByVal sql As String, _con As MySqlConnection)
            Dim cmd As MySqlCommand
            cmd = New MySqlCommand(sql, _con)
            cmd.ExecuteNonQuery()
        End Sub

        Public Function ExecuteScalar(ByVal sql As String) As String
            Dim res As String = ""
            Dim cmd As MySqlCommand
            cmd = New MySqlCommand(sql, Connection)
            res = cmd.ExecuteScalar()
            Return res
        End Function

    Public Shared Sub AlterTablename(tablename As String, newTablename As String, con As MySqlConnection)
        manager.ExecuteDataReader("Alter Table `" & tablename & "` Rename To `" & newTablename & "`", con)
    End Sub

    Public Sub AlterTablename(tablename As String, newTablename As String)
        AlterTablename(tablename, newTablename, Connection)
    End Sub

        Public Function ToDT(sql As String) As DataTable
            Dim dt As DataTable = New DataTable()
            Dim dAdapter As New MySqlDataAdapter(sql, Connection)
            dAdapter.Fill(dt)
            Return dt
        End Function

        Public Shared Function ToDT(sql As String, _con As MySqlConnection) As DataTable
            Dim dt As DataTable = New DataTable()
            Dim dAdapter As New MySqlDataAdapter(sql, _con)
            dAdapter.Fill(dt)
            Return dt
        End Function

        Public Function CreateSchema(ByVal dbname As String) As String
            Dim res As String = ""
            res = "CREATE SCHEMA " & dbname & ";"
            Return res
        End Function

        Public Sub TryCreateTable(ByVal tbl As String, ByVal flds As String(), Optional overwrite As Boolean = False)
            If CheckTable(tbl) Then
                If Not overwrite Then Exit Sub
                ExecuteQuery("DROP TABLE `" & tbl & "`")
            End If

            CreateTable(tbl, flds)
        End Sub

        Public Sub CreateTable(ByVal tbl As String, ByVal flds As String())
        CreateTable(tbl, flds, Connection)
    End Sub
        Public Shared Sub CreateTable(ByVal tbl As String, ByVal flds As String(), ByVal con As MySqlConnection)
            Dim qry As String = String.Format("CREATE TABLE `{0}`(", tbl)
            For i As Integer = 0 To flds.Length - 1
                qry &= IIf(i = 0, flds(i), "," & flds(i))
            Next
            qry &= ")"

        ExecuteQuery(qry, con)
    End Sub

        'SQL COMMANDS
        Public Function CheckTable(tbl As String) As Boolean
        Return CheckTable(DatabaseName, tbl, Connection)
    End Function
        Public Shared Function CheckTable(DatabaseName As String, tbl As String, _con As MySqlConnection) As Boolean
        Return GetTables(DatabaseName, _con).Contains(tbl.ToLower)
    End Function

        Public Shared Function GetTables(DatabaseName As String, _con As MySqlConnection) As List(Of String)
            Dim lst As New List(Of String)
        Using rdr As MySqlDataReader = ExecuteDataReader("SELECT DISTINCT TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE  TABLE_SCHEMA='" & DatabaseName & "';", _con)
            While rdr.Read
                lst.Add(rdr.Item(0).ToString.ToLower)
            End While
        End Using
        Return lst
        End Function
        Public Function GetTables() As List(Of String)
        Return GetTables(DatabaseName, Connection)
    End Function
        'Public Shared Sub CreateTable(ByVal tbl As String, ByVal flds As String(), ByVal con As MySqlConnection)
        '    Dim qry As String = String.Format("CREATE TABLE `{0}`(", tbl)
        '    For i As Integer = 0 To flds.Length - 1
        '        qry &= IIf(i = 0, flds(i), "," & flds(i))
        '    Next
        '    qry &= ")"

        '    MysqlConfiguration.ExecuteNonQuery(qry, con)
        'End Sub

        Public Shared Function CreateTableGenerator(ByVal table As String, ByVal fields As String(), ByVal dataTypes As String(), ByVal withPrimary As Boolean, Optional ByVal type As String = "") As String
            Dim res As String = ""
            res = "CREATE TABLE " & table & " ("

            If withPrimary Then
                res += fields(0) & " " & type & ", "

                For i As Integer = 1 To fields.Length - 1
                    res += fields(i) & " " & dataTypes(i) & ", "
                Next

                res += "PRIMARY KEY (" & fields(0) & "));"
            Else
                res += fields(0) & " " & dataTypes(0) & ","

                For i As Integer = 1 To fields.Length - 1
                    If i = fields.Length - 1 Then
                        res += fields(i) & " " & dataTypes(i) & ");"
                    Else
                        res += fields(i) & " " & dataTypes(i) & ","
                    End If
                Next

            End If

            Return res
        End Function

        Public Sub Insert(ByVal schema As String, ByVal tbl As String, ByVal fld As String(), ByVal val As Object())
        Insert(schema, tbl, fld, val, Connection)
    End Sub

        Public Sub Insert(ByVal tbl As String, ByVal fld As String(), ByVal val As Object())
        Insert(DatabaseName, tbl, fld, val, Connection)
    End Sub

        Public Shared Sub Insert(schema As String, ByVal table As String, ByVal fields As String(), ByVal values As Object(), con As MySqlConnection)
            Dim qry As String = String.Format("INSERT INTO `{0}` (", table)
            Dim valtype As String = ""

            For i As Integer = 0 To fields.Length - 1
                Dim f As String = fields(i)
                If f = fields(0) Then
                    qry &= String.Format("`{0}`", f)
                Else
                    qry &= String.Format(",`{0}`", f)
                End If
            Next

            qry &= ") VALUES("

            For i As Integer = 0 To values.Length - 1
                Dim v = values(i)
                valtype = TypeName(v)
                Select Case valtype
                    Case "String"
                        qry &= String.Format("'{0}',", v)
                    Case "Date"
                        qry &= String.Format("'{0}',", Date.Parse(v).ToString("yyyy-MM-dd HH:mm:ss"))
                    Case Else
                        qry &= String.Format("{0},", v)
                End Select
            Next
            qry = System.Text.RegularExpressions.Regex.Replace(qry, ",+$", "")
            qry &= ")"


        ExecuteQuery(qry, con)
    End Sub

        Public Shared Function GenerateSearch(ByVal columns() As String, ByVal table As String, ByVal ID As String, ByVal IDval As String) As String
            Dim sql As String = "Select "

            For i As Integer = 0 To columns.Length - 1
                If i = columns.Length - 1 Then
                    sql += "[" & columns(i) & "] "
                Else
                    sql += "[" & columns(i) & "], "
                End If
            Next

            sql += " from " & table & " where " & ID & " = '" & IDval & "'"

            Return sql.Trim
        End Function

        Public Sub Update(schema As String, ByVal tbl As String, ByVal fld As String(), ByVal val As Object(), ByVal condition As SQLCondition())
        Update(schema, tbl, fld, val, condition, Connection)
    End Sub
        Public Sub Update(ByVal tbl As String, ByVal fld As String(), ByVal val As Object(), ByVal condition As SQLCondition())
        Update(DatabaseName, tbl, fld, val, condition, Connection)
    End Sub
        Public Shared Sub Update(schema As String, ByVal table As String, ByVal fields As String(), ByVal values As Object(), ByVal condition As SQLCondition(), con As MySqlConnection)
            Dim qry As String = String.Format("UPDATE {0} SET ", table)
            Dim valtype As String = ""

            If fields.Length = values.Length Then
                For i As Integer = 0 To fields.Length - 1
                    Dim v = values(i)
                    valtype = TypeName(v)
                    Select Case valtype
                        Case "String"
                            qry &= String.Format("`{0}`='{1}',", fields(i), v)
                        Case "Date"
                            qry &= String.Format("`{0}`='{1}',", fields(i), Date.Parse(v).ToString("yyyy-MM-dd HH:mm:ss"))
                        Case Else
                            qry &= String.Format("`{0}`={1},", fields(i), v)
                    End Select
                Next
            End If

            qry = System.Text.RegularExpressions.Regex.Replace(qry, ",+$", "")
            If Not condition Is Nothing Then
                qry &= " WHERE"
                For i As Integer = 0 To condition.Length - 1
                    qry &= condition(i).ToString
                Next
            End If


        ExecuteQuery(qry, con)
    End Sub

        Public Shared Function Update(ByVal table As String, ByVal fields As String(), ByVal values As String(), ByVal ID As String, ByVal IDVal As String)
            Dim sql As String = ""

            sql = "UPDATE " & table & " SET "
            For i As Integer = 1 To values.Length - 1
                If values(i) Is Nothing Then values(i) = ""
                If i < values.Length - 1 Then
                    sql += fields(i) & "='" & values(i).Replace("'", "''") & "',"
                Else
                    sql += fields(i) & "='" & values(i).Replace("'", "''") & "'"
                End If
            Next
            sql += " WHERE " & ID & " = '" & IDVal & "'"


            Return sql
        End Function

        Public Function SchemaExist(ByVal dbname As String) As Boolean
            Dim cmd As MySqlCommand = New MySqlCommand("SELECT IF(EXISTS (SELECT SCHEMA_NAME " &
                 "FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '" & dbname & "'), 'Y','N')", Connection)
            'cmd.Parameters.AddWithValue("@DbName", dbname)

            Dim exists As String = cmd.ExecuteScalar().ToString()
            Return If(exists = "Y", True, False)
        End Function

        Public Shared Function SchemaExist(ByVal dbname As String, con As MySqlConnection) As Boolean
            Dim cmd As MySqlCommand = New MySqlCommand("SELECT IF(EXISTS (SELECT SCHEMA_NAME " &
                 "FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '" & dbname & "'), 'Y','N')", con)
            'cmd.Parameters.AddWithValue("@DbName", dbname)

            Dim exists As String = cmd.ExecuteScalar().ToString()
            Return If(exists = "Y", True, False)
        End Function

        Public Function TableExist(ByVal table As String) As Boolean
            Dim restrictions(4) As String
            restrictions(2) = table

            Dim dbTbl As DataTable = Connection.GetSchema("Tables", restrictions)
            If dbTbl.Rows.Count = 0 Then
                Return False
            Else
                Return True
            End If
        End Function

#Region "Other Methods"
        Public Shared Function OpenEditor(_config As manager, Optional appLocation As String = "", Optional appName As String = "Local") As manager
            Dim filePath As String = Path.Combine(appLocation, appName & MySQLConfigFileExtension)
            Dim newConnection As manager
            Dim settingsEditor As New MySQL_Configuration(_config, appName, filePath)
            newConnection = settingsEditor.Config
            settingsEditor.ShowDialog()
            settingsEditor.Dispose()
            Return newConnection
        End Function

        Public Shared Function StartDefaultSetup(appLocation As String, Optional appName As String = "Local")
            Dim filePath As String = Path.Combine(appLocation, appName & MySQLConfigFileExtension)
            If File.Exists(filePath) Then
                Return confsaver_xml.XmlSerialization.ReadFromFile(filePath, New MysqlConfiguration)
            Else
                Return OpenEditor(New manager, appLocation, appName)
            End If
        End Function
#End Region

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    Connection.Dispose()
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

#Region "Misc"
    Public Class SQLCondition
        Public Field As String
        Public Value As Object
        Public Conjunction As String

        Sub New(_field As String, _value As Object, Optional _conjunction As String = "")
            Field = _field
            Value = _value
            Conjunction = _conjunction
        End Sub

        Public Overrides Function ToString() As String
            Dim v = Value
            Dim valtype As String = TypeName(v)
            Select Case valtype
                Case "String"
                    Return String.Format(" `{0}` = '{1}' {2}", Field, Value, Conjunction)
                Case "Date"
                    Return String.Format(" `{0}` = '{1}' {2}", Field, Date.Parse(Value).ToString("yyyy-MM-dd HH:mm:ss"), Conjunction)
                Case Else
                    Return String.Format(" {0} = {1} {2}", Field, Value, Conjunction)
            End Select
        End Function
    End Class
#End Region
End Class
