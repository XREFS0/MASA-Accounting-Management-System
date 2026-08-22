Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MasaAccounting.Core.Infrastructure.Security

Namespace UI.Common
    Public Module UITheme
        ' Modern Enterprise Palette
        Public ReadOnly PrimaryColor As Color = Color.FromArgb(15, 23, 42)        ' Slate 900
        Public ReadOnly SidebarBg As Color = Color.FromArgb(30, 41, 59)           ' Slate 800
        Public ReadOnly SidebarHover As Color = Color.FromArgb(51, 65, 85)        ' Slate 700
        Public ReadOnly SidebarActive As Color = Color.FromArgb(2, 132, 199)       ' Sky Blue 600
        Public ReadOnly AccentColor As Color = Color.FromArgb(14, 165, 233)       ' Sky Blue 500
        
        Public ReadOnly SuccessColor As Color = Color.FromArgb(16, 185, 129)      ' Emerald 500
        Public ReadOnly DangerColor As Color = Color.FromArgb(239, 68, 68)        ' Rose 500
        Public ReadOnly WarningColor As Color = Color.FromArgb(245, 158, 11)      ' Amber 500

        ' Backgrounds & Cards
        Public ReadOnly ContentBg As Color = Color.FromArgb(248, 250, 252)        ' Slate 50
        Public ReadOnly CardBg As Color = Color.White
        Public ReadOnly HeaderBg As Color = Color.White
        Public ReadOnly BorderColor As Color = Color.FromArgb(226, 232, 240)      ' Slate 200
        Public ReadOnly BorderLight As Color = Color.FromArgb(241, 245, 249)

        ' Text Colors
        Public ReadOnly TextDark As Color = Color.FromArgb(15, 23, 42)            ' Slate 900
        Public ReadOnly TextMuted As Color = Color.FromArgb(100, 116, 139)        ' Slate 500
        Public ReadOnly TextLight As Color = Color.FromArgb(241, 245, 249)

        ' Typography
        Public ReadOnly FontRegular As New Font("Segoe UI", 9.5F, FontStyle.Regular)
        Public ReadOnly FontBold As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Public ReadOnly FontHeader As New Font("Segoe UI", 15.0F, FontStyle.Bold)
        Public ReadOnly FontSubheader As New Font("Segoe UI", 11.5F, FontStyle.Bold)
        Public ReadOnly FontMetric As New Font("Segoe UI", 20.0F, FontStyle.Bold)
        Public ReadOnly FontBadge As New Font("Segoe UI", 8.5F, FontStyle.Bold)

        Public Sub ApplyGridStyle(grid As DataGridView)
            grid.EnableHeadersVisualStyles = False
            grid.BackgroundColor = CardBg
            grid.BorderStyle = BorderStyle.None
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            grid.GridColor = BorderColor
            grid.RowHeadersVisible = False
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grid.MultiSelect = False
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.ReadOnly = True
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            grid.RowTemplate.Height = 40

            ' Header Styling
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249)
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85)
            grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(12, 8, 12, 8)
            grid.ColumnHeadersHeight = 44

            ' Row Styling
            grid.DefaultCellStyle.BackColor = CardBg
            grid.DefaultCellStyle.ForeColor = TextDark
            grid.DefaultCellStyle.Font = FontRegular
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 58, 138)
            grid.DefaultCellStyle.Padding = New Padding(12, 6, 12, 6)
        End Sub

        Public Sub StylePrimaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = Color.FromArgb(15, 23, 42)
            btn.ForeColor = Color.White
            btn.Font = FontBold
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        Public Sub StyleSecondaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.BackColor = CardBg
            btn.ForeColor = TextDark
            btn.Font = FontRegular
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        Public Sub StyleDangerButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = DangerColor
            btn.ForeColor = Color.White
            btn.Font = FontBold
            btn.Cursor = Cursors.Hand
            btn.Height = 38
        End Sub

        Public Function FormatCurrency(amount As Decimal, Optional currencyCode As String = Nothing) As String
            Dim code = If(Not String.IsNullOrEmpty(currencyCode), currencyCode, If(UserSession.CurrentCompany IsNot Nothing AndAlso Not String.IsNullOrEmpty(UserSession.CurrentCompany.BaseCurrency), UserSession.CurrentCompany.BaseCurrency, "EGP"))
            Return $"{code} {amount:N2}"
        End Function
    End Module
End Namespace
