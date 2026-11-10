Attribute VB_Name = "Report_Check_Print_rep"
Attribute VB_Base = "0{D40B3B91-68AF-4939-990C-01E586210444}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Private Sub Report_Open(Cancel As Integer)
    S = "SELECT Check.*, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]) AS Zin,Format([D1],'@@@@\/@@\/@@') AS DI, Trim([Checktypen]) AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Format([Maturity],'@@@@\/@@\/@@') AS M FROM [Check] WHERE Check.Dp=2 AND Check.Checktype In (0,1,2) AND Check.D1 Between [Forms]![Check_Print_m].[dd1] And [Forms]![Check_Print_m].[dd2] AND Check.Paied=True AND Check.Signed=True "
    'S = "SELECT Check.Checkid, Check.Dp, Check.Checktype, Check.Currate, Check.Sal, Check.Iscur, Check.Empid, Check.Rfpid,Check.D1,Check.Checktypen,Check.Maturity,Check.Amount, Format([D1],'@@@@\/@@\/@@') AS DI, Trim([Checktypen]) AS CHty, IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')) AS Am, Check.Curid, Check.Baccno, Check.Checkno, Format([Maturity],'@@@@\/@@\/@@') AS M, Check.Person, Check.Babat FROM [Check] WHERE Check.Dp=2 AND Check.Checktype In (0,1,2) AND Check.D1 Between Forms!Check_Print_m.dd1 And Forms!Check_Print_m.dd2 AND Check.Paied=True AND Check.Signed=True "
    Select Case Forms!Check_Print_m.Frame1
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
            Fldname = "Baccno"
        Case 8
            Fldname = "Checkno"
        Case 9
            Fldname = "Maturity"
        Case 10
            Fldname = "IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno])"
        Case 11
            Fldname = "Babat"
    End Select

    If IsNull(Forms!Check_Print_m.Text0) Or Forms!Check_Print_m.Text0 = "" Then
        S = S & "ORDER BY " & Fldname
    Else
        A = BSPACE(Forms!Check_Print_m.Text0)
        Select Case Forms!Check_Print_m.Frame1
            Case 5
            S = S & " And Amount=" & A & " ORDER BY " & Fldname
            Case Else
            S = S & " And (((BSPACE(" & CStr(Fldname) & ")) Like '*" & Trim(A) & "*'))" & "ORDER BY " & Fldname
        End Select
    End If
    S = S & " ;"
    Me.RecordSource = S
    Select Case Forms!Check_Print_m.Frame2
        Case 1
            Ordstr = ""
        Case 2
            Ordstr = " Desc"
    End Select
    Me.OrderBy = Fldname & Ordstr

End Sub
