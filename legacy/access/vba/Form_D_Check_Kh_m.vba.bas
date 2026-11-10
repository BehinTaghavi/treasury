Attribute VB_Name = "Form_D_Check_Kh_m"
Attribute VB_Base = "0{1040E7FE-F05E-11D7-BC94-8154B80D7F75}"
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
    
    mystr = "Select * from Check Where Dp=1 and Stcode=11 and Sel=" & True
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
                S = "Account_code='" & CStr(Nz(!TafKharj)) & "'"
                !Vagno = DLookup("Account_name", "t_unique_accounts", S)
                !D3 = Me.Gdate
                !G3 = Me.Gid
                !EbtorKharj = True
                !Stcode = 16
                !Sel = False
                !Status = "واگذاربه سايرين"
            .Update
            .MoveNext
        Wend
        Me.Verit = True
        Me.Check_s.Form.Requery
    End With

End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Gcode = 19
    Me.Gno = tellmelastuser(2, Me.Gcode)
    Me.Gtime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]

End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    'Changeinmain Me.Gid, Me.Gdate, Me.Baccno
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    'DoCmd.GoToRecord , , acFirst
    'DoCmd.GoToRecord , , acLast
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
