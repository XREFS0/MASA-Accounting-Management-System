Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcSuppliers
        Inherits UserControl

        Private gridSuppliers As DataGridView
        Private btnAddSupplier As Button
        Private btnEditSupplier As Button
        Private btnRefresh As Button
        Private txtSearch As TextBox
        Private _supplierService As New SupplierService()

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
                .Text = "Supplier Management & Payables",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            txtSearch = New TextBox With {
                .PlaceholderText = "Search supplier...",
                .Size = New Size(220, 30),
                .Location = New Point(380, 12)
            }
            AddHandler txtSearch.TextChanged, Sub() LoadSuppliers()
            pnlToolbar.Controls.Add(txtSearch)

            btnAddSupplier = New Button With {
                .Text = "+ Add Supplier",
                .Size = New Size(140, 36),
                .Location = New Point(Me.Width - 360, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnAddSupplier)
            AddHandler btnAddSupplier.Click, AddressOf BtnAddSupplier_Click
            pnlToolbar.Controls.Add(btnAddSupplier)

            btnEditSupplier = New Button With {
                .Text = "Edit",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 210, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnEditSupplier)
            AddHandler btnEditSupplier.Click, AddressOf BtnEditSupplier_Click
            pnlToolbar.Controls.Add(btnEditSupplier)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 110, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadSuppliers()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridSuppliers = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridSuppliers)
            Me.Controls.Add(gridSuppliers)

            gridSuppliers.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadSuppliers()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim suppliers = Await _supplierService.GetSuppliersAsync(UserSession.CurrentCompany.Id, txtSearch.Text.Trim())
            gridSuppliers.DataSource = suppliers.Select(Function(s) New With {
                .Id = s.Id,
                .Code = s.Code,
                .Name = s.Name,
                .CompanyName = s.CompanyName,
                .TaxNumber = s.TaxNumber,
                .Email = s.Email,
                .Phone = s.Phone,
                .OutstandingBalance = UITheme.FormatCurrency(s.OutstandingBalance),
                .Status = If(s.IsActive, "Active", "Inactive")
            }).ToList()

            If gridSuppliers.Columns.Contains("Id") Then
                gridSuppliers.Columns("Id").Visible = False
            End If
        End Sub

        Private Sub BtnAddSupplier_Click(sender As Object, e As EventArgs)
            ShowSupplierDialog(New Supplier() With {.CompanyId = UserSession.CurrentCompany.Id, .PaymentTermsDays = 30, .IsActive = True})
        End Sub

        Private Async Sub BtnEditSupplier_Click(sender As Object, e As EventArgs)
            If gridSuppliers.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a supplier to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim selectedId = CType(gridSuppliers.SelectedRows(0).Cells("Id").Value, Guid)
            Dim allSuppliers = Await _supplierService.GetSuppliersAsync(UserSession.CurrentCompany.Id)
            Dim targetSupp = allSuppliers.FirstOrDefault(Function(s) s.Id = selectedId)

            If targetSupp IsNot Nothing Then
                ShowSupplierDialog(targetSupp)
            End If
        End Sub

        Private Sub ShowSupplierDialog(supp As Supplier)
            Using dlg As New Form()
                dlg.Text = If(supp.Id = Guid.Empty, "Register New Supplier", "Edit Supplier Details")
                dlg.Size = New Size(540, 480)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblCode As New Label With {.Text = "Supplier Code:", .Location = New Point(25, 20), .Size = New Size(120, 20)}
                Dim txtCode As New TextBox With {.Text = supp.Code, .Location = New Point(25, 45), .Size = New Size(220, 26)}

                Dim lblTax As New Label With {.Text = "Tax Registration #:", .Location = New Point(270, 20), .Size = New Size(150, 20)}
                Dim txtTax As New TextBox With {.Text = supp.TaxNumber, .Location = New Point(270, 45), .Size = New Size(220, 26)}

                Dim lblName As New Label With {.Text = "Vendor / Company Name:", .Location = New Point(25, 85), .Size = New Size(180, 20)}
                Dim txtName As New TextBox With {.Text = supp.Name, .Location = New Point(25, 110), .Size = New Size(465, 26)}

                Dim lblEmail As New Label With {.Text = "Email Address:", .Location = New Point(25, 150), .Size = New Size(120, 20)}
                Dim txtEmail As New TextBox With {.Text = supp.Email, .Location = New Point(25, 175), .Size = New Size(220, 26)}

                Dim lblPhone As New Label With {.Text = "Phone Number:", .Location = New Point(270, 150), .Size = New Size(120, 20)}
                Dim txtPhone As New TextBox With {.Text = supp.Phone, .Location = New Point(270, 175), .Size = New Size(220, 26)}

                Dim lblAddress As New Label With {.Text = "Address / Location:", .Location = New Point(25, 215), .Size = New Size(150, 20)}
                Dim txtAddress As New TextBox With {.Text = supp.Address, .Location = New Point(25, 240), .Size = New Size(465, 26)}

                Dim chkActive As New CheckBox With {.Text = "Supplier is Active", .Checked = supp.IsActive, .Location = New Point(25, 285), .Size = New Size(180, 26)}

                Dim btnSave As New Button With {.Text = "Save Supplier", .Location = New Point(25, 350), .Size = New Size(465, 42)}
                UITheme.StylePrimaryButton(btnSave)

                AddHandler btnSave.Click, Async Sub()
                    If String.IsNullOrWhiteSpace(txtCode.Text) OrElse String.IsNullOrWhiteSpace(txtName.Text) Then
                        MessageBox.Show("Please enter supplier code and name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    supp.Code = txtCode.Text.Trim()
                    supp.Name = txtName.Text.Trim()
                    supp.TaxNumber = txtTax.Text.Trim()
                    supp.Email = txtEmail.Text.Trim()
                    supp.Phone = txtPhone.Text.Trim()
                    supp.Address = txtAddress.Text.Trim()
                    supp.IsActive = chkActive.Checked

                    Dim result = Await _supplierService.SaveSupplierAsync(supp)
                    If result.Success Then
                        MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadSuppliers()
                    Else
                        MessageBox.Show(result.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblCode, txtCode, lblTax, txtTax, lblName, txtName, lblEmail, txtEmail, lblPhone, txtPhone, lblAddress, txtAddress, chkActive, btnSave})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
