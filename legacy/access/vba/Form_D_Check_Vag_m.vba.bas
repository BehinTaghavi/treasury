Attribute VB_Name = "Form_D_Check_Vag_m"
Attribute VB_Base = "0{1040E823-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String, mystr1 As String
Dim rst As Recordset
Private Sub Check_s_Enter()
    If Me.Verit = True Then
        MsgBox "جهت ثبت عمليات ركورد جديد ايجاد نماييد"
        Exit Sub
    End If

    If Me.Gid = 0 Then
        Me.Gdate.SetFocus
        Exit Sub
    End If
    
    If IsNull(Me.Gdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Gdate.SetFocus
        Exit Sub
    End If


End Sub

Private Sub Command0_Click()
'    If Delmains("G1", Me.Gid) = 0 Then
        DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
        DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70
'    Else
'        MsgBox "به دليل وجود ركورد متناظر امكان حذف وجود ندارد"
'    End If

End Sub

Private Sub Command4_Click()
    DoCmd.Close
End Sub

Private Sub Command6_Click()
    If Me.Gid = 0 Then
        Me.Gdate.SetFocus
        Exit Sub
    End If
    
    If IsNull(Me.Gdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Gdate.SetFocus
        Exit Sub
    End If
    [Check_s].Form.Recalc

    mystr = "Select * from Check Where Stcode=11 and Dp=1 and Sel=" & True
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
        End If
        While Not .EOF
            If IsNull(!Vagno) Or !Vagno = 0 Then
                MsgBox "حساب واگذاري نامشخص است"
                Exit Sub
            End If
        .MoveNext
        Wend
        
        .MoveFirst

        While Not .EOF
            .Edit
                !D3 = Nz(Me.Gdate, CurrentDateFarsi(Date))
                !G3 = Me.Gid
                '!Delivervag = True
                !Sel = False
                !Stcode = 12
                !Status = "واگذارجهت وصول"
            .Update
             Isbankicheck !Checkid, Nz(Me.Gdate, CurrentDateFarsi(Date))
            .MoveNext
        Wend
        Me.Verit = True
        Me.Check_s.Form.Requery
    End With

End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Gcode = 12
    Me.Gno = tellmelastuser(2, Me.Gcode)
    Me.Gtime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    'Changeinmain Me.Gid, Me.Gdate, Me.Baccno
End Sub

Private Sub Form_Current()
    Me.Gdate.SetFocus
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
    Delallsel
End Sub

Private Sub Frame9_AfterUpdate()
    Dim S As String
    Select Case Frame9
        Case 1
            S = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0 Or (Check.Checktype)=2) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity ;"
        Case 2
            S = "SELECT Check.* FROM [Check] WHERE (((Check.Maturity) Between [dd1] And [dd2]) AND ((Check.Dp)=1) AND ((Check.Checktype)=0 Or (Check.Checktype)=2) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity ;"
    End Select
    Me.Check_s.Form.RecordSource = S
    Me.Check_s.Form.Requery
End Sub

Private Sub Gdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Gdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Gdate, 0) = False Then Cancel = True
End Sub
