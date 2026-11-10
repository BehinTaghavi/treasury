Attribute VB_Name = "Report_DP_searchrep"
Attribute VB_Base = "0{EA90ADB4-CF7F-4104-B49C-0A97C4E8BA3D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String, myd1 As String, myd2 As String, mybacc As String
Dim accstr As String, typestr As String, Ordstr As String
Private Sub Report_Close()
    Forms!DP_search.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!DP_search.Visible = False
    S = "SELECT Subs.*, Format([Sdate],'@@@@\/@@\/@@') AS DI, IIf([Iscur]=0,Format([Bed],'#,##0'),Format([Bed],'Standard')) AS Bedd, IIf([Iscur]=0,Format([Bes],'#,##0'),Format([Bes],'Standard')) AS Bess FROM Subs WHERE Subs.Scode<>10 AND Subs.Card=0 AND Subs.Sdate Between Forms!DP_search.[DD1] And Forms!DP_search.[DD2] "
    Select Case Forms!DP_search.PT
        Case 0
            S1 = ""
        Case 1
            S1 = " And (Scode=16 or Scode=17) "
        Case 2
            S1 = " And (Scode=26 or Scode=27) "
        Case Else
            S1 = " And Scode=" & Forms!DP_search.PT & " "
    End Select
    S = S & S1
    Select Case Forms!DP_search.Frame1
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
    Forms!DP_search.Text0.SetFocus
    If IsNull(Forms!DP_search.Text0) Or Forms!DP_search.Text0 = "" Then
        S = S & "ORDER BY " & Fldname
    Else
        A = BSPACE(Forms!DP_search.Text0)
        Select Case Forms!DP_search.Frame1
            Case 7
            S = S & " And Bed=" & A & " ORDER BY " & Fldname
            Case 8
            S = S & " And Bes=" & A & " ORDER BY " & Fldname
            Case Else
            S = S & " And (((BSPACE(" & CStr(Fldname) & ")) Like '*" & Trim(A) & "*'))" & "ORDER BY " & Fldname
        End Select
    End If
    Me.RecordSource = S & " ;"
    Select Case Forms!DP_search.Frame2
        Case 1
             fldsort = ""
        Case 2
            fldsort = " Desc"
    End Select
    Me.OrderBy = Fldname & fldsort
End Sub
