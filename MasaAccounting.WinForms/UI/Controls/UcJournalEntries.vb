Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MasaAccounting.Core.Application.Services
Imports MasaAccounting.Core.Domain.Entities
Imports MasaAccounting.Core.Infrastructure.Security
Imports MasaAccounting.WinForms.UI.Common

Namespace UI.Controls
    Public Class UcJournalEntries
        Inherits UserControl

        Private gridEntries As DataGridView
        Private btnNewEntry As Button
        Private btnReverse As Button
        Private btnRefresh As Button
        Private _journalService As New JournalEntryService()
        Private _accountService As New AccountService()

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
                .Text = "General Journal & Double-Entry Postings",
                .Font = UITheme.FontHeader,
                .ForeColor = UITheme.PrimaryColor,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }
            pnlToolbar.Controls.Add(lblTitle)

            btnNewEntry = New Button With {
                .Text = "+ New Journal Entry",
                .Size = New Size(160, 36),
                .Location = New Point(Me.Width - 390, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StylePrimaryButton(btnNewEntry)
            AddHandler btnNewEntry.Click, AddressOf BtnNewEntry_Click
            pnlToolbar.Controls.Add(btnNewEntry)

            btnReverse = New Button With {
                .Text = "Reverse Entry",
                .Size = New Size(110, 36),
                .Location = New Point(Me.Width - 220, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleDangerButton(btnReverse)
            AddHandler btnReverse.Click, AddressOf BtnReverse_Click
            pnlToolbar.Controls.Add(btnReverse)

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Size = New Size(90, 36),
                .Location = New Point(Me.Width - 100, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Sub() LoadEntries()
            pnlToolbar.Controls.Add(btnRefresh)

            Me.Controls.Add(pnlToolbar)

            ' Grid
            gridEntries = New DataGridView With {.Dock = DockStyle.Fill}
            UITheme.ApplyGridStyle(gridEntries)
            Me.Controls.Add(gridEntries)

            gridEntries.BringToFront()
            pnlToolbar.BringToFront()
        End Sub

        Public Async Sub LoadEntries()
            If UserSession.CurrentCompany Is Nothing Then Return

            Dim entries = Await _journalService.GetEntriesAsync(UserSession.CurrentCompany.Id)
            gridEntries.DataSource = entries.Select(Function(e) New With {
                .Id = e.Id,
                .Date = e.EntryDate.ToShortDateString(),
                .EntryNumber = e.EntryNumber,
                .Reference = e.ReferenceNumber,
                .SourceModule = e.SourceModule,
                .Memo = e.Memo,
                .TotalDebit = UITheme.FormatCurrency(e.TotalDebit),
                .TotalCredit = UITheme.FormatCurrency(e.TotalCredit),
                .Status = e.Status,
                .PostedBy = e.PostedByName
            }).ToList()

            If gridEntries.Columns.Contains("Id") Then
                gridEntries.Columns("Id").Visible = False
            End If
        End Sub

        Private Async Sub BtnReverse_Click(sender As Object, e As EventArgs)
            If gridEntries.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select an entry to reverse.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim selectedRow = gridEntries.SelectedRows(0)
            Dim entryId = CType(selectedRow.Cells("Id").Value, Guid)
            Dim entryNumber = CStr(selectedRow.Cells("EntryNumber").Value)

            Dim confirm = MessageBox.Show($"Are you sure you want to reverse posted entry {entryNumber}? This will create a reversal transaction to preserve the audit trail.", "Confirm Reversal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If confirm = DialogResult.Yes Then
                Dim result = Await _journalService.ReverseJournalEntryAsync(entryId, UserSession.CurrentUser.Id, "Auditor manual reversal")
                If result.Success Then
                    MessageBox.Show(result.Message, "Reversal Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadEntries()
                Else
                    MessageBox.Show(result.Message, "Reversal Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        End Sub

        Private Sub BtnNewEntry_Click(sender As Object, e As EventArgs)
            Using dlg As New Form()
                dlg.Text = "Create Balanced Double-Entry Journal Transaction"
                dlg.Size = New Size(700, 520)
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog
                dlg.MaximizeBox = False
                dlg.MinimizeBox = False

                Dim lblMemo As New Label With {.Text = "Memo / Description:", .Location = New Point(25, 20), .Size = New Size(150, 20)}
                Dim txtMemo As New TextBox With {.Location = New Point(25, 45), .Size = New Size(380, 26)}

                Dim lblRef As New Label With {.Text = "Reference #:", .Location = New Point(425, 20), .Size = New Size(120, 20)}
                Dim txtRef As New TextBox With {.Location = New Point(425, 45), .Size = New Size(230, 26)}

                Dim lblNotice As New Label With {
                    .Text = "Double-Entry Lines (Total Debit must strictly equal Total Credit):",
                    .Font = UITheme.FontBold,
                    .Location = New Point(25, 90),
                    .Size = New Size(630, 20)
                }

                ' Line 1 (Debit)
                Dim lblLine1 As New Label With {.Text = "1. Debit Account:", .Location = New Point(25, 120), .Size = New Size(120, 20)}
                Dim cmbAcc1 As New ComboBox With {.Location = New Point(25, 145), .Size = New Size(300, 26), .DropDownStyle = ComboBoxStyle.DropDownList}
                Dim lblD1 As New Label With {.Text = "Debit Amount:", .Location = New Point(345, 120), .Size = New Size(120, 20)}
                Dim txtDebit1 As New NumericUpDown With {.Location = New Point(345, 145), .Size = New Size(140, 26), .Maximum = 10000000D, .DecimalPlaces = 2}

                ' Line 2 (Credit)
                Dim lblLine2 As New Label With {.Text = "2. Credit Account:", .Location = New Point(25, 190), .Size = New Size(120, 20)}
                Dim cmbAcc2 As New ComboBox With {.Location = New Point(25, 215), .Size = New Size(300, 26), .DropDownStyle = ComboBoxStyle.DropDownList}
                Dim lblC2 As New Label With {.Text = "Credit Amount:", .Location = New Point(345, 190), .Size = New Size(120, 20)}
                Dim txtCredit2 As New NumericUpDown With {.Location = New Point(345, 215), .Size = New Size(140, 26), .Maximum = 10000000D, .DecimalPlaces = 2}

                ' Populate Accounts
                Dim accounts = _accountService.GetAccountsAsync(UserSession.CurrentCompany.Id).GetAwaiter().GetResult()
                Dim accList1 = accounts.ToList()
                Dim accList2 = accounts.ToList()

                cmbAcc1.DataSource = accList1
                cmbAcc1.DisplayMember = "FullDisplay"
                cmbAcc1.ValueMember = "Id"

                cmbAcc2.DataSource = accList2
                cmbAcc2.DisplayMember = "FullDisplay"
                cmbAcc2.ValueMember = "Id"

                Dim btnPost As New Button With {.Text = "Validate & Post to General Ledger", .Location = New Point(25, 380), .Size = New Size(630, 42)}
                UITheme.StylePrimaryButton(btnPost)

                AddHandler btnPost.Click, Async Sub()
                    Dim dVal = txtDebit1.Value
                    Dim cVal = txtCredit2.Value

                    If dVal <= 0 OrElse cVal <= 0 Then
                        MessageBox.Show("Amounts must be greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    If dVal <> cVal Then
                        MessageBox.Show($"Debit ({UITheme.FormatCurrency(dVal)}) does not balance with Credit ({UITheme.FormatCurrency(cVal)})!", "Unbalanced Transaction", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If

                    Dim entry As New JournalEntry With {
                        .CompanyId = UserSession.CurrentCompany.Id,
                        .EntryDate = DateTime.Today,
                        .ReferenceNumber = txtRef.Text.Trim(),
                        .Memo = If(String.IsNullOrWhiteSpace(txtMemo.Text), "Manual Journal Voucher", txtMemo.Text.Trim())
                    }

                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = CType(cmbAcc1.SelectedValue, Guid),
                        .Description = entry.Memo,
                        .Debit = dVal,
                        .Credit = 0D
                    })

                    entry.Lines.Add(New JournalEntryLine With {
                        .AccountId = CType(cmbAcc2.SelectedValue, Guid),
                        .Description = entry.Memo,
                        .Debit = 0D,
                        .Credit = cVal
                    })

                    Dim result = Await _journalService.PostJournalEntryAsync(entry, UserSession.CurrentUser.Id)
                    If result.Success Then
                        MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                        LoadEntries()
                    Else
                        MessageBox.Show(result.Message, "Posting Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End Sub

                dlg.Controls.AddRange(New Control() {lblMemo, txtMemo, lblRef, txtRef, lblNotice, lblLine1, cmbAcc1, lblD1, txtDebit1, lblLine2, cmbAcc2, lblC2, txtCredit2, btnPost})
                dlg.ShowDialog()
            End Using
        End Sub
    End Class
End Namespace
