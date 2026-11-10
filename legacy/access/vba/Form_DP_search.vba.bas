Attribute VB_Name = "Form_DP_search"
Attribute VB_Base = "0{1A50F0EA-16BE-4119-8F20-59145226DF3E}"
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
    Select Case Me.List1.Column(1)
        Case 16
            stDocName = "D_Variz"
        Case 17
            stDocName = "D_Naghd"
        Case 26
            stDocName = "P_Bardasht"
        Case 27
            stDocName = "P_Naghd"
    End Select
    
    stLinkCriteria = "[Sid]=" & Me.List1
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub PT_AfterUpdate()
    Makesource
End Sub

Private Sub Text0_Change()
    Makesource
End Sub
Private Sub Makesource()
'SELECT Check.*, Babs.Bdesc
'FROM [Check] LEFT JOIN Babs ON Check.Babatid = Babs.Bid;

    S = "SELECT Subs.Sid,Subs.Scode, Format([Sdate],'@@@@\/@@\/@@') AS DI, Subs.Transtype, Subs.Basetype, Subs.Checkno,Subs.Ourno, Subs.Baccno, IIf([Iscur]=0,Format([Bed],'#,##0'),Format([Bed],'Standard')) AS Bedd, IIf([Iscur]=0,Format([Bes],'#,##0'),Format([Bes],'Standard')) AS Bess, Subs.Curid,Subs.Person, Subs.Babat, Babs.Bdesc FROM Subs LEFT JOIN Babs ON Subs.Babatid = Babs.Bid WHERE Subs.Scode<>10 AND Subs.Card=0 AND Subs.Sdate Between [DD1] And [DD2] "
    Select Case Me.PT
        Case 0
            S1 = ""
        Case 1
            S1 = " And (Scode=16 or Scode=17) "
        Case 2
            S1 = " And (Scode=26 or Scode=27) "
        Case Else
            S1 = " And Scode=" & Me.PT & " "
    End Select
    S = S & S1
    Select Case Me.Frame1
        Case 1
            Fldname = "Sdate"
        Case 2
            Fldname = "Transtype"
        Case 3
            Fldname = "Basetype"
        Case 4
            Fldname = "Checkno"
        Case 5
            Fldname = "Ourno"
        Case 6
            Fldname = "Baccno"
        Case 7
            Fldname = "Bed"
        Case 8
            Fldname = "Bes"
        Case 9
            Fldname = "Curid"
        Case 10
            Fldname = "Person"
        Case 11
            Fldname = "Babat"
        End Select
    Me.Text0.SetFocus
    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
        S = S & "ORDER BY " & Fldname
    Else
        A = BSPACE([Text0].Text)
        Select Case Me.Frame1
            Case 7
            S = S & " And Bed=" & A & " ORDER BY " & Fldname
            Case 8
            S = S & " And Bes=" & A & " ORDER BY " & Fldname
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

    stDocName = "Dp_Searchrep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command43_Click:
    Exit Sub

Err_Command43_Click:
    MsgBox Err.Description
    Resume Exit_Command43_Click
    
End Sub
