Attribute VB_Name = "global_vars"
Option Compare Database
Public Pubcontrol As Control
Public myopenform As String, Lockit As Byte, Pubint As Byte
Public Mysal As Integer, Mainformint As Byte
Public tolast As Boolean, Mybalance As Double
Public pubform1 As Form
Public pubformsub As Form
Public pubcheckid As Long
Public pubvid As Long
Public sstring As String
Public pepname As String
Public pepno As String
Public myemp As Integer
Public dup As Boolean, Isfinal As Boolean
Public Searchtype As Byte
Public dupit As Boolean, Arzifarsi As Boolean
Public Filterison As Boolean
Public oldbankacc As String, Nochangedate As String
Public Spfilter As Byte, lockcontrol As Byte
Public disableit As Boolean ' 1 disable bed ,2 disablebes
Public S As String, tempmydesc As String
Public Sub docommand(DS As String)
    DoCmd.SetWarnings False
    DoCmd.RunSQL DS
    DoCmd.SetWarnings True
End Sub
Public Sub Openaccs(Myint As Byte)
    Select Case Myint
        Case 1
            Set Pubcontrol = Forms!Check_Print_single!Accno
        Case 2
            Set Pubcontrol = Forms!Check_Print_single!Tafno
        Case 3
            Set Pubcontrol = Forms!Check_Print_single!Accnotcash
        Case 4
            Set Pubcontrol = Forms!Check_Print_single!Tafnotcash
    
        Case 5
            Set Pubcontrol = Forms!Payables!Accno
        Case 6
            Set Pubcontrol = Forms!Payables!Tafno
        Case 7
            Set Pubcontrol = Forms!Payables!Accnotcash
        Case 8
            Set Pubcontrol = Forms!Payables!Tafnotcash
    
        Case 9
            Set Pubcontrol = Forms!P_Bardasht!Accno
        Case 10
            Set Pubcontrol = Forms!P_Bardasht!Tafno
    
        Case 11
            Set Pubcontrol = Forms!P_Naghd!Accno
        Case 12
            Set Pubcontrol = Forms!P_Naghd!Tafno

        Case 13
            Set Pubcontrol = Forms!D_variz!Accno
        Case 14
            Set Pubcontrol = Forms!D_variz!Tafno
    
        Case 15
            Set Pubcontrol = Forms!D_Naghd!Accno
        Case 16
            Set Pubcontrol = Forms!D_Naghd!Tafno

        Case 17
            Set Pubcontrol = Forms!D_check!Accno
        Case 18
            Set Pubcontrol = Forms!D_check!Tafno
        Case 19
            Set Pubcontrol = Forms!D_check!Accnotcash
        Case 20
            Set Pubcontrol = Forms!D_check!Tafnotcash
    
        Case 21
            Set Pubcontrol = Forms!D_Check_Kh_m!Check_s.Form!ComboC
        Case 22
            Set Pubcontrol = Forms!D_Check_Kh_m!Check_s.Form!ComboD
    
        Case 25
            Set Pubcontrol = Forms!Bankacc!ComboA
        Case 26
            Set Pubcontrol = Forms!Bankacc!ComboB
        Case 27
            Set Pubcontrol = Forms!Bankacc!ComboC
        Case 28
            Set Pubcontrol = Forms!Bankacc!ComboD
        Case 29
            Set Pubcontrol = Forms!Cashandpetty!ComboA
        Case 30
            Set Pubcontrol = Forms!Cashandpetty!ComboB
        Case 31
            Set Pubcontrol = Forms!Pordersub_vouch!ComboA
        Case 32
            Set Pubcontrol = Forms!Pordersub_vouch!ComboB
        Case 33
            Select Case Mainformint
                Case 1
                    Set Pubcontrol = Forms!P_Bardasht!Po_vouch!ComboA
                Case 2
                    Set Pubcontrol = Forms!P_Naghd!Po_vouch!ComboA
                Case 3
                    Set Pubcontrol = Forms!D_variz!Po_vouch!ComboA
                Case 4
                    Set Pubcontrol = Forms!D_Naghd!Po_vouch!ComboA
                Case 5
                    Set Pubcontrol = Forms!D_check!Po_vouch!ComboA
                Case 6
                    Set Pubcontrol = Forms!Porder!Po_vouch!ComboA
            End Select
        Case 34
            Select Case Mainformint
                Case 1
                    Set Pubcontrol = Forms!P_Bardasht!Po_vouch!ComboB
                Case 2
                    Set Pubcontrol = Forms!P_Naghd!Po_vouch!ComboB
                Case 3
                    Set Pubcontrol = Forms!D_variz!Po_vouch!ComboB
                Case 4
                    Set Pubcontrol = Forms!D_Naghd!Po_vouch!ComboB
                Case 5
                    Set Pubcontrol = Forms!D_check!Po_vouch!ComboB
                Case 6
                    Set Pubcontrol = Forms!Porder!Po_vouch!ComboB
            End Select
            
         Case 35
                    Set Pubcontrol = Forms!D_check!Person
         Case 36
                    Set Pubcontrol = Forms!D_check!Babat
                    
         Case 37
                    Set Pubcontrol = Forms!P_Naghd!ComboP
         Case 38
                    Set Pubcontrol = Forms!P_Naghd!Babat
                    
         Case 39
                    Set Pubcontrol = Forms!D_Naghd!ComboP
         Case 40
                    Set Pubcontrol = Forms!D_Naghd!Babat
                    
         Case 41
                    Set Pubcontrol = Forms!Payables!ComboP
         Case 42
                    Set Pubcontrol = Forms!Payables!Babat
                    
         Case 43
                    Set Pubcontrol = Forms!P_Bardasht!ComboP
         Case 44
                    Set Pubcontrol = Forms!P_Bardasht!Babat
         Case 45
                    Set Pubcontrol = Forms!D_variz!ComboP
         Case 46
                    Set Pubcontrol = Forms!D_variz!Babat
         Case 47
                    Set Pubcontrol = Forms!Porder!ComboP
         Case 48
                    Set Pubcontrol = Forms!Porder!Babat
        Case 101
            Set Pubcontrol = Forms!Porder!Accno
        Case 102
            Set Pubcontrol = Forms!Porder!Tafno
        Case 103
            Set Pubcontrol = Forms!Porder!Accnotcash
        Case 104
            Set Pubcontrol = Forms!Porder!Tafnotcash
                    
    End Select
      
    Pubint = Myint
    Select Case Myint
        Case 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 101, 103
            Forms!Sal!Accortaf = 1
             Fromacc = 1
        Case Else
            Forms!Sal!Accortaf = 0
             Fromacc = 0
    End Select
    Select Case Myint
        Case Is < 35
                DoCmd.OpenForm "Sm"
       Case Is > 100
                DoCmd.OpenForm "Sm"
         Case Else
                DoCmd.OpenForm "SPerson"
    End Select
