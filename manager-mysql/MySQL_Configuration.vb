Imports confsaver_xml

Public Class MySQL_Configuration

    Public Config As manager
    Private configFilePath As String

    Sub New(ByRef _config As manager, Optional _connectionName As String = "", Optional _configFilePath As String = Nothing)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.Text &= " - " & _connectionName
        Config = _config
        configFilePath = _configFilePath

        With Config
            tbServerName.Text = .Server
            tbDatabaseName.Text = .DatabaseName
            tbPort.Text = .Port

            tbUsername.Text = .UserID
            tbPassword.Text = .Password
        End With
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        If Config.Connection.State = ConnectionState.Open Then
            Config.Close()
        End If
        Me.Close()
    End Sub

    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        copyDetails()
        'If Not config.Connection.State = ConnectionState.Open Then
        If Config.SetupConnection(tbConnectionString.Text) Then
            lbStatus.Text = "Connected..."
        Else
            lbStatus.Text = "Error in Connecting..."
        End If
        'End If
    End Sub

    Private Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        If Config.Connection.State = ConnectionState.Open Then
            Config.Close()
            lbStatus.Text = "Disconnected..."
        End If
    End Sub

    Private Sub btnApply_Click(sender As Object, e As EventArgs) Handles btnApply.Click
        copyDetails()
        If configFilePath IsNot Nothing Then
            XmlSerialization.WriteToFile(configFilePath, Config)
        End If

        lbStatus.Text = "Settings has been modified..."
    End Sub

    Private Sub copyDetails()
        With Config
            .Server = tbServerName.Text
            .DatabaseName = tbDatabaseName.Text
            .Port = tbPort.Text

            .UserID = tbUsername.Text
            .Password = tbPassword.Text
        End With
    End Sub
End Class
