Imports System
Imports System.Windows.Forms
Imports MasaAccounting.Core.Infrastructure.Data
Imports MasaAccounting.WinForms.Tools
Imports MasaAccounting.WinForms.UI.Forms

Namespace MasaAccounting.WinForms
    Public Module Program
        <STAThread>
        Public Sub Main(args As String())
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            ' If launched with --capture-screenshots or --capture, run the automatic capture tool
            If args IsNot Nothing AndAlso args.Length > 0 AndAlso (args(0) = "--capture-screenshots" OrElse args(0) = "--capture") Then
                ScreenshotCaptureTool.CaptureAllScreenshotsSync()
                Return
            End If

            ' Initialize database configuration and bootstrap schema
            DatabaseConfiguration.Initialize()
            DatabaseInitializer.EnsureDatabaseSetupAsync().GetAwaiter().GetResult()

            ' Launch Login Form
            Using frmLogin As New FrmLogin()
                If frmLogin.ShowDialog() = DialogResult.OK Then
                    Application.Run(New FrmMain())
                End If
            End Using
        End Sub
    End Module
End Namespace
