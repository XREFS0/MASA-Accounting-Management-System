Imports System
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports System.Windows.Forms
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Controls
Imports MasaAccounting.WinForms.UI.Forms

Namespace Tools
    Public Module ScreenshotCaptureTool
        Public Sub CaptureAllScreenshotsSync()
            Dim targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ScreenShot")
            If Not Directory.Exists(targetDir) Then
                targetDir = Path.Combine(Directory.GetCurrentDirectory(), "ScreenShot")
            End If
            Directory.CreateDirectory(targetDir)

            DatabaseConfiguration.Initialize()
            Dim comp As New Company With {
                .Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                .Code = "MASA-EG",
                .Name = "MASA Egypt Enterprise S.A.E.",
                .BaseCurrency = "EGP"
            }
            Dim user As New AppUser With {
                .Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                .CompanyId = comp.Id,
                .Username = "admin",
                .FullName = "Ahmed Mostafa El-Sayed",
                .RoleName = "Administrator"
            }
            UserSession.CurrentCompany = comp
            UserSession.CurrentUser = user

            ' 1. Capture Login Screen
            Using frmLogin As New FrmLogin()
                frmLogin.Show()
                For i = 0 To 5
                    Application.DoEvents()
                    Thread.Sleep(50)
                Next
                CaptureForm(frmLogin, Path.Combine(targetDir, "01_Login_Screen.png"))
                frmLogin.Close()
            End Using

            ' 2. Capture Main Views with complete data binding
            Using frmMain As New FrmMain()
                frmMain.Show()
                For i = 0 To 10
                    Application.DoEvents()
                    Thread.Sleep(50)
                Next

                Dim pnlContent = frmMain.Controls.OfType(Of Panel)().FirstOrDefault(Function(p) p.Dock = DockStyle.Fill)

                If pnlContent IsNot Nothing Then
                    ' View 1: Dashboard
                    Dim ucDash = pnlContent.Controls.OfType(Of UcDashboard)().FirstOrDefault()
                    If ucDash IsNot Nothing Then
                        ucDash.Visible = True
                        ucDash.BringToFront()
                        ucDash.LoadDashboardData()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucDash, Path.Combine(targetDir, "02_Executive_Dashboard.png"))
                    End If

                    ' View 2: Chart of Accounts
                    Dim ucAcc = pnlContent.Controls.OfType(Of UcAccounts)().FirstOrDefault()
                    If ucAcc IsNot Nothing Then
                        ucAcc.Visible = True
                        ucAcc.BringToFront()
                        ucAcc.LoadAccounts()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucAcc, Path.Combine(targetDir, "03_Chart_Of_Accounts.png"))
                    End If

                    ' View 3: General Journal Entries
                    Dim ucJour = pnlContent.Controls.OfType(Of UcJournalEntries)().FirstOrDefault()
                    If ucJour IsNot Nothing Then
                        ucJour.Visible = True
                        ucJour.BringToFront()
                        ucJour.LoadEntries()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucJour, Path.Combine(targetDir, "04_General_Journal_Entries.png"))
                    End If

                    ' View 4: Sales Invoices
                    Dim ucSales = pnlContent.Controls.OfType(Of UcSalesInvoices)().FirstOrDefault()
                    If ucSales IsNot Nothing Then
                        ucSales.Visible = True
                        ucSales.BringToFront()
                        ucSales.LoadInvoices()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucSales, Path.Combine(targetDir, "05_Sales_Invoices.png"))
                    End If

                    ' View 5: Customers
                    Dim ucCust = pnlContent.Controls.OfType(Of UcCustomers)().FirstOrDefault()
                    If ucCust IsNot Nothing Then
                        ucCust.Visible = True
                        ucCust.BringToFront()
                        ucCust.LoadCustomers()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucCust, Path.Combine(targetDir, "06_Customer_Management.png"))
                    End If

                    ' View 6: Suppliers
                    Dim ucSupp = pnlContent.Controls.OfType(Of UcSuppliers)().FirstOrDefault()
                    If ucSupp IsNot Nothing Then
                        ucSupp.Visible = True
                        ucSupp.BringToFront()
                        ucSupp.LoadSuppliers()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucSupp, Path.Combine(targetDir, "07_Supplier_Management.png"))
                    End If

                    ' View 7: Products & Inventory
                    Dim ucProd = pnlContent.Controls.OfType(Of UcProducts)().FirstOrDefault()
                    If ucProd IsNot Nothing Then
                        ucProd.Visible = True
                        ucProd.BringToFront()
                        ucProd.LoadProducts()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucProd, Path.Combine(targetDir, "08_Products_And_Inventory.png"))
                    End If

                    ' View 8: Financial Reports (Trial Balance)
                    Dim ucRep = pnlContent.Controls.OfType(Of UcReports)().FirstOrDefault()
                    If ucRep IsNot Nothing Then
                        ucRep.Visible = True
                        ucRep.BringToFront()
                        ucRep.RunSelectedReport()
                        PumpEvents(600)
                        CaptureComposite(frmMain, ucRep, Path.Combine(targetDir, "09_Financial_Reports.png"))
                    End If
                End If

                frmMain.Close()
            End Using
        End Sub

        Private Sub PumpEvents(durationMs As Integer)
            Dim sw = System.Diagnostics.Stopwatch.StartNew()
            While sw.ElapsedMilliseconds < durationMs
                Application.DoEvents()
                Thread.Sleep(30)
            End While
        End Sub

        Private Sub CaptureForm(frm As Form, savePath As String)
            Using bmp As New Bitmap(frm.Width, frm.Height)
                frm.DrawToBitmap(bmp, New Rectangle(0, 0, frm.Width, frm.Height))
                bmp.Save(savePath, ImageFormat.Png)
            End Using
        End Sub

        Private Sub CaptureComposite(frmMain As Form, activeUc As UserControl, savePath As String)
            Using bmp As New Bitmap(frmMain.Width, frmMain.Height)
                frmMain.DrawToBitmap(bmp, New Rectangle(0, 0, frmMain.Width, frmMain.Height))

                ' Draw UserControl content directly into the content area of the main window screenshot
                Using ucBmp As New Bitmap(activeUc.Width, activeUc.Height)
                    activeUc.DrawToBitmap(ucBmp, New Rectangle(0, 0, activeUc.Width, activeUc.Height))

                    Using g As Graphics = Graphics.FromImage(bmp)
                        Dim pt = frmMain.PointToClient(activeUc.PointToScreen(Point.Empty))
                        g.DrawImage(ucBmp, pt)
                    End Using
                End Using

                bmp.Save(savePath, ImageFormat.Png)
            End Using
        End Sub
    End Module
End Namespace
