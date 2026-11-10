Attribute VB_Name = "Report_Babrep2"
Attribute VB_Base = "0{8E4285FC-55D1-4746-AA63-F955184CAE38}"
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
        S = "SELECT Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي') AS Dpname, Babs.GBid, Babs_1.Bdesc, Babs.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي'), Babs.GBid, Babs_1.Bdesc, Babs.Bdesc ;"
        Case 1, 2
        S = "SELECT Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي') AS Dpname, Babs.GBid, Babs_1.Bdesc, Babs.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Switch(Babs.Dp=1,'دريافتها',Babs.Dp=2,'پرداختها',Babs.DP=0,'عمومي'), Babs.GBid, Babs_1.Bdesc, Babs.Bdesc HAVING (((Babs.Dp)=" & Forms!Babsrepfrm!Frame0 & "));"
    End Select
    Me.RecordSource = S
End Sub
