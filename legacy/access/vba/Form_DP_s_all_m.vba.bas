Attribute VB_Name = "Form_DP_s_all_m"
Attribute VB_Base = "0{B39D8EDE-50DD-4C2D-91A5-EC006EBC89D6}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim Fldname As String, fldsort As String
Dim A As String, S As String, S1 As String, myd1 As String, myd2 As String

Private Sub Command21_Click()
    DoCmd.Close
End Sub

Private Sub Form_Open(Cancel As Integer)
    S = "Delete * from Balance"
    docommand S
    
    myd1 = Mid(CurrentDateFarsi(Date), 1, 4) & "0101"
    myd2 = CurrentDateFarsi(Date)
    S = "INSERT INTO Balance ( Scode, Sid, Sdate, Transtype, Basetype, Baccno, Bed, Bes, Iscur, Currate, Sal, Babat, Checkno,Person) SELECT Subs.Scode, Subs.Checkid, Subs.Sdate, Subs.Transtype, Subs.Basetype, Subs.Baccno, Subs.Bed, Subs.Bes, Subs.Iscur, Subs.Currate, Subs.Sal, Subs.Babat, Subs.Checkno,Subs.Person FROM Subs WHERE (((Subs.Scode)<>10) AND ((Subs.Sdate) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Subs.Card)=False));"
    docommand S
    S = "INSERT INTO Balance ( Checkid, Transtype, Checkno, Maturity, Bed, Bes, Status, Scode, Sdate, Baccno, Iscur, Babat, Invno, Contract, Sal, Curid,Person ) SELECT Check.Checkid, Check.Checktypen, Check.Checkno, Check.Maturity, IIf([dp]=1,[amount],0) AS 1, IIf([dp]=2,[amount],0) AS 2, Check.Status, Check.Dp, Check.D1, Check.Baccno, Check.Iscur, Check.Babat, Check.Invno, Check.Contract, Check.Sal, Check.Curid ,Check.Person FROM [Check] WHERE (((Check.D1) Between '" & CStr(myd1) & "' And '" & CStr(myd2) & "') AND ((Check.Checktype)<>9));"
    docommand S
    Me.DP_s_all.Form.Requery
    Me.Text0.SetFocus
End Sub
Private Sub Frame0_AfterUpdate()
     Makesource
End Sub

Private Sub Frame1_AfterUpdate()
     Makesource
End Sub
Private Sub Frame2_AfterUpdate()
     Makesource
End Sub

Private Sub Text0_Change()
    Makesource
End Sub

Private Sub Makesource()
'    Select Case Me.Frame1
'        Case 1
'        S = ""
'    End Select
'
'    S = "SELECT invs.invid, Nz([Invno],'و.ن') AS Ino, Nz([Invdate],'و.ن') AS Ind, Nz([refno],'و.ن') AS ref, Nz([Sdesc],'و.ن') AS Sd, Nz(invs.Cusname,'و.ن') AS Cname, Sum(IIf([Iscur]=False,1,[Currate])*[quant]*[sprice]) AS p, Nz([Cusno],'و.ن') AS Cno, Nz([tafno],'و.ن') AS taf FROM (invs LEFT JOIN customer ON invs.cusid = customer.cusid) LEFT JOIN invs_row ON invs.invid = invs_row.invid WHERE invs.maintype=[Forms]![Ssales_m]![frame0] " '
'    S1 = "GROUP BY invs.invid, Nz([Invno],'و.ن'), Nz([Invdate],'و.ن'), Nz([refno],'و.ن'), Nz([Sdesc],'و.ن'), Nz(invs.Cusname,'و.ن'), Nz([Cusno],'و.ن'), Nz([tafno],'و.ن') HAVING (((Nz([Invdate],'و.ن')) Between [Forms]![Ssales_m]![DD1] And [Forms]![Ssales_m]![DD2]))"
'    Select Case Me.Frame1
'        Case 1
'            Fldname = "Invdate"
'        Case 2
'            Fldname = "Invno"
'        Case 3
'            Fldname = "Invs.Cusname"
'        Case 4
'            Fldname = "Cusno"
'        Case 5
'            Fldname = "Tafno"
'        Case 6
'            Fldname = "Sdesc"
'        Case 7
'            Fldname = "Refno"
'        Case 8
'            Fldname = "P"
'    End Select
'
'    Me.Text0.SetFocus
'    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
'        'S = S & S1 & "ORDER BY " & Fldname
'         S = S & S1 & "ORDER BY Nz(" & CStr(Fldname) & " ,'و.ن')"
'    Else
'        a = BSPACE([Text0].Text)
'        S = S & "And iif(isnull(" & CStr(Fldname) & "),'',BSPACE(" & CStr(Fldname) & ") Like '*" & Trim(a) & "*')" & S1 & " ORDER BY Nz(" & CStr(Fldname) & " ,'و.ن')"
'
'    End If
'
'    Select Case Me.Frame2
'        Case 1
'            S = S & " ;"
'        Case 2
'            S = S & " Desc ;"
'    End Select
'
'    Me.SACC_SUB.Form.RecordSource = S
'    Me.SACC_SUB.Form.Requery

End Sub
