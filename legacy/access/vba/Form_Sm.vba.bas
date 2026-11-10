Attribute VB_Name = "Form_Sm"
Attribute VB_Base = "0{9BA0C51B-2147-4932-A6EE-357E8D5C400B}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim Fldname As String, fldsort As String
Dim A As String, S As String

Private Sub Command21_Click()
    DoCmd.Close
End Sub

Private Sub Form_Open(Cancel As Integer)
    If Forms!Sal!Accortaf = 0 Then
        Me.T1 = "حسابهاي تفصيل"
    Else
        Me.T1 = "حسابهاي معين"
    End If
    Me.Text0.SetFocus
    Makesource
    If Lockit = 1 Then
        Lockit = 0
        Me.List1.Enabled = False
    End If
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

Private Sub List1_DblClick(Cancel As Integer)
If Isfinal = True Then Exit Sub
    
     Pubcontrol = Me.List1
        Select Case Pubint
            Case 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 101, 103
                If Me.List1.Column(2) = "دارد" Then
                    Select Case Pubint
                        Case 1
                          Forms!Check_Print_single!Tafno.Enabled = True
                          Set Pubcontrol = Forms!Check_Print_single!Tafno
                          Forms!Check_Print_single!Tafno.SetFocus
                         Case 3
                          Forms!Check_Print_single!Tafnotcash.Enabled = True
                          Set Pubcontrol = Forms!Check_Print_single!Tafnotcash
                          Forms!Check_Print_single!Tafnotcash.SetFocus
                         Case 5
                          Forms!Payables!Tafno.Enabled = True
                          Set Pubcontrol = Forms!Payables!Tafno
                          Forms!Payables!Tafno.SetFocus
                         Case 7
                          Forms!Payables!Tafnotcash.Enabled = True
                          Set Pubcontrol = Forms!Payables!Tafnotcash
                          Forms!Payables!Tafnotcash.SetFocus
                         Case 9
                          Forms!P_Bardasht!Tafno.Enabled = True
                          Set Pubcontrol = Forms!P_Bardasht!Tafno
                          Forms!P_Bardasht!Tafno.SetFocus
                         Case 11
                          Forms!P_Naghd!Tafno.Enabled = True
                          Set Pubcontrol = Forms!P_Naghd!Tafno
                          Forms!P_Naghd!Tafno.SetFocus
                         Case 13
                          Forms!D_variz!Tafno.Enabled = True
                          Set Pubcontrol = Forms!D_variz!Tafno
                          Forms!D_variz!Tafno.SetFocus
                         Case 15
                          Forms!D_Naghd!Tafno.Enabled = True
                          Set Pubcontrol = Forms!D_Naghd!Tafno
                          Forms!D_Naghd!Tafno.SetFocus
                         Case 17
                          Forms!D_check!Tafno.Enabled = True
                          Set Pubcontrol = Forms!D_check!Tafno
                          Forms!D_check!Tafno.SetFocus
                         Case 19
                          Forms!D_check!Tafnotcash.Enabled = True
                          Set Pubcontrol = Forms!D_check!Tafnotcash
                          Forms!D_check!Tafnotcash.SetFocus
                         Case 21
                          Forms!D_Check_Kh_m!Check_s.Form!ComboD.Enabled = True
                          Set Pubcontrol = Forms!D_Check_Kh_m!Check_s.Form!ComboD
                          Forms!D_Check_Kh_m!Check_s.Form!ComboD.SetFocus
                         Case 25
                          Forms!Bankacc!ComboB.Enabled = True
                          Set Pubcontrol = Forms!Bankacc!ComboB
                          Forms!Bankacc!ComboB.SetFocus
                         Case 27
                          Forms!Bankacc!ComboD.Enabled = True
                          Set Pubcontrol = Forms!Bankacc!ComboD
                          Forms!Bankacc!ComboD.SetFocus
                         Case 29
                          Forms!Cashandpetty!ComboB.Enabled = True
                          Set Pubcontrol = Forms!Cashandpetty!ComboB
                          Forms!Cashandpetty!ComboB.SetFocus
                         Case 31
                          Forms!Pordersub_vouch!ComboB.Enabled = True
                          Set Pubcontrol = Forms!Pordersub_vouch!ComboB
                          Forms!Pordersub_vouch!ComboB.SetFocus
                         Case 101
                          Forms!Porder!Tafno.Enabled = True
                          Set Pubcontrol = Forms!Porder!Tafno
                          Forms!Porder!Tafno.SetFocus
                         Case 103
                          Forms!Porder!Tafnotcash.Enabled = True
                          Set Pubcontrol = Forms!Porder!Tafnotcash
                          Forms!Porder!Tafnotcash.SetFocus
                        Case 33
                            Select Case Mainformint
                                Case 1
                                    Forms!P_Bardasht!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!P_Bardasht!Po_vouch!ComboB
                                    Forms!P_Bardasht!Po_vouch!ComboB.SetFocus
                                Case 2
                                    Forms!P_Naghd!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!P_Naghd!Po_vouch!ComboB
                                    Forms!P_Naghd!Po_vouch!ComboB.SetFocus
                                Case 3
                                    Forms!D_variz!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!D_variz!Po_vouch!ComboB
                                    Forms!D_variz!Po_vouch!ComboB.SetFocus
                                Case 4
                                    Forms!D_Naghd!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!D_Naghd!Po_vouch!ComboB
                                    Forms!D_Naghd!Po_vouch!ComboB.SetFocus
                                Case 5
                                    Forms!D_check!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!D_check!Po_vouch!ComboB
                                    Forms!D_check!Po_vouch!ComboB.SetFocus
                                Case 6
                                    Forms!Porder!Po_vouch!ComboB.Enabled = True
                                    Set Pubcontrol = Forms!Porder!Po_vouch!ComboB
                                    Forms!Porder!Po_vouch!ComboB.SetFocus
                            End Select
                     End Select
                Else
                    Select Case Pubint
                        Case 1
                          Forms!Check_Print_single!Tafno = Null
                          Forms!Check_Print_single!Tafno.Enabled = False
                         Case 3
                          Forms!Check_Print_single!Tafnotcash = Null
                          Forms!Check_Print_single!Tafnotcash.Enabled = False
                         Case 5
                          Forms!Payables!Tafno = Null
                          Forms!Payables!Tafno.Enabled = False
                         Case 7
                          Forms!Payables!Tafnotcash = Null
                          Forms!Payables!Tafnotcash.Enabled = False
                         Case 9
                          Forms!P_Bardasht!Tafno = Null
                          Forms!P_Bardasht!Tafno.Enabled = False
                         Case 11
                          Forms!P_Naghd!Tafno = Null
                          Forms!P_Naghd!Tafno.Enabled = False
                         Case 13
                          Forms!D_variz!Tafno = Null
                          Forms!D_variz!Tafno.Enabled = False
                         Case 15
                          Forms!D_Naghd!Tafno = Null
                          Forms!D_Naghd!Tafno.Enabled = False
                         Case 17
                          Forms!D_check!Tafno = Null
                          Forms!D_check!Tafno.Enabled = False
                         Case 19
                          Forms!D_check!Tafnotcash = Null
                          Forms!D_check!Tafnotcash.Enabled = False
                         Case 21
                          Forms!D_Check_Kh_m!Check_s.Form!ComboD = Null
                          Forms!D_Check_Kh_m!Check_s.Form!ComboD.Enabled = False
                         Case 23
                          Forms!D_Check_od_m!D_check_od!Tafno = Null
                          Forms!D_Check_od_m!D_check_od!Tafno.Enabled = False
                         Case 25
                          Forms!Bankacc!ComboB = Null
                          Forms!Bankacc!ComboB.Enabled = False
                         Case 27
                          Forms!Bankacc!ComboD = Null
                          Forms!Bankacc!ComboD.Enabled = False
                         Case 29
                          Forms!Cashandpetty!ComboB = Null
                          Forms!Cashandpetty!ComboB.Enabled = False
                         Case 31
                          Forms!Pordersub_vouch!ComboB = Null
                          Forms!Pordersub_vouch!ComboB.Enabled = False
                         Case 101
                          Forms!Porder!Tafno = Null
                          Forms!Porder!Tafno.Enabled = False
                         Case 103
                          Forms!Porder!Tafnotcash = Null
                          Forms!Porder!Tafnotcash.Enabled = False
                          
                         Case 33
                            Select Case Mainformint
                                Case 1
                                    Forms!P_Bardasht!Po_vouch!ComboB = Null
                                    Forms!P_Bardasht!Po_vouch!ComboB.Enabled = False
                                Case 2
                                    Forms!P_Naghd!Po_vouch!ComboB = Null
                                    Forms!P_Naghd!Po_vouch!ComboB.Enabled = False
                                Case 3
                                    Forms!D_variz!Po_vouch!ComboB = Null
                                    Forms!D_variz!Po_vouch!ComboB.Enabled = False
                                Case 4
                                    Forms!D_Naghd!Po_vouch!ComboB = Null
                                    Forms!D_Naghd!Po_vouch!ComboB.Enabled = False
                                Case 5
                                    Forms!D_check!Po_vouch!ComboB = Null
                                    Forms!D_check!Po_vouch!ComboB.Enabled = False
                                Case 6
                                    Forms!Porder!Po_vouch!ComboB = Null
                                    Forms!Porder!Po_vouch!ComboB.Enabled = False
                            End Select
                     End Select
                End If
            Case Else
        End Select
        DoCmd.Close acForm, "Sm"
