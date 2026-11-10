Attribute VB_Name = "Onebyonevoucher"
Option Compare Database
Dim Newvno As Long, Itdesc As String, ItMab As Double, Itcuram As Double, ItPraccno As String, ItPrtafno As String, Itvfld As String
Dim Itjaryanvosool As String, Itbacked As String, S10 As String, rst10 As Recordset, Dowhat As Byte
Dim A As String, Tarafaccnew As String, Taraftafnew As String, Praccnonew As String, Prtafnonew As String
Public Sub CreatevoucherNg(Mydate As String)
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
        MyChecknazdsandogh = Nz(!Checknazdsandogh)
        MyAsnadnazdsandogh = Nz(!Asnadnazdsandogh)
        Itjaryanvosool = Nz(!Jaryanvosool)
        Itbacked = Nz(!Backedchecks)
        Myasnadpardakhtani = Nz(!asnadpardakhtani)
        Myaccentezami = Nz(!Accentezami)
        Myacctentezami = Nz(!Acctentezami)
    .Close
    End With
    
    Forms!Voucher_create.TD.Visible = True
    Forms!Voucher_create.LD.Visible = True
    Forms!Voucher_create.T31.Visible = True
    Forms!Voucher_create.L31.Visible = True
    Forms!Voucher_create.T32.Visible = True
    Forms!Voucher_create.L32.Visible = True
    Forms!Voucher_create.T33.Visible = True
    Forms!Voucher_create.L33.Visible = True
    Forms!Voucher_create.T34.Visible = True
    Forms!Voucher_create.L34.Visible = True
    Forms!Voucher_create.T35.Visible = True
    Forms!Voucher_create.L35.Visible = True
    Forms!Voucher_create.Label10.Visible = True
    Forms!Voucher_create.Label10.Caption = "درحال ايجاد اسناد حسابداري واريز/برداشت ودريافت/پرداخت نقدي"
    DoEvents
    S = "UPDATE [Check] SET [Check].EbtorKharj = False WHERE (((Check.Dp)=2) AND ((Check.EbtorKharj) Is Null));"
    docommand S
    
    S = "Select * from Subs where Scode In(16,17,26,27) And Card=0 and Sdate>='" & CStr(Mydate) & "' Order by Scode,Sdate"
    Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then GoTo Line10
        .MoveLast
        .MoveFirst
        While Not .EOF
        Forms!Voucher_create.TD = !Sdate
        Forms!Voucher_create.T31 = !Checkno
        Forms!Voucher_create.T32 = !Person
        Forms!Voucher_create.T33 = !bed
        'Forms!Voucher_create.T34 = !Maturity
        Forms!Voucher_create.T35 = !Babat
        DoEvents
         A = "Vid=" & Nz(!Vno)
         If !Vno = 0 Or IsNull(!Vno) Or DCount("Vid", "Voucher", A) = 0 Then
            .Edit
                 Newvno = Newvoucher(Nz(!Sdate), Nz(!Scode))
                !Vno = Newvno
            .Update
          Else
            Newvno = !Vno
            Editvdate !Vno, Nz(!Sdate)
        End If
        S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Sid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
        docommand S
        Modifyvrows Newvno, !Sid, 1, 0
       .MoveNext
        Wend
    End With
    Forms!Voucher_create.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسناددريافتي"
    DoEvents
Line10:
    S10 = "Select * from Check where Dp=1 and D1>='" & CStr(Mydate) & "'Order by D1,Checkid"
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then GoTo Line101
        .MoveLast
        .MoveFirst
        Dim Mycheckid As Long
        While Not .EOF
                Forms!Voucher_create.TD = !D1
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents

                A = "Vid=" & !V1
                If !V1 = 0 Or IsNull(!V1) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D1), 11)
                 Else
                   Newvno = !V1
                   Editvdate !V1, Nz(!D1)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 2, 1
       .MoveNext
        Wend
    End With

