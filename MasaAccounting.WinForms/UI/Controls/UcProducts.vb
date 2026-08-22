Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcProducts
        Inherits UserControl

        Private gridProducts As DataGridView
        Private btnAddProduct As Button
        Private btnEditProduct As Button
        Private btnRefresh As Button
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
                .Text = "Products & Warehouse Stock",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            btnAddProduct = New Button With {
                .Text = "+ Add Product",
                .Size = New Size(140, 36),
                .Location = New Point(Me.Width - 360, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnAddProduct)
            AddHandler btnAddProduct.Click, AddressOf BtnAddProduct_Click
            pnlToolbar.Controls.Add(btnAddProduct)

            btnEditProduct = New Button With {
                .Text = "Edit",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 210, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnEditProduct)
            AddHandler btnEditProduct.Click, AddressOf BtnEditProduct_Click
            pnlToolbar.Controls.Add(btnEditProduct)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 110, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadProducts()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridProducts = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridProducts)
            Me.Controls.Add(gridProducts)

            gridProducts.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadProducts()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim products = Await _productService.GetProductsAsync(UserSession.CurrentCompany.Id)
            gridProducts.DataSource = products.Select(Function(p) New With {
                .Id = p.Id,
                .SKU = p.Sku,
                .Name = p.Name,
                .UOM = p.UomCode,
                .CostPrice = UITheme.FormatCurrency(p.CostPrice),
                .SellingPrice = UITheme.FormatCurrency(p.SellingPrice),
                .CurrentStock = If(p.IsService, "Service", p.CurrentStock.ToString("N0")),
                .Type = If(p.IsService, "Service", "Merchandise"),
                .Status = If(p.IsActive, "Active", "Inactive")
            }).ToList()

            If gridProducts.Columns.Contains("Id") Then
                gridProducts.Columns("Id").Visible = False
            End If
        End Sub

        Private Sub BtnAddProduct_Click(sender As Object, e As EventArgs)
            ShowProductDialog(New Product() With {.CompanyId = UserSession.CurrentCompany.Id, .SellingPrice = 1000D, .CostPrice = 750D, .IsActive = True})
        End Sub

        Private Async Sub BtnEditProduct_Click(sender As Object, e As EventArgs)
            If gridProducts.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a product to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim selectedId = CType(gridProducts.SelectedRows(0).Cells("Id").Value, Guid)
            Dim allProds = Await _productService.GetProductsAsync(UserSession.CurrentCompany.Id)
            Dim targetProd = allProds.FirstOrDefault(Function(p) p.Id = selectedId)

            If targetProd IsNot Nothing Then
                ShowProductDialog(targetProd)
            End If
        End Sub

        Private Sub ShowProductDialog(prod As Product)
            Using dlg As New Form()
                dlg.Text = If(prod.Id = Guid.Empty, "Register New Product", "Edit Product Details")
                dlg.Size = New Size(540, 480)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblSku As New Label With {.Text = "Product SKU / Code:", .Location = New Point(25, 20), .Size = New Size(140, 20)}
                Dim txtSku As New TextBox With {.Text = prod.Sku, .Location = New Point(25, 45), .Size = New Size(220, 26)}

                Dim lblBarcode As New Label With {.Text = "Barcode (EAN-13):", .Location = New Point(270, 20), .Size = New Size(140, 20)}
                Dim txtBarcode As New TextBox With {.Text = prod.Barcode, .Location = New Point(270, 45), .Size = New Size(220, 26)}

                Dim lblName As New Label With {.Text = "Product Name:", .Location = New Point(25, 85), .Size = New Size(140, 20)}
                Dim txtName As New TextBox With {.Text = prod.Name, .Location = New Point(25, 110), .Size = New Size(465, 26)}

                Dim lblCost As New Label With {.Text = "Cost Price:", .Location = New Point(25, 150), .Size = New Size(140, 20)}
                Dim numCost As New NumericUpDown With {.Location = New Point(25, 175), .Size = New Size(220, 26), .Maximum = 10000000D, .Value = prod.CostPrice, .DecimalPlaces = 2}

                Dim lblSell As New Label With {.Text = "Selling Price:", .Location = New Point(270, 150), .Size = New Size(140, 20)}
                Dim numSell As New NumericUpDown With {.Location = New Point(270, 175), .Size = New Size(220, 26), .Maximum = 10000000D, .Value = prod.SellingPrice, .DecimalPlaces = 2}

                Dim chkService As New CheckBox With {.Text = "Is Service / Consulting", .Checked = prod.IsService, .Location = New Point(25, 225), .Size = New Size(200, 26)}
                Dim chkActive As New CheckBox With {.Text = "Product is Active", .Checked = prod.IsActive, .Location = New Point(270, 225), .Size = New Size(180, 26)}

                Dim btnSave As New Button With {.Text = "Save Product", .Location = New Point(25, 330), .Size = New Size(465, 42)}
                UITheme.StylePrimaryButton(btnSave)

                AddHandler btnSave.Click, Async Sub()
                    If String.IsNullOrWhiteSpace(txtSku.Text) OrElse String.IsNullOrWhiteSpace(txtName.Text) Then
                        MessageBox.Show("Please enter product SKU and name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    prod.Sku = txtSku.Text.Trim()
                    prod.Barcode = txtBarcode.Text.Trim()
                    prod.Name = txtName.Text.Trim()
                    prod.CostPrice = numCost.Value
                    prod.SellingPrice = numSell.Value
                    prod.IsService = chkService.Checked
                    prod.IsActive = chkActive.Checked

                    Dim result = Await _productService.SaveProductAsync(prod)
                    If result.Success Then
                        MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadProducts()
                    Else
                        MessageBox.Show(result.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblSku, txtSku, lblBarcode, txtBarcode, lblName, txtName, lblCost, numCost, lblSell, numSell, chkService, chkActive, btnSave})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
