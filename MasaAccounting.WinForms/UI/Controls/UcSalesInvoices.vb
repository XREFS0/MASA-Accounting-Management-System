Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcSalesInvoices
        Inherits UserControl

        Private gridInvoices As DataGridView
        Private btnNewInvoice As Button
        Private btnRefresh As Button
        Private _salesService As New SalesInvoiceService()
        Private _customerService As New CustomerService()
        Private _productService As New ProductService()

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
                .Text = "Sales Invoices & Receivables",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            btnNewInvoice = New Button With {
                .Text = "+ Create Invoice",
                .Size = New Size(150, 36),
                .Location = New Point(Me.Width - 270, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnNewInvoice)
            AddHandler btnNewInvoice.Click, AddressOf BtnNewInvoice_Click
            pnlToolbar.Controls.Add(btnNewInvoice)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 100, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadInvoices()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridInvoices = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridInvoices)
            Me.Controls.Add(gridInvoices)

            gridInvoices.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadInvoices()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim invoices = Await _salesService.GetInvoicesAsync(UserSession.CurrentCompany.Id)
            gridInvoices.DataSource = invoices.Select(Function(i) New With {
                .InvoiceNumber = i.InvoiceNumber,
                .Customer = i.CustomerName,
                .Date = i.InvoiceDate.ToShortDateString(),
                .DueDate = i.DueDate.ToShortDateString(),
                .Subtotal = UITheme.FormatCurrency(i.Subtotal, i.Currency),
                .Tax = UITheme.FormatCurrency(i.TaxAmount, i.Currency),
                .Total = UITheme.FormatCurrency(i.TotalAmount, i.Currency),
                .Paid = UITheme.FormatCurrency(i.PaidAmount, i.Currency),
                .BalanceDue = UITheme.FormatCurrency(i.OutstandingAmount, i.Currency),
                .Status = i.Status
            }).ToList()
        End Sub

        Private Sub BtnNewInvoice_Click(sender As Object, e As EventArgs)
            Using dlg As New Form()
                dlg.Text = "Create and Post Sales Invoice"
                dlg.Size = New Size(650, 480)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblCust As New Label With {.Text = "Customer:", .Location = New Point(25, 20), .Size = New Size(100, 20)}
                Dim cmbCust As New ComboBox With {.Location = New Point(25, 45), .Size = New Size(300, 26), .DropDownStyle = ComboBoxStyle.DropDownList}

                Dim lblProd As New Label With {.Text = "Product / Service:", .Location = New Point(25, 90), .Size = New Size(120, 20)}
                Dim cmbProd As New ComboBox With {.Location = New Point(25, 115), .Size = New Size(300, 26), .DropDownStyle = ComboBoxStyle.DropDownList}

                Dim lblQty As New Label With {.Text = "Quantity:", .Location = New Point(345, 90), .Size = New Size(80, 20)}
                Dim numQty As New NumericUpDown With {.Location = New Point(345, 115), .Size = New Size(110, 26), .Value = 1, .Minimum = 1, .Maximum = 10000}

                Dim lblPrice As New Label With {.Text = "Unit Price:", .Location = New Point(475, 90), .Size = New Size(100, 20)}
                Dim numPrice As New NumericUpDown With {.Location = New Point(475, 115), .Size = New Size(130, 26), .Value = 100D, .Maximum = 10000000D, .DecimalPlaces = 2}

                Dim lblTax As New Label With {.Text = "Tax Rate (%):", .Location = New Point(25, 160), .Size = New Size(140, 20)}
                Dim numTax As New NumericUpDown With {.Location = New Point(25, 185), .Size = New Size(140, 26), .Value = 14D, .Maximum = 100D, .DecimalPlaces = 2}

                Dim lblNotes As New Label With {.Text = "Invoice Notes:", .Location = New Point(25, 230), .Size = New Size(120, 20)}
                Dim txtNotes As New TextBox With {.Location = New Point(25, 255), .Size = New Size(580, 26)}

                ' Load customers & products
                Dim customers = _customerService.GetCustomersAsync(UserSession.CurrentCompany.Id).GetAwaiter().GetResult()
                cmbCust.DataSource = customers
                cmbCust.DisplayMember = "Name"
                cmbCust.ValueMember = "Id"

                Dim products = _productService.GetProductsAsync(UserSession.CurrentCompany.Id).GetAwaiter().GetResult()
                cmbProd.DataSource = products
                cmbProd.DisplayMember = "Name"
                cmbProd.ValueMember = "Id"

                AddHandler cmbProd.SelectedIndexChanged, Sub()
                    Dim selProd = TryCast(cmbProd.SelectedItem, Product)
                    If selProd IsNot Nothing Then
                        numPrice.Value = selProd.SellingPrice
                    End If
                End Sub

                Dim btnPost As New Button With {.Text = "Post Invoice & Update Accounts Receivable", .Location = New Point(25, 360), .Size = New Size(580, 42)}
                UITheme.StylePrimaryButton(btnPost)

                AddHandler btnPost.Click, Async Sub()
                    If cmbCust.SelectedValue Is Nothing Then
                        MessageBox.Show("Please select a customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    Dim inv As New SalesInvoice With {
                        .CompanyId = UserSession.CurrentCompany.Id,
                        .CustomerId = CType(cmbCust.SelectedValue, Guid),
                        .InvoiceDate = DateTime.Today,
                        .DueDate = DateTime.Today.AddDays(30),
                        .Notes = txtNotes.Text.Trim()
                    }

                    inv.Items.Add(New SalesInvoiceItem With {
                        .ProductId = CType(cmbProd.SelectedValue, Guid),
                        .Description = cmbProd.Text,
                        .Quantity = numQty.Value,
                        .UnitPrice = numPrice.Value,
                        .TaxRate = numTax.Value
                    })

                    Dim res = Await _salesService.PostSalesInvoiceAsync(inv, UserSession.CurrentUser.Id)
                    If res.Success Then
                        MessageBox.Show(res.Message, "Invoice Posted", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadInvoices()
                    Else
                        MessageBox.Show(res.Message, "Posting Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblCust, cmbCust, lblProd, cmbProd, lblQty, numQty, lblPrice, numPrice, lblTax, numTax, lblNotes, txtNotes, btnPost})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
