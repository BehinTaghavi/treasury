Attribute VB_Name = "Form_Deaily_rep"
Attribute VB_Base = "0{1040E877-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String, mystr1 As String, myrepdate As String
Dim rst As Recordset, rst1 As Recordset

Private Sub Command2_Click()
    Select Case Me.Text1
        Case 0
            GoTo line0
        Case 1
            DoCmd.OpenReport "Dailyrepbanks", acPreview
            Exit Sub
    End Select
    
line0:
    DoCmd.SetWarnings False
    myrepdate = Me.Text0
    mystr1 = "Delete * from Dailyrep"
    DoCmd.RunSQL mystr1
    '***********daryaft
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 0 as N,Check.Vagno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'وصول چكهاي واگذاري'FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=13) AND ((Check.D4)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 0 as N,Subs.Baccno, Subs.Checkno, subs.Sdate, Subs.Bed, Subs.Person, Subs.Babat, 'واريز به حسابهاي بانكي' FROM [Subs] WHERE (((Subs.Scode)=16) AND ((Subs.Sdate)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1
    
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 0 as N,Subs.Baccno, Subs.Checkno, subs.Sdate, Subs.Bed, Subs.Person, Subs.Babat, 'دريافت نقدي' FROM [Subs] WHERE (((Subs.Scode)=17) AND ((Subs.Sdate)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1
    
    'ebtal
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 0 as N,Check.Baccno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'ابطال چكهاي نقدي صادره' FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.EbtorKharj)=True) AND ((Check.Checktype)=0) AND ((Check.D2)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1


    '***********pardakht
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bes, Person, Babat, Rowdesc ) SELECT 0 as N,Check.Baccno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'صدورچك' FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0) AND ((Check.D1)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1
    
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bes, Person, Babat, Rowdesc ) SELECT 0 as N,Check.Baccno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'تبديل تعهدات مدت داربه جاري'  FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Ctocash)=True) AND ((Check.D3)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1
    
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bes, Person, Babat, Rowdesc ) SELECT 0 as N,Subs.Baccno, Subs.Checkno, subs.Sdate, Subs.Bes, Subs.Person, Subs.Babat, ' برداشت ازحسابهاي بانكي'  FROM [Subs] WHERE (((Subs.Scode)=26) AND ((Subs.Sdate)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1
    
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bes, Person, Babat, Rowdesc ) SELECT 0 as N,Subs.Baccno, Subs.Checkno, subs.Sdate, Subs.Bes, Subs.Person, Subs.Babat, 'پرداخت نقدي'  FROM [Subs] WHERE (((Subs.Scode)=27) AND ((Subs.Sdate)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

'*************** Not effectiva on banks or cash
    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 1 as N,Check.Baccno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'ابطال چكهاي انتظامي/مدت دارصادره'  FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.EbtorKharj)=True) AND ((Check.Checktype)<>0) AND ((Check.D2)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bes, Person, Babat, Rowdesc ) SELECT 1 as N,Check.Baccno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'صدورچك /اسنادانتظامي/مدت دار'  FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)<>0) AND ((Check.D1)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 1 as N,Check.Vagno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'دريافت چك/اسناد بهادار'  FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.D1)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 1 as N,Check.Vagno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'واگذاري چكهاي نزد صندوق'  FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=12 Or (Check.Stcode)=13 Or (Check.Stcode)=14 ) AND ((Check.D3)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    mystr1 = "INSERT INTO Dailyrep ( Mtype,Baccno, CheckNo, Maturity, Bed, Person, Babat, Rowdesc ) SELECT 1 as N,Check.Vagno, Check.Checkno, Check.Maturity, Check.Amount, Check.Person, Check.Babat, 'برگشت چكهاي واگذاري' AS R FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=14) AND ((Check.D4)=" & "'" & CStr(myrepdate) & "'));"
    DoCmd.RunSQL mystr1

    DoCmd.SetWarnings True
'line0:
    
    DoCmd.OpenReport "Dailyrep", acPreview

End Sub

Private Sub Command4_Click()
    DoCmd.Close
End Sub

Private Sub Command5_Click()
    DoCmd.OpenReport "Dailyrepbanks_sub_sub", acPreview
End Sub

Private Sub Command6_Click()
    DoCmd.OpenReport "Dailyrepbanks_sub_sub_p", acPreview
End Sub

Private Sub Form_Open(Cancel As Integer)
    Me.L1.Caption = "درحال پردازش عمليات"
    DoEvents
        updatecard
    Me.L1.Caption = "اتمام پردازش عمليات"
    DoEvents
    Me.Command2.Enabled = True
    Me.Command4.Enabled = True
    Me.Command5.Enabled = True
    Me.Command6.Enabled = True
End Sub
