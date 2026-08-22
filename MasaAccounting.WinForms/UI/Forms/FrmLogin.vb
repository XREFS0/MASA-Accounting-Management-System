Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Forms
    Public Class FrmLogin
        Inherits Form

        Private txtUsername As TextBox
        Private txtPassword As TextBox
        Private btnLogin As Button
        Private lblStatus As Label
        Private pnlCard As Panel

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "MASA Accounting System - Sign In"
            Me.Size = New Size(460, 520)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = UITheme.SidebarBg

            ' Center Card
            pnlCard = New Panel With {
                .Size = New Size(380, 420),
                .Location = New Point(35, 30),
                .BackColor = UITheme.CardBg
            }
            Me.Controls.Add(pnlCard)

            ' Title
            Dim lblTitle As New Label With {
                .Text = "MASA Accounting",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .Location = New Point(25, 25),
                .Size = New Size(330, 30),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlCard.Controls.Add(lblTitle)

            Dim lblSub As New Label With {
                .Text = "Enterprise Financial Management",
                .Font = UITheme.FontRegular,
                .ForeColor = UITheme.TextMuted,
                .Location = New Point(25, 55),
                .Size = New Size(330, 20),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlCard.Controls.Add(lblSub)

            ' Username
            Dim lblUser As New Label With {
                .Text = "Username or Email",
                .Font = UITheme.FontBold,
                .ForeColor = UITheme.TextDark,
                .Location = New Point(30, 95),
                .Size = New Size(320, 20)
            }
            pnlCard.Controls.Add(lblUser)

            txtUsername = New TextBox With {
                .Text = "admin",
                .Font = UITheme.FontRegular,
                .Location = New Point(30, 120),
                .Size = New Size(320, 30)
            }
            pnlCard.Controls.Add(txtUsername)

            ' Password
            Dim lblPass As New Label With {
                .Text = "Password",
                .Font = UITheme.FontBold,
                .ForeColor = UITheme.TextDark,
                .Location = New Point(30, 165),
                .Size = New Size(320, 20)
            }
            pnlCard.Controls.Add(lblPass)

            txtPassword = New TextBox With {
                .Text = "Admin@123",
                .Font = UITheme.FontRegular,
                .Location = New Point(30, 190),
                .Size = New Size(320, 30),
                .UseSystemPasswordChar = True
            }
            pnlCard.Controls.Add(txtPassword)

            ' Status
            lblStatus = New Label With {
                .Text = "",
                .Font = UITheme.FontRegular,
                .ForeColor = UITheme.DangerColor,
                .Location = New Point(30, 230),
                .Size = New Size(320, 35),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlCard.Controls.Add(lblStatus)

            ' Login Button
            btnLogin = New Button With {
                .Text = "Sign In",
                .Location = New Point(30, 275),
                .Size = New Size(320, 42)
            }
            UITheme.StylePrimaryButton(btnLogin)
            AddHandler btnLogin.Click, AddressOf BtnLogin_Click
            pnlCard.Controls.Add(btnLogin)

            ' Hint
            Dim lblHint As New Label With {
                .Text = "Default Login: admin / Admin@123",
                .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic),
                .ForeColor = UITheme.TextMuted,
                .Location = New Point(30, 335),
                .Size = New Size(320, 20),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlCard.Controls.Add(lblHint)

            Me.AcceptButton = btnLogin
        End Sub

        Private Async Sub BtnLogin_Click(sender As Object, e As EventArgs)
            lblStatus.ForeColor = UITheme.TextMuted
            lblStatus.Text = "Verifying credentials..."
            btnLogin.Enabled = False

            Dim authService As New AuthenticationService()
            Dim result = Await authService.LoginAsync(txtUsername.Text, txtPassword.Text)

            If result.Success Then
                UserSession.CurrentUser = result.User
                UserSession.CurrentCompany = result.Company
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                lblStatus.ForeColor = UITheme.DangerColor
                lblStatus.Text = result.Message
                btnLogin.Enabled = True
            End If
        End Sub
    End Class
End Namespace