End Sub

Private Sub Text0_Change()
    Makesource
End Sub

Private Sub Makesource()
    Select Case Forms!Sal!Accortaf
        Case 0
            S = "SELECT Taf.Tafno, Taf.Tafname,'ندارد' FROM Taf Where "
            Select Case Me.Frame1
                Case 1
                    Fldname = "Tafname"
                Case 2
                    Fldname = "Tafno"
            End Select
        Case 1
            S = "SELECT Acc.Accno as Tafno, Acc.Accname as Tafname, IIf(Acc.Has_Unique_Account=true,'دارد','ندارد') FROM Acc WHERE Len(Nz(Trim([Accno0]))) >= [Forms]![Sal]![Lmoeen] And Acc.Has_Sub_Subsidery = False and "
            'S = "SELECT Acc.Accno as Tafno, Acc.Accname as Tafname FROM Acc WHERE "
            Select Case Me.Frame1
                Case 1
                    Fldname = "Accname"
                Case 2
                    Fldname = "Accno"
            End Select
    End Select

    Select Case Me.Frame2
        Case 1
            fldsort = "Tafname"
        Case 2
            fldsort = "Tafno"
    End Select
    Me.Text0.SetFocus
    If IsNull(Me.Text0.Text) Or Me.Text0.Text = "" Then
        S = S & "1=1 " & "ORDER BY " & Fldname
    Else
        A = BSPACE([Text0].Text)
        S = S & "(((BSPACE(" & CStr(Fldname) & ")) Like '*" & Trim(A) & "*'))" & "ORDER BY " & Fldname
    End If
    
    Select Case Me.Frame2
        Case 1
            S = S & " ;"
        Case 2
            S = S & " Desc ;"
    End Select
    Me.List1.RowSource = S
End Sub
