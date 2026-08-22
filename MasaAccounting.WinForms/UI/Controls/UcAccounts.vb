Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcAccounts
        Inherits UserControl

        Private gridAccounts As DataGridView
        Private btnNewAccount As Button
        Private btnRefresh As Button
        Private txtSearch As TextBox
        Private _accountService As New AccountService()

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = UITheme.ContentBg
            Me.Padding = New Padding(20)

            ' Top Toolbar
            Dim pnlToolbar As New Panel With {.Dock = DockStyle.Top, .Height = 50}

            Dim lblTitle As New Label With {
                .Text = "Chart of Accounts & Balances",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            btnNewAccount = New Button With {
                .Text = "+ Add Account",
                .Size = New Size(130, 36),
                .Location = New Point(Me.Width - 280, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnNewAccount)
            AddHandler btnNewAccount.Click, AddressOf BtnNewAccount_Click
            pnlToolbar.Controls.Add(btnNewAccount)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 140, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadAccounts()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridAccounts = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridAccounts)
            Me.Controls.Add(gridAccounts)

            gridAccounts.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadAccounts()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim accounts = Await _accountService.GetAccountsAsync(UserSession.CurrentCompany.Id)
            gridAccounts.DataSource = accounts.Select(Function(a) New With {
                .Code = a.AccountCode,
                .Name = a.AccountName,
                .Category = a.CategoryName,
                .Type = a.CategoryType,
                .Currency = a.Currency,
                .CurrentBalance = UITheme.FormatCurrency(a.CurrentBalance, a.Currency),
                .Status = If(a.IsActive, "Active", "Inactive")
            }).ToList()
        End Sub

        Private Sub BtnNewAccount_Click(sender As Object, e As EventArgs)
            Using dlg As New Form()
                dlg.Text = "New General Ledger Account"
                dlg.Size = New Size(400, 360)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblCode As New Label With {.Text = "Account Code:", .Location = New Point(30, 20), .Size = New Size(100, 20)}
                Dim txtCode As New TextBox With {.Location = New Point(30, 45), .Size = New Size(320, 26)}

                Dim lblName As New Label With {.Text = "Account Name:", .Location = New Point(30, 80), .Size = New Size(100, 20)}
                Dim txtName As New TextBox With {.Location = New Point(30, 105), .Size = New Size(320, 26)}

                Dim lblCat As New Label With {.Text = "Category:", .Location = New Point(30, 140), .Size = New Size(100, 20)}
                Dim cmbCat As New ComboBox With {.Location = New Point(30, 165), .Size = New Size(320, 26), .DropDownStyle = ComboBoxStyle.DropDownList}

                ' Load categories
                Dim categories = _accountService.GetCategoriesAsync().GetAwaiter().GetResult()
                cmbCat.DataSource = categories
                cmbCat.DisplayMember = "Name"
                cmbCat.ValueMember = "Id"

                Dim btnSave As New Button With {.Text = "Save Account", .Location = New Point(30, 230), .Size = New Size(320, 38)}
                UITheme.StylePrimaryButton(btnSave)

                AddHandler btnSave.Click, Async Sub()
                    If String.IsNullOrWhiteSpace(txtCode.Text) OrElse String.IsNullOrWhiteSpace(txtName.Text) Then
                        MessageBox.Show("Please enter code and name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    Dim acc As New Account With {
                        .CompanyId = UserSession.CurrentCompany.Id,
                        .CategoryId = CType(cmbCat.SelectedValue, Guid),
                        .AccountCode = txtCode.Text.Trim(),
                        .AccountName = txtName.Text.Trim(),
                        .IsActive = True
                    }

                    Dim res = Await _accountService.SaveAccountAsync(acc)
                    If res.Success Then
                        MessageBox.Show(res.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadAccounts()
                    Else
                        MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblCode, txtCode, lblName, txtName, lblCat, cmbCat, btnSave})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
