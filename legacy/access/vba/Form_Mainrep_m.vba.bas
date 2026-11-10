Attribute VB_Name = "Form_Mainrep_m"
Attribute VB_Base = "0{9FD64E41-7198-4FD6-A72B-F60E061B654D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim Fldname As String, fldsort As String
Dim A As String, S As String, S1 As String

Private Sub Command21_Click()
    DoCmd.Close
End Sub
Private Sub entez_AfterUpdate()
     Makesource
End Sub
Private Sub Form_Close()
    Forms!Mainrep.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Forms!Mainrep.Visible = False
    Me.SACC_SUB.Form.RecordSource = "SELECT Mainrep.*, * FROM Mainrep WHERE (((Mainrep.Scode) Not In (1,6))) ORDER BY Mainrep.Sdate;"
    Me.Text0.SetFocus
End Sub
Private Sub Frame0_AfterUpdate()
     Makesource
End Sub

Private Sub Frame1_AfterUpdate()
     Makesource
End Sub
Private Sub Frame2_AfterUpdate()
     Makesource
End Sub

Private Sub Frame3_AfterUpdate()
    Makesource
End Sub

Private Sub Text0_Change()
    Makesource
End Sub

Private Sub Makesource()
    If Me.Frame3 = 1 Then
        S = "SELECT * From Mainrep Where Scode not in(1,6)And "
    Else
        S = "SELECT * From Mainrep Where Scode in(1,6)And "
    End If
        
        Select Case Me.Frame0
        Case 0
            S1 = "1=1 "
        Case 1
            S1 = "Sid=0 "
        Case 2
            S1 = "Sid=0 And Bes>0 "
        Case 3
            S1 = "Sid=0 And Bed>0 "
        Case 4
            S1 = "Scode =16 Or Scode=26 "
        Case 5
            S1 = "Scode =16 "
        Case 6
            S1 = "Scode =26 "
        Case 7
            S1 = "Scode =17 Or Scode=27 "
        Case 8
            S1 = "Scode =17 "
        Case 9
            S1 = "Scode =27 "
    End Select
    S = S & S1
    Select Case Me.Frame1
        Case 1
            Fldname = "Sdate"
        Case 2
            Fldname = "Transtype"
        Case 3
            Fldname = "Baccno"
        Case 4
            Fldname = "Checkno"
        Case 5
            Fldname = "Curid"
        Case 6
            Fldname = "Maturity"
        Case 7
            Fldname = "Person"
        Case 8
            Fldname = "Babat"
        Case 9
            Fldname = "Bed"
        Case 10
            Fldname = "Bes"
        Case 11
            Fldname = "Status"
    End Select

    Me.Text0.SetFocus
    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
        'S = S & S1 & "ORDER BY " & Fldname
         S = S & "ORDER BY Nz(" & CStr(Fldname) & " ,'و.ن')"
    Else
        A = BSPACE([Text0].Text)
        S = S & "And iif(isnull(" & CStr(Fldname) & "),'',BSPACE(" & CStr(Fldname) & ") Like '*" & Trim(A) & "*')" & " ORDER BY Nz(" & CStr(Fldname) & " ,'و.ن')"

    End If
    
    Select Case Me.Frame2
        Case 1
            S = S & " ;"
        Case 2
            S = S & " Desc ;"
    End Select
    
    Me.SACC_SUB.Form.RecordSource = S
    Me.SACC_SUB.Form.Requery

End Sub
Private Sub Command85_Click()
On Error GoTo Err_Command85_Click

    Dim stDocName As String

    stDocName = "Mainrep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command85_Click:
    Exit Sub

Err_Command85_Click:
    MsgBox Err.Description
    Resume Exit_Command85_Click
    
End Sub
