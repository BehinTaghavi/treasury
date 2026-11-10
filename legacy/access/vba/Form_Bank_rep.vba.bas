Attribute VB_Name = "Form_Bank_rep"
Attribute VB_Base = "0{0F9C444C-6D66-4660-80FE-812200F4D1D6}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String, mystr1 As String, myrepdate As String
Dim rst As Recordset, rst1 As Recordset
Dim stDocName As String
Dim stLinkCriteria As String

Private Sub Command2_Click()
    Me.L1.Caption = "درحال پردازش عمليات"
    DoEvents
        updatecard
    S = "Delete * from Bankrep"
    docommand S
    
    delfromtemp
    
    S = "INSERT INTO Bankrep ( Baccno, Balance, Bank, Branch )" & _
    " SELECT Subs.Baccno, Sum(IIf([SDATE]<=Forms!Bank_rep!Text0,[bed],0))-Sum(IIf([SDATE]<=Forms!Bank_rep!Text0,[bes],0)) AS M1, Bankacc.Bank, Bankacc.Branch " & _
    " FROM Subs LEFT JOIN Bankacc ON Subs.Baccno = Bankacc.Baccno" & _
    " WHERE(((Subs.Sal) = [Forms]![Sal]![Tsal]))" & _
    " GROUP BY Subs.Baccno, Bankacc.Bank, Bankacc.Branch;"
    docommand S

    S = "INSERT INTO Bankrep_t ( Baccno, am) SELECT Check.Baccno, Sum(Check.Amount) AS SumOfAmount FROM Check WHERE (((Check.Maturity)=[Forms]!Bank_rep![Text0]) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND ((Check.Checktype)<>1 And (Check.Checktype)<>6 And (Check.Checktype)<>9) AND Check.Ebtorkharj=false AND ((Check.Calcit)=False)) GROUP BY Check.Baccno ORDER BY Check.Baccno;"
    docommand S

    S = "UPDATE Bankrep INNER JOIN Bankrep_t ON Bankrep.Baccno = Bankrep_t.Baccno SET Bankrep.Inpayday = Bankrep_t.am;"
    docommand S

    delfromtemp

    ' future pays
    S = "INSERT INTO Bankrep_t ( Baccno, am )" & _
    " SELECT Check.Baccno, Sum(Check.Amount) AS SumOfAmount " & _
    " FROM Check " & _
    " WHERE (((Check.Maturity) Between [Forms]![Bank_rep]![D0] And [Forms]![Bank_rep]![D1]) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND Check.Ebtorkharj=false AND ((Check.Checktype)<>1 And (Check.Checktype)<>6) AND ((Check.Calcit)=False)) " & _
    " GROUP BY Check.Baccno " & _
    " ORDER BY Check.Baccno;"
    docommand S

    S = "UPDATE Bankrep INNER JOIN Bankrep_t ON Bankrep.Baccno = Bankrep_t.Baccno SET Bankrep.Inpaynext = Bankrep_t.am;"
    docommand S

    delfromtemp

    S = "INSERT INTO Bankrep_t ( Baccno, am ) SELECT Check.Baccno, Sum(Check.Amount) AS SumOfAmount FROM Check WHERE (((Check.Maturity)<[Forms]![Bank_rep]![Text0]) AND ((Check.Dp)=2) AND ((Check.Checktype)<>1 And (Check.Checktype)<>6 And (Check.Checktype)<>9) AND ((Check.Calcit)=False)AND ((Check.stcode)<>26)) GROUP BY Check.Baccno ORDER BY Check.Baccno;"
    docommand S

    S = "UPDATE Bankrep INNER JOIN Bankrep_t ON Bankrep.Baccno = Bankrep_t.Baccno SET Bankrep.Inpaypast = Bankrep_t.am;"
    docommand S

    delfromtemp

    S = "INSERT INTO Bankrep_t ( Baccno, am )SELECT Check.Vagno, Sum(Check.Amount) AS SumOfAmount FROM Check WHERE (((Check.Far) = False) And ((Check.Dp) = 1) And ((Check.Checktype) <> 1 And (Check.Checktype) <> 6) And ((Check.Stcode) = 12))GROUP BY Check.Vagno ORDER BY Check.Vagno;"
    docommand S

    S = "UPDATE Bankrep INNER JOIN Bankrep_t ON Bankrep.Baccno = Bankrep_t.Baccno SET Bankrep.Jvosoolm = Bankrep_t.am;"
    docommand S

    delfromtemp

    S = "INSERT INTO Bankrep_t ( Baccno, am )SELECT Check.Vagno, Sum(Check.Amount) AS SumOfAmount FROM ‍‍Check WHERE (((Check.Far) = True) And ((Check.Dp) = 1) And ((‍‍Check.Checktype) <> 1 And (Check.Checktype) <> 6) And ((Check.Stcode) = 12))GROUP BY Check.Vagno ORDER BY Check.Vagno;"
    docommand S

    S = "UPDATE Bankrep INNER JOIN Bankrep_t ON Bankrep.Baccno = Bankrep_t.Baccno SET Bankrep.Jvosoolfar = Bankrep_t.am;"
    docommand S

    delfromtemp
    
    S = "INSERT INTO Bankrep_t ( am, Far, Baccno ) SELECT Sum(Check.Amount) AS SumOfAmount, Check.Far, IIf([far]=True,'چكهاي نزد صندوق - شهرستان','چكهاي نزد صندوق - تهران') AS a FROM Check WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0 or (Check.Checktype)=2) AND ((‍‍Check.Stcode)=11)) GROUP BY Check.Far, IIf([far]=True,'چكهاي نزد صندوق - شهرستان','چكهاي نزد صندوق - تهران');"
    docommand S
    
    Me.L1.Caption = "اتمام پردازش عمليات"
    DoEvents

    DoCmd.OpenReport "Bankrep", acPreview

