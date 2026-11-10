Attribute VB_Name = "Form_Check_p_vos_m"
Attribute VB_Base = "0{1040E7C6-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim rst As Recordset, rst1 As Recordset
Dim mystr As String

Private Sub ComboBacc_AfterUpdate()
'    ComboBundle.Requery
'    Me.Curid = ComboBacc.Column(7)
'    If Me.ComboBacc.Column(7) <> 0 Then
'        Me.L‍cur.Visible = True
'        Me.Currate.Visible = True
'    Else
'        Me.L‍cur.Visible = False
'        Me.Currate.Visible = False
'    End If
End Sub
Private Sub Command23_Click()
    mystr = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.RegVos)=False) AND ((Check.Vos)=True)) OR (((Check.Backed)=True)); "
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
                !Status = "وصول شده"
            Else
                !Status = "برگشت شده"
            End If
                !D4 = Me.Gdate
                !G4 = Me.Gid
                !RegVos = True
            .Update
            .MoveNext
        Wend
        Me.Verit = True
        Me.Porder_search.Form.Requery
    End With

End Sub

Private Sub Command4_Click()
On Error GoTo Err_Command4_Click


    DoCmd.Close

Exit_Command4_Click:
    Exit Sub

Err_Command4_Click:
    MsgBox Err.Description
    Resume Exit_Command4_Click
    
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Gcode = 23
    Me.Gno = tellmelastuser(2, Me.Gcode)
    Me.Gtime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]
End Sub
Private Sub Command24_Click()
On Error GoTo Err_Command24_Click


    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command24_Click:
    Exit Sub

Err_Command24_Click:
    MsgBox Err.Description
    Resume Exit_Command24_Click
    
End Sub


Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
End Sub
