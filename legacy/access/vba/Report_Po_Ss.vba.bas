Attribute VB_Name = "Report_Po_Ss"
Attribute VB_Base = "0{AD6F25A2-CF2F-4EE4-B9E4-858A9822F10E}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim Ordstr As String, A As String

Private Sub Report_Activate()
    Me.Coname = Tellconame
End Sub

Private Sub Report_Close()
    Forms!Po_Sm.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Po_Sm.Visible = False
    
    S = "SELECT * ,IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]) AS Zin FROM Check WHERE Dp=2 AND Rfpdate Between [Forms]![Po_Sm]![dd1] And [Forms]![Po_Sm]![dd2]"
    Select Case Forms!Po_Sm.Frame0
        Case 1
            S = S & " And Checktype in(0,1,2,9)"
        Case 2
            S = S & " And Checktype in(0,1,2)"
        Case 3
            S = S & " And Checktype in(9)"
    End Select

    Select Case Forms!Po_Sm.FramePay
        Case 1
            S = S
        Case 2
            S = S & " And Paied=false "
        Case 3
            S = S & " And Paied=True "
    End Select
    
    Select Case Forms!Po_Sm.Frame1
        Case 1
            Ordstr = "Empid"
        Case 2
            Ordstr = "Rfpid"
        Case 3
            Ordstr = "Rfpdate"
        Case 4
            Ordstr = "Rfpref"
        Case 5
            Ordstr = "Checktypen"
        Case 6
            Ordstr = "Curid"
        Case 7
            Ordstr = "Amount"
        Case 8
            Ordstr = "IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]) "
        Case 9
            Ordstr = "Babat"
    End Select

    If IsNull(Forms!Po_Sm!Text0) Or Forms!Po_Sm!Text0 = "" Then
         S = S
    Else
        A = BSPACE(Forms!Po_Sm![Text0])
        S = S & "And iif(isnull(" & CStr(Ordstr) & "),'',BSPACE(" & CStr(Ordstr) & ") Like '*" & Trim(A) & "*')"
    End If
    Me.RecordSource = S & ";"
    
    Select Case Forms!Po_Sm.Frame1
        Case 4
            Ordstr = " CInt(nz([Rfpref],0))"
        Case Else
            Ordstr = Ordstr
    End Select
   
    Select Case Forms!Po_Sm.Frame2
        Case 1
            Ordstr = Ordstr
        Case 2
            Ordstr = Ordstr & " Desc"
    End Select
    Select Case Forms!Po_Sm.Frame1
        Case 2
            Ordstr = Ordstr
        Case Else
            Ordstr = Ordstr & ",Rfpid Desc"
    End Select
    Me.OrderBy = Ordstr
End Sub
