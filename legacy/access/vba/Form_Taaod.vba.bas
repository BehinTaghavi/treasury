Attribute VB_Name = "Form_Taaod"
Attribute VB_Base = "0{FE86325A-737E-4F7B-9BEE-BD2A2480086A}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command14_Click()
    Dim S1 As String, Dos1 As Boolean, Docname As String
    Dos1 = False
    S = "Delete * from Taaod"
    docommand S
    Select Case Me.Frame1
        Case 1
            Select Case Me.Frame0
                Case 1
                   S = "INSERT INTO Taaod ( Dp, Doctype, Status, Stcode, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Check.Checktypen, Check.Status, Check.Stcode, Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check] GROUP BY Check.Dp, Check.Checktypen, Check.Status, Check.Stcode, Mid([Maturity],1,4), Check.Checktype HAVING (((Check.Dp)=1) AND ((Check.Stcode)=11 Or (Check.Stcode)=12) AND ((Check.Checktype) Not In (1,6)));"
                Case 2
                    S = "INSERT INTO Taaod ( Dp, Doctype, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Check.Checktypen, Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check]WHERE (((Check.Paied) = True) And ((Check.Calcit) = False) And ((Check.Stcode) <> 26))GROUP BY Check.Dp, Check.Checktypen, Mid([Maturity],1,4), Check.Checktype HAVING (((Check.Dp)=2) AND ((Check.Checktype) Not In (0,1,6)));"
                Case 3
                    S = "INSERT INTO Taaod ( Dp, Doctype,Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp,'جمع اقلام دريافتني', Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check] WHERE (((Check.Stcode)=11 Or (Check.Stcode)=12) AND ((Check.Checktype) Not In (1,6)))GROUP BY Check.Dp, Mid([Maturity],1,4) HAVING (((Check.Dp)=1));"
                    S1 = "INSERT INTO Taaod ( Dp, Doctype,Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp,'جمع اقلام پرداختني' ,Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check] WHERE (((Check.Checktype) Not In (1,6)) AND ((Check.Paied)=True) AND ((Check.Calcit)=False) AND ((Check.Stcode)<>26)) GROUP BY Check.Dp, Mid([Maturity],1,4) HAVING (((Check.Dp)=2));"
                     Dos1 = True
            End Select
        Case 2
            Select Case Me.Frame0
                Case 1
                    S = "INSERT INTO Taaod ( Dp, Sal, Person, Doctype, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Mid([Maturity],1,4) AS S, Check.Person, Check.Checktypen, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12" & _
                    " FROM [Check] WHERE (((Check.Stcode) = 11 Or (Check.Stcode) = 12))GROUP BY Check.Dp, Mid([Maturity],1,4), Check.Person, Check.Checktypen, Check.Checktype HAVING (((Check.Dp)=1) AND ((Check.Checktype) Not In (1,6)));"
                Case 2
                    S = "INSERT INTO Taaod ( Dp, Doctype, Person, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Check.Checktypen, Check.Person, Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check] WHERE (((Check.Paied) = True) And ((Check.Calcit) = False) And ((Check.Stcode) <> 26))GROUP BY Check.Dp, Check.Checktypen, Check.Person, Mid([Maturity],1,4), Check.Checktype HAVING (((Check.Dp)=2) AND ((Check.Checktype) Not In (0,1,6)));"
                Case 3
            End Select
        Case 3
            Select Case Me.Frame0
                Case 1
                    S = "INSERT INTO Taaod ( Dp, Person, Checkno, Maturity, Doctype, Bank, Stcode, Status, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Check.Person, Check.Checkno, Check.Maturity, Check.Checktypen, Bank.Bankname, Check.Stcode, Check.Status, Sum(Mid([Maturity],1,4)) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12 " & _
                    " FROM [Check] LEFT JOIN Bank ON Check.Bankid = Bank.Bankname GROUP BY Check.Dp, Check.Person, Check.Checkno, Check.Maturity, Check.Checktypen, Bank.Bankname, Check.Stcode, Check.Status, Check.Checktype HAVING (((Check.Dp)=1) AND ((Check.Stcode)=11 Or (Check.Stcode)=12) AND ((Check.Checktype) Not In (1,6)));"
                Case 2
                    S = "INSERT INTO Taaod ( Dp, Doctype, Person, Maturity, Checkno, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )" & _
                    " SELECT Check.Dp, Check.Checktypen, Check.Person, Check.Maturity, Check.Checkno, Mid([Maturity],1,4) AS S, Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)) AS 1, Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)) AS 2, Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)) AS 3, Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)) AS 4, Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)) AS 5, Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)) AS 6, Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)) AS 7, Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)) AS 8, Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)) AS 9, Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)) AS 10, Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)) AS 11, Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) AS 12" & _
                    " FROM [Check]WHERE (((Check.Paied) = True) And ((Check.Calcit) = False) And ((Check.Stcode) <> 26))GROUP BY Check.Dp, Check.Checktypen, Check.Person, Check.Maturity, Check.Checkno, Mid([Maturity],1,4), Check.Checktype HAVING (((Check.Dp)=2) AND ((Check.Checktype) Not In (0,1,6)));"
                Case 3
            End Select
    End Select
    docommand S
    
    If Dos1 = True Then
        docommand S1
        Docname = "Taaodrepall"
    Else
        Docname = "Taaodrep"
    End If
    If Me.Frameragham = 1 Then
        S = "UPDATE Taaod SET Taaod.M1 = [M1]/1000, Taaod.M2 = [M2]/1000, Taaod.M3 = [M3]/1000, Taaod.M4 = [M4]/1000, Taaod.M5 = [M5]/1000, Taaod.M6 = [M6]/1000, Taaod.M7 = [M7]/1000, Taaod.M8 = [M8]/1000, Taaod.M9 = [M9]/1000, Taaod.M10 = [M10]/1000, Taaod.M11 = [M11]/1000, Taaod.M12 = [M12]/1000;"
        docommand S
        If Me.Frameonerow = 1 Then
        
            S = "Delete * from Taaodonerow"
            docommand S
            S = "INSERT INTO Taaodonerow ( Dp, Doctype, Sal, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12 )SELECT Taaod.Dp, (IIf([Dp]=1,'اقلام دريافتني','اقلام پرداختني')) AS itname, Taaod.Sal, Sum(Taaod.M1) AS SumOfM1, Sum(Taaod.M2) AS SumOfM2, Sum(Taaod.M3) AS SumOfM3, Sum(Taaod.M4) AS SumOfM4, Sum(Taaod.M5) AS SumOfM5, Sum(Taaod.M6) AS SumOfM6, Sum(Taaod.M7) AS SumOfM7, Sum(Taaod.M8) AS SumOfM8, Sum(Taaod.M9) AS SumOfM9, Sum(Taaod.M10) AS SumOfM10, Sum(Taaod.M11) AS SumOfM11, Sum(Taaod.M12) AS SumOfM12 FROM Taaod GROUP BY Taaod.Dp, Taaod.Sal;"
            docommand S

            Docname = "Taaodrepcompactonerow"
        Else
            Docname = "Taaodrepcompact"
        End If
    End If
    
    
    DoCmd.OpenReport Docname, acPreview

End Sub
        
Private Sub Command26_Click()
    DoCmd.Close
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Close acForm, "Bank_rep"
End Sub
