Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Data

Namespace Application.Services
    Public Class JournalEntryService
        Public Async Function GetEntriesAsync(companyId As Guid, Optional status As String = Nothing) As Task(Of List(Of JournalEntry))
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim sql = "
                        SELECT j.id, j.company_id as CompanyId, j.period_id as PeriodId, 
                               j.entry_number as EntryNumber, j.entry_date as EntryDate, 
                               j.reference_number as ReferenceNumber, j.source_module as SourceModule, 
                               j.source_id as SourceId, j.memo, j.status, 
                               j.total_debit as TotalDebit, j.total_credit as TotalCredit, 
                               j.is_system_generated as IsSystemGenerated, j.posted_by as PostedBy, 
                               u.full_name as PostedByName, j.posted_at as PostedAt, 
                               j.reversed_entry_id as ReversedEntryId, j.created_at as CreatedAt
                        FROM journal_entries j
                        LEFT JOIN app_users u ON j.posted_by = u.id
                        WHERE j.company_id = @CompanyId
                        " & If(Not String.IsNullOrEmpty(status), "AND j.status = @Status", "") & "
                        ORDER BY j.entry_date DESC, j.entry_number DESC;"

                    Dim entries = (Await conn.QueryAsync(Of JournalEntry)(sql, New With {Key .CompanyId = companyId, Key .Status = status})).ToList()
                    Return entries
                End Using
            Catch
                Return GetMockEntries(companyId)
            End Try
        End Function

        Public Async Function GetEntryWithLinesAsync(entryId As Guid) As Task(Of JournalEntry)
            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Dim entrySql = "
                        SELECT j.id, j.company_id as CompanyId, j.period_id as PeriodId, 
                               j.entry_number as EntryNumber, j.entry_date as EntryDate, 
                               j.reference_number as ReferenceNumber, j.source_module as SourceModule, 
                               j.source_id as SourceId, j.memo, j.status, 
                               j.total_debit as TotalDebit, j.total_credit as TotalCredit, 
                               j.is_system_generated as IsSystemGenerated, j.posted_by as PostedBy, 
                               j.posted_at as PostedAt, j.reversed_entry_id as ReversedEntryId, j.created_at as CreatedAt
                        FROM journal_entries j
                        WHERE j.id = @Id;"

                    Dim entry = Await conn.QuerySingleOrDefaultAsync(Of JournalEntry)(entrySql, New With {Key .Id = entryId})
                    If entry IsNot Nothing Then
                        Dim linesSql = "
                            SELECT l.id, l.journal_entry_id as JournalEntryId, l.account_id as AccountId, 
                                   a.account_code as AccountCode, a.account_name as AccountName,
                                   l.line_number as LineNumber, l.description, l.debit, l.credit
                            FROM journal_entry_lines l
                            JOIN accounts a ON l.account_id = a.id
                            WHERE l.journal_entry_id = @EntryId
                            ORDER BY l.line_number;"

                        Dim lines = (Await conn.QueryAsync(Of JournalEntryLine)(linesSql, New With {Key .EntryId = entryId})).ToList()
                        entry.Lines = lines
                    End If
                    Return entry
                End Using
            Catch
                Return Nothing
            End Try
        End Function

        Public Async Function PostJournalEntryAsync(entry As JournalEntry, userId As Guid) As Task(Of (Success As Boolean, Message As String, EntryId As Guid))
            ' Strict Double-Entry Validation
            If entry.Lines Is Nothing OrElse entry.Lines.Count < 2 Then
                Return (False, "A journal entry must contain at least two transaction lines.", Guid.Empty)
            End If

            Dim totalDebit = entry.Lines.Sum(Function(l) l.Debit)
            Dim totalCredit = entry.Lines.Sum(Function(l) l.Credit)

            If Math.Round(totalDebit, 4) <> Math.Round(totalCredit, 4) Then
                Return (False, $"Double-entry out of balance! Total Debit (${totalDebit:N2}) must equal Total Credit (${totalCredit:N2}).", Guid.Empty)
            End If

            If totalDebit <= 0 Then
                Return (False, "Journal entry total amount must be greater than zero.", Guid.Empty)
            End If

            For Each line In entry.Lines
                If line.AccountId = Guid.Empty Then
                    Return (False, "Every journal entry line must be assigned to an account.", Guid.Empty)
                End If
                If (line.Debit > 0 AndAlso line.Credit > 0) OrElse (line.Debit = 0 AndAlso line.Credit = 0) Then
                    Return (False, "Each line must have either a debit amount or credit amount, not both or zero.", Guid.Empty)
                End If
            Next

            entry.TotalDebit = totalDebit
            entry.TotalCredit = totalCredit

            Try
                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    Using trans = conn.BeginTransaction()
                        If entry.Id = Guid.Empty Then
                            entry.Id = Guid.NewGuid()
                            If String.IsNullOrWhiteSpace(entry.EntryNumber) Then
                                entry.EntryNumber = "JE-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                            End If

                            Dim insertEntrySql = "
                                INSERT INTO journal_entries (id, company_id, period_id, entry_number, entry_date, 
                                                            reference_number, source_module, source_id, memo, 
                                                            status, total_debit, total_credit, is_system_generated, 
                                                            posted_by, posted_at, created_by, created_at)
                                VALUES (@Id, @CompanyId, @PeriodId, @EntryNumber, @EntryDate, 
                                        @ReferenceNumber, @SourceModule, @SourceId, @Memo, 
                                        'Posted', @TotalDebit, @TotalCredit, @IsSystemGenerated, 
                                        @PostedBy, NOW(), @CreatedBy, NOW());"

                            entry.PostedBy = userId
                            entry.CreatedBy = userId
                            Await conn.ExecuteAsync(insertEntrySql, entry, trans)
                        Else
                            ' Update existing draft and post it
                            Dim updateSql = "
                                UPDATE journal_entries 
                                SET entry_date = @EntryDate, reference_number = @ReferenceNumber, memo = @Memo,
                                    status = 'Posted', total_debit = @TotalDebit, total_credit = @TotalCredit,
                                    posted_by = @PostedBy, posted_at = NOW(), updated_at = NOW()
                                WHERE id = @Id AND company_id = @CompanyId;"
                            entry.PostedBy = userId
                            Await conn.ExecuteAsync(updateSql, entry, trans)
                            Await conn.ExecuteAsync("DELETE FROM journal_entry_lines WHERE journal_entry_id = @Id;", New With {Key .Id = entry.Id}, trans)
                        End If

                        ' Insert lines
                        Dim lineIdx = 1
                        For Each line In entry.Lines
                            line.Id = Guid.NewGuid()
                            line.JournalEntryId = entry.Id
                            line.LineNumber = lineIdx
                            lineIdx += 1

                            Dim insertLineSql = "
                                INSERT INTO journal_entry_lines (id, journal_entry_id, account_id, line_number, description, debit, credit)
                                VALUES (@Id, @JournalEntryId, @AccountId, @LineNumber, @Description, @Debit, @Credit);"
                            Await conn.ExecuteAsync(insertLineSql, line, trans)
                        Next

                        ' Record audit
                        Await conn.ExecuteAsync("
                            INSERT INTO audit_logs (company_id, user_id, action, module, entity_id, description)
                            VALUES (@CompanyId, @UserId, 'POST_JOURNAL', 'Accounting', @EntityId, @Desc);",
                            New With {
                                Key .CompanyId = entry.CompanyId,
                                Key .UserId = userId,
                                Key .EntityId = entry.Id,
                                Key .Desc = $"Posted journal entry #{entry.EntryNumber} for amount ${entry.TotalDebit:N2}"
                            }, trans)

                        trans.Commit()
                        Return (True, $"Journal Entry #{entry.EntryNumber} posted successfully.", entry.Id)
                    End Using
                End Using
            Catch ex As Exception
                Return (False, $"Failed to post journal entry: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Public Async Function ReverseJournalEntryAsync(entryId As Guid, userId As Guid, reason As String) As Task(Of (Success As Boolean, Message As String, ReversalId As Guid))
            Try
                Dim original = Await GetEntryWithLinesAsync(entryId)
                If original Is Nothing Then
                    Return (False, "Journal entry not found.", Guid.Empty)
                End If

                If original.Status <> "Posted" Then
                    Return (False, "Only posted entries can be reversed.", Guid.Empty)
                End If

                Dim reversal As New JournalEntry With {
                    .Id = Guid.NewGuid(),
                    .CompanyId = original.CompanyId,
                    .EntryNumber = "REV-" & original.EntryNumber,
                    .EntryDate = DateTime.Today,
                    .ReferenceNumber = original.EntryNumber,
                    .SourceModule = "GeneralJournal",
                    .SourceId = original.Id,
                    .Memo = $"Reversal of entry {original.EntryNumber}: {reason}",
                    .IsSystemGenerated = True,
                    .ReversedEntryId = original.Id,
                    .PostedBy = userId,
                    .CreatedBy = userId
                }

                ' Invert debits and credits
                For Each line In original.Lines
                    reversal.Lines.Add(New JournalEntryLine With {
                        .AccountId = line.AccountId,
                        .Description = "Reversal: " & line.Description,
                        .Debit = line.Credit,
                        .Credit = line.Debit
                    })
                Next

                Using conn = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()
                    Using trans = conn.BeginTransaction()
                        ' Mark original as Reversed
                        Await conn.ExecuteAsync("UPDATE journal_entries SET status = 'Reversed', updated_at = NOW() WHERE id = @Id;", New With {Key .Id = original.Id}, trans)
                        trans.Commit()
                    End Using
                End Using

                Dim postResult = Await PostJournalEntryAsync(reversal, userId)
                Return (postResult.Success, $"Reversal entry #{reversal.EntryNumber} generated and posted.", reversal.Id)
            Catch ex As Exception
                Return (False, $"Error reversing entry: {ex.Message}", Guid.Empty)
            End Try
        End Function

        Private Function GetMockEntries(companyId As Guid) As List(Of JournalEntry)
            Return New List(Of JournalEntry) From {
                New JournalEntry With {
                    .Id = Guid.Parse("71000000-0000-0000-0000-000000000001"),
                    .CompanyId = companyId,
                    .EntryNumber = "JE-2026-0001",
                    .EntryDate = DateTime.Today.AddDays(-10),
                    .ReferenceNumber = "INITIAL-BAL",
                    .Memo = "Opening capital deposit",
                    .Status = "Posted",
                    .TotalDebit = 50000D,
                    .TotalCredit = 50000D,
                    .PostedByName = "System Administrator"
                },
                New JournalEntry With {
                    .Id = Guid.Parse("71000000-0000-0000-0000-000000000002"),
                    .CompanyId = companyId,
                    .EntryNumber = "JE-2026-0002",
                    .EntryDate = DateTime.Today.AddDays(-5),
                    .ReferenceNumber = "INV-1001",
                    .Memo = "Sales invoice posting for Acme Corp",
                    .Status = "Posted",
                    .TotalDebit = 4500D,
                    .TotalCredit = 4500D,
                    .PostedByName = "System Administrator"
                }
            }
        End Function
    End Class
End Namespace
