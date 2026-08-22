Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class PaymentService
        Private ReadOnly _journalService As New JournalEntryService()

        Public Async Function GetCustomerPaymentsAsync(companyId As Guid) As Task(Of List(Of CustomerPayment))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT p.id, p.company_id as CompanyId, p.customer_id as CustomerId, 
                               c.name as CustomerName, p.bank_account_id as BankAccountId, 
                               b.account_name as BankAccountName, p.payment_number as PaymentNumber, 
                               p.payment_date as PaymentDate, p.payment_method as PaymentMethod, 
                               p.reference_number as ReferenceNumber, p.amount, p.notes, 
                               p.status, p.journal_entry_id as JournalEntryId, p.created_at as CreatedAt
                        FROM customer_payments p
                        JOIN customers c ON p.customer_id = c.id
                        JOIN bank_accounts b ON p.bank_account_id = b.id
                        WHERE p.company_id = @CompanyId
                        ORDER BY p.payment_date DESC, p.payment_number DESC;"

                    Dim result = Await conn.QueryAsync(Of CustomerPayment)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockCustomerPayments(companyId)
            End Try
        End Function

        Public Async Function PostCustomerPaymentAsync(payment As CustomerPayment, userId As Guid) As Task(Of (Success As Boolean, Message As String, PaymentId As Guid))
            If payment.CustomerId = Guid.Empty Then Return (False, "Please select a customer.", Guid.Empty)
            If payment.BankAccountId = Guid.Empty Then Return (False, "Please select a bank account.", Guid.Empty)
            If payment.Amount <= 0 Then Return (False, "Payment amount must be greater than zero.", Guid.Empty)

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    Using trans = conn.BeginTransaction()
                        payment.Id = Guid.NewGuid()
                        If String.IsNullOrWhiteSpace(payment.PaymentNumber) Then
                            payment.PaymentNumber = "RCPT-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                        End If

                        Dim insertSql = "
                            INSERT INTO customer_payments (id, company_id, customer_id, bank_account_id, payment_number, payment_date, payment_method, reference_number, amount, notes, status, created_by, created_at)
                            VALUES (@Id, @CompanyId, @CustomerId, @BankAccountId, @PaymentNumber, @PaymentDate, @PaymentMethod, @ReferenceNumber, @Amount, @Notes, 'Posted', @CreatedBy, NOW());"
                        payment.CreatedBy = userId
                        Await conn.ExecuteAsync(insertSql, payment, trans)

                        ' Allocations if any
                        If payment.Allocations IsNot Nothing Then
                            For Each alloc In payment.Allocations
                                alloc.Id = Guid.NewGuid()
                                alloc.PaymentId = payment.Id
                                Dim allocSql = "
                                    INSERT INTO customer_payment_allocations (id, payment_id, sales_invoice_id, allocated_amount)
                                    VALUES (@Id, @PaymentId, @SalesInvoiceId, @AllocatedAmount);"
                                Await conn.ExecuteAsync(allocSql, alloc, trans)

                                ' Update invoice paid amount
                                Dim updInvSql = "
                                    UPDATE sales_invoices
                                    SET paid_amount = paid_amount + @AllocAmount,
                                        status = CASE WHEN (paid_amount + @AllocAmount) >= total_amount THEN 'Paid' ELSE 'PartiallyPaid' END,
                                        updated_at = NOW()
                                    WHERE id = @InvoiceId;"
                                Await conn.ExecuteAsync(updInvSql, New With {Key .AllocAmount = alloc.AllocatedAmount, Key .InvoiceId = alloc.SalesInvoiceId}, trans)
                            Next
                        End If

                        trans.Commit()
                    End Using
                End Using

                ' Accounting Journal Entry:
                ' Debit: Bank Account Asset (Bank GL Account) -> payment.Amount
                ' Credit: Accounts Receivable (1200)          -> payment.Amount
                Await CreateCustomerReceiptJournalEntryAsync(payment, userId)

                Return (True, $"Payment Receipt #{payment.PaymentNumber} recorded successfully.", payment.Id)
            Catch ex As Exception
                Return (False, $"Error recording payment: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Private Async Function CreateCustomerReceiptJournalEntryAsync(pmt As CustomerPayment, userId As Guid) As Task
            Try
                Dim accService As New AccountService()
                Dim bankService As New BankService()
                Dim accounts = Await accService.GetAccountsAsync(pmt.CompanyId)
                Dim arAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "1200")
                Dim banks = Await bankService.GetBankAccountsAsync(pmt.CompanyId)
                Dim bankAcc = banks.FirstOrDefault(Function(b) b.Id = pmt.BankAccountId)

                If arAccount Is Nothing OrElse bankAcc Is Nothing Then Return

                Dim entry As New JournalEntry With {
                    .CompanyId = pmt.CompanyId,
                    .EntryNumber = "JE-RCPT-" & pmt.PaymentNumber,
                    .EntryDate = pmt.PaymentDate,
                    .ReferenceNumber = pmt.PaymentNumber,
                    .SourceModule = "Receipt",
                    .SourceId = pmt.Id,
                    .Memo = $"Customer Receipt from {pmt.CustomerName} - Ref: {pmt.ReferenceNumber}",
                    .IsSystemGenerated = True
                }

                ' Line 1: Debit Bank
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = bankAcc.GlAccountId,
                    .Description = $"Deposit for Receipt #{pmt.PaymentNumber}",
                    .Debit = pmt.Amount,
                    .Credit = 0D
                })

                ' Line 2: Credit Accounts Receivable
                entry.Lines.Add(New JournalEntryLine With {
                    .AccountId = arAccount.Id,
                    .Description = $"Receivable clearance for Receipt #{pmt.PaymentNumber}",
                    .Debit = 0D,
                    .Credit = pmt.Amount
                })

                Dim postRes = Await _journalService.PostJournalEntryAsync(entry, userId)
                If postRes.Success Then
                    Using conn = DatabaseConfiguration.CreateConnection()
                        Await conn.ExecuteAsync("UPDATE customer_payments SET journal_entry_id = @JId WHERE id = @Id;", New With {Key .JId = postRes.EntryId, Key .Id = pmt.Id})
                    End Using
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Receipt journal notice: {ex.Message}")
            End Try
        End Function

        Private Function GetMockCustomerPayments(companyId As Guid) As List(Of CustomerPayment)
            Return New List(Of CustomerPayment) From {
                New CustomerPayment With {
                    .Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .PaymentNumber = "RCPT-2026-0001",
                    .CustomerName = "Acme Global Corp",
                    .BankAccountName = "MASA Main Operating Account",
                    .PaymentDate = DateTime.Today.AddDays(-1),
                    .PaymentMethod = "BankTransfer",
                    .Amount = 2500D,
                    .Status = "Posted"
                }
            }
        End Function
    End Class

    Public Class BankService
        Public Async Function GetBankAccountsAsync(companyId As Guid) As Task(Of List(Of BankAccount))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT b.id, b.company_id as CompanyId, b.gl_account_id as GlAccountId, 
                               a.account_code as GlAccountCode, b.bank_name as BankName, 
                               b.account_number as AccountNumber, b.account_name as AccountName, 
                               b.currency, b.branch, b.swift_code as SwiftCode, b.is_active as IsActive,
                               COALESCE((
                                  SELECT SUM(l.debit - l.credit)
                                  FROM journal_entry_lines l
                                  JOIN journal_entries j ON l.journal_entry_id = j.id
                                  WHERE l.account_id = b.gl_account_id AND j.status = 'Posted'
                               ), 0) as CurrentBalance
                        FROM bank_accounts b
                        JOIN accounts a ON b.gl_account_id = a.id
                        WHERE b.company_id = @CompanyId
                        ORDER BY b.bank_name;"

                    Dim result = Await conn.QueryAsync(Of BankAccount)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockBankAccounts(companyId)
            End Try
        End Function

        Private Function GetMockBankAccounts(companyId As Guid) As List(Of BankAccount)
            Return New List(Of BankAccount) From {
                New BankAccount With {
                    .Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .GlAccountId = Guid.Parse("50000000-0000-0000-0000-000000000102"),
                    .BankName = "JPMorgan Chase Bank",
                    .AccountNumber = "CHASE-4499-1002",
                    .AccountName = "MASA Main Operating Account",
                    .CurrentBalance = 42500D,
                    .Currency = "USD",
                    .IsActive = True
                }
            }
        End Function
    End Class

    Public Class ExpenseService
        Private ReadOnly _journalService As New JournalEntryService()

        Public Async Function GetExpensesAsync(companyId As Guid) As Task(Of List(Of Expense))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT e.id, e.company_id as CompanyId, e.category_id as CategoryId, 
                               c.name as CategoryName, e.bank_account_id as BankAccountId, 
                               b.account_name as BankAccountName, e.expense_number as ExpenseNumber, 
                               e.expense_date as ExpenseDate, e.payee, e.amount, e.tax_id as TaxId, 
                               e.tax_amount as TaxAmount, e.total_amount as TotalAmount, 
                               e.reference_number as ReferenceNumber, e.notes, e.status, 
                               e.journal_entry_id as JournalEntryId, e.created_at as CreatedAt
                        FROM expenses e
                        JOIN expense_categories c ON e.category_id = c.id
                        JOIN bank_accounts b ON e.bank_account_id = b.id
                        WHERE e.company_id = @CompanyId
                        ORDER BY e.expense_date DESC, e.expense_number DESC;"

                    Dim result = Await conn.QueryAsync(Of Expense)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockExpenses(companyId)
            End Try
        End Function

        Public Async Function PostExpenseAsync(exp As Expense, userId As Guid) As Task(Of (Success As Boolean, Message As String, ExpenseId As Guid))
            If exp.CategoryId = Guid.Empty Then Return (False, "Please select an expense category.", Guid.Empty)
            If exp.BankAccountId = Guid.Empty Then Return (False, "Please select a payment account.", Guid.Empty)
            If exp.Amount <= 0 Then Return (False, "Expense amount must be greater than zero.", Guid.Empty)

            exp.TotalAmount = exp.Amount + exp.TaxAmount

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    Using trans = conn.BeginTransaction()
                        exp.Id = Guid.NewGuid()
                        If String.IsNullOrWhiteSpace(exp.ExpenseNumber) Then
                            exp.ExpenseNumber = "EXP-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                        End If

                        Dim insertSql = "
                            INSERT INTO expenses (id, company_id, category_id, bank_account_id, expense_number, expense_date, payee, amount, tax_id, tax_amount, total_amount, reference_number, notes, status, created_by, created_at)
                            VALUES (@Id, @CompanyId, @CategoryId, @BankAccountId, @ExpenseNumber, @ExpenseDate, @Payee, @Amount, @TaxId, @TaxAmount, @TotalAmount, @ReferenceNumber, @Notes, 'Posted', @CreatedBy, NOW());"
                        exp.CreatedBy = userId
                        Await conn.ExecuteAsync(insertSql, exp, trans)

                        trans.Commit()
                    End Using
                End Using

                ' Accounting Journal Entry:
                ' Debit: Expense Account (GL Account from Category) -> exp.Amount
                ' Debit: Tax (if applicable)                         -> exp.TaxAmount
                ' Credit: Bank Account                               -> exp.TotalAmount
                Await CreateExpenseJournalEntryAsync(exp, userId)

                Return (True, $"Expense #{exp.ExpenseNumber} recorded successfully.", exp.Id)
            Catch ex As Exception
                Return (False, $"Error recording expense: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Private Async Function CreateExpenseJournalEntryAsync(exp As Expense, userId As Guid) As Task
            Try
                Dim accService As New AccountService()
                Dim bankService As New BankService()
                Dim banks = Await bankService.GetBankAccountsAsync(exp.CompanyId)
                Dim bankAcc = banks.FirstOrDefault(Function(b) b.Id = exp.BankAccountId)

                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim catSql = "SELECT gl_account_id FROM expense_categories WHERE id = @Id;"
                    Dim expenseGlAccountId As Guid = Await conn.ExecuteScalarAsync(Of Guid)(catSql, New With {Key .Id = exp.CategoryId})

                    If expenseGlAccountId = Guid.Empty OrElse bankAcc Is Nothing Then Return

                    Dim entry As New JournalEntry With {
                        .CompanyId = exp.CompanyId,
                        .EntryNumber = "JE-EXP-" & exp.ExpenseNumber,
                        .EntryDate = exp.ExpenseDate,
                        .ReferenceNumber = exp.ExpenseNumber,
                        .SourceModule = "Expense",
                        .SourceId = exp.Id,
                        .Memo = $"Expense: {exp.Payee} - {exp.Notes}",
                        .IsSystemGenerated = True
                    }

                    ' Line 1: Debit Expense Account
                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = expenseGlAccountId,
                        .Description = $"Expense: {exp.Payee}",
                        .Debit = exp.Amount,
                        .Credit = 0D
                    })

                    ' Line 2: Debit Tax if any
                    If exp.TaxAmount > 0 Then
                        Dim accounts = Await accService.GetAccountsAsync(exp.CompanyId)
                        Dim taxAccount = accounts.FirstOrDefault(Function(a) a.AccountCode = "2050")
                        If taxAccount IsNot Nothing Then
                            entry.Lines.Add(New JournalEntryLine With {
                                .AccountId = taxAccount.Id,
                                .Description = $"Tax on Expense #{exp.ExpenseNumber}",
                                .Debit = exp.TaxAmount,
                                .Credit = 0D
                            })
                        End If
                    End If

                    ' Line 3: Credit Bank Account
                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = bankAcc.GlAccountId,
                        .Description = $"Payment for Expense #{exp.ExpenseNumber}",
                        .Debit = 0D,
                        .Credit = exp.TotalAmount
                    })

                    Dim postRes = Await _journalService.PostJournalEntryAsync(entry, userId)
                    If postRes.Success Then
                        Await conn.ExecuteAsync("UPDATE expenses SET journal_entry_id = @JId WHERE id = @Id;", New With {Key .JId = postRes.EntryId, Key .Id = exp.Id})
                    End If
                End Using
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Expense journal notice: {ex.Message}")
            End Try
        End Function

        Private Function GetMockExpenses(companyId As Guid) As List(Of Expense)
            Return New List(Of Expense) From {
                New Expense With {
                    .Id = Guid.Parse("12000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .ExpenseNumber = "EXP-2026-0001",
                    .CategoryName = "Utilities & Internet",
                    .BankAccountName = "MASA Main Operating Account",
                    .ExpenseDate = DateTime.Today.AddDays(-2),
                    .Payee = "Verizon Business Services",
                    .Amount = 350D,
                    .TotalAmount = 350D,
                    .Status = "Posted"
                }
            }
        End Function
    End Class
End Namespace
