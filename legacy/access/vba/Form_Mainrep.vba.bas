Attribute VB_Name = "Form_Mainrep"
Attribute VB_Base = "0{779ABE52-EEBC-4AC2-AF9D-5171531D14F8}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim myd1 As String, myd2 As String

Private Sub Command0_Click()
    Me.Recalc
    myd1 = Nz(Me.DD1)
    myd2 = Nz(Me.DD2)
    S = "Delete * from Mainrep"
    docommand S
    If Me.Frame10 = 1 Then
        S = "INSERT INTO Mainrep ( Sid, Scode, Checkid, Sdate, Basetype, Transtype, Baccno, Bed, Bes, Iscur, Curid, Currate, Sal, Babat, Babatid, Invno, Contract, Checkno, Person, Ourno )SELECT Subs.Sid, Subs.Scode, Subs.Checkid, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Bed, Subs.Bes, Subs.Iscur, Subs.Curid, Subs.Currate, Subs.Sal, Subs.Babat, Subs.Babatid, Subs.Invno, Subs.Contract, Subs.Checkno, Subs.Person, Subs.Ourno FROM Subs WHERE (((Subs.Scode)<>10) AND ((Subs.Sdate) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Subs.Card)=False));"
        docommand S
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Transtype, Maturity, Bed )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.vagno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Dp)=1));"
        docommand S
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Transtype, Maturity, Bes )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, Check.Maturity, Check.Amount FROM [Check]WHERE (((Check.D1) Between [Forms]![Mainrep]![dd1] And [Forms]![mainrep]![dd2]) AND ((Check.Checktype)<>9) AND ((Check.Dp)=2) AND ((Check.EbtorKharj)=False) AND ((Check.Payit)=True) AND ((Check.Paied)=True));"
        'S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Transtype, Maturity, Bes )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Dp)=2) AND ((Check.EbtorKharj)=False) AND ((Check.Payit)=True) AND ((Check.Checktype)<>9) AND ((Check.Transaccid)=Null) AND ((Check.Paied)=True));"
        docommand S
            'Exit Sub
        DoCmd.OpenForm "Mainrep_m"
    Else
'        S = "INSERT INTO Mainrep ( Sid, Scode, Checkid, Sdate, Basetype, Transtype, Baccno, Bed, Bes, Iscur, Curid, Currate, Sal, Babat, Babatid, Invno, Contract, Checkno, Person, Ourno )SELECT Subs.Sid, Subs.Scode, Subs.Checkid, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Bed, Subs.Bes, Subs.Iscur, Subs.Curid, Subs.Currate, Subs.Sal, Subs.Babat, Subs.Babatid, Subs.Invno, Subs.Contract, Subs.Checkno, Subs.Person, Subs.Ourno FROM Subs WHERE (((Subs.Scode)<>10) AND ((Subs.Sdate) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') And ((Subs.Babatid)=33) AND ((Subs.Card)=False));"
'        docommand S
'        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Basetype, Transtype, Maturity, Bes )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, 'چكها/اسناد صادره' , Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Checktype)<>9) AND ((Check.Dp)=2) AND ((Check.EbtorKharj)=False) AND ((Check.Payit)=True) AND ((Check.Transaccid) Is Null) And ((Check.Babatid)=32) AND ((Check.Paied)=True));"
'        docommand S
        
        S = "INSERT INTO Mainrep ( Sid, Scode, Checkid, Sdate, Basetype, Transtype, Baccno, Bed, Bes, Iscur, Curid, Currate, Sal, Babat, Babatid, Invno, Contract, Checkno, Person, Ourno )SELECT Subs.Sid, Subs.Scode, Subs.Checkid, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Bed, Subs.Bes, Subs.Iscur, Subs.Curid, Subs.Currate, Subs.Sal, Subs.Babat, Subs.Babatid, Subs.Invno, Subs.Contract, Subs.Checkno, Subs.Person, Subs.Ourno FROM Subs WHERE (((Subs.Scode)<>10) AND ((Subs.Sdate) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Subs.Card)=False));"
        docommand S
        
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Basetype, Transtype, Maturity, Bes )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, 'چكها/اسناد صادره' , Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Checktype)<>9) AND ((Check.Dp)=2) AND ((Check.EbtorKharj)=False) AND ((Check.Payit)=True) AND ((Check.Transaccid) Is Null) AND ((Check.Paied)=True));"
        docommand S
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Basetype, Transtype, Maturity, Bed )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, Check.Babatid, Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, 'چكها/اسناد دريافتي' , Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Stcode)in(11,12,13,14)) AND ((Check.Dp)=1));"
        docommand S
        
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Basetype,Transtype, Maturity, Bes )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, '32', Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, 'انتقالي به ساير حسابها', Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D5)Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Dp)=2) AND ((Check.Transaccid) Is Not Null) AND ((Check.Calcit)=True));"
        docommand S
        S = "INSERT INTO Mainrep ( Sdate, Checkid, Checkno, Iscur, Curid, Currate, Sal, Person, Baccno, Babat, Babatid, Invno, Contract, Status, Stcode, Scode, Basetype,Transtype, Maturity, Bed )SELECT Check.D1, Check.Checkid, Check.Checkno, Check.Iscur, Check.Curid, Check.Currate, Check.Sal, Check.Person, Check.Baccno, Check.Babat, '33', Check.Invno, Check.Contract, Check.Status, Check.Stcode, Check.Checktype, Check.Checktypen, 'انتقالي از ساير حسابها', Check.Maturity, Check.Amount FROM [Check] WHERE (((Check.D5)Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Dp)=2) AND ((Check.Transaccid) Is Not Null) AND ((Check.Calcit)=True));"
        docommand S
        DoCmd.OpenForm "Babsrepfrm"
    End If
End Sub

Private Sub Form_Open(Cancel As Integer)
    updatecard
End Sub
