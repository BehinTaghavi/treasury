Attribute VB_Name = "Form_Check_s"
Attribute VB_Base = "0{1040E7E9-F05E-11D7-BC94-8154B80D7F75}"
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
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Checktypen,'" & Me!T4 & "',0) > 0"
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


Private Sub Sel_AfterUpdate()
End Sub
Private Sub Sel_GotFocus()
        If IsNull(Forms![Check_pay_m]![ComboBacc]) Then
            MsgBox "شماره حساب نامشخص است"
            Forms![Check_pay_m]![ComboBacc].SetFocus
            Exit Sub
        End If

        If IsNull(Forms![Check_pay_m]![ComboBundle]) Then
            MsgBox "دسته چك نامشخص است"
            Forms![Check_pay_m]![ComboBundle].SetFocus
            Exit Sub
        End If
        
        If IsNull(Forms![Check_pay_m]![Bundfrom]) Or Forms![Check_pay_m]![Bundfrom] = 0 Then
            MsgBox "دسته چك نامشخص است"
            Forms![Check_pay_m]![Bundfrom].SetFocus
            Exit Sub
        End If
End Sub
Private Sub Command27_Click()
On Error GoTo Err_Command27_Click

    Dim stDocName As String
    Dim stLinkCriteria As String
    updatecard
    Filterison = True
    stDocName = "Check_Print_single"
    
    stLinkCriteria = "[Checkid]=" & Me![Checkid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command27_Click:
    Exit Sub

Err_Command27_Click:
    MsgBox Err.Description
    Resume Exit_Command27_Click
    
End Sub

Private Sub Frame1_AfterUpdate()
    Select Case Me.Frame1
        Case 1
            mys = "SELECT Check.*, Switch([Checktype]=0,'چك نقدي',[Checktype]=1,'چك انتظامي',[Checktype]=2,'چك مدت دار') AS PT FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype) In (0,1,2))) ORDER BY Check.Maturity, Check.Rfpid;"
        Case 2
            mys = "SELECT Check.*, Switch([Checktype]=0,'چك نقدي',[Checktype]=1,'چك انتظامي',[Checktype]=2,'چك مدت دار') AS PT FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype) In (0,1,2))) ORDER BY Check.Rfpdate, Check.Rfpid;"
    End Select
    Me.RecordSource = mys
End Sub

Private Sub Rfpdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Rfpdate, 0) = False Then Cancel = True
End Sub
