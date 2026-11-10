Attribute VB_Name = "Form_TaaodTamin"
Attribute VB_Base = "0{B2E3BB64-3D33-46FB-9826-1016AF53A4BE}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command14_Click()
    Dim S1 As String, Dos1 As Boolean, Docname As String
    S = "Delete * from Taaod"
    docommand S
    Select Case Me.Frame1
    Case 1
    Docname = "Taaodrepalltaminkoli"

    S = "INSERT INTO Taaod ( Person, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12, Status, Sal, Stcode )SELECT Check.Person, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0))/1000 AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0))/1000 AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0))/1000 AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0))/1000 AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0))/1000 AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0))/1000 AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0))/1000 AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0))/1000 AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0))/1000 AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0))/1000 AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0))/1000 AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0))/1000 AS 12," & _
        " Check.Status, Mid([Maturity],1,4) AS S, Check.Stcode FROM [Check]WHERE (((Check.Dp) = 1))GROUP BY Check.Person, Check.Status, Check.Checktype, Mid([Maturity],1,4), Check.Stcode HAVING (((Check.Checktype) Not In (1,6)) AND ((Mid([Maturity],1,4)) Between [forms]![Taaodtamin]![Sal1] And [forms]![Taaodtamin]![Sal2]) AND "
        Select Case Me.Frame0
            Case 1
                S = S & "((Check.Stcode)=11));"
            Case 2
                S = S & "((Check.Stcode)=12));"
            Case 3
                S = S & "((Check.Stcode)=11 Or (Check.Stcode)=12));"
        End Select
    Case 2
    Docname = "Taaodrepalltamin"

    S = "INSERT INTO Taaod ( Dp, Person, Checkno, Maturity, Doctype, Bank, Stcode, Status, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
    " SELECT Check.Dp, Check.Person, Check.Checkno, Check.Maturity, Check.Checktypen, Bank.Bankname, Check.Stcode, Check.Status, Sum(Mid([Maturity],1,4)) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
    " FROM [Check] LEFT JOIN Bank ON Check.Bankid = Bank.Bankname GROUP BY Check.Dp, Check.Person, Check.Checkno, Check.Maturity, Check.Checktypen, Bank.Bankname, Check.Stcode, Check.Status, Check.Checktype HAVING (((Check.Dp)=1) AND "  '((Check.Stcode)=11 Or (Check.Stcode)=12) AND ((Check.Checktype) Not In (1,6)));"
        Select Case Me.Frame0
            Case 1
                S = S & "(Check.Stcode)=11 AND ((Check.Checktype) Not In (1,6)));"
            Case 2
                S = S & "(Check.Stcode)=12 AND ((Check.Checktype) Not In (1,6)));"
            Case 3
                S = S & "((Check.Stcode)=11 Or (Check.Stcode)=12) AND ((Check.Checktype) Not In (1,6)));"
        End Select
    End Select

    docommand S
    DoCmd.OpenReport Docname, acPreview

End Sub
        
Private Sub Command26_Click()
    DoCmd.Close
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Close acForm, "Bank_rep"
End Sub

Private Sub Sal1_AfterUpdate()
    Me.Sal2 = Me.Sal1
End Sub
