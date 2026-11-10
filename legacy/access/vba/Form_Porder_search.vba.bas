Attribute VB_Name = "Form_Porder_search"
Attribute VB_Base = "0{1040E8D9-F05E-11D7-BC94-8154B80D7F75}"
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
       FilterStr = "InStr(1,Rfpid,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,Rfpdate,'" & Me!T2 & "',0) > 0"
    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Rfpref,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,PT,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Person,'" & Me!T6 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Babat,'" & Me!T7 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T8) Then
       FilterStr = "InStr(1,Payit,'" & Me!T8 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T9) Then
       FilterStr = "InStr(1,Paied,'" & Me!T9 & "',0) > 0"
    End If

    If Not IsNull(Me!T10) Then
       FilterStr = "InStr(1,Invno,'" & Me!T10 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T11) Then
       FilterStr = "InStr(1,Curid,'" & Me!T11 & "',0) > 0"
    End If

    If Not IsNull(Me!T12) Then
       FilterStr = "InStr(1,Empid,'" & Me!T12 & "',0) > 0"
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

Private Sub Form_Open(Cancel As Integer)
    On Error GoTo Err1
    
        If Forms!Sal!Isadd = True Then
            Me.RecordSource = "SELECT Check.* FROM Check WHERE (((Check.Sal)=Forms!sal!Tsal) And ((Check.dp)=2) And ((Check.Rfpdate) Between [D11] And [d21]));"
        Else
            Me.RecordSource = "SELECT Check.* FROM Check WHERE (((Check.Sal)=Forms!sal!Tsal) And ((Check.dp)=2) And ((Check.Rfpdate) Between [D11] And [d21]) And ((Check.Empid)=Forms!sal!Tempid));"
        End If
        
        DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
Err1:
    If Err.number = 2450 Then
      MsgBox "كاربر و يا سال مالي نامشخص است"
      DoCmd.Close
      DoCmd.OpenForm "sal"
    End If
End Sub


Private Sub Rfpid_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Porder"
    
    stLinkCriteria = "[Rfpid]=" & Me![Rfpid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command84_Click()
On Error GoTo Err_Command84_Click

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Porder"
    
    stLinkCriteria = "[Checkid]=" & Me![Checkid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command84_Click:
    Exit Sub

Err_Command84_Click:
    MsgBox Err.Description
    Resume Exit_Command84_Click
    
End Sub