End Sub
Public Function Newvoucher(HVdate As String, Itid As Byte) As Long
        Select Case Itid
            Case 11
                Myvdesc = "چكهاواسناددريافتي"
            Case 12
                Myvdesc = "چكهاي واگذارشده به حساب"
            Case 13
                Myvdesc = "ثبت وصول/برگشت چكهاي واگذارشده"
            Case 16
                Myvdesc = "واريز به حسابها"
            Case 17
                Myvdesc = "دريافتهاي نقدي"
            Case 18
                Myvdesc = "عودت چكهاي دريافتي/پرداختي"
            Case 19
                Myvdesc = "خرج چكهاي دريافتي"
            Case 21
                Myvdesc = "چكهاي صادره"
            Case 22
                Myvdesc = "ابطال چكهاي صادره"
            Case 25
                Myvdesc = "تبديل اسناد/تعهدات پرداختني به نقد"
            Case 26
                Myvdesc = "برداشت از حسابها"
            Case 27
                Myvdesc = "پرداختهاي نقدي"
        End Select
        S = "INSERT INTO Voucher ( Vgroup, Vtype, Vdate )SELECT " & Itid & ", '" & CStr(Myvdesc) & "', " & CStr(HVdate) & ";"
        docommand S
        Newvoucher = DMax("Vid", "Voucher")
