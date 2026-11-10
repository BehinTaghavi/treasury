Attribute VB_Name = "Form_Check_longtocash_m"
Attribute VB_Base = "0{6A530EE5-8233-4A24-B89D-CA765998972A}"
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
    If Me.Verit = True Then
        MsgBox "جهت ثبت عمليات ركورد جديد ايجاد نماييد"
        Exit Sub
    End If
    mystr = "Select * from Check Where Checktype <> 0 and Ctocash=false and Sel=" & True
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
        End If
        
            Dim Mykasrtime As Byte
            Set rst1 = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
            With rst1
                Mykasrtime = !Kasrtime
            .Close
            End With
        
        While Not .EOF
            .Edit
                !D3 = Me.Gdate
                !G3 = Me.Gid
                !Ctocash = True
                !Sel = False
                !Calcit = True
                !D5 = Me.Gdate
            .Update
            .MoveNext
        Wend
        Me.Verit = True
        Me.Check_s.Form.Requery
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
    Me.Gcode = 25
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
Private Sub Form_Current()
    Me.Gdate.SetFocus
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
            S = "SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM [Check] WHERE (((Check.Checktype) not in(0,1)) AND ((Check.Ctocash)=False) AND ((Check.Paied)=true) AND ((Check.Dp)=2)And Check.Ebtorkharj=0 ) ORDER BY Check.Maturity;"
        Case 2
            S = "SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) As Nak FROM [Check] WHERE (((Check.Maturity) Between [dd1] And [dd2]) AND ((Check.Checktype)<>0) AND ((Check.Ctocash)=False) AND ((Check.Paied)=true) AND ((Check.Dp)=2)And Check.Ebtorkharj=0 And Check.Checktype not in(0,1)) ORDER BY Check.Maturity;"
    End Select
    Me.Check_s.Form.RecordSource = S
    Me.Check_s.Form.Requery

End Sub

Private Sub Porder_search_Enter()
    If IsNull(Me.Gdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Gdate.SetFocus
    End If
End Sub

Private Sub Gdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Gdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Gdate, 0) = False Then Cancel = True

    If Me.Gdate > CurrentDateFarsi(Date) Then
     MsgBox "تاريخ ازتاريخ روز بزرگتراست!"
     Cancel = True
    End If
    
End Sub
Private Sub Command38_Click()
On Error GoTo Err_Command38_Click
    Me.Recalc
    Dim stDocName As String

    stDocName = "Check_p_longsrep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command38_Click:
    Exit Sub

Err_Command38_Click:
    MsgBox Err.Description
    Resume Exit_Command38_Click
    
End Sub
