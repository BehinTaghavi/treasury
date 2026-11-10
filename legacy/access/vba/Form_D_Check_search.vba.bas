Attribute VB_Name = "Form_D_Check_search"
Attribute VB_Base = "0{D946DAFB-3415-4546-BC57-034FDA68F68C}"
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
    stDocName = "D_Check"
    
    stLinkCriteria = "[Checkid]=" & Me.List1
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Text0_Change()
    Makesource
End Sub

Private Sub Makesource()


    ' IIf(IsNull([Pbycheckno]),[Checkno],[Checkno] & " چ.ب " & [Pbycheckno]) AS Chbno
    'S = "SELECT Check.Checkid, Check.Dp, Check.Checktype, Check.Currate, Check.Sal, Check.Iscur, Check.Empid, Check.Rfpid,Format([D1],'@@@@\/@@\/@@') AS DI,Check.Rfpref, Trim([Checktypen]) AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Check.Curid, Check.Checkno, Format([Maturity],'@@@@\/@@\/@@') AS M, Check.Person, Check.Babat,Trim(Check.Status) as St FROM [Check] WHERE Check.Dp=1 AND Check.D1 Between dd1 And dd2  "
     S = "SELECT Check.Checkid, Check.Dp, Check.Checktype, Check.Currate, Check.Sal, Check.Iscur, Check.Empid, Check.Rfpid,Format([D1],'@@@@\/@@\/@@') AS DI,Check.Rfpref, Trim([Checktypen]) & iif([Faragir]=true,'-فراگير ','')AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Check.Curid, IIf(IsNull([Pbycheckno]),[Checkno],[Checkno] & ' چ.ب ' & [Pbycheckno]) AS Checknobank, Format([Maturity],'@@@@\/@@\/@@') AS M, Check.Person, Check.Babat,Trim(Check.Status) as St , Babs.Bdesc FROM [Check] LEFT JOIN Babs ON Check.Babatid = Babs.Bid WHERE Check.Dp=1 AND Check.D1 Between dd1 And dd2  "
    Select Case Me.Frame1
        Case 1
            Fldname = "Empid"
        Case 2
            Fldname = "Rfpid"
        Case 3
            Fldname = "D1"
        Case 4
            Fldname = "Checktypen"
        Case 5
            Fldname = "Amount"
        Case 6
            Fldname = "Curid"
        Case 7
            Fldname = "Rfpref"
        Case 8
            Fldname = "IIf(IsNull([Pbycheckno]),[Checkno],[Checkno] & ' چ.ب ' & [Pbycheckno])"
        Case 9
            Fldname = "Maturity"
        Case 10
            Fldname = "Person"
        Case 11
            Fldname = "Babat"
        Case 12
            Fldname = "Status"
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

    stDocName = "D_Check_rep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command43_Click:
    Exit Sub

Err_Command43_Click:
    MsgBox Err.Description
    Resume Exit_Command43_Click
    
End Sub
