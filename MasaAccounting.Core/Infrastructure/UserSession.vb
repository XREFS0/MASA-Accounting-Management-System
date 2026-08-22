Imports System
Imports System.IO
Imports MasaAccounting.Core.Domain.Entities

Namespace Infrastructure.Security
    Public Class UserSession
        Private Shared _currentUser As AppUser
        Private Shared _currentCompany As Company

        Public Shared Property CurrentUser As AppUser
            Get
                Return _currentUser
            End Get
            Set(value As AppUser)
                _currentUser = value
            End Set
        End Property

        Public Shared Property CurrentCompany As Company
            Get
                Return _currentCompany
            End Get
            Set(value As Company)
                _currentCompany = value
            End Set
        End Property

        Public Shared ReadOnly Property IsAuthenticated As Boolean
            Get
                Return _currentUser IsNot Nothing
            End Get
        End Property

        Public Shared Sub Logout()
            _currentUser = Nothing
            _currentCompany = Nothing
        End Sub
    End Class
End Namespace