Line101:
    S10 = "Select * from Check where Dp=1 and D3>='" & CStr(Mydate) & "'Order by D3,Checkid"
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then GoTo Line102
        .MoveLast
        .MoveFirst
        While Not .EOF
                Forms!Voucher_create.TD = !D3
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents

                A = "Vid=" & !V3
                If !V3 = 0 Or IsNull(!V3) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D3), 12)
                 Else
                   Newvno = !V3
                   Editvdate !V3, Nz(!D3)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 2, 3
       .MoveNext
        Wend
    End With

Line102:
    S10 = "Select * from Check where Dp=1 and D4>='" & CStr(Mydate) & "' Order by D4,Checkid"
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then GoTo Line20
        .MoveLast
        .MoveFirst
        While Not .EOF
                Forms!Voucher_create.TD = !D4
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents
                A = "Vid=" & !V4
                If !V4 = 0 Or IsNull(!V4) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D4), 13)
                 Else
                   Newvno = !V4
                   Editvdate !V4, Nz(!D4)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 2, 4
       .MoveNext
        Wend
    End With

'Checks Issued By us
Line20:
    Forms!Voucher_create.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسنادپرداختني صادره"
    DoEvents
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        S10 = "Select * from Check where Dp=2 and D1>='" & CStr(Mydate) & "'Order by D1,Checkid"
    Else
        S10 = "Select * from Check where Dp=2 and D1>='" & CStr(Mydate) & "'And Ebtorkharj=false Order by D1,Checkid"
    End If
    
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then GoTo Line201
        .MoveLast
        .MoveFirst
        While Not .EOF
                Forms!Voucher_create.TD = !D1
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents
                A = "Vid=" & !V1
                If !V1 = 0 Or IsNull(!V1) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D1), 21)
                 Else
                   Newvno = !V1
                   Editvdate !V1, Nz(!D1)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 3, 1
       .MoveNext
        Wend
    End With

Line201:
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        'S10 = "Select * from Check where Dp=2 and D3>='" & CStr(Mydate) & "' And Checkno='761483'Order by D3,Checkid"
        S10 = "Select * from Check where Dp=2 and D3>='" & CStr(Mydate) & "'Order by D3,Checkid"
    Else
        S10 = "Select * from Check where Dp=2 and D3>='" & CStr(Mydate) & "'And Ebtorkharj=false Order by D3,Checkid"
    End If
    
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then GoTo Line202
        .MoveLast
        .MoveFirst
        While Not .EOF
                Forms!Voucher_create.TD = !D3
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents
                A = "Vid=" & !V3
                If !V3 = 0 Or IsNull(!V3) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D3), 25)
                 Else
                   Newvno = !V3
                   Editvdate !V3, Nz(!D3)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 3, 3
       .MoveNext
        Wend
    End With
    
Line202:
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        S10 = "Select * from Check where Dp=2 and D2>='" & CStr(Mydate) & "' Order by D2,Checkid"
    Else
        S10 = "Select * from Check where Dp=2 and D2>='" & CStr(Mydate) & "' And Ebtorkharj=false Order by D2,Checkid"
    End If
    Set rst10 = CurrentDb.OpenRecordset(S10, dbOpenDynaset, dbSeeChanges)
    With rst10
        If .RecordCount = 0 Then Exit Sub
        .MoveLast
        .MoveFirst
        While Not .EOF
                Forms!Voucher_create.TD = !D2
                Forms!Voucher_create.T31 = !Checkno
                Forms!Voucher_create.T32 = !Person
                Forms!Voucher_create.T33 = !Amount
                Forms!Voucher_create.T34 = !Maturity
                Forms!Voucher_create.T35 = !Babat
                DoEvents
                A = "Vid=" & !V2
                If !V2 = 0 Or IsNull(!V2) Or DCount("Vid", "Voucher", A) = 0 Then
                   Newvno = Newvoucher(Nz(!D2), 22)
                 Else
                   Newvno = !V2
                   Editvdate !V2, Nz(!D2)
                End If
            S = "UPDATE Voucher SET Voucher.Checkid =" & Nz(!Checkid) & " WHERE (((Voucher.Vid)=" & Newvno & "));"
            docommand S
            Mycheckid = !Checkid
            Modifyvrows Newvno, !Checkid, 3, 2
       .MoveNext
        Wend
    End With
End Sub

Private Sub Modifyvrows(Vno As Long, Itid As Long, Itcode As Byte, Dowhat As Byte)
    
Select Case Itcode
    Case 1
        Delrows Vno
        S = "Select * from Subs Where Sid=" & Itid
        Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
        With rst
        Mycode = !Scode
                mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                Set rst1 = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                With rst1
                    If .RecordCount = 0 Then
                        ItPraccno = 0
                        ItPrtafno = 0
                    Else
                        ItPraccno = Nz(!Accno)
                        ItPrtafno = Nz(!Tafno)
                    End If
                .Close
                End With
                
        Select Case Mycode
            Case 16
                Itdesc = "واريز وجه توسط  " & !Person & " بابت " & !Babat
            Case 17
                Itdesc = "دريافت نقدي از " & !Person & " بابت " & !Babat
            Case 26
                Itdesc = " برداشت ازحساب توسط  " & !Person & " بابت " & !Babat
            Case 27
                Itdesc = " پرداخت نقدي به " & !Person & " بابت " & !Babat
        End Select
            
        If !Iscur = True Then
            If Mycode = 16 Or Mycode = 17 Then
                ItMab = Round(Nz(!bed) * Nz(!Currate))
                Itvfld = Nz(!Checkno)
                Itcuram = Nz(!bed)
            Else
                ItMab = Round(Nz(!bes) * Nz(!Currate))
                Itvfld = Nz(!Checkno)
                Itcuram = Nz(!bes)
            End If
        Else
            If Mycode = 16 Or Mycode = 17 Then
                ItMab = Nz(!bed)
            Else
                ItMab = Nz(!bes)
            End If
        Itcuram = 0
        End If
         Createvoucherrows 1, !Scode, !Sid, Vno, Nz(!Accno), Nz(!Tafno), Itdesc, ItMab, Itcuram, Nz(!Sdate), Nz(!Checkno), Nz(!Babatid), ItPraccno, ItPrtafno, False
        .Close
        End With
        
 '******
 Case 2
    S = "Select * from Check where Checkid=" & Itid
    Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
    With rst
            If !Iscur = True Then
                ItMab = Round(Nz(!Amount) * Nz(!Currate))
                Itvfld = Nz(!Checkno)
                Itcuram = Nz(!Amount)
            Else
                ItMab = Nz(!Amount)
                Itvfld = Nz(!Checkno)
                Itcuram = 0
            End If
            '***************8
            Mycheckid = !Checkid
            Select Case !Checktype
                Case 0
                    mychecktype = "چك عادي "
                Case 1
                    mychecktype = " چك انتظامي"
                Case 2
                    mychecktype = "(چك رمزدار(بانكي"
                Case 3
                    mychecktype = " اقساط دريافتني"
                Case 4
                    mychecktype = " سفته دريافتني"
                Case 5
                    mychecktype = " تعهدات دريافتني"
                Case 6
                    mychecktype = " سفته انتظامي"
            End Select
            mys2 = !Checkno & IIf(IsNull(!PbyCheckno), "", " چ.ب.ش " & !PbyCheckno) & "دريافتي از " & Nz(!Person)
            
        Select Case !Stcode
                Case 12, 13, 14
                mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Vagno)) & "'"
                Set rst2 = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                With rst2
                    If .RecordCount = 0 Then
                        ItPraccno = 0
                        ItPrtafno = 0
                    Else
                        ItPraccno = Nz(!Accno)
                        ItPrtafno = Nz(!Tafno)
                        Itjaryanvosool = Nz(!Vagno, Itjaryanvosool)
                    End If
                .Close
                End With
            End Select
            Delrows Vno
            Select Case Dowhat
                Case 1
                    GoTo Lineyaaft
                Case 3
                    GoTo Linevag
                Case 4
                    GoTo LineVos
            End Select
