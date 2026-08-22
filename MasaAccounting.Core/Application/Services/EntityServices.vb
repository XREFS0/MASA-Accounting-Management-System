Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class CustomerService
        Public Async Function GetCustomersAsync(companyId As Guid, Optional search As String = "") As Task(Of List(Of Customer))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT c.id, c.company_id as CompanyId, c.code, c.name, c.company_name as CompanyName, 
                               c.tax_number as TaxNumber, c.email, c.phone, c.billing_address as BillingAddress, 
                               c.shipping_address as ShippingAddress, c.credit_limit as CreditLimit, 
                               c.payment_terms_days as PaymentTermsDays, c.is_active as IsActive,
                               COALESCE((
                                  SELECT SUM(i.total_amount - i.paid_amount)
                                  FROM sales_invoices i
                                  WHERE i.customer_id = c.id AND i.status IN ('Posted', 'PartiallyPaid')
                               ), 0) as OutstandingBalance
                        FROM customers c
                        WHERE c.company_id = @CompanyId
                        " & If(Not String.IsNullOrEmpty(search), "AND (LOWER(c.name) LIKE @Search OR LOWER(c.code) LIKE @Search OR LOWER(c.email) LIKE @Search)", "") & "
                        ORDER BY c.name;"

                    Dim result = Await conn.QueryAsync(Of Customer)(sql, New With {Key .CompanyId = companyId, Key .Search = $"%{search.ToLower()}%"})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockCustomers(companyId)
            End Try
        End Function

        Public Async Function SaveCustomerAsync(cust As Customer) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(cust.Name) Then Return (False, "Customer name is required.")
            If String.IsNullOrWhiteSpace(cust.Code) Then Return (False, "Customer code is required.")

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    If cust.Id = Guid.Empty Then
                        cust.Id = Guid.NewGuid()
                        Dim sql = "
                            INSERT INTO customers (id, company_id, code, name, company_name, tax_number, email, phone, billing_address, shipping_address, credit_limit, payment_terms_days, is_active)
                            VALUES (@Id, @CompanyId, @Code, @Name, @CompanyName, @TaxNumber, @Email, @Phone, @BillingAddress, @ShippingAddress, @CreditLimit, @PaymentTermsDays, @IsActive);"
                        Await conn.ExecuteAsync(sql, cust)
                        Return (True, "Customer registered successfully.")
                    Else
                        Dim sql = "
                            UPDATE customers
                            SET code = @Code, name = @Name, company_name = @CompanyName, tax_number = @TaxNumber,
                                email = @Email, phone = @Phone, billing_address = @BillingAddress,
                                shipping_address = @ShippingAddress, credit_limit = @CreditLimit,
                                payment_terms_days = @PaymentTermsDays, is_active = @IsActive, updated_at = NOW()
                            WHERE id = @Id AND company_id = @CompanyId;"
                        Await conn.ExecuteAsync(sql, cust)
                        Return (True, "Customer updated successfully.")
                    End If
                End Using
            Catch ex As Exception
                Return (False, $"Database error: {ex.Message}")
            End Try
        End Function

        Private Function GetMockCustomers(companyId As Guid) As List(Of Customer)
            Return New List(Of Customer) From {
                New Customer With {.Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), .CompanyId = companyId, .Code = "CUST-EG-001", .Name = "El Sewedy Electric S.A.E.", .CompanyName = "El Sewedy Electric Corporation", .Email = "procurement@elsewedy.com", .Phone = "+20 2 2759 9700", .CreditLimit = 500000D, .OutstandingBalance = 48500D, .IsActive = True},
                New Customer With {.Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), .CompanyId = companyId, .Code = "CUST-EG-002", .Name = "Talaat Moustafa Group (TMG)", .CompanyName = "Alexandria Real Estate S.A.E.", .Email = "finance@tmg.com.eg", .Phone = "+20 2 3331 2000", .CreditLimit = 350000D, .OutstandingBalance = 12600D, .IsActive = True}
            }
        End Function
    End Class

    Public Class SupplierService
        Public Async Function GetSuppliersAsync(companyId As Guid, Optional search As String = "") As Task(Of List(Of Supplier))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT s.id, s.company_id as CompanyId, s.code, s.name, s.company_name as CompanyName, 
                               s.tax_number as TaxNumber, s.email, s.phone, s.address, 
                               s.payment_terms_days as PaymentTermsDays, s.is_active as IsActive,
                               COALESCE((
                                  SELECT SUM(p.total_amount - p.paid_amount)
                                  FROM purchase_invoices p
                                  WHERE p.supplier_id = s.id AND p.status IN ('Posted', 'PartiallyPaid')
                               ), 0) as OutstandingBalance
                        FROM suppliers s
                        WHERE s.company_id = @CompanyId
                        " & If(Not String.IsNullOrEmpty(search), "AND (LOWER(s.name) LIKE @Search OR LOWER(s.code) LIKE @Search)", "") & "
                        ORDER BY s.name;"

                    Dim result = Await conn.QueryAsync(Of Supplier)(sql, New With {Key .CompanyId = companyId, Key .Search = $"%{search.ToLower()}%"})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockSuppliers(companyId)
            End Try
        End Function

        Public Async Function SaveSupplierAsync(supp As Supplier) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(supp.Name) Then Return (False, "Supplier name is required.")
            If String.IsNullOrWhiteSpace(supp.Code) Then Return (False, "Supplier code is required.")

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    If supp.Id = Guid.Empty Then
                        supp.Id = Guid.NewGuid()
                        Dim sql = "
                            INSERT INTO suppliers (id, company_id, code, name, company_name, tax_number, email, phone, address, payment_terms_days, is_active)
                            VALUES (@Id, @CompanyId, @Code, @Name, @CompanyName, @TaxNumber, @Email, @Phone, @Address, @PaymentTermsDays, @IsActive);"
                        Await conn.ExecuteAsync(sql, supp)
                        Return (True, "Supplier registered successfully.")
                    Else
                        Dim sql = "
                            UPDATE suppliers
                            SET code = @Code, name = @Name, company_name = @CompanyName, tax_number = @TaxNumber,
                                email = @Email, phone = @Phone, address = @Address,
                                payment_terms_days = @PaymentTermsDays, is_active = @IsActive, updated_at = NOW()
                            WHERE id = @Id AND company_id = @CompanyId;"
                        Await conn.ExecuteAsync(sql, supp)
                        Return (True, "Supplier updated successfully.")
                    End If
                End Using
            Catch ex As Exception
                Return (False, $"Database error: {ex.Message}")
            End Try
        End Function

        Private Function GetMockSuppliers(companyId As Guid) As List(Of Supplier)
            Return New List(Of Supplier) From {
                New Supplier With {.Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"), .CompanyId = companyId, .Code = "SUPP-EG-001", .Name = "Mantrac Egypt LLC", .CompanyName = "Mantrac Unatrac Distribution LLC", .Email = "orders@mantracegypt.com", .Phone = "+20 2 3539 0000", .OutstandingBalance = 38000D, .IsActive = True},
                New Supplier With {.Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"), .CompanyId = companyId, .Code = "SUPP-EG-002", .Name = "Telecom Egypt (WE)", .CompanyName = "Telecom Egypt S.A.E.", .Email = "enterprise@te.eg", .Phone = "+20 2 3131 5555", .OutstandingBalance = 4200D, .IsActive = True}
            }
        End Function
    End Class

    Public Class ProductService
        Public Async Function GetProductsAsync(companyId As Guid) As Task(Of List(Of Product))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT p.id, p.company_id as CompanyId, p.category_id as CategoryId, 
                               c.name as CategoryName, p.uom_id as UomId, u.code as UomCode,
                               p.sku, p.barcode, p.name, p.description, p.cost_price as CostPrice, 
                               p.selling_price as SellingPrice, p.tax_id as TaxId, t.rate as TaxRate,
                               p.is_service as IsService, p.is_active as IsActive, p.reorder_level as ReorderLevel,
                               COALESCE((
                                  SELECT SUM(
                                      CASE 
                                          WHEN m.movement_type IN ('StockIn', 'TransferIn') THEN m.quantity
                                          WHEN m.movement_type IN ('StockOut', 'TransferOut') THEN -m.quantity
                                          WHEN m.movement_type = 'Adjustment' THEN m.quantity
                                          ELSE 0
                                      END
                                  )
                                  FROM stock_movements m
                                  WHERE m.product_id = p.id
                               ), 0) as CurrentStock
                        FROM products p
                        LEFT JOIN product_categories c ON p.category_id = c.id
                        LEFT JOIN units_of_measure u ON p.uom_id = u.id
                        LEFT JOIN taxes t ON p.tax_id = t.id
                        WHERE p.company_id = @CompanyId
                        ORDER BY p.sku;"

                    Dim result = Await conn.QueryAsync(Of Product)(sql, New With {Key .CompanyId = companyId})
                    Return result.ToList()
                End Using
            Catch
                Return GetMockProducts(companyId)
            End Try
        End Function

        Public Async Function SaveProductAsync(prod As Product) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(prod.Name) Then Return (False, "Product name is required.")
            If String.IsNullOrWhiteSpace(prod.Sku) Then Return (False, "Product SKU is required.")

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    If prod.Id = Guid.Empty Then
                        prod.Id = Guid.NewGuid()
                        Dim sql = "
                            INSERT INTO products (id, company_id, category_id, uom_id, sku, barcode, name, description, cost_price, selling_price, tax_id, is_service, is_active, reorder_level)
                            VALUES (@Id, @CompanyId, @CategoryId, @UomId, @Sku, @Barcode, @Name, @Description, @CostPrice, @SellingPrice, @TaxId, @IsService, @IsActive, @ReorderLevel);"
                        Await conn.ExecuteAsync(sql, prod)
                        Return (True, "Product saved successfully.")
                    Else
                        Dim sql = "
                            UPDATE products
                            SET category_id = @CategoryId, uom_id = @UomId, sku = @Sku, barcode = @Barcode,
                                name = @Name, description = @Description, cost_price = @CostPrice,
                                selling_price = @SellingPrice, tax_id = @TaxId, is_service = @IsService,
                                is_active = @IsActive, reorder_level = @ReorderLevel, updated_at = NOW()
                            WHERE id = @Id AND company_id = @CompanyId;"
                        Await conn.ExecuteAsync(sql, prod)
                        Return (True, "Product updated successfully.")
                    End If
                End Using
            Catch ex As Exception
                Return (False, $"Database error: {ex.Message}")
            End Try
        End Function

        Private Function GetMockProducts(companyId As Guid) As List(Of Product)
            Return New List(Of Product) From {
                New Product With {.Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"), .CompanyId = companyId, .Sku = "HW-DELL-5520", .Name = "Dell Latitude 5520 Laptop", .CostPrice = 36500D, .SellingPrice = 46000D, .CurrentStock = 18D, .IsActive = True, .UomCode = "PCS"},
                New Product With {.Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"), .CompanyId = companyId, .Sku = "NET-CISCO-C9200", .Name = "Cisco Catalyst 9200 Switch", .CostPrice = 28000D, .SellingPrice = 37500D, .CurrentStock = 12D, .IsActive = True, .UomCode = "PCS"},
                New Product With {.Id = Guid.Parse("c0000000-0000-0000-0000-000000000003"), .CompanyId = companyId, .Sku = "SRV-ETA-INTEG", .Name = "ETA E-Invoicing Support (Hour)", .CostPrice = 0D, .SellingPrice = 1800D, .CurrentStock = 0D, .IsService = True, .IsActive = True, .UomCode = "HRS"}
            }
        End Function
    End Class
End Namespace
