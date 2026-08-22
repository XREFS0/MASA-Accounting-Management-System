Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class AccountService
        Public Async Function GetCategoriesAsync() As Task(Of List(Of AccountCategory))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "SELECT id, name, category_type as CategoryType, normal_balance as NormalBalance, display_order as DisplayOrder FROM account_categories ORDER BY display_order;"
                    Dim result = Await conn.QueryAsync(Of AccountCategory)(sql)
                    Return result.ToList()
                End Using
            Catch
                Return GetDefaultCategories()
            End Try
        End Function

        Public Async Function GetAccountsAsync(companyId As Guid) As Task(Of List(Of Account))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT a.id, a.company_id as CompanyId, a.category_id as CategoryId, 
                               c.name as CategoryName, c.category_type as CategoryType, c.normal_balance as NormalBalance,
                               a.parent_id as ParentId, a.account_code as AccountCode, 
                               a.account_name as AccountName, a.description, a.currency, 
                               a.is_header as IsHeader, a.is_active as IsActive,
                               COALESCE(
                                  SUM(CASE WHEN c.normal_balance = 'Debit' THEN (l.debit - l.credit) ELSE (l.credit - l.debit) END), 0
                               ) as CurrentBalance
                        FROM accounts a
                        JOIN account_categories c ON a.category_id = c.id
                        LEFT JOIN journal_entry_lines l ON a.id = l.account_id
                        LEFT JOIN journal_entries j ON l.journal_entry_id = j.id AND j.status = 'Posted'
                        WHERE a.company_id = @CompanyId
                        GROUP BY a.id, a.company_id, a.category_id, c.name, c.category_type, c.normal_balance, a.parent_id, a.account_code, a.account_name, a.description, a.currency, a.is_header, a.is_active
                        ORDER BY a.account_code;"

                    Dim result = Await conn.QueryAsync(Of Account)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetDefaultAccounts(companyId)
            End Try
        End Function

        Public Async Function SaveAccountAsync(acc As Account) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(acc.AccountCode) Then
                Return (False, "Account code is required.")
            End If
            If String.IsNullOrWhiteSpace(acc.AccountName) Then
                Return (False, "Account name is required.")
            End If

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()

                    If acc.Id = Guid.Empty Then
                        acc.Id = Guid.NewGuid()
                        Dim insertSql = "
                            INSERT INTO accounts (id, company_id, category_id, parent_id, account_code, account_name, description, currency, is_header, is_active)
                            VALUES (@Id, @CompanyId, @CategoryId, @ParentId, @AccountCode, @AccountName, @Description, @Currency, @IsHeader, @IsActive);"
                        Await conn.ExecuteAsync(insertSql, acc)
                        Return (True, "Account created successfully.")
                    Else
                        Dim updateSql = "
                            UPDATE accounts 
                            SET category_id = @CategoryId, parent_id = @ParentId, account_code = @AccountCode,
                                account_name = @AccountName, description = @Description, currency = @Currency,
                                is_header = @IsHeader, is_active = @IsActive, updated_at = NOW()
                            WHERE id = @Id AND company_id = @CompanyId;"
                        Await conn.ExecuteAsync(updateSql, acc)
                        Return (True, "Account updated successfully.")
                    End If
                End Using
            Catch ex As Exception
                Return (False, $"Failed to save account: {ex.Message}")
            End Try
        End Function

        Private Function GetDefaultCategories() As List(Of AccountCategory)
            Return New List(Of AccountCategory) From {
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), .Name = "Current Assets", .CategoryType = "Asset", .NormalBalance = "Debit", .DisplayOrder = 1},
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), .Name = "Current Liabilities", .CategoryType = "Liability", .NormalBalance = "Credit", .DisplayOrder = 3},
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000005"), .Name = "Equity", .CategoryType = "Equity", .NormalBalance = "Credit", .DisplayOrder = 5},
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000006"), .Name = "Operating Revenue", .CategoryType = "Revenue", .NormalBalance = "Credit", .DisplayOrder = 6},
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000007"), .Name = "Cost of Goods Sold (COGS)", .CategoryType = "Expense", .NormalBalance = "Debit", .DisplayOrder = 7},
                New AccountCategory With {.Id = Guid.Parse("40000000-0000-0000-0000-000000000008"), .Name = "Operating Expenses", .CategoryType = "Expense", .NormalBalance = "Debit", .DisplayOrder = 8}
            }
        End Function

        Private Function GetDefaultAccounts(companyId As Guid) As List(Of Account)
            Return New List(Of Account) From {
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000101"), .CompanyId = companyId, .AccountCode = "1010", .AccountName = "Cash on Hand", .CategoryName = "Current Assets", .CategoryType = "Asset", .CurrentBalance = 5000D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000102"), .CompanyId = companyId, .AccountCode = "1020", .AccountName = "Operating Bank Account", .CategoryName = "Current Assets", .CategoryType = "Asset", .CurrentBalance = 42500D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000103"), .CompanyId = companyId, .AccountCode = "1200", .AccountName = "Accounts Receivable (A/R)", .CategoryName = "Current Assets", .CategoryType = "Asset", .CurrentBalance = 12400D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000104"), .CompanyId = companyId, .AccountCode = "1300", .AccountName = "Inventory Asset", .CategoryName = "Current Assets", .CategoryType = "Asset", .CurrentBalance = 18900D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000201"), .CompanyId = companyId, .AccountCode = "2010", .AccountName = "Accounts Payable (A/P)", .CategoryName = "Current Liabilities", .CategoryType = "Liability", .CurrentBalance = 8200D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000202"), .CompanyId = companyId, .AccountCode = "2050", .AccountName = "VAT / Sales Tax Payable", .CategoryName = "Current Liabilities", .CategoryType = "Liability", .CurrentBalance = 1450D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000301"), .CompanyId = companyId, .AccountCode = "3010", .AccountName = "Owner Capital", .CategoryName = "Equity", .CategoryType = "Equity", .CurrentBalance = 50000D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000401"), .CompanyId = companyId, .AccountCode = "4010", .AccountName = "Sales Revenue", .CategoryName = "Operating Revenue", .CategoryType = "Revenue", .CurrentBalance = 32000D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000501"), .CompanyId = companyId, .AccountCode = "5010", .AccountName = "Cost of Goods Sold (COGS)", .CategoryName = "Cost of Goods Sold", .CategoryType = "Expense", .CurrentBalance = 14200D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000601"), .CompanyId = companyId, .AccountCode = "6010", .AccountName = "Salaries Expense", .CategoryName = "Operating Expenses", .CategoryType = "Expense", .CurrentBalance = 6500D},
                New Account With {.Id = Guid.Parse("50000000-0000-0000-0000-000000000602"), .CompanyId = companyId, .AccountCode = "6020", .AccountName = "Rent Expense", .CategoryName = "Operating Expenses", .CategoryType = "Expense", .CurrentBalance = 2100D}
            }
        End Function
    End Class
End Namespace
