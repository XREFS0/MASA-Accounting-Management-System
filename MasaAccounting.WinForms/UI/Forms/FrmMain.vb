Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common
Imports MasaAccounting.WinForms.UI.Controls

Namespace UI.Forms
    Public Class FrmMain
        Inherits Form

        Private pnlSidebar As Panel
        Private pnlHeader As Panel
        Private pnlContent As Panel
        Private lblUserBadge As Label
        Private lblCompanyBadge As Label

        ' Active Module Views
        Private ucDashboard As UcDashboard
        Private ucAccounts As UcAccounts
        Private ucJournals As UcJournalEntries
        Private ucSales As UcSalesInvoices
        Private ucCustomers As UcCustomers
        Private ucSuppliers As UcSuppliers
        Private ucProducts As UcProducts
        Private ucReports As UcReports

        Private activeNavButton As Button

        Public Sub New()
            InitializeComponent()
            InitializeModules()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "MASA Enterprise Accounting Management System"
            Me.Size = New Size(1360, 840)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.MinimumSize = New Size(1150, 740)
            Me.BackColor = UITheme.ContentBg

            ' 1. Main Content Area
            pnlContent = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = UITheme.ContentBg,
                .Padding = New Padding(10)
            }

            ' 2. Top Header Bar
            pnlHeader = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 62,
                .BackColor = UITheme.HeaderBg
            }

            ' Header bottom border separator
            Dim pnlHeaderBorder As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 1,
                .BackColor = UITheme.BorderColor
            }
            pnlHeader.Controls.Add(pnlHeaderBorder)

            Dim lblBrand As New Label With {
                .Text = "MASA ACCOUNTING",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblBrand)

            Dim pnlCompanyBadge As New Panel With {
                .Size = New Size(320, 32),
                .Location = New Point(230, 15),
                .BackColor = Color.FromArgb(240, 249, 255)
            }
            lblCompanyBadge = New Label With {
                .Text = "MASA Egypt Enterprise S.A.E. (EGP)",
                .Font = UITheme.FontBadge,
                .ForeColor = Color.FromArgb(3, 105, 161),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlCompanyBadge.Controls.Add(lblCompanyBadge)
            pnlHeader.Controls.Add(pnlCompanyBadge)

            lblUserBadge = New Label With {
                .Text = "Administrator",
                .Font = UITheme.FontBold,
                .ForeColor = UITheme.TextDark,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Location = New Point(Me.Width - 280, 20),
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblUserBadge)

            ' 3. Left Sidebar Navigation
            pnlSidebar = New Panel With {
                .Dock = DockStyle.Left,
                .Width = 240,
                .BackColor = UITheme.SidebarBg,
                .AutoScroll = True
            }

            ' Sidebar Brand Header
            Dim pnlSidebarTitle As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 62,
                .BackColor = UITheme.SidebarBg
            }
            Dim lblNavTitle As New Label With {
                .Text = "MAIN NAVIGATION",
                .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(20, 25),
                .AutoSize = True
            }
            pnlSidebarTitle.Controls.Add(lblNavTitle)
            pnlSidebar.Controls.Add(pnlSidebarTitle)

            ' Add controls to Form in correct docking order
            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlSidebar)
            Me.Controls.Add(pnlHeader)

            ' Navigation Items
            Dim navItems = New (String, Action)() {
                ("Dashboard", Sub() SwitchView(ucDashboard)),
                ("Chart of Accounts", Sub() SwitchView(ucAccounts)),
                ("General Journal", Sub() SwitchView(ucJournals)),
                ("Sales & Invoices", Sub() SwitchView(ucSales)),
                ("Customers", Sub() SwitchView(ucCustomers)),
                ("Suppliers", Sub() SwitchView(ucSuppliers)),
                ("Products & Inventory", Sub() SwitchView(ucProducts)),
                ("Financial Reports", Sub() SwitchView(ucReports)),
                ("Sign Out", Sub() HandleLogout())
            }

            Dim topPos = 70
            For Each item In navItems
                Dim action = item.Item2
                Dim btn As New Button With {
                    .Text = item.Item1,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Font = UITheme.FontRegular,
                    .ForeColor = Color.FromArgb(226, 232, 240),
                    .BackColor = UITheme.SidebarBg,
                    .FlatStyle = FlatStyle.Flat,
                    .Size = New Size(240, 46),
                    .Location = New Point(0, topPos),
                    .Cursor = Cursors.Hand,
                    .Padding = New Padding(24, 0, 0, 0)
                }
                btn.FlatAppearance.BorderSize = 0
                AddHandler btn.Click, Sub(s, e)
                                          SetActiveNav(btn)
                                          action.Invoke()
                                      End Sub
                AddHandler btn.MouseEnter, Sub(s, e)
                                               If btn IsNot activeNavButton Then btn.BackColor = UITheme.SidebarHover
                                           End Sub
                AddHandler btn.MouseLeave, Sub(s, e)
                                               If btn IsNot activeNavButton Then btn.BackColor = UITheme.SidebarBg
                                           End Sub
                pnlSidebar.Controls.Add(btn)
                topPos += 48
            Next
        End Sub

        Private Sub InitializeModules()
            ucDashboard = New UcDashboard() With {.Dock = DockStyle.Fill}
            ucAccounts = New UcAccounts() With {.Dock = DockStyle.Fill}
            ucJournals = New UcJournalEntries() With {.Dock = DockStyle.Fill}
            ucSales = New UcSalesInvoices() With {.Dock = DockStyle.Fill}
            ucCustomers = New UcCustomers() With {.Dock = DockStyle.Fill}
            ucSuppliers = New UcSuppliers() With {.Dock = DockStyle.Fill}
            ucProducts = New UcProducts() With {.Dock = DockStyle.Fill}
            ucReports = New UcReports() With {.Dock = DockStyle.Fill}

            pnlContent.Controls.AddRange(New Control() {ucDashboard, ucAccounts, ucJournals, ucSales, ucCustomers, ucSuppliers, ucProducts, ucReports})

            If UserSession.CurrentUser IsNot Nothing Then
                lblUserBadge.Text = UserSession.CurrentUser.FullName
            End If
            If UserSession.CurrentCompany IsNot Nothing Then
                lblCompanyBadge.Text = $"{UserSession.CurrentCompany.Name} ({UserSession.CurrentCompany.BaseCurrency})"
            End If

            For Each ctrl In pnlSidebar.Controls
                If TypeOf ctrl Is Button AndAlso DirectCast(ctrl, Button).Text = "Dashboard" Then
                    SetActiveNav(DirectCast(ctrl, Button))
                    Exit For
                End If
            Next

            SwitchView(ucDashboard)
        End Sub

        Private Sub SwitchView(activeUc As UserControl)
            For Each ctrl As Control In pnlContent.Controls
                If ctrl Is activeUc Then
                    ctrl.Visible = True
                    ctrl.BringToFront()
                Else
                    ctrl.Visible = False
                End If
            Next

            If TypeOf activeUc Is UcDashboard Then
                DirectCast(activeUc, UcDashboard).LoadDashboardData()
            ElseIf TypeOf activeUc Is UcAccounts Then
                DirectCast(activeUc, UcAccounts).LoadAccounts()
            ElseIf TypeOf activeUc Is UcJournalEntries Then
                DirectCast(activeUc, UcJournalEntries).LoadEntries()
            ElseIf TypeOf activeUc Is UcSalesInvoices Then
                DirectCast(activeUc, UcSalesInvoices).LoadInvoices()
            ElseIf TypeOf activeUc Is UcCustomers Then
                DirectCast(activeUc, UcCustomers).LoadCustomers()
            ElseIf TypeOf activeUc Is UcSuppliers Then
                DirectCast(activeUc, UcSuppliers).LoadSuppliers()
            ElseIf TypeOf activeUc Is UcProducts Then
                DirectCast(activeUc, UcProducts).LoadProducts()
            ElseIf TypeOf activeUc Is UcReports Then
                DirectCast(activeUc, UcReports).RunSelectedReport()
            End If
        End Sub

        Private Sub SetActiveNav(btn As Button)
            If activeNavButton IsNot Nothing Then
                activeNavButton.BackColor = UITheme.SidebarBg
                activeNavButton.Font = UITheme.FontRegular
                activeNavButton.ForeColor = Color.FromArgb(226, 232, 240)
            End If
            activeNavButton = btn
            activeNavButton.BackColor = UITheme.SidebarActive
            activeNavButton.Font = UITheme.FontBold
            activeNavButton.ForeColor = Color.White
        End Sub

        Private Sub HandleLogout()
            Dim confirm = MessageBox.Show("Are you sure you want to sign out?", "Sign Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If confirm = DialogResult.Yes Then
                UserSession.Logout()
                Me.Hide()
                Using frmLogin As New FrmLogin()
                    If frmLogin.ShowDialog() = DialogResult.OK Then
                        Me.Show()
                        InitializeModules()
                    Else
                        Application.Exit()
                    End If
                End Using
            End If
        End Sub
    End Class
End Namespace
