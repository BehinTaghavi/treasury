Attribute VB_Name = "Form_Advance"
Attribute VB_Base = "0{1040E756-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Combo6_AfterUpdate()
    If Me.ADVANCEID = 0 Then
        Dim dbs As Database
        Dim rst As Recordset
        Set dbs = CurrentDb
        Set rst = dbs.OpenRecordset("SELECT MAX(ADVANCEID) AS [MYMAX] FROM ADVANCE", dbOpenDynaset, dbSeeChanges)
        With rst
       ' MsgBox !MYMAX
       ' Exit Sub
            If IsNull(!mymax) Then
                Me.ADVANCEID = 1
            Else
                Me.ADVANCEID = !mymax + 1
            End If
        End With
    End If

End Sub

Private Sub Command15_Click()
    Me.Recalc
    If Me.ADVANCEID = 0 Then
        Exit Sub
    End If
    
    'If Me.Child13.Form.RecordsetClone.RecordCount > 0 Then
    '    Exit Sub
    'End If
        Dim dbs As Database
        Dim rst, rst2 As Recordset, Myid As Integer, MYAM As Double, Myname As String
        Set dbs = CurrentDb
        Set rst = dbs.OpenRecordset("SELECT * FROM EMPS_ADVANCE ORDER BY EMPNAME", dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                Exit Sub
            Else
                Set rst2 = dbs.OpenRecordset("ADVANCE_ROW", dbOpenDynaset, dbSeeChanges)
                While Not .EOF
                Myid = !EMPNO
                MYAM = !Amount
                Myname = !EMPNAME
                    With rst2
                    .AddNew
                    !ADVANCEID = Me.ADVANCEID
                    !PERID = Myid
                    !Amount = MYAM
                    !Pername = Myname
                    .Update
                    End With
                .MoveNext
                Wend
            End If
        End With
        Me.Recalc
End Sub


Private Sub Command16_Click()
On Error GoTo Err_Command16_Click


    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command16_Click:
    Exit Sub

Err_Command16_Click:
    MsgBox Err.Description
    Resume Exit_Command16_Click
    
End Sub
Private Sub Command17_Click()
On Error GoTo Err_Command17_Click


    DoCmd.Close

Exit_Command17_Click:
    Exit Sub

Err_Command17_Click:
    MsgBox Err.Description
    Resume Exit_Command17_Click
    
End Sub

Private Sub Command18_Click()

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "ADVANCE_REP"
    stLinkCriteria = "[ADVANCEID]=" & Me.ADVANCEID
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria


End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Dim dbs As Database
    Dim rst As Recordset
    Dim mystr As String
    
    Set dbs = CurrentDb
    mystr = "select max(ADVANCEID) as mymax from ADVANCE"

    Set rst = dbs.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If IsNull(!mymax) Then
            Me.ADVANCEID = 1
        Else
            Me.ADVANCEID = !mymax + 1
        End If
    End With
    
End Sub

Private Sub Form_Current()
    Me.Combo6.SetFocus
End Sub

Private Sub Form_Open(Cancel As Integer)
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
End Sub
