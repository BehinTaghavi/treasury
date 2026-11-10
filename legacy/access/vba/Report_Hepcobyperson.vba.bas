Attribute VB_Name = "Report_Hepcobyperson"
Attribute VB_Base = "0{0F9CAE48-9B3D-4601-A181-6CE5EC3AA849}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim Ordstr As String

Private Sub Report_Close()
    Forms!Hepcoperson.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Hepcoperson.Visible = False
    
    S = "SELECT Check.Person, Sum(Check.Amount) AS SumOfAmount, Count(Check.Checkid) AS CountOfCheckid FROM [Check] WHERE Check.Dp=1 AND (Check.Rfpdate Between [Forms]![Hepcoperson]![D1] And [Forms]![Hepcoperson]![D2]) AND " 'Check.Checktype In (0,2,3,4) AND Check.Subtype=1 AND Check.Stcode Not In 14,15 GROUP BY Check.Person;"
    
    Select Case Forms!Hepcoperson!Frametype
        Case 0
            S = S & "1=1 And "
        Case 1
            S = S & "Checktype In (0,2,3,4) And Check.Stcode Not In (14,15) And "
        Case 2
            S = S & "Checktype In (1,6) And "
        Case 10
            S = S & "Checktype In (0,1,2) And "
        Case 11
            S = S & "Checktype In (0,2) And Check.Stcode Not In (14,15) And "
        Case 12
            S = S & "Checktype In (1) And "
        Case 20
            S = S & "Checktype In (4,6) And "
        Case 21
            S = S & "Checktype In (4) And Check.Stcode Not In (14,15) And "
        Case 22
            S = S & "Checktype In (6) And "
    End Select
    Select Case Forms!Hepcoperson!Framewhatpay
        Case 1
            S = S & " 1=1 "
        Case 2
            S = S & " Subtype=1 "
        Case 3
            S = S & " Subtype=2 "
        Case 4
            S = S & " Subtype=3 "
    End Select
    
    Select Case Forms!Hepcoperson!Frameordfield
        Case 1
            Ordstr = "Person"
        Case 2
            Ordstr = "SumOfAmount"
        Case 3
            Ordstr = "CountOfCheckid"
    End Select
    
    If Forms!Hepcoperson!Frameord = 1 Then
       Ordstr = Ordstr
    Else
       Ordstr = Ordstr & " Desc"
    End If
    S = S & " GROUP BY Check.Person ;"
    Me.RecordSource = S
    Me.OrderBy = Ordstr
End Sub
