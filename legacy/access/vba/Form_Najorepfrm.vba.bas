Attribute VB_Name = "Form_Najorepfrm"
Attribute VB_Base = "0{759127C8-8F0E-4716-B322-5FEFD0CFB502}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command19_Click()
    S = "Delete from Najorep"
    docommand S
    If Me.Frame0 = 1 Then
        S = "INSERT INTO Najorep ( Itid,Babat,Maturity,Person, Amount, Curid, Currate, Status, Stcode, Vagno, Itdate, Vagdate, Vosdate, Typeid, Checktypen,Tafno,Checkno) SELECT Check.Checkid,Check.Babat,Check.Maturity,Check.Person, Check.Amount, Check.Curid, Check.Currate, Check.Status, Check.Stcode, Check.Vagno, Check.D1, Check.D3, Check.D4, Check.Checktype, Trim(Check.Checktypen),Check.Tafnotcash,Check.Checkno FROM [Check]WHERE (((Check.Dp)=1) AND ((Check.Checktype) Not In (1,6)));"
        docommand S
        S = "INSERT INTO Najorep ( Itid, Itdate, Cors, Typeid, Vagno, Curid, Currate, Babat, Person, Checkno, Amount, Checktypen,Tafno )SELECT Subs.Sid, Subs.Sdate, 2 AS 1, Subs.Scode, Subs.Baccno, Subs.Curid, Subs.Currate, Subs.Babat, Subs.Person, Subs.Checkno, Subs.Bed , Subs.Basetype, Subs.Tafno FROM Subs WHERE (((Subs.Scode) In (16,17)));"
        docommand S
    Else
        S = "INSERT INTO Najorep ( Itid,Babat,Maturity,Person, Amount, Curid, Currate, Status, Stcode, Vagno, Itdate, Vagdate, Vosdate, Typeid, Checktypen,Tafno,Checkno) SELECT Check.Checkid,Check.Babat,Check.Maturity,Check.Person, Check.Amount, Check.Curid, Check.Currate, Check.Status, Check.Stcode, Check.Vagno, Check.D1, Check.D3, Check.D4, Check.Checktype, Trim(Check.Checktypen),Check.Tafnotcash,Check.Checkno FROM [Check]WHERE Check.Dp=2 AND Check.Checktype  In (0,2) And Check.Paied =true ;"
        docommand S
        S = "INSERT INTO Najorep ( Itid, Itdate, Cors, Typeid, Vagno, Curid, Currate, Babat, Person, Checkno, Amount, Checktypen,Tafno )SELECT Subs.Sid, Subs.Sdate, 2 AS 1, Subs.Scode, Subs.Baccno, Subs.Curid, Subs.Currate, Subs.Babat, Subs.Person, Subs.Checkno, Subs.Bed , Subs.Basetype, Subs.Tafno FROM Subs WHERE (((Subs.Scode) In (26,27)));"
        docommand S
    End If
    
    Dim stDocName As String

    stDocName = "Najorep_D"
    DoCmd.OpenReport stDocName, acPreview

End Sub
Private Sub Form_Close()
    Form_DATE_DCHECK.Visible = False
End Sub

Private Sub Form_Open(Cancel As Integer)
    Form_DATE_DCHECK.Visible = False
End Sub

Private Sub Frame0_AfterUpdate()
    Me.Label1.Caption = IIf(Me.Frame0 = 1, "تاريخ دريافت", "تاريخ پرداخت")
End Sub
