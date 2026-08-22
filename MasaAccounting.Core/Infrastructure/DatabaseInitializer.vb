Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports Dapper
Imports Npgsql
Imports MasaAccounting.Core.Domain.Entities

Namespace Infrastructure.Data
    Public Class DatabaseInitializer
        ''' <summary>
        ''' Ensures tables and seed data exist if connecting to a fresh database.
        ''' </summary>
        Public Shared Async Function EnsureDatabaseSetupAsync() As Task
            Try
                Using conn As NpgsqlConnection = DatabaseConfiguration.CreateConnection()
                    Await conn.OpenAsync()

                    ' Quick check if accounts table exists
                    Dim checkSql = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'accounts');"
                    Dim exists As Boolean = Await conn.ExecuteScalarAsync(Of Boolean)(checkSql)

                    If Not exists Then
                        ' Run base schema & seed
                        Dim currentDir = AppDomain.CurrentDomain.BaseDirectory
                        Dim schemaPath = FindFileInTree("database/01_schema.sql", currentDir)
                        Dim seedPath = FindFileInTree("database/02_seed_data.sql", currentDir)

                        If File.Exists(schemaPath) Then
                            Dim schemaSql = Await File.ReadAllTextAsync(schemaPath)
                            Await conn.ExecuteAsync(schemaSql)
                        End If

                        If File.Exists(seedPath) Then
                            Dim seedSql = Await File.ReadAllTextAsync(seedPath)
                            Await conn.ExecuteAsync(seedSql)
                        End If
                    End If
                End Using
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Database initialization notice: {ex.Message}")
            End Try
        End Function

        Private Shared Function FindFileInTree(relativePath As String, startDir As String) As String
            Dim cur = startDir
            For i As Integer = 0 To 5
                If String.IsNullOrEmpty(cur) Then Exit For
                Dim candidate = Path.Combine(cur, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()))
                If File.Exists(candidate) Then Return candidate
                Dim parent = Directory.GetParent(cur)
                If parent Is Nothing Then Exit For
                cur = parent.FullName
            Next
            Return String.Empty
        End Function
    End Class
End Namespace
