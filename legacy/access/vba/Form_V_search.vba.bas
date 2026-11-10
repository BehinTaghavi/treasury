Attribute VB_Name = "Form_V_search"
Attribute VB_Base = "0{1040E92F-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T1) Then
       FilterStr = "Vno=" & Me!T1
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,Vdate,'" & Me!T2 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Vtype,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Vdesc,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Sended,'" & Me!T5 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Accvno,'" & Me!T6 & "',0) > 0"
    End If

    If FilterStr = "" Then
       Me.FilterOn = False
    Else
       Me.Filter = FilterStr
       Me.FilterOn = True
    End If
End Sub

Private Sub Command31_Click()
       Me.FilterOn = False
End Sub


Private Sub Vno_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Voucher"
    
    stLinkCriteria = "[Voucher.Vid]=" & Me![Vid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub
