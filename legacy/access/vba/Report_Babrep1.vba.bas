Attribute VB_Name = "Report_Babrep1"
Attribute VB_Base = "0{6E9EE0E4-BC96-435E-9852-3B4F2F02FFB8}"
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
            S = "SELECT Babs.Dp, Babs.GBid, Babs_1.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي') AS Dpname FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Babs.GBid, Babs_1.Bdesc, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي');"
        Case 1, 2
            S = "SELECT Babs.Dp, Babs.GBid, Babs_1.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي') AS Dpname FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Babs.GBid, Babs_1.Bdesc, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي')HAVING (((Babs.Dp)=" & Forms!Babsrepfrm!Frame0 & "));"
    End Select
    Me.RecordSource = S
    
'    Select Case Forms!babsrepfrm!Frame0
'        Case 1
'
'    End Select

End Sub
