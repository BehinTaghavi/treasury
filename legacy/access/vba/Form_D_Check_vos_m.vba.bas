Attribute VB_Name = "Form_D_Check_vos_m"
Attribute VB_Base = "0{1040E831-F05E-11D7-BC94-8154B80D7F75}"
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
        Me.ComboBacc.SetFocus
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
    
    If IsNull(Me.ComboBacc) Or Me.ComboBacc = 0 Then
        MsgBox "شماره حساب نامشخص است"
        Me.ComboBacc.SetFocus
        Exit Sub
    End If

End Sub

Private Sub ComboBacc_AfterUpdate()
    Me.Curid = ComboBacc.Column(3)
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.Iscur = True
        Me.L‍cur.Visible = True
        Me.Currate.Visible = True
    Else
        Me.Iscur = False
        Me.L‍cur.Visible = False
        Me.Currate.Visible = False
        Me.Curid = "ريال"
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
    
    If IsNull(Me.ComboBacc) Or Me.ComboBacc = 0 Then
        MsgBox "شماره حساب نامشخص است"
        Me.ComboBacc.SetFocus
        Exit Sub
    End If

    mystr = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=12) AND (Check.Vos=True OR Check.Backed=True)); "
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
        End If
        
        While Not .EOF
            .Edit
            If !Vos = True Then
                !Stcode = 13
                !Status = "وصول شده"
            Else
                !Status = "برگشت شده"
                !Stcode = 14
            End If
                !D4 = Me.Gdate
                !G4 = Me.Gid
                '!RegVos = True
            .Update
            .MoveNext
        Wend
        Me.Verit = True
        Me.Check_s.Form.Requery
    End With

End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Gcode = 13
    Me.Gno = tellmelastuser(2, Me.Gcode)
    Me.Gtime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]

End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
   ' Changeinmain Me.Gid, Me.Gdate, Me.Baccno
End Sub

Private Sub Form_Current()
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.L‍cur.Visible = True
        Me.Currate.Visible = True
    Else
        Me.L‍cur.Visible = False
        Me.Currate.Visible = False
    End If
    Me.ComboBacc.SetFocus
    
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
End Sub

Private Sub Frame9_AfterUpdate()
    Dim S As String
    Select Case Frame9
        Case 1
            S = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Vagno)=[ComboBacc]) AND ((Check.Stcode)=12)) ORDER BY Check.D3;"
        Case 2
            S = "SELECT Check.* FROM [Check] WHERE (((Check.D3) Between [dd1] And [dd2]) AND ((Check.Dp)=1) AND ((Check.Vagno)=[ComboBacc]) AND ((Check.Stcode)=12)) ORDER BY Check.D3;"
    End Select
    Me.Check_s.Form.RecordSource = S
    Me.Check_s.Form.Requery

End Sub
