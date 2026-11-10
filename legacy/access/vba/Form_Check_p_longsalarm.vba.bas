Attribute VB_Name = "Form_Check_p_longsalarm"
Attribute VB_Base = "0{9F5EB6C7-6605-4A7C-80CE-57C22D38F7E4}"
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
       FilterStr = "InStr(1,Baccno,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,Rfpdate,'" & Me!T2 & "',0) > 0"
    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Checktypen,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Maturity,'" & Me!T6 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Babat,'" & Me!T7 & "',0) > 0"
    End If
    If Not IsNull(Me!T9) Then
       FilterStr = "InStr(1,Curid,'" & Me!T9 & "',0) > 0"
    End If
    If Not IsNull(Me!T10) Then
       FilterStr = "InStr(1,Person,'" & Me!T10 & "',0) > 0"
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

Private Sub Command38_Click()
    stDocName = "Check_p_longsrepalarm"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
'    If IsNull(Me.Maturity) Then
'        MsgBox "سررسيد نامشخص است"
'        Cancel = True
'        Me.Maturity.SetFocus
'        Exit Sub
'    End If

    If IsNull(Me.Combobaccno) Then
        MsgBox "شماره حساب نامشخص است"
        Cancel = True
        Me.Combobaccno.SetFocus
    End If
End Sub
Private Sub Form_Close()
        If Nz(DLookup("befreccome", "Globals"), False) = True Then
            DoCmd.OpenForm "Check_d_longsalarm"
        Else
            DoCmd.OpenForm "Main"
        End If
End Sub
Private Sub Sel_AfterUpdate()
    Me.Recalc
End Sub
Private Sub Sel_GotFocus()
    If IsNull(Forms![Check_longtocash_m]![Gdate]) Or Forms![Check_longtocash_m]![Gdate] = 0 Then
        MsgBox "تاريخ نامشخص است"
        Forms![Check_longtocash_m]![Gdate].SetFocus
    End If
End Sub
