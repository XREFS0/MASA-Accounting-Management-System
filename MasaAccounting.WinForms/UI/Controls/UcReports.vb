Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcReports
        Inherits UserControl

        Private gridReport As DataGridView
        Private cmbReportType As ComboBox
        Private dtpFrom As DateTimePicker
        Private dtpTo As DateTimePicker
        Private btnGenerate As Button
        Private lblTotalDebit As Label
        Private lblTotalCredit As Label
        Private lblBalanceStatus As Label
        Private pnlSummary As Panel
        Private _reportService As New FinancialReportService()

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBg
            Me.Padding = New Padding(20)

            ' Top Filter Bar
            Dim pnlFilter As New Panel With {.Dock = DockStyle.Top, .Height = 70}

            Dim lblReport As New Label With {.Text = "Select Report:", .Location = New Point(0, 10), .Size = New Size(100, 20), .Font = UITheme.FontBold}
            cmbReportType = New ComboBox With {.Location = New Point(0, 32), .Size = New Size(220, 26), .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbReportType.Items.AddRange(New Object() {"Trial Balance", "General Ledger", "Profit & Loss (Summary)", "Balance Sheet (Summary)"})
            cmbReportType.SelectedIndex = 0

            Dim lblFrom As New Label With {.Text = "From Date:", .Location = New Point(240, 10), .Size = New Size(100, 20), .Font = UITheme.FontBold}
            dtpFrom = New DateTimePicker With {.Location = New Point(240, 32), .Size = New Size(130, 26), .Value = New DateTime(DateTime.Today.Year, 1, 1)}

            Dim lblTo As New Label With {.Text = "To Date:", .Location = New Point(390, 10), .Size = New Size(100, 20), .Font = UITheme.FontBold}
            dtpTo = New DateTimePicker With {.Location = New Point(390, 32), .Size = New Size(130, 26), .Value = DateTime.Today}

            btnGenerate = New Button With {.Text = "Run Financial Report", .Location = New Point(540, 28), .Size = New Size(160, 34)}
            UITheme.StylePrimaryButton(btnGenerate)
            AddHandler btnGenerate.Click, Sub() RunSelectedReport()

            pnlFilter.Controls.AddRange(New Control() {lblReport, cmbReportType, lblFrom, dtpFrom, lblTo, dtpTo, btnGenerate})
            Me.Controls.Add(pnlFilter)

            ' Bottom Summary Panel (for Trial Balance Totals)
            pnlSummary = New Panel With {.Dock = DockStyle.Bottom, .Height = 45, .BackColor = UITheme.CardBg}
            lblTotalDebit = New Label With {.Text = "Total Debits: $0.00", .Font = UITheme.FontBold, .ForeColor = UITheme.TextDark, .Location = New Point(20, 12), .Size = New Size(200, 25)}
            lblTotalCredit = New Label With {.Text = "Total Credits: $0.00", .Font = UITheme.FontBold, .ForeColor = UITheme.TextDark, .Location = New Point(240, 12), .Size = New Size(200, 25)}
            lblBalanceStatus = New Label With {.Text = "Status: Balanced", .Font = UITheme.FontBold, .ForeColor = UITheme.SuccessColor, .Location = New Point(460, 12), .Size = New Size(200, 25)}

            pnlSummary.Controls.AddRange(New Control() {lblTotalDebit, lblTotalCredit, lblBalanceStatus})
            Me.Controls.Add(pnlSummary)

            ' Grid
            gridReport = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridReport)
            Me.Controls.Add(gridReport)

            gridReport.BringToFront()
            pnlFilter.BringToFront()
            pnlSummary.BringToFront()
        End Sub

        Public Async Sub RunSelectedReport()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim reportIdx = cmbReportType.SelectedIndex
            If reportIdx = 0 Then
                ' Trial Balance
                pnlSummary.Visible = True
                Dim tb = Await _reportService.GetTrialBalanceAsync(UserSession.CurrentCompany.Id, dtpTo.Value)

                gridReport.DataSource = tb.Rows.Select(Function(r) New With {
                    .AccountCode = r.AccountCode,
                    .AccountName = r.AccountName,
                    .Category = r.CategoryName,
                    .Type = r.CategoryType,
                    .Debit = If(r.DebitBalance > 0, UITheme.FormatCurrency(r.DebitBalance), ""),
                    .Credit = If(r.CreditBalance > 0, UITheme.FormatCurrency(r.CreditBalance), "")
                }).ToList()

                lblTotalDebit.Text = $"Total Debits: {UITheme.FormatCurrency(tb.TotalDebit)}"
                lblTotalCredit.Text = $"Total Credits: {UITheme.FormatCurrency(tb.TotalCredit)}"
                If tb.IsBalanced Then
                    lblBalanceStatus.Text = "Status: BALANCED"
                    lblBalanceStatus.ForeColor = UITheme.SuccessColor
                Else
                    lblBalanceStatus.Text = "Status: UNBALANCED"
                    lblBalanceStatus.ForeColor = UITheme.DangerColor
                End If

            ElseIf reportIdx = 1 Then
                ' General Ledger
                pnlSummary.Visible = False
                Dim gl = Await _reportService.GetGeneralLedgerAsync(UserSession.CurrentCompany.Id, dtpFrom.Value, dtpTo.Value)

                gridReport.DataSource = gl.Select(Function(r) New With {
                    .Code = r.AccountCode,
                    .Account = r.AccountName,
                    .Date = r.EntryDate.ToShortDateString(),
                    .EntryNumber = r.EntryNumber,
                    .Reference = r.ReferenceNumber,
                    .Description = r.Description,
                    .Debit = If(r.Debit > 0, UITheme.FormatCurrency(r.Debit), ""),
                    .Credit = If(r.Credit > 0, UITheme.FormatCurrency(r.Credit), ""),
                    .Balance = UITheme.FormatCurrency(r.RunningBalance)
                }).ToList()

            Else
                ' P&L or Balance Sheet summaries
                pnlSummary.Visible = False
                Dim summary = Await _reportService.GetDashboardSummaryAsync(UserSession.CurrentCompany.Id)
                Dim items = New List(Of Object)()

                If reportIdx = 2 Then
                    items.Add(New With {.FinancialItem = "Total Operating Revenue", .Amount = UITheme.FormatCurrency(summary.Revenue)})
                    items.Add(New With {.FinancialItem = "Total Operating Expenses & COGS", .Amount = UITheme.FormatCurrency(summary.Expenses)})
                    items.Add(New With {.FinancialItem = "NET OPERATING PROFIT / (LOSS)", .Amount = UITheme.FormatCurrency(summary.NetIncome)})
                Else
                    items.Add(New With {.FinancialItem = "Total Liquid Cash & Bank Balances", .Amount = UITheme.FormatCurrency(summary.CashBank)})
                    items.Add(New With {.FinancialItem = "Accounts Receivable (Customer Balances)", .Amount = UITheme.FormatCurrency(summary.AR)})
                    items.Add(New With {.FinancialItem = "Accounts Payable (Supplier Balances)", .Amount = UITheme.FormatCurrency(summary.AP)})
                End If

                gridReport.DataSource = items
            End If
        End Sub
    End Class
End Namespace