End Sub

Private Sub Command4_Click()
    DoCmd.Close
End Sub
Private Sub Command64_Click()
    Select Case Me.Frame2
        Case 1, 2, 3
        stDocName = "Bankrep_d_1"
        Case Else
        stDocName = "Bankrep_d_jv"
    End Select
    DoCmd.OpenReport stDocName, acViewPreview

End Sub

Private Sub Command79_Click()

    stDocName = "Taaod"
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Command80_Click()
    stDocName = "TaaodTamin"
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Command81_Click()
    stDocName = "longs_indetailmonth"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command82_Click()
    stDocName = "longs_indetail"
    DoCmd.OpenReport stDocName, acPreview

End Sub

Private Sub Form_Open(Cancel As Integer)
    Me.D1 = "140" & AddNumberAndConvertToNewDate([Text0], [T0], True)
    Me.Dta = "140" & AddNumberAndConvertToNewDate([Text0], [T0], True)
'    Me.Command2.Enabled = True
'    Me.Command4.Enabled = True
'    Me.Command5.Enabled = True
'    Me.Command6.Enabled = True
End Sub

Private Sub Frame1_AfterUpdate()
    Select Case Me.Frame1
        Case 1
            Me.T0.Enabled = True
            Me.T1.Enabled = False
        Case 2
            Me.T0.Enabled = False
            Me.T1.Enabled = True
    End Select
    
End Sub

Private Sub Frame3_AfterUpdate()
    Select Case Me.Frame3
        Case 1
            Me.Bacc = Null
            Me.Bacc.Enabled = False
        Case 2
            Me.Bacc.Enabled = True
    End Select
End Sub

Private Sub Frame4_AfterUpdate()
    Select Case Me.Frame4
        Case 1
            Me.S1.Enabled = False
            Me.S2.Enabled = False
        Case 2
            Me.S1.Enabled = True
            Me.S2.Enabled = True
    End Select
End Sub

Private Sub T0_AfterUpdate()
    If Me.T0 = 0 Or IsNull(Me.T0) Then
        Me.T0 = 1
    End If
    Me.D1 = "140" & AddNumberAndConvertToNewDate([Text0], [T0], True)
    Me.Dta = "140" & AddNumberAndConvertToNewDate([Text0], [T0], True)
End Sub

Private Sub T1_AfterUpdate()
    Me.D1 = Me.T1
    Me.Dta = Me.T1
End Sub

Public Sub delfromtemp()
    S = "Delete * from Bankrep_t"
    docommand S
End Sub

