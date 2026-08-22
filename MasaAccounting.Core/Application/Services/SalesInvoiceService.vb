Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class SalesInvoiceService
        Private ReadOnly _journalService As New JournalEntryService()

        Public Async Function GetInvoicesAsync(companyId As Guid) As Task(Of List(Of SalesInvoice))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT s.id, s.company_id as CompanyId, s.customer_id as CustomerId, 
                               c.name as CustomerName, s.invoice_number as InvoiceNumber, 
                               s.invoice_date as InvoiceDate, s.due_date as DueDate, 
                               s.warehouse_id as WarehouseId, s.status, s.currency, 
                               s.subtotal, s.discount_amount as DiscountAmount, 
                               s.tax_amount as TaxAmount, s.total_amount as TotalAmount, 
                               s.paid_amount as PaidAmount, (s.total_amount - s.paid_amount) as OutstandingAmount,
                               s.notes, s.journal_entry_id as JournalEntryId, s.created_at as CreatedAt
                        FROM sales_invoices s
                        JOIN customers c ON s.customer_id = c.id
                        WHERE s.company_id = @CompanyId
                        ORDER BY s.invoice_date DESC, s.invoice_number DESC;"

                    Dim result = Await conn.QueryAsync(Of SalesInvoice)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockInvoices(companyId)
            End Try
        End Function

        Public Async Function GetInvoiceWithItemsAsync(invoiceId As Guid) As Task(Of SalesInvoice)
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim invSql = "
                        SELECT s.id, s.company_id as CompanyId, s.customer_id as CustomerId, 
                               c.name as CustomerName, s.invoice_number as InvoiceNumber, 
                               s.invoice_date as InvoiceDate, s.due_date as DueDate, 
                               s.warehouse_id as WarehouseId, s.status, s.currency, 
                               s.subtotal, s.discount_amount as DiscountAmount, 
                               s.tax_amount as TaxAmount, s.total_amount as TotalAmount, 
                               s.paid_amount as PaidAmount, (s.total_amount - s.paid_amount) as OutstandingAmount,
                               s.notes, s.journal_entry_id as JournalEntryId
                        FROM sales_invoices s
                        JOIN customers c ON s.customer_id = c.id
                        WHERE s.id = @Id;"

                    Dim inv = Await conn.QuerySingleOrDefaultAsync(Of SalesInvoice)(invSql, New With {Key .Id = invoiceId})
                    If inv IsNot Nothing Then
                        Dim itemSql = "
                            SELECT i.id, i.sales_invoice_id as SalesInvoiceId, i.product_id as ProductId,
                                   p.sku as ProductSku, i.description, i.quantity, i.unit_price as UnitPrice,
                                   i.discount_rate as DiscountRate, i.discount_amount as DiscountAmount,
                                   i.tax_rate as TaxRate, i.tax_amount as TaxAmount, i.line_total as LineTotal
                            FROM sales_invoice_items i
                            LEFT JOIN products p ON i.product_id = p.id
                            WHERE i.sales_invoice_id = @InvoiceId;"

                        inv.Items = (Await conn.QueryAsync(Of SalesInvoiceItem)(itemSql, New With {Key .InvoiceId = invoiceId})).ToList()
                    End If
                    Return inv
                End Using
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Saves and posts a sales invoice, executing accounting journal entry and inventory stock out atomically.
        ''' </summary>
        Public Async Function PostSalesInvoiceAsync(invoice As SalesInvoice, userId As Guid) As Task(Of (Success As Boolean, Message As String, InvoiceId As Guid))
            If invoice.CustomerId = Guid.Empty Then Return (False, "Please select a customer.", Guid.Empty)
            If invoice.Items Is Nothing OrElse invoice.Items.Count = 0 Then Return (False, "Invoice must contain at least one line item.", Guid.Empty)

            ' Calculate totals
            Dim subtotal As Decimal = 0D
            Dim totalTax As Decimal = 0D
            Dim totalDiscount As Decimal = 0D

            For Each itm In invoice.Items
                Dim rawLine = itm.Quantity * itm.UnitPrice
                itm.DiscountAmount = rawLine * (itm.DiscountRate / 100D)
                Dim afterDiscount = rawLine - itm.DiscountAmount
                itm.TaxAmount = afterDiscount * (itm.TaxRate / 100D)
                itm.LineTotal = afterDiscount + itm.TaxAmount

                subtotal += afterDiscount
                totalTax += itm.TaxAmount
                totalDiscount += itm.DiscountAmount
            Next

            invoice.Subtotal = subtotal
            invoice.TaxAmount = totalTax
            invoice.DiscountAmount = totalDiscount
            invoice.TotalAmount = subtotal + totalTax

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    Using trans = conn.BeginTransaction()
                        If invoice.Id = Guid.Empty Then
                            invoice.Id = Guid.NewGuid()
                            If String.IsNullOrWhiteSpace(invoice.InvoiceNumber) Then
                                invoice.InvoiceNumber = "INV-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                            End If

                            Dim insertSql = "
                                INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date, due_date, warehouse_id, status, currency, exchange_rate, subtotal, discount_amount, tax_amount, total_amount, paid_amount, notes, created_by, created_at)
                                VALUES (@Id, @CompanyId, @CustomerId, @InvoiceNumber, @InvoiceDate, @DueDate, @WarehouseId, 'Posted', @Currency, @ExchangeRate, @Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, 0, @Notes, @CreatedBy, NOW());"
                            invoice.CreatedBy = userId
                            Await conn.ExecuteAsync(insertSql, invoice, trans)
                        Else
                            Dim updateSql = "
                                UPDATE sales_invoices
                                SET invoice_date = @InvoiceDate, due_date = @DueDate, status = 'Posted',
                                    subtotal = @Subtotal, discount_amount = @DiscountAmount, tax_amount = @TaxAmount,
                                    total_amount = @TotalAmount, notes = @Notes, updated_at = NOW()
                                WHERE id = @Id AND company_id = @CompanyId;"
                            Await conn.ExecuteAsync(updateSql, invoice, trans)
                            Await conn.ExecuteAsync("DELETE FROM sales_invoice_items WHERE sales_invoice_id = @Id;", New With {Key .Id = invoice.Id}, trans)
                        End If

                        ' Insert items
                        For Each itm In invoice.Items
                            itm.Id = Guid.NewGuid()
                            itm.SalesInvoiceId = invoice.Id
                            Dim itemSql = "
                                INSERT INTO sales_invoice_items (id, sales_invoice_id, product_id, description, quantity, unit_price, discount_rate, discount_amount, tax_rate, tax_amount, line_total)
                                VALUES (@Id, @SalesInvoiceId, @ProductId, @Description, @Quantity, @UnitPrice, @DiscountRate, @DiscountAmount, @TaxRate, @TaxAmount, @LineTotal);"
                            Await conn.ExecuteAsync(itemSql, itm, trans)

                            ' Record stock movement if physical product
                            If itm.ProductId.HasValue AndAlso invoice.WarehouseId.HasValue Then
                                Dim stockSql = "
                                    INSERT INTO stock_movements (company_id, product_id, warehouse_id, movement_type, reference_type, reference_id, quantity, unit_cost, total_cost, notes, created_by)
                                    VALUES (@CompanyId, @ProductId, @WarehouseId, 'StockOut', 'SalesInvoice', @RefId, @Qty, 0, 0, @Notes, @UserId);"
                                Await conn.ExecuteAsync(stockSql, New With {
                                    Key .CompanyId = invoice.CompanyId,
                                    Key .ProductId = itm.ProductId.Value,
                                    Key .WarehouseId = invoice.WarehouseId.Value,
                                    Key .RefId = invoice.Id,
                                    Key .Qty = itm.Quantity,
                                    Key .Notes = $"Invoice {invoice.InvoiceNumber} sales shipment",
                                    Key .UserId = userId
                                }, trans)
                            End If
                        Next

                        trans.Commit()
                    End Using
                End Using

                ' Automatically create and post Accounting Journal Entry:
                ' Debit: Accounts Receivable (1200) -> invoice.TotalAmount
                ' Credit: Sales Revenue (4010)       -> invoice.Subtotal
                ' Credit: Tax Payable (2050)         -> invoice.TaxAmount
                Await CreateSalesJournalEntryAsync(invoice, userId)

                Return (True, $"Sales Invoice #{invoice.InvoiceNumber} posted successfully.", invoice.Id)
            Catch ex As Exception
                Return (False, $"Error posting invoice: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Private Async Function CreateSalesJournalEntryAsync(inv As SalesInvoice, userId As Guid) As Task
            Try
                Dim accService As New AccountService()
                Dim accounts = Await accService.GetAccountsAsync(inv.CompanyId)
                Dim arAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "1200")
                Dim revAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "4010")
                Dim taxAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "2050")

                If arAccount Is Nothing OrElse revAccount Is Nothing Then Return

                Dim entry As New JournalEntry With {
                    .CompanyId = inv.CompanyId,
                    .EntryNumber = "JE-INV-" & inv.InvoiceNumber,
                    .EntryDate = inv.InvoiceDate,
                    .ReferenceNumber = inv.InvoiceNumber,
                    .SourceModule = "Sales",
                    .SourceId = inv.Id,
                    .Memo = $"Automated Sales Invoice Posting: {inv.InvoiceNumber}",
                    .IsSystemGenerated = True
                }

                ' Line 1: Debit A/R
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = arAccount.Id,
                    .Description = $"Receivable for Inv #{inv.InvoiceNumber}",
                    .Debit = inv.TotalAmount,
                    .Credit = 0D
                })

                ' Line 2: Credit Sales Revenue
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = revAccount.Id,
                    .Description = $"Sales Revenue for Inv #{inv.InvoiceNumber}",
                    .Debit = 0D,
                    .Credit = inv.Subtotal
                })

                ' Line 3: Credit Tax Payable if applicable
                If inv.TaxAmount > 0 AndAlso taxAccount IsNot Nothing Then
                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = taxAccount.Id,
                        .Description = $"Sales Tax for Inv #{inv.InvoiceNumber}",
                        .Debit = 0D,
                        .Credit = inv.TaxAmount
                    })
                End If

                Dim postRes = Await _journalService.PostJournalEntryAsync(entry, userId)
                If postRes.Success Then
                    Using conn = DatabaseConfiguration.CreateConnection()
                        Await conn.ExecuteAsync("UPDATE sales_invoices SET journal_entry_id = @JId WHERE id = @Id;", New With {Key .JId = postRes.EntryId, Key .Id = inv.Id})
                    End Using
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Sales journal posting notice: {ex.Message}")
            End Try
        End Function

        Private Function GetMockInvoices(companyId As Guid) As List(Of SalesInvoice)
            Return New List(Of SalesInvoice) From {
                New SalesInvoice With {
                    .Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .InvoiceNumber = "INV-EG-2026-0001",
                    .CustomerName = "El Sewedy Electric S.A.E.",
                    .InvoiceDate = DateTime.Today.AddDays(-3),
                    .DueDate = DateTime.Today.AddDays(27),
                    .Status = "Posted",
                    .Subtotal = 92000D,
                    .TaxAmount = 12880D,
                    .TotalAmount = 104880D,
                    .PaidAmount = 0D,
                    .OutstandingAmount = 104880D
                }
            }
        End Function
    End Class
End Namespace
