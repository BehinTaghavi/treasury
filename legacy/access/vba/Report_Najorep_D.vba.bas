Attribute VB_Name = "Report_Najorep_D"
Attribute VB_Base = "0{6AAE0354-1404-4EDF-B469-BE4C80CEF8C7}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim S As String, mainstr As String, typestr As String, datestr As String, Baccstr As String, kaz As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String

Private Sub Report_Close()
    Form_Najorepfrm.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_Najorepfrm.Visible = False
    S = "SELECT Najorep.*, t_unique_accounts.Account_Name FROM Najorep LEFT JOIN t_unique_accounts ON Najorep.Tafno = t_unique_accounts.Account_Code Where "
    Select Case Form_Najorepfrm.Frame1
        Case 1
            S = S & " 1=1 And "
        Case 2
            S = S & " Cors=1 And "
        Case 3
            S = S & " Cors=2 And "
    End Select
    
    Select Case Form_Najorepfrm.Frame2
        Case 0
            S = S & " 1=1  And "
        Case 1
            S = S & " Itdate Between Forms!Najorepfrm!D11 and Forms!Najorepfrm!D21 And "
            Me.OrderBy = "Itdate"

        Case 2
            S = S & " Maturity Between Forms!Najorepfrm!D11 and Forms!Najorepfrm!D21 and "
            Me.OrderBy = "Maturity"

    End Select
   
    If IsNull(Form_Najorepfrm.Tafno) Then
        S = S & " 1=1   "
    Else
        S = S & " Tafno='" & Form_Najorepfrm.Tafno & "'  "
    End If

    If IsNull(Form_Najorepfrm.Person) Then
        perstr = ""
    Else
        perstr = BSPACE(Form_Najorepfrm.Person)
        perstr = " And IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
    End If


    If IsNull(Form_Najorepfrm.Babat) Then
        babstr = ""
    Else
        babstr = BSPACE(Form_Najorepfrm.Babat)
        babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
    End If
    
    
    
    S = S & perstr & babstr
    Me.RecordSource = S
End Sub
