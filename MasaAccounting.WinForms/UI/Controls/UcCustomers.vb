Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcCustomers
        Inherits UserControl

        Private gridCustomers As DataGridView
        Private btnAddCustomer As Button
        Private btnEditCustomer As Button
        Private btnRefresh As Button
        Private txtSearch As TextBox
        Private _customerService As New CustomerService()

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
                .Text = "Customer Management & Receivables",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            txtSearch = New TextBox With {
                .PlaceholderText = "Search by code or name...",
                .Size = New Size(220, 30),
                .Location = New Point(380, 12)
            }
            AddHandler txtSearch.TextChanged, Sub() LoadCustomers()
            pnlToolbar.Controls.Add(txtSearch)

            btnAddCustomer = New Button With {
                .Text = "+ Add Customer",
                .Size = New Size(140, 36),
                .Location = New Point(Me.Width - 360, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnAddCustomer)
            AddHandler btnAddCustomer.Click, AddressOf BtnAddCustomer_Click
            pnlToolbar.Controls.Add(btnAddCustomer)

            btnEditCustomer = New Button With {
                .Text = "Edit",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 210, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnEditCustomer)
            AddHandler btnEditCustomer.Click, AddressOf BtnEditCustomer_Click
            pnlToolbar.Controls.Add(btnEditCustomer)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 110, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadCustomers()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridCustomers = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridCustomers)
            Me.Controls.Add(gridCustomers)

            gridCustomers.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadCustomers()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim customers = Await _customerService.GetCustomersAsync(UserSession.CurrentCompany.Id, txtSearch.Text.Trim())
            gridCustomers.DataSource = customers.Select(Function(c) New With {
                .Id = c.Id,
                .Code = c.Code,
                .Name = c.Name,
                .CompanyName = c.CompanyName,
                .TaxNumber = c.TaxNumber,
                .Email = c.Email,
                .Phone = c.Phone,
                .CreditLimit = UITheme.FormatCurrency(c.CreditLimit),
                .OutstandingBalance = UITheme.FormatCurrency(c.OutstandingBalance),
                .Status = If(c.IsActive, "Active", "Inactive")
            }).ToList()

            If gridCustomers.Columns.Contains("Id") Then
                gridCustomers.Columns("Id").Visible = False
            End If
        End Sub

        Private Sub BtnAddCustomer_Click(sender As Object, e As EventArgs)
            ShowCustomerDialog(New Customer() With {.CompanyId = UserSession.CurrentCompany.Id, .CreditLimit = 100000D, .PaymentTermsDays = 30, .IsActive = True})
        End Sub

        Private Async Sub BtnEditCustomer_Click(sender As Object, e As EventArgs)
            If gridCustomers.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim selectedId = CType(gridCustomers.SelectedRows(0).Cells("Id").Value, Guid)
            Dim allCustomers = Await _customerService.GetCustomersAsync(UserSession.CurrentCompany.Id)
            Dim targetCust = allCustomers.FirstOrDefault(Function(c) c.Id = selectedId)

            If targetCust IsNot Nothing Then
                ShowCustomerDialog(targetCust)
            End If
        End Sub

        Private Sub ShowCustomerDialog(cust As Customer)
            Using dlg As New Form()
                dlg.Text = If(cust.Id = Guid.Empty, "Register New Customer", "Edit Customer Details")
                dlg.Size = New Size(540, 520)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblCode As New Label With {.Text = "Customer Code:", .Location = New Point(25, 20), .Size = New Size(120, 20)}
                Dim txtCode As New TextBox With {.Text = cust.Code, .Location = New Point(25, 45), .Size = New Size(220, 26)}

                Dim lblTax As New Label With {.Text = "Tax Registration #:", .Location = New Point(270, 20), .Size = New Size(150, 20)}
                Dim txtTax As New TextBox With {.Text = cust.TaxNumber, .Location = New Point(270, 45), .Size = New Size(220, 26)}

                Dim lblName As New Label With {.Text = "Full Customer Name:", .Location = New Point(25, 85), .Size = New Size(150, 20)}
                Dim txtName As New TextBox With {.Text = cust.Name, .Location = New Point(25, 110), .Size = New Size(465, 26)}

                Dim lblEmail As New Label With {.Text = "Email Address:", .Location = New Point(25, 150), .Size = New Size(120, 20)}
                Dim txtEmail As New TextBox With {.Text = cust.Email, .Location = New Point(25, 175), .Size = New Size(220, 26)}

                Dim lblPhone As New Label With {.Text = "Phone Number:", .Location = New Point(270, 150), .Size = New Size(120, 20)}
                Dim txtPhone As New TextBox With {.Text = cust.Phone, .Location = New Point(270, 175), .Size = New Size(220, 26)}

                Dim lblAddress As New Label With {.Text = "Billing Address:", .Location = New Point(25, 215), .Size = New Size(150, 20)}
                Dim txtAddress As New TextBox With {.Text = cust.BillingAddress, .Location = New Point(25, 240), .Size = New Size(465, 26)}

                Dim lblLimit As New Label With {.Text = "Credit Limit:", .Location = New Point(25, 280), .Size = New Size(150, 20)}
                Dim numLimit As New NumericUpDown With {.Location = New Point(25, 305), .Size = New Size(220, 26), .Maximum = 10000000D, .Value = cust.CreditLimit, .DecimalPlaces = 2}

                Dim chkActive As New CheckBox With {.Text = "Account is Active", .Checked = cust.IsActive, .Location = New Point(270, 305), .Size = New Size(180, 26)}

                Dim btnSave As New Button With {.Text = "Save Customer", .Location = New Point(25, 390), .Size = New Size(465, 42)}
                UITheme.StylePrimaryButton(btnSave)

                AddHandler btnSave.Click, Async Sub()
                    If String.IsNullOrWhiteSpace(txtCode.Text) OrElse String.IsNullOrWhiteSpace(txtName.Text) Then
                        MessageBox.Show("Please enter customer code and name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    cust.Code = txtCode.Text.Trim()
                    cust.Name = txtName.Text.Trim()
                    cust.TaxNumber = txtTax.Text.Trim()
                    cust.Email = txtEmail.Text.Trim()
                    cust.Phone = txtPhone.Text.Trim()
                    cust.BillingAddress = txtAddress.Text.Trim()
                    cust.CreditLimit = numLimit.Value
                    cust.IsActive = chkActive.Checked

                    Dim result = Await _customerService.SaveCustomerAsync(cust)
                    If result.Success Then
                        MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadCustomers()
                    Else
                        MessageBox.Show(result.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblCode, txtCode, lblTax, txtTax, lblName, txtName, lblEmail, txtEmail, lblPhone, txtPhone, lblAddress, txtAddress, lblLimit, numLimit, chkActive, btnSave})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
