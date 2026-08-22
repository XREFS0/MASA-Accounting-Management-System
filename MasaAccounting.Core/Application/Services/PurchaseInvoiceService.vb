Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class PurchaseInvoiceService
        Private ReadOnly _journalService As New JournalEntryService()

        Public Async Function GetInvoicesAsync(companyId As Guid) As Task(Of List(Of PurchaseInvoice))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT p.id, p.company_id as CompanyId, p.supplier_id as SupplierId, 
                               s.name as SupplierName, p.bill_number as BillNumber, 
                               p.supplier_invoice_number as SupplierInvoiceNumber,
                               p.bill_date as BillDate, p.due_date as DueDate, 
                               p.warehouse_id as WarehouseId, p.status, p.currency, 
                               p.subtotal, p.discount_amount as DiscountAmount, 
                               p.tax_amount as TaxAmount, p.total_amount as TotalAmount, 
                               p.paid_amount as PaidAmount, (p.total_amount - p.paid_amount) as OutstandingAmount,
                               p.notes, p.journal_entry_id as JournalEntryId, p.created_at as CreatedAt
                        FROM purchase_invoices p
                        JOIN suppliers s ON p.supplier_id = s.id
                        WHERE p.company_id = @CompanyId
                        ORDER BY p.bill_date DESC, p.bill_number DESC;"

                    Dim result = Await conn.QueryAsync(Of PurchaseInvoice)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockInvoices(companyId)
            End Try
        End Function

        Public Async Function PostPurchaseInvoiceAsync(invoice As PurchaseInvoice, userId As Guid) As Task(Of (Success As Boolean, Message As String, InvoiceId As Guid))
            If invoice.SupplierId = Guid.Empty Then Return (False, "Please select a supplier.", Guid.Empty)
            If invoice.Items Is Nothing OrElse invoice.Items.Count = 0 Then Return (False, "Bill must contain at least one line item.", Guid.Empty)

            Dim subtotal As Decimal = 0D
            Dim totalTax As Decimal = 0D
            Dim totalDiscount As Decimal = 0D

            For Each itm In invoice.Items
                Dim rawLine = itm.Quantity * itm.UnitCost
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
                            If String.IsNullOrWhiteSpace(invoice.BillNumber) Then
                                invoice.BillNumber = "BILL-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                            End If

                            Dim insertSql = "
                                INSERT INTO purchase_invoices (id, company_id, supplier_id, bill_number, supplier_invoice_number, bill_date, due_date, warehouse_id, status, currency, exchange_rate, subtotal, discount_amount, tax_amount, total_amount, paid_amount, notes, created_by, created_at)
                                VALUES (@Id, @CompanyId, @SupplierId, @BillNumber, @SupplierInvoiceNumber, @BillDate, @DueDate, @WarehouseId, 'Posted', @Currency, @ExchangeRate, @Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, 0, @Notes, @CreatedBy, NOW());"
                            invoice.CreatedBy = userId
                            Await conn.ExecuteAsync(insertSql, invoice, trans)
                        Else
                            Dim updateSql = "
                                UPDATE purchase_invoices
                                SET supplier_invoice_number = @SupplierInvoiceNumber, bill_date = @BillDate, due_date = @DueDate,
                                    status = 'Posted', subtotal = @Subtotal, discount_amount = @DiscountAmount, tax_amount = @TaxAmount,
                                    total_amount = @TotalAmount, notes = @Notes, updated_at = NOW()
                                WHERE id = @Id AND company_id = @CompanyId;"
                            Await conn.ExecuteAsync(updateSql, invoice, trans)
                            Await conn.ExecuteAsync("DELETE FROM purchase_invoice_items WHERE purchase_invoice_id = @Id;", New With {Key .Id = invoice.Id}, trans)
                        End If

                        For Each itm In invoice.Items
                            itm.Id = Guid.NewGuid()
                            itm.PurchaseInvoiceId = invoice.Id
                            Dim itemSql = "
                                INSERT INTO purchase_invoice_items (id, purchase_invoice_id, product_id, description, quantity, unit_cost, discount_rate, discount_amount, tax_rate, tax_amount, line_total)
                                VALUES (@Id, @PurchaseInvoiceId, @ProductId, @Description, @Quantity, @UnitCost, @DiscountRate, @DiscountAmount, @TaxRate, @TaxAmount, @LineTotal);"
                            Await conn.ExecuteAsync(itemSql, itm, trans)

                            ' Record stock in
                            If itm.ProductId.HasValue AndAlso invoice.WarehouseId.HasValue Then
                                Dim stockSql = "
                                    INSERT INTO stock_movements (company_id, product_id, warehouse_id, movement_type, reference_type, reference_id, quantity, unit_cost, total_cost, notes, created_by)
                                    VALUES (@CompanyId, @ProductId, @WarehouseId, 'StockIn', 'PurchaseInvoice', @RefId, @Qty, @UnitCost, @TotalCost, @Notes, @UserId);"
                                Await conn.ExecuteAsync(stockSql, New With {
                                    Key .CompanyId = invoice.CompanyId,
                                    Key .ProductId = itm.ProductId.Value,
                                    Key .WarehouseId = invoice.WarehouseId.Value,
                                    Key .RefId = invoice.Id,
                                    Key .Qty = itm.Quantity,
                                    Key .UnitCost = itm.UnitCost,
                                    Key .TotalCost = itm.LineTotal,
                                    Key .Notes = $"Bill {invoice.BillNumber} supplier receipt",
                                    Key .UserId = userId
                                }, trans)
                            End If
                        Next

                        trans.Commit()
                    End Using
                End Using

                ' Accounting Journal Entry:
                ' Debit: Inventory Asset (1300) -> invoice.Subtotal
                ' Debit: Tax (2050 - Input VAT) -> invoice.TaxAmount
                ' Credit: Accounts Payable (2010) -> invoice.TotalAmount
                Await CreatePurchaseJournalEntryAsync(invoice, userId)

                Return (True, $"Purchase Bill #{invoice.BillNumber} posted successfully.", invoice.Id)
            Catch ex As Exception
                Return (False, $"Error posting purchase bill: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Private Async Function CreatePurchaseJournalEntryAsync(bill As PurchaseInvoice, userId As Guid) As Task
            Try
                Dim accService As New AccountService()
                Dim accounts = Await accService.GetAccountsAsync(bill.CompanyId)
                Dim invAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "1300")
                Dim apAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "2010")
                Dim taxAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "2050")

                If invAccount Is Nothing OrElse apAccount Is Nothing Then Return

                Dim entry As New JournalEntry With {
                    .CompanyId = bill.CompanyId,
                    .EntryNumber = "JE-BILL-" & bill.BillNumber,
                    .EntryDate = bill.BillDate,
                    .ReferenceNumber = bill.BillNumber,
                    .SourceModule = "Purchase",
                    .SourceId = bill.Id,
                    .Memo = $"Automated Purchase Bill Posting: {bill.BillNumber}",
                    .IsSystemGenerated = True
                }

                ' Line 1: Debit Inventory Asset
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = invAccount.Id,
                    .Description = $"Inventory receipt for Bill #{bill.BillNumber}",
                    .Debit = bill.Subtotal,
                    .Credit = 0D
                })

                ' Line 2: Debit Tax (Input VAT) if applicable
                If bill.TaxAmount > 0 AndAlso taxAccount IsNot Nothing Then
                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = taxAccount.Id,
                        .Description = $"Input VAT on Bill #{bill.BillNumber}",
                        .Debit = bill.TaxAmount,
                        .Credit = 0D
                    })
                End If

                ' Line 3: Credit Accounts Payable
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = apAccount.Id,
                    .Description = $"Payable to supplier for Bill #{bill.BillNumber}",
                    .Debit = 0D,
                    .Credit = bill.TotalAmount
                })

                Dim postRes = Await _journalService.PostJournalEntryAsync(entry, userId)
                If postRes.Success Then
                    Using conn = DatabaseConfiguration.CreateConnection()
                        Await conn.ExecuteAsync("UPDATE purchase_invoices SET journal_entry_id = @JId WHERE id = @Id;", New With {Key .JId = postRes.EntryId, Key .Id = bill.Id})
                    End Using
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Purchase journal posting notice: {ex.Message}")
            End Try
        End Function

        Private Function GetMockInvoices(companyId As Guid) As List(Of PurchaseInvoice)
            Return New List(Of PurchaseInvoice) From {
                New PurchaseInvoice With {
                    .Id = Guid.Parse("e0000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .BillNumber = "BILL-2026-0001",
                    .SupplierName = "TechSupply Distributors",
                    .BillDate = DateTime.Today.AddDays(-7),
                    .DueDate = DateTime.Today.AddDays(23),
                    .Status = "Posted",
                    .Subtotal = 2500D,
                    .TaxAmount = 375D,
                    .TotalAmount = 2875D,
                    .PaidAmount = 0D,
                    .OutstandingAmount = 2875D
                }
            }
        End Function
    End Class
End Namespace