End Function
Public Sub Createvoucherrows(Cors As Byte, Itid As Byte, Csid As Long, Vno As Long, acc As String, taf As String, mydesc As String, mymab As Double, Itcuram As Double, Dfld As String, Vfld As String, Itbabid As Long, tacc As String, ttaf As String, Noconsidervuser As Boolean)
    If Cors = 0 Then
        myfld = "[Checkid]"
        Myshart = "[Checkid]=" & Csid
    Else
        myfld = "[Sid]"
        Myshart = "[Sid]=" & Csid
    End If
    
    Select Case Itid
        Case 11, 12, 13, 16, 17, 18, 19
            Mybed = mymab
            Mybes = 0
            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField,Curam,Bid) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Itcuram & "," & Itbabid & " ;"
            docommand S
        Case 21, 22, 25, 26, 27
            Mybed = 0
            Mybes = mymab
    End Select
    Vrowuser = DCount(myfld, "Povouch", Myshart)
    If Vrowuser = 0 Or Noconsidervuser = True Then
        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField,Curam,Bid) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(acc) & "','" & CStr(taf) & "'," & Mybes & " , " & Mybed & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Itcuram & "," & Itbabid & " ;"
        docommand S
        If Itid = 22 Then
            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField,Curam,Bid) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Itcuram & "," & Itbabid & " ;"
            docommand S
        End If
    Else
        If Itid = 22 Then
            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField,Curam,Bid) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(acc) & "','" & CStr(taf) & "'," & Mybes & " , " & Mybed & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Itcuram & "," & Itbabid & " ;"
            'S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "';"
            docommand S
        End If
       Insfrompovouch Itid, Cors, Csid, Vno
    End If
    Select Case Itid
        Case 21, 25, 26, 27
        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, DateField, ValueField,Curam,Bid) SELECT " & Vrowno(Vno) & "," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "','" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Itcuram & "," & Itbabid & " ;"
        docommand S
    End Select
End Sub
Public Function Vrowno(Myvid As Long) As Integer
    Vrowno = DCount("Vid", "Voucher_row", "Vid=" & Myvid) + 1
End Function
Public Sub Insfrompovouch(Myitid As Byte, Cors As Byte, Csid As Long, Vno As Long)
    If Cors = 0 Then
        If Myitid <> 22 Then
            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc ) SELECT " & Vrowno(Vno) & "," & Vno & ", Povouch.Checkid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc FROM Povouch WHERE (((Povouch.Checkid)=" & Csid & "))Order by Bed Desc;"
        Else
            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc ) SELECT " & Vrowno(Vno) & "," & Vno & ", Povouch.Checkid, Povouch.accno, Povouch.tafno, Povouch.bes, Povouch.bed, Povouch.rowdesc FROM Povouch WHERE (((Povouch.Checkid)=" & Csid & "))Order by Bed Desc;"
        End If
    Else
        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc ) SELECT " & Vrowno(Vno) & "," & Vno & ", Povouch.Sid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc FROM Povouch WHERE (((Povouch.Sid)=" & Csid & "))Order by Bed Desc ;"
    End If
    docommand S
End Sub
Public Sub V_tmp(Cid As Long)
    mystr = "Select * from check where checkid=" & Cid
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
    '************8
    '**************
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
            tempmydesc = mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " از " & !Person & " بابت " & !Babat
            If DCount("Checkid", "Povouch", "Checkid=" & !Checkid) = 0 Then
                vu = False
            Else
                vu = True
                S = "INSERT INTO Voucher_row_tmp ( Checkid, accno, tafno, bed, bes, rowdesc )SELECT Povouch.Checkid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc FROM Povouch WHERE (((Povouch.Checkid)=" & !Checkid & "));"
                docommand S
            End If

           '*************
linex:
'            Select Case !Checktype
'                Case 0, 2, 3, 4, 5
                        Newvrow_tmp !Checkid, Nz(!Accnotcash, ""), Nz(!Tafnotcash, ""), !Amount, 0, tempmydesc, Nz(!Maturity, "00000000"), Nz(!Checkno, 0)
                    If vu = False Then
                        Newvrow_tmp !Checkid, Nz(!Accno, ""), Nz(!Tafno, ""), 0, !Amount, tempmydesc, Nz(!Maturity, "00000000"), Nz(!Checkno, 0)
                    End If
'                Case 1, 6  'entz
'
'                End Select

    End With
End Sub
Private Sub Newvrow_tmp(itcheckid As Long, itacc As String, ittaf As String, ItBed As Double, ItBes As Double, Itdesc As String, dfield As String, vfield As String)
    Set rst = CurrentDb.OpenRecordset("Voucher_row_tmp", dbOpenDynaset, dbSeeChanges)
    With rst
        .AddNew
            !Checkid = itcheckid
            !Accno = itacc
            !Tafno = ittaf
            !bed = ItBed
            !bes = ItBes
            !Recno = Accrowid
            !rowdesc = Mid(Itdesc, 1, 250)
            !DateField = dfield
            !ValueField = vfield
        .Update
        .Close
    End With
End Sub

Public Sub Finaldate()
    Dim cashdate As String, accdate As String
        S = "Is_Updated=" & True
        accdate = Nz(DMax("Voucher_Date", "t_Voucher", S), "0")
        cashdate = Nz(DLookup("Lastcreatev", "Globals"), Form_Sal.Combo4 & "0101")
    If accdate > cashdate Then
        Nochangedate = accdate
    Else
        Nochangedate = cashdate
    End If
End Sub
