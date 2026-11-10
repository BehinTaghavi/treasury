Attribute VB_Name = "Report_Mainrep"
Attribute VB_Base = "0{78EFCEEA-8230-4665-9F5F-45F378F8F9E9}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Activate()
    Me.Textco = Tellconame
End Sub

Private Sub Report_Close()
    Forms!Mainrep_m.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Mainrep_m.Visible = False
    If Forms!Mainrep_m.Frame3 = 1 Then
        S = "SELECT * From Mainrep Where Scode not in(1,6)And "
    Else
        S = "SELECT * From Mainrep Where Scode in(1,6)And "
    End If
        
        Select Case Forms!Mainrep_m!Frame0
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
    Select Case Forms!Mainrep_m!Frame1
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

    If IsNull(Forms!Mainrep_m!Text0) Or Forms!Mainrep_m!Text0 = "" Then
         S = S
    Else
        A = BSPACE(Forms!Mainrep_m![Text0])
        S = S & "And iif(isnull(" & CStr(Fldname) & "),'',BSPACE(" & CStr(Fldname) & ") Like '*" & Trim(A) & "*')"

    End If
    
    Me.RecordSource = S
    Select Case Forms!Mainrep_m!Frame2
        Case 1
            S = ""
        Case 2
            S = " Desc"
    End Select
    Me.OrderBy = Fldname & S

End Sub
