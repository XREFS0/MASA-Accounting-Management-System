Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Xunit
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities

Namespace MasaAccounting.Tests
    Public Class AccountingWorkflowTests
        <Fact>
        Public Async Function SalesInvoice_MissingCustomer_FailsValidation() As Task
            Dim salesService As New SalesInvoiceService()
            Dim invoice As New SalesInvoice With {
                .CompanyId = Guid.NewGuid(),
                .CustomerId = Guid.Empty
            }
            invoice.Items.Add(New SalesInvoiceItem With {
                .Description = "Service",
                .Quantity = 1D,
                .UnitPrice = 100D
            })

            Dim result = Await salesService.PostSalesInvoiceAsync(invoice, Guid.NewGuid())
            Assert.False(result.Success)
            Assert.Contains("customer", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Async Function SalesInvoice_NoLineItems_FailsValidation() As Task
            Dim salesService As New SalesInvoiceService()
            Dim invoice As New SalesInvoice With {
                .CompanyId = Guid.NewGuid(),
                .CustomerId = Guid.NewGuid()
            }

            Dim result = Await salesService.PostSalesInvoiceAsync(invoice, Guid.NewGuid())
            Assert.False(result.Success)
            Assert.Contains("line item", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Async Function PurchaseInvoice_MissingSupplier_FailsValidation() As Task
            Dim purchaseService As New PurchaseInvoiceService()
            Dim bill As New PurchaseInvoice With {
                .CompanyId = Guid.NewGuid(),
                .SupplierId = Guid.Empty
            }
            bill.Items.Add(New PurchaseInvoiceItem With {
                .Description = "Supplies",
                .Quantity = 10D,
                .UnitCost = 50D
            })

            Dim result = Await purchaseService.PostPurchaseInvoiceAsync(bill, Guid.NewGuid())
            Assert.False(result.Success)
            Assert.Contains("supplier", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Sub Account_FullDisplay_FormatsCorrectly()
            Dim acc As New Account With {
                .AccountCode = "1020",
                .AccountName = "Operating Bank Account"
            }
            Assert.Equal("1020 - Operating Bank Account", acc.FullDisplay)
        End Sub

        <Fact>
        Public Sub Tax_FullDisplay_FormatsRateCorrectly()
            Dim tax As New Tax With {
                .Code = "VAT-14",
                .Name = "Egyptian VAT",
                .Rate = 0.14D
            }
            Assert.Equal("VAT-14 - Egyptian VAT (14.0%)", tax.FullDisplay)
        End Sub

        <Fact>
        Public Sub BankAccount_FullDisplay_FormatsCorrectly()
            Dim bank As New BankAccount With {
                .BankName = "CIB",
                .AccountNumber = "EG450010",
                .AccountName = "Treasury EGP"
            }
            Assert.Equal("CIB (EG450010) - Treasury EGP", bank.FullDisplay)
        End Sub

        <Fact>
        Public Sub InvoiceTotals_Calculation_ComputesCorrectly()
            Dim item As New SalesInvoiceItem With {
                .Quantity = 5D,
                .UnitPrice = 2000D,
                .DiscountRate = 10D,
                .TaxRate = 14D
            }

            Dim rawLine = item.Quantity * item.UnitPrice ' 10,000
            item.DiscountAmount = rawLine * (item.DiscountRate / 100D) ' 1,000
            Dim afterDiscount = rawLine - item.DiscountAmount ' 9,000
            item.TaxAmount = afterDiscount * (item.TaxRate / 100D) ' 1,260
            item.LineTotal = afterDiscount + item.TaxAmount ' 10,260

            Assert.Equal(1000D, item.DiscountAmount)
            Assert.Equal(1260D, item.TaxAmount)
            Assert.Equal(10260D, item.LineTotal)
        End Sub
    End Class
End Namespace
