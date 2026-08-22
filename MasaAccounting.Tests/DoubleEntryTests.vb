Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Xunit
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities

Namespace MasaAccounting.Tests
    Public Class DoubleEntryTests
        <Fact>
        Public Async Function PostJournalEntry_UnbalancedDebitsAndCredits_FailsValidation() As Task
            Dim journalService As New JournalEntryService()
            Dim entry As New JournalEntry With {
                .CompanyId = Guid.NewGuid(),
                .EntryDate = DateTime.Today,
                .ReferenceNumber = "TEST-UNBALANCED"
            }

            ' Debit $1000, Credit $900 -> Unbalanced
            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 1000D,
                .Credit = 0D
            })
            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 0D,
                .Credit = 900D
            })

            Dim result = Await journalService.PostJournalEntryAsync(entry, Guid.NewGuid())

            Assert.False(result.Success)
            Assert.Contains("out of balance", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Async Function PostJournalEntry_LessThanTwoLines_FailsValidation() As Task
            Dim journalService As New JournalEntryService()
            Dim entry As New JournalEntry With {
                .CompanyId = Guid.NewGuid(),
                .EntryDate = DateTime.Today
            }

            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 500D,
                .Credit = 0D
            })

            Dim result = Await journalService.PostJournalEntryAsync(entry, Guid.NewGuid())

            Assert.False(result.Success)
            Assert.Contains("at least two transaction lines", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Async Function PostJournalEntry_MissingAccountId_FailsValidation() As Task
            Dim journalService As New JournalEntryService()
            Dim entry As New JournalEntry With {
                .CompanyId = Guid.NewGuid(),
                .EntryDate = DateTime.Today
            }

            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.Empty,
                .Debit = 500D,
                .Credit = 0D
            })
            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 0D,
                .Credit = 500D
            })

            Dim result = Await journalService.PostJournalEntryAsync(entry, Guid.NewGuid())

            Assert.False(result.Success)
            Assert.Contains("must be assigned to an account", result.Message, StringComparison.OrdinalIgnoreCase)
        End Function

        <Fact>
        Public Async Function PostJournalEntry_DualDebitAndCreditOnSameLine_FailsValidation() As Task
            Dim journalService As New JournalEntryService()
            Dim entry As New JournalEntry With {
                .CompanyId = Guid.NewGuid(),
                .EntryDate = DateTime.Today
            }

            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 500D,
                .Credit = 500D
            })
            entry.Lines.Add(New JournalEntryLine With {
                .AccountId = Guid.NewGuid(),
                .Debit = 0D,
                .Credit = 500D
            })

            Dim result = Await journalService.PostJournalEntryAsync(entry, Guid.NewGuid())

            Assert.False(result.Success)
        End Function
    End Class
End Namespace
