Imports System

Namespace Domain.Entities
    Public Class Company
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property LegalName As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property RegistrationNumber As String = String.Empty
        Public Property BaseCurrency As String = "USD"
        Public Property FiscalYearStartMonth As Integer = 1
        Public Property FiscalYearStartDay As Integer = 1
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property City As String = String.Empty
        Public Property Country As String = String.Empty
        Public Property LogoUrl As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Role
        Public Property Id As Guid
        Public Property CompanyId As Guid?
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsSystemRole As Boolean = False
    End Class

    Public Class AppUser
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property RoleId As Guid
        Public Property RoleName As String = String.Empty
        Public Property Username As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property PasswordHash As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property IsLocked As Boolean = False
        Public Property LastLoginAt As DateTime?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class FiscalYear
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property Name As String = String.Empty
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property IsClosed As Boolean = False
    End Class

    Public Class AccountingPeriod
        Public Property Id As Guid
        Public Property FiscalYearId As Guid
        Public Property PeriodNumber As Integer
        Public Property Name As String = String.Empty
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property IsClosed As Boolean = False
    End Class

    Public Class AccountCategory
        Public Property Id As Guid
        Public Property Name As String = String.Empty
        Public Property CategoryType As String = "Asset"
        Public Property NormalBalance As String = "Debit"
        Public Property DisplayOrder As Integer = 0
    End Class

    Public Class Account
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property CategoryId As Guid
        Public Property CategoryName As String = String.Empty
        Public Property CategoryType As String = "Asset"
        Public Property ParentId As Guid?
        Public Property AccountCode As String = String.Empty
        Public Property AccountName As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Currency As String = "USD"
        Public Property IsHeader As Boolean = False
        Public Property IsActive As Boolean = True
        Public Property CurrentBalance As Decimal = 0D

        Public ReadOnly Property FullDisplay As String
            Get
                Return $"{AccountCode} - {AccountName}"
            End Get
        End Property
    End Class

    Public Class JournalEntry
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property PeriodId As Guid?
        Public Property EntryNumber As String = String.Empty
        Public Property EntryDate As DateTime = DateTime.Today
        Public Property ReferenceNumber As String = String.Empty
        Public Property SourceModule As String = "GeneralJournal"
        Public Property SourceId As Guid?
        Public Property Memo As String = String.Empty
        Public Property Status As String = "Draft"
        Public Property TotalDebit As Decimal = 0D
        Public Property TotalCredit As Decimal = 0D
        Public Property IsSystemGenerated As Boolean = False
        Public Property PostedBy As Guid?
        Public Property PostedByName As String = String.Empty
        Public Property PostedAt As DateTime?
        Public Property ReversedEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Lines As List(Of JournalEntryLine) = New List(Of JournalEntryLine)()
    End Class

    Public Class JournalEntryLine
        Public Property Id As Guid
        Public Property JournalEntryId As Guid
        Public Property AccountId As Guid
        Public Property AccountCode As String = String.Empty
        Public Property AccountName As String = String.Empty
        Public Property LineNumber As Integer
        Public Property Description As String = String.Empty
        Public Property Debit As Decimal = 0D
        Public Property Credit As Decimal = 0D
    End Class

    Public Class Customer
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property BillingAddress As String = String.Empty
        Public Property ShippingAddress As String = String.Empty
        Public Property CreditLimit As Decimal = 0D
        Public Property PaymentTermsDays As Integer = 30
        Public Property OutstandingBalance As Decimal = 0D
        Public Property IsActive As Boolean = True
    End Class

    Public Class Supplier
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property PaymentTermsDays As Integer = 30
        Public Property OutstandingBalance As Decimal = 0D
        Public Property IsActive As Boolean = True
    End Class

    Public Class Warehouse
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Location As String = String.Empty
        Public Property IsActive As Boolean = True
    End Class

    Public Class Product
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property CategoryId As Guid?
        Public Property CategoryName As String = String.Empty
        Public Property UomId As Guid?
        Public Property UomCode As String = "PCS"
        Public Property Sku As String = String.Empty
        Public Property Barcode As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property CostPrice As Decimal = 0D
        Public Property SellingPrice As Decimal = 0D
        Public Property TaxId As Guid?
        Public Property TaxRate As Decimal = 0D
        Public Property IsService As Boolean = False
        Public Property IsActive As Boolean = True
        Public Property ReorderLevel As Decimal = 0D
        Public Property CurrentStock As Decimal = 0D
    End Class

    Public Class StockMovement
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property ProductId As Guid
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property WarehouseId As Guid
        Public Property WarehouseName As String = String.Empty
        Public Property MovementType As String = "StockIn"
        Public Property ReferenceType As String = String.Empty
        Public Property ReferenceId As Guid?
        Public Property Quantity As Decimal = 0D
        Public Property UnitCost As Decimal = 0D
        Public Property TotalCost As Decimal = 0D
        Public Property MovementDate As DateTime = DateTime.UtcNow
        Public Property Notes As String = String.Empty
        Public Property CreatedBy As Guid?
    End Class

    Public Class SalesInvoice
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property CustomerId As Guid
        Public Property CustomerName As String = String.Empty
        Public Property InvoiceNumber As String = String.Empty
        Public Property InvoiceDate As DateTime = DateTime.Today
        Public Property DueDate As DateTime = DateTime.Today.AddDays(30)
        Public Property WarehouseId As Guid?
        Public Property Status As String = "Draft"
        Public Property Currency As String = "USD"
        Public Property ExchangeRate As Decimal = 1D
        Public Property Subtotal As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxAmount As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
        Public Property PaidAmount As Decimal = 0D
        Public Property OutstandingAmount As Decimal = 0D
        Public Property Notes As String = String.Empty
        Public Property TermsAndConditions As String = String.Empty
        Public Property JournalEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Items As List(Of SalesInvoiceItem) = New List(Of SalesInvoiceItem)()
    End Class

    Public Class SalesInvoiceItem
        Public Property Id As Guid
        Public Property SalesInvoiceId As Guid
        Public Property ProductId As Guid?
        Public Property ProductSku As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal = 1D
        Public Property UnitPrice As Decimal = 0D
        Public Property DiscountRate As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxId As Guid?
        Public Property TaxRate As Decimal = 0D
        Public Property TaxAmount As Decimal = 0D
        Public Property LineTotal As Decimal = 0D
    End Class

    Public Class PurchaseInvoice
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property SupplierId As Guid
        Public Property SupplierName As String = String.Empty
        Public Property BillNumber As String = String.Empty
        Public Property SupplierInvoiceNumber As String = String.Empty
        Public Property BillDate As DateTime = DateTime.Today
        Public Property DueDate As DateTime = DateTime.Today.AddDays(30)
        Public Property WarehouseId As Guid?
        Public Property Status As String = "Draft"
        Public Property Currency As String = "USD"
        Public Property ExchangeRate As Decimal = 1D
        Public Property Subtotal As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxAmount As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
        Public Property PaidAmount As Decimal = 0D
        Public Property OutstandingAmount As Decimal = 0D
        Public Property Notes As String = String.Empty
        Public Property JournalEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Items As List(Of PurchaseInvoiceItem) = New List(Of PurchaseInvoiceItem)()
    End Class

    Public Class PurchaseInvoiceItem
        Public Property Id As Guid
        Public Property PurchaseInvoiceId As Guid
        Public Property ProductId As Guid?
        Public Property ProductSku As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal = 1D
        Public Property UnitCost As Decimal = 0D
        Public Property DiscountRate As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxId As Guid?
        Public Property TaxRate As Decimal = 0D
        Public Property TaxAmount As Decimal = 0D
        Public Property LineTotal As Decimal = 0D
    End Class

    Public Class BankAccount
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property GlAccountId As Guid
        Public Property GlAccountCode As String = String.Empty
        Public Property BankName As String = String.Empty
        Public Property AccountNumber As String = String.Empty
        Public Property AccountName As String = String.Empty
        Public Property Currency As String = "USD"
        Public Property Branch As String = String.Empty
        Public Property SwiftCode As String = String.Empty
        Public Property CurrentBalance As Decimal = 0D
        Public Property IsActive As Boolean = True

        Public ReadOnly Property FullDisplay As String
            Get
                Return $"{BankName} ({AccountNumber}) - {AccountName}"
            End Get
        End Property
    End Class

    Public Class CustomerPayment
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property CustomerId As Guid
        Public Property CustomerName As String = String.Empty
        Public Property BankAccountId As Guid
        Public Property BankAccountName As String = String.Empty
        Public Property PaymentNumber As String = String.Empty
        Public Property PaymentDate As DateTime = DateTime.Today
        Public Property PaymentMethod As String = "BankTransfer"
        Public Property ReferenceNumber As String = String.Empty
        Public Property Amount As Decimal = 0D
        Public Property Notes As String = String.Empty
        Public Property Status As String = "Posted"
        Public Property JournalEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Allocations As List(Of CustomerPaymentAllocation) = New List(Of CustomerPaymentAllocation)()
    End Class

    Public Class CustomerPaymentAllocation
        Public Property Id As Guid
        Public Property PaymentId As Guid
        Public Property SalesInvoiceId As Guid
        Public Property InvoiceNumber As String = String.Empty
        Public Property AllocatedAmount As Decimal = 0D
    End Class

    Public Class SupplierPayment
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property SupplierId As Guid
        Public Property SupplierName As String = String.Empty
        Public Property BankAccountId As Guid
        Public Property BankAccountName As String = String.Empty
        Public Property PaymentNumber As String = String.Empty
        Public Property PaymentDate As DateTime = DateTime.Today
        Public Property PaymentMethod As String = "BankTransfer"
        Public Property ReferenceNumber As String = String.Empty
        Public Property Amount As Decimal = 0D
        Public Property Notes As String = String.Empty
        Public Property Status As String = "Posted"
        Public Property JournalEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Allocations As List(Of SupplierPaymentAllocation) = New List(Of SupplierPaymentAllocation)()
    End Class

    Public Class SupplierPaymentAllocation
        Public Property Id As Guid
        Public Property PaymentId As Guid
        Public Property PurchaseInvoiceId As Guid
        Public Property BillNumber As String = String.Empty
        Public Property AllocatedAmount As Decimal = 0D
    End Class

    Public Class ExpenseCategory
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property GlAccountId As Guid
        Public Property GlAccountName As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsActive As Boolean = True
    End Class

    Public Class Expense
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property CategoryId As Guid
        Public Property CategoryName As String = String.Empty
        Public Property BankAccountId As Guid
        Public Property BankAccountName As String = String.Empty
        Public Property ExpenseNumber As String = String.Empty
        Public Property ExpenseDate As DateTime = DateTime.Today
        Public Property Payee As String = String.Empty
        Public Property Amount As Decimal = 0D
        Public Property TaxId As Guid?
        Public Property TaxAmount As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
        Public Property ReferenceNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property Status As String = "Posted"
        Public Property JournalEntryId As Guid?
        Public Property CreatedBy As Guid?
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class AuditLog
        Public Property Id As Guid
        Public Property CompanyId As Guid?
        Public Property UserId As Guid?
        Public Property UserName As String = String.Empty
        Public Property Action As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property EntityId As Guid?
        Public Property Description As String = String.Empty
        Public Property IpAddress As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Tax
        Public Property Id As Guid
        Public Property CompanyId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Rate As Decimal = 0D
        Public Property TaxType As String = "Sales"
        Public Property IsActive As Boolean = True

        Public ReadOnly Property FullDisplay As String
            Get
                Return $"{Code} - {Name} ({(Rate * 100):N1}%)"
            End Get
        End Property
    End Class

    Public Class Currency
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Symbol As String = String.Empty
        Public Property DecimalPlaces As Integer = 2
        Public Property ExchangeRate As Decimal = 1D
        Public Property IsBase As Boolean = False
        Public Property IsActive As Boolean = True
    End Class
End Namespace
