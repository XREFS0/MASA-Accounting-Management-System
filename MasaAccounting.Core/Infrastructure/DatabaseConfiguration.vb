Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports DotNetEnv
Imports Newtonsoft.Json
Imports Npgsql
Imports Supabase

Namespace Infrastructure.Data
    Public Class DatabaseConfiguration
        Private Shared _isInitialized As Boolean = False
        Private Shared _connectionString As String = String.Empty
        Private Shared _supabaseUrl As String = String.Empty
        Private Shared _supabaseAnonKey As String = String.Empty
        Private Shared _supabaseClient As Supabase.Client = Nothing

        Public Shared Sub Initialize()
            If _isInitialized Then Return

            Dim currentDir = AppDomain.CurrentDomain.BaseDirectory
            Dim envPath = Path.Combine(currentDir, ".env")

            If Not File.Exists(envPath) Then
                Dim parent = Directory.GetParent(currentDir)
                While parent IsNot Nothing
                    Dim candidate = Path.Combine(parent.FullName, ".env")
                    If File.Exists(candidate) Then
                        envPath = candidate
                        Exit While
                    End If
                    parent = parent.Parent
                End While
            End If

            If File.Exists(envPath) Then
                Env.Load(envPath)
            End If

            _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
            If String.IsNullOrEmpty(_supabaseUrl) Then
                _supabaseUrl = "https://your-project.supabase.co"
            End If

            _supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
            If String.IsNullOrEmpty(_supabaseAnonKey) Then
                _supabaseAnonKey = String.Empty
            End If

            _connectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
            If String.IsNullOrEmpty(_connectionString) Then
                _connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION")
            End If
            If String.IsNullOrEmpty(_connectionString) Then
                _connectionString = "Host=localhost;Port=5432;Database=masa_accounting;Username=postgres;Password=postgres;Pooling=true;"
            End If

            _isInitialized = True
        End Sub

        Public Shared Function GetConnectionString() As String
            If Not _isInitialized Then Initialize()
            Return _connectionString
        End Function

        Public Shared Function CreateConnection() As NpgsqlConnection
            Return New NpgsqlConnection(GetConnectionString())
        End Function

        Public Shared ReadOnly Property SupabaseUrl As String
            Get
                If Not _isInitialized Then Initialize()
                Return _supabaseUrl
            End Get
        End Property

        Public Shared ReadOnly Property SupabaseAnonKey As String
            Get
                If Not _isInitialized Then Initialize()
                Return _supabaseAnonKey
            End Get
        End Property

        Public Shared Function CreateSupabaseHttpClient() As HttpClient
            If Not _isInitialized Then Initialize()
            Dim client As New HttpClient()
            client.BaseAddress = New Uri(SupabaseUrl.TrimEnd("/"c) & "/rest/v1/")
            client.DefaultRequestHeaders.Add("apikey", SupabaseAnonKey)
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseAnonKey}")
            Return client
        End Function
    End Class
End Namespace
