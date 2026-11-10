Attribute VB_Name = "Report_Babrepnew2"
Attribute VB_Base = "0{1295AD87-F413-46DB-9596-EB690682397E}"
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
S = "SELECT Val(Mid([Scode],1,1)) AS Dpid, Subs.Curid, Subs.Babatid, Babs_1.Bdesc, Babs.Bdesc, Subs.Bed, Subs.Bes, [Currate]*[Bed] AS Rbed, [Currate]*[Bes] AS Rbes, IIf(Val(Mid([Scode],1,1))=1,'دريافتها','پرداختها') AS Dpname, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Currate, Subs.Babat, Subs.Person, Subs.Scode FROM (Subs LEFT JOIN Babs ON Subs.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid WHERE (Subs.Sdate Between [Forms]![Babsrepfrm]![DD1] And [Forms]![Babsrepfrm]![DD2]) "

Select Case Forms!Babsrepfrm.T0
    Case 0
        S = S
    Case 1
        S = S & " And (Subs.Babatid=0 Or Subs.Babatid Is Null)"
    Case 2
        'S = S & " And (Subs.Babatid=[Forms]![Babsrepfrm]![Babs]) And Subs.Curid=Nz([Forms]![Babsrepfrm]![Curid])"
        S = S & " And (Subs.Babatid=[Forms]![Babsrepfrm]![Babs]) " 'And Subs.Curid=Nz([Forms]![Babsrepfrm]![Curid])"
End Select
    S = S & " ORDER BY Subs.Curid;"
    
 Me.RecordSource = S
'S = "SELECT Val(Mid([Scode],1,1)) AS Dpid, Subs.Curid, Subs.Babatid, Babs_1.Bdesc, Babs.Bdesc, Subs.Bed, Subs.Bes, [Currate]*[Bed] AS Rbed, [Currate]*[Bes] AS Rbes, IIf(Val(Mid([Scode],1,1))=1,'دريافتها','پرداختها') AS Dpname, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Currate, Subs.Babat, Subs.Person, Subs.Scode FROM (Subs LEFT JOIN Babs ON Subs.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid WHERE (((Subs.Babatid)=0 Or (Subs.Babatid) Is Null) AND ((Subs.Sdate) Between [Forms]![Babsrepfrm]![DD1] And [Forms]![Babsrepfrm]![DD2])) ORDER BY Subs.Curid;"
'S = "SELECT Val(Mid([Scode],1,1)) AS Dpid, Subs.Curid, Subs.Babatid, Babs_1.Bdesc, Babs.Bdesc, Subs.Bed, Subs.Bes, [Currate]*[Bed] AS Rbed, [Currate]*[Bes] AS Rbes, IIf(Val(Mid([Scode],1,1))=1,'دريافتها','پرداختها') AS Dpname, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Currate, Subs.Babat, Subs.Person, Subs.Scode FROM (Subs LEFT JOIN Babs ON Subs.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid WHERE (((Subs.Babatid)=[Forms]![Babsrepfrm]![Babs]) AND ((Subs.Sdate) Between [Forms]![Babsrepfrm]![DD1] And [Forms]![Babsrepfrm]![DD2])) ORDER BY Subs.Curid;"

'S="SELECT Val(Mid([Scode],1,1)) AS Dpid, Subs.Curid, Subs.Babatid, Babs_1.Bdesc, Babs.Bdesc, Subs.Bed, Subs.Bes, [Currate]*[Bed] AS Rbed, [Currate]*[Bes] AS Rbes, IIf(Val(Mid([Scode],1,1))=1,"دريافتها","پرداختها") AS Dpname, Subs.Sdate, Subs.Basetype, Subs.Transtype, Subs.Baccno, Subs.Currate, Subs.Babat, Subs.Person, Subs.Scode
'FROM (Subs LEFT JOIN Babs ON Subs.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid
'WHERE (((Subs.Babatid)=0 Or (Subs.Babatid) Is Null) AND ((Subs.Sdate) Between [Forms]![Babsrepfrm]![DD1] And [Forms]![Babsrepfrm]![DD2]))
'ORDER BY Subs.Curid;"


'    Forms!babsrepfrm.Visible = False
'    Select Case Forms!babsrepfrm!Frame0
'        Case 0
'            S = "SELECT Babs.Dp, Babs.GBid, Babs_1.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي') AS Dpname FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Babs.GBid, Babs_1.Bdesc, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي');"
'        Case 1, 2
'            S = "SELECT Babs.Dp, Babs.GBid, Babs_1.Bdesc, Sum(Mainrep.Bed) AS SumOfBed, Sum(Mainrep.Bes) AS SumOfBes, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي') AS Dpname FROM (Mainrep LEFT JOIN Babs ON Mainrep.Babatid = Babs.Bid) LEFT JOIN Babs AS Babs_1 ON Babs.GBid = Babs_1.Bid GROUP BY Babs.Dp, Babs.GBid, Babs_1.Bdesc, Switch([Babs].[Dp]=1,'دريافتها',[Babs].[Dp]=2,'پرداختها',[Babs].[DP]=0,'عمومي')HAVING (((Babs.Dp)=" & Forms!babsrepfrm!Frame0 & "));"
'    End Select
'    Me.RecordSource = S
    
'    Select Case Forms!babsrepfrm!Frame0
'        Case 1
'
'    End Select

End Sub
