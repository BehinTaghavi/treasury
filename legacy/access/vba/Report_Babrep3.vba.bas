Attribute VB_Name = "Report_Babrep3"
Attribute VB_Base = "0{E039257B-B548-4210-A9F7-FF6C124E9440}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Close()
    Forms!Babsrepfrm.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Babsrepfrm.Visible = False
    Select Case Forms!Babsrepfrm!Frame0
        Case 0
        S = "SELECT Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي') AS Dpname, Babs.GBid, Babs_1.Bdesc, Babs.Bdesc, Mainrep.Bed, Mainrep.Bes, Mainrep.Babat, Mainrep.Person, Mainrep.Checkno, Mainrep.Sdate, Mainrep.Basetype, Mainrep.Transtype, Mainrep.Maturity, Mainrep.Status FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid ;"
        Case 1, 2
        S = "SELECT Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي') AS Dpname, Babs.GBid, Babs_1.Bdesc, Babs.Bdesc, Mainrep.Bed, Mainrep.Bes, Mainrep.Babat, Mainrep.Person, Mainrep.Checkno, Mainrep.Sdate, Mainrep.Basetype, Mainrep.Transtype, Mainrep.Maturity, Mainrep.Status FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid WHERE (((Babs.Dp)=" & Forms!Babsrepfrm!Frame0 & "));"
    End Select
    Me.RecordSource = S
End Sub
