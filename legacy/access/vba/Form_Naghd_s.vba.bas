Attribute VB_Name = "Form_Naghd_s"
Attribute VB_Base = "0{1040E8AF-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim newsub As Long


Private Sub Command1_Click()
    If MsgBox("پرداخت نقدي صورت گيرد؟", vbYesNo) = vbYes Then
        Me.Recalc
        Me.Paied = True
        S = "INSERT INTO Subs ( Checkid, Empid, Ourno,Bed, Bes, Iscur, Curid, Currate, Person, Baccno, Babat, Invno, Contract, Accno, Tafno, Sal, Sdate, Stime, Scode )" & _
        " SELECT Check.Checkid, Check.Empid, Check.Rfpref,0, Check.Amount, Check.Iscur, Check.Curid, Check.Currate, Check.Person, Check.Baccno, Check.Babat, Check.Invno, Check.Contract, Check.Accno, Check.Tafno, forms!sal!tsal , Check.D1, Time() , 27 FROM [Check] WHERE (((Check.Checkid)= " & Me.Checkid & "));"
        docommand S
        newsub = DMax("Sid", "Subs")
        
        S = "INSERT INTO Povouch ( Sid, accno, tafno, bed, bes, rowdesc )SELECT " & newsub & ", Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc FROM Povouch WHERE (((Povouch.Checkid)= " & Me.Checkid & "));"
        docommand S
            
        Me.Requery
    End If
End Sub

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
Private Sub D1_AfterUpdate()
    If IsNull(Me.D1) = False Then
        Me.Command1.Enabled = True
    Else
        Me.Command1.Enabled = False
    End If
End Sub

Private Sub Form_Current()
    If IsNull(Me.D1) = False Then
        Me.Command1.Enabled = True
    Else
        Me.Command1.Enabled = False
    End If
End Sub

Private Sub Rfpdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Rfpdate, 0) = False Then Cancel = True
End Sub