Lineyaaft:
            Itdesc = mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " از " & Nz(!Person) & " بابت " & Nz(!Babat)
            Createvoucherrows 0, 11, !Checkid, Vno, Nz(!Accno), Nz(!Tafno), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Nz(!Accnotcash), Nz(!Tafnotcash), False
            .Edit
                !V1 = Vno
            .Update
            Exit Sub

Linevag:
            Select Case !Stcode
                Case 12, 13, 14
                    Itdesc = "واگذاري چك " & !Checkno & IIf(IsNull(!PbyCheckno), "", " چ.ب.ش " & !PbyCheckno) & " به حساب " & !Vagno
                    Createvoucherrows 0, 12, !Checkid, Vno, Nz(!Accnotcash), Nz(!Tafnotcash), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Itjaryanvosool, ItPrtafno, True
                Case 15
                    Itdesc = "عودت " & mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " به " & Nz(!Person)
                    Createvoucherrows 0, 18, !Checkid, Vno, Nz(!Accnotcash), Nz(!Tafnotcash), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Nz(!Accno), Nz(!Tafno), True
                Case 16
                    mys = "Select * from t_unique_accounts where Account_Code='" & CStr(Nz(!TafKharj)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            ItPrtafno = 0
                        Else
                            ItPrtafno = Nz(!Account_Name)
                        End If
                    .Close
                    End With
                    Itdesc = "واگذاري(خرج) " & mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " به " & ItPrtafno
                    Createvoucherrows 0, 19, !Checkid, Vno, Nz(!Accnotcash), Nz(!Tafnotcash), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Nz(!Acckharj), Nz(!TafKharj), False
            End Select
                .Edit
                !V3 = Vno
                .Update
            Exit Sub
            
LineVos:
                Select Case !Stcode
                    Case 13
                        Itdesc = "وصول چك " & mys2
                        Createvoucherrows 0, 13, !Checkid, Vno, Itjaryanvosool, ItPrtafno, Itdesc, ItMab, Itcuram, Nz(!Maturity, "00000000"), Itvfld, Nz(!Babatid), ItPraccno, ItPrtafno, True
                    Case 14
                        Itdesc = "برگشت چك " & mys2
                        Createvoucherrows 0, 13, !Checkid, Vno, Itjaryanvosool, ItPrtafno, Itdesc, ItMab, Itcuram, Nz(!Maturity, "00000000"), Itvfld, Nz(!Babatid), Itbacked, Nz(!Tafnotcash), False
                End Select
                
                .Edit
                !V4 = Vno
                .Update
    .Close
    End With
    
    '******************8
    Case 3
         S = "Select * from Check where Checkid=" & Itid
         Set rst1 = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
         With rst1
                Mybabid = Nz(!Babatid)
                DoEvents
                    mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            Praccnonew = 0
                            Prtafnonew = 0
                        Else
                            Praccnonew = Nz(!Accno)
                            Prtafnonew = Nz(!Tafno)
                        End If
                    .Close
                    End With
                    
                    If !Checktype = 0 Then
                        Tarafaccnew = Praccnonew
                        Taraftafnew = Prtafnonew
                    Else
                        Tarafaccnew = Nz(!Accnotcash)
                        Taraftafnew = Nz(!Tafnotcash)
                    End If
                    
                    If !Iscur = True Then
                        ItMab = Round(Nz(!Amount) * Nz(!Currate))
                        Itvfld = Nz(!Checkno)
                        Itcuram = Nz(!Amount)
                    Else
                        ItMab = Nz(!Amount)
                        Itvfld = Nz(!Checkno, 0)
                        Itcuram = 0
                    End If
                    '***************8
                    Mycheckid = !Checkid
                    Select Case !Checktype
                        Case 0
                            mychecktype = " چك"
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = " چك مدت دار"
                        Case 3
                            mychecktype = " اقساط پرداختني"
                        Case 4
                            mychecktype = " سفته پرداختني"
                        Case 5
                            mychecktype = " تعهدات پرداختني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                    End Select
                    
                    Delrows Vno
                    Select Case Dowhat
                        Case 1
                            GoTo LineIssue
                        Case 3
                            GoTo LineChangetocash
                        Case 2
                            GoTo LineVoid
                    End Select

LineIssue:
                    If !Checktype <= 2 Then
                        Itdesc = mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " بابت " & Nz(!Babat, "")
                        Createvoucherrows 0, 21, !Checkid, Vno, Nz(!Accno), Nz(!Tafno), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Tarafaccnew, Taraftafnew, False
                    Else
                        If Nz(!GoInlaw, False) = False Then
                            Itdesc = mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " بابت " & Nz(!Babat, "")
                            Createvoucherrows 0, 21, !Checkid, Vno, Nz(!Accno), Nz(!Tafno), Itdesc, ItMab, Itcuram, Nz(!Maturity, "0000/00/00"), Itvfld, Nz(!Babatid), Tarafaccnew, Taraftafnew, False
                        End If
                    End If
                    .Edit
                        !V1 = Vno
                    .Update

                    Exit Sub
                    
LineChangetocash:
                    If !Ctocash = True Then
                        Itdesc = "تبديل " & mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " به چك نقدي "
                        Createvoucherrows 0, 25, !Checkid, Vno, Tarafaccnew, Taraftafnew, Itdesc, ItMab, Itcuram, Nz(!Maturity, "00000000"), Itvfld, Nz(!Babatid), Praccnonew, Prtafnonew, True
                        .Edit
                            !V3 = Vno
                        .Update
                    End If
                    Exit Sub

LineVoid:
                    If !EbtorKharj = True Then
                        Itdesc = "ابطال " & mychecktype & Nz(!Checkno) & " صادره دروجه " & Nz(!Person)
                        Createvoucherrows 0, 22, !Checkid, Vno, Tarafaccnew, Taraftafnew, Itdesc, ItMab, Itcuram, Nz(!Maturity, "00000000"), Itvfld, Nz(!Babatid), Nz(!Accno), Nz(!Tafno), False
                        .Edit
                            !V2 = Vno
                        .Update
                    End If
                    
                 End With
    '*****************8
    End Select
End Sub
Private Sub Delrows(Myvid As Long)
    S = "Delete * from Voucher_row Where Vid=" & Myvid
    docommand S
End Sub
Private Sub Editvdate(Myvid As Long, Mydate As String)
    S = "UPDATE Voucher SET Voucher.Vdate = " & CStr(Mydate) & " WHERE Voucher.Vid=" & Myvid & ";"
    docommand S
End Sub
Public Function DelonebyoneCancel(Itid As Long) As Boolean
    S = "Select * from Voucher where Checkid=" & Itid
    Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then Exit Function
        .MoveLast
        .MoveFirst
        While Not .EOF
             A = "Is_Updated=True and Voucher_No=" & Nz(!Accvno)
             If DCount("Voucher_No", "t_Voucher", A) <> 0 Then
                MsgBox "سند در دفتر داري به روزرساني شده وامكان حذف وجود ندارد"
                DelonebyoneCancel = True
                Exit Function
             Else
                S10 = "DELETE t_voucher.*,t_voucher.Voucher_No FROM t_voucher WHERE t_voucher.Voucher_No=" & Nz(!Accvno) & " ;"
                docommand S10
                S10 = "DELETE t_voucher_row.*,t_voucher_row.Voucher_No FROM t_voucher_row WHERE t_voucher_row.Voucher_No=" & Nz(!Accvno) & " ;"
                docommand S10
             .Delete
              DelonebyoneCancel = False
             End If
        .MoveNext
        Wend
    End With
End Function
Public Sub Isbankicheck(Mycid As Long, Myvosdate As String)
    A = "Checkid=" & Mycid
    If Nz(DLookup("Checktype", "Check", A) = 2) And Nz(DLookup("Vosbankichecks", "Globals") = True, False) Then
        S = "UPDATE [Check] SET [Check].Stcode = 13, [Check].Status = 'وصول شده', [Check].D4 = '" & CStr(Myvosdate) & " ' WHERE (((Check.Checkid)=" & Mycid & "));"
        docommand S
    End If
End Sub
