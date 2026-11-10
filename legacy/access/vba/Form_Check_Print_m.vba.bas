Attribute VB_Name = "Form_Check_Print_m"
Attribute VB_Base = "0{1040E7DB-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Private Sub ComboBacc_AfterUpdate()
    ComboBundle.Requery
End Sub


Private Sub Command4_Click()
On Error GoTo Err_Command4_Click
    Me.List1.RowSource = "" ' "SELECT Check.Checkid, Check.Dp, Check.Checktype, Check.Currate, Check.Sal, Check.Iscur, Check.Empid, Check.Rfpid, Check.D1, Check.Checktypen, Check.Maturity, Check.Amount, Format([D1],'@@@@\/@@\/@@') AS DI, Trim([Checktypen]) AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Check.Curid, Check.Baccno, Check.Checkno, Format([Maturity],'@@@@\/@@\/@@') AS M, Check.Person, Check.Babat " & _
                        " FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype) In (0,1,2)) AND ((Check.Paied)=True) AND ((Check.Signed)=True));"
    DoCmd.Close
Exit_Command4_Click:
    Exit Sub

Err_Command4_Click:
    MsgBox Err.Description
    Resume Exit_Command4_Click
    
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

Private Sub DD1_AfterUpdate()
    Makesource
End Sub
Private Sub DD2_AfterUpdate()
    Makesource
End Sub
Private Sub Form_Close()
    'DoCmd.Close acForm, "Check_pay_m"
    Spfilter = 0
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    Spfilter = 1
    Makesource
'    DoCmd.OpenForm "Check_pay_m"
'    Forms!Check_pay_m.Visible = False
End Sub

Private Sub Frame1_AfterUpdate()
    Makesource
End Sub

Private Sub Frame2_AfterUpdate()
    Makesource
End Sub
Private Sub List1_DblClick(Cancel As Integer)
    stDocName = "Check_Print_single"
    
    stLinkCriteria = "[Checkid]=" & Me.List1
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub
Private Sub Text0_Change()
    Makesource
End Sub
Private Sub Makesource()
    S = "SELECT Check.Checkid, Check.Dp, Check.Checktype, Check.Currate, Check.Sal, Check.Iscur, Check.Empid, Check.Rfpid, Format([D1],'@@@@\/@@\/@@') AS DI, Trim([Checktypen]) AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Check.Curid, Check.Baccno, Check.Checkno, Format([Maturity],'@@@@\/@@\/@@') AS M, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]) AS Zin, Check.Babat , Babs.Bdesc FROM [Check] LEFT JOIN Babs ON Check.Babatid = Babs.Bid WHERE (((Check.Dp)=2) AND ((Check.Checktype) In (0,1,2)) AND ((Check.D1) Between [Forms]![Check_Print_m].[dd1] And [Forms]![Check_Print_m].[dd2]) AND ((Check.Paied)=True) AND ((Check.Signed)=True))"
    Select Case Me.Frame1
        Case 1
            Fldname = "Nz(Empid,0)"
        Case 2
            Fldname = "Nz(Rfpid,0)"
        Case 3
            Fldname = "Nz(D1,0)"
        Case 4
            Fldname = "Nz(Checktypen,0)"
        Case 5
            Fldname = "Nz(Amount,0)"
        Case 6
            Fldname = "Nz(Curid,0)"
        Case 7
            Fldname = "Nz(Baccno,0)"
        Case 8
            Fldname = "Nz(Checkno,)"
        Case 9
            Fldname = "Nz(Maturity,0)"
        Case 10
            Fldname = "IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno])"
        Case 11
            Fldname = "Nz(Babat,0)"
    End Select
    Me.Text0.SetFocus
    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
        S = S & "ORDER BY " & Fldname
    Else
        A = BSPACE([Text0].Text)
        Select Case Me.Frame1
            Case 5
            S = S & " And Amount=" & A & " ORDER BY " & Fldname
            Case Else
            S = S & " And (((BSPACE(" & CStr(Fldname) & ")) Like '*" & Trim(A) & "*'))" & "ORDER BY " & Fldname
        End Select
    End If
    
    Select Case Me.Frame2
        Case 1
            S = S & " ;"
        Case 2
            S = S & " Desc ;"
    End Select
    Me.List1.RowSource = S
End Sub

Private Sub Command43_Click()
On Error GoTo Err_Command43_Click
    Me.Recalc
    Dim stDocName As String

    stDocName = "Check_Print_rep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command43_Click:
    Exit Sub

Err_Command43_Click:
    MsgBox Err.Description
    Resume Exit_Command43_Click
    
End Sub
