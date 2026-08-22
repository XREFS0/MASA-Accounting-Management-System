Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcDashboard
        Inherits UserControl

        Private lblRevenue As Label
        Private lblExpense As Label
        Private lblNetIncome As Label
        Private lblCashBank As Label
        Private lblAR As Label
        Private lblAP As Label
        Private gridRecent As DataGridView
        Private btnRefresh As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBg
            Me.Padding = New Padding(24)

            ' Header
            Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = 55}
            Dim lblTitle As New Label With {
                .Text = "Executive Financial Dashboard",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 5)
            }
            pnlHeader.Controls.Add(lblTitle)

            Dim lblSubtitle As New Label With {
                .Text = "Real-time key accounting performance indicators & general ledger summary",
                .Font = UITheme.FontRegular,
                .ForeColor = UITheme.TextMuted,
                .AutoSize = True,
                .Location = New Point(0, 30)
            }
            pnlHeader.Controls.Add(lblSubtitle)

            btnRefresh = New Button With {
                .Text = "Refresh Metrics",
                .Size = New Size(140, 38),
                .Location = New Point(Me.Width - 160, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadDashboardData()
            pnlHeader.Controls.Add(btnRefresh)
            Me.Controls.Add(pnlHeader)

            ' KPI Cards Container
            Dim flowCards As New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 140,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .AutoScroll = True,
                .Padding = New Padding(0, 10, 0, 10)
            }

            flowCards.Controls.Add(CreateKpiCard("TOTAL REVENUE", UITheme.FormatCurrency(0D), UITheme.SuccessColor, lblRevenue))
            flowCards.Controls.Add(CreateKpiCard("TOTAL EXPENSES", UITheme.FormatCurrency(0D), UITheme.DangerColor, lblExpense))
            flowCards.Controls.Add(CreateKpiCard("NET OPERATING PROFIT", UITheme.FormatCurrency(0D), Color.FromArgb(2, 132, 199), lblNetIncome))
            flowCards.Controls.Add(CreateKpiCard("CASH & BANK BALANCES", UITheme.FormatCurrency(0D), UITheme.PrimaryColor, lblCashBank))
            flowCards.Controls.Add(CreateKpiCard("ACCOUNTS RECEIVABLE (A/R)", UITheme.FormatCurrency(0D), UITheme.WarningColor, lblAR))
            flowCards.Controls.Add(CreateKpiCard("ACCOUNTS PAYABLE (A/P)", UITheme.FormatCurrency(0D), Color.FromArgb(225, 29, 72), lblAP))

            Me.Controls.Add(flowCards)

            ' Bottom Section: Recent Postings
            Dim pnlRecent As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(0, 15, 0, 0)
            }

            Dim pnlCardContainer As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UITheme.CardBg,
                .Padding = New Padding(16)
            }

            Dim lblSection As New Label With {
                .Text = "Recent General Ledger Postings & Audit Trail",
                .Font = UITheme.FontSubheader,
                .ForeColor = UITheme.TextDark,
                .Dock = DockStyle.Top,
                .Height = 35
            }
            pnlCardContainer.Controls.Add(lblSection)

            gridRecent = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridRecent)
            pnlCardContainer.Controls.Add(gridRecent)

            pnlRecent.Controls.Add(pnlCardContainer)
            Me.Controls.Add(pnlRecent)

            pnlRecent.BringToFront()
            flowCards.BringToFront()
            pnlHeader.BringToFront()
        End Sub

        Private Function CreateKpiCard(title As String, initialVal As String, accent As Color, ByRef valLabel As Label) As Panel
            Dim card As New Panel With {
                .Size = New Size(220, 115),
                .BackColor = UITheme.CardBg,
                .Margin = New Padding(0, 0, 16, 0)
            }

            ' Top subtle accent stripe
            Dim bar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 4,
                .BackColor = accent
            }
            card.Controls.Add(bar)

            Dim lblT As New Label With {
                .Text = title,
                .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
                .ForeColor = UITheme.TextMuted,
                .Location = New Point(16, 18),
                .Size = New Size(190, 20)
            }
            card.Controls.Add(lblT)

            valLabel = New Label With {
                .Text = initialVal,
                .Font = UITheme.FontMetric,
                .ForeColor = UITheme.TextDark,
                .Location = New Point(16, 45),
                .Size = New Size(190, 45),
                .TextAlign = ContentAlignment.MiddleLeft
            }
            card.Controls.Add(valLabel)

            Return card
        End Function

        Public Async Sub LoadDashboardData()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim reportService As New FinancialReportService()
            Dim journalService As New JournalEntryService()

            Dim summary = Await reportService.GetDashboardSummaryAsync(UserSession.CurrentCompany.Id)
            lblRevenue.Text = UITheme.FormatCurrency(summary.Revenue)
            lblExpense.Text = UITheme.FormatCurrency(summary.Expenses)
            lblNetIncome.Text = UITheme.FormatCurrency(summary.NetIncome)
            lblCashBank.Text = UITheme.FormatCurrency(summary.CashBank)
            lblAR.Text = UITheme.FormatCurrency(summary.AR)
            lblAP.Text = UITheme.FormatCurrency(summary.AP)

            ' Load recent transactions
            Dim entries = Await journalService.GetEntriesAsync(UserSession.CurrentCompany.Id)
            gridRecent.DataSource = entries.Select(Function(e) New With {
                .Date = e.EntryDate.ToShortDateString(),
                .EntryNumber = e.EntryNumber,
                .Reference = e.ReferenceNumber,
                .Memo = e.Memo,
                .Status = e.Status,
                .TotalAmount = UITheme.FormatCurrency(e.TotalDebit),
                .PostedBy = e.PostedByName
            }).ToList()
        End Sub
    End Class
End Namespace
