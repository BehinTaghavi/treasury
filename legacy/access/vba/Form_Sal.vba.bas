Attribute VB_Name = "Form_Sal"
Attribute VB_Base = "0{17C4D39A-F041-4CC4-8EBA-EC965E78F1A2}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
    Dim rst As Recordset, rst1 As Recordset
    Dim mystr As String, mystr1 As String, mypass As String, mymenu As String
    Dim mypath As String, myemp As Long
    Dim menuname As String, tofcontrols As Byte
    Dim intI As Integer
    Dim Endit As Byte

Private Sub Combo4_AfterUpdate()
    Mysal = Me.Combo4
    Me.Tsal = Mysal
End Sub


Private Sub Command15_Click()
    If IsNull(Me.Combo4) Then
        MsgBox "سال عمليات نامشخص است"
        Exit Sub
    End If
    
    Finddatabase
    mypass = IIf(IsNull(Me.Text0), "", Me.Text0)
    mystr = "select * from Emps where Pass=" & "'" & CStr(mypass) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            MsgBox "رمزعبورمعتبر نيست"
            Exit Sub
        Else
        myemp = !Empid
        Me.Tempid = Nz(!EMPNAME, "نامشخص")
        Me.Isadd = Nz(!isadmin, 0)
        Me.Canvp = Nz(!Canverifypay, 0)
        menucontrol
        If Endit = 1 Then Exit Sub
        
        Me.L0.Visible = True
        DoEvents
        
        
        Nochangedate = Nz(DLookup("Lastcreatev", "Globals"), Me.Combo4 & "0101")
        Me.L0.Visible = False
        DoEvents
        
        Me.Visible = False
            If Nz(DLookup("befpaycome", "Globals"), False) = True Then
                DoCmd.OpenForm "Check_p_longsalarm"
            ElseIf Nz(DLookup("befreccome", "Globals"), False) = True Then
                DoCmd.OpenForm "Check_d_longsalarm"
            Else
                DoCmd.OpenForm "Main"
            End If
        End If
        .Close
    End With
    
End Sub
Private Sub Command6_Click()
    If IsNull(Me.Combo4) Then
        MsgBox "سال عمليات نامشخص است"
        DoCmd.Close
    Else
        Me.Visible = False
    End If
    DoCmd.Quit
End Sub

Private Sub Coyear_AfterUpdate()
    CoyearRoutin
End Sub

Private Sub Coyear_GotFocus()
    If IsNull(Me.Text0) Or Me.Text0 = "" Then
        MsgBox "كلمه عبورنامشخص است"
        Me.Text0.SetFocus
        Exit Sub
    End If
    If Me.Command15.Enabled = True Then Me.Command15.Enabled = False
End Sub
Private Sub Form_Open(Cancel As Integer)
    Me.Tsal = Mysal
    Me.Text0.SetFocus
End Sub
Public Sub menucontrol()
    Dim menuname As String, tofcontrols As Byte
    Dim intI As Integer
 
    mystr = "select * from rights where App=0 and empid=" & (myemp) & " Order by Ord;"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            MsgBox "سطوح دسترسي براي كاربر مشخص نشده است"
            Endit = 1
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
        End If
        
        Dim g As Integer
            While Not .EOF
                menuname = !Mname
                Set newmenu = CommandBars("Cash").Controls(menuname)
                tofcontrols = newmenu.commandbar.Controls.Count
                
                For intI = 1 To tofcontrols
                    newmenu.commandbar.Controls(intI).Enabled = True
                    newmenu.commandbar.Controls(intI).Enabled = .Fields(intI + 5).Value
                Next
                
            .MoveNext
            Wend
    End With
End Sub
Private Sub CoyearRoutin()
    If IsNull(Me.Coyear) Then
        MsgBox "شركت و سال مالي نامشخص است"
        Exit Sub
    Else
        Me.Combo4 = Me.Coyear.Column(1)
        Me.Tsal = Me.Coyear.Column(1)
        Managedates
        Finddatabase
    End If
End Sub
Private Sub Finddatabase()
    Dim Tbldef As TableDef
    Dim Strdb As String, Strmaindsn As String, Stracc As String, Strcash As String
    Dim Strinv As String, Strsales As String, Strdsn As String, Strconv As String
    
    Strdb = "ODBC;DATABASE="
    Strmaindsn = ";UID=sa;PWD=***MASKED***;DSN=Sqlcsd"
    Stracc = Me.Coyear.Column(2)
    Strcash = Me.Coyear.Column(3)
    Strinv = Me.Coyear.Column(4)
    Strsales = Me.Coyear.Column(5)
    Strdsn = Me.Coyear.Column(6)
    Strconv = Me.Coyear.Column(7)
    
    Dim Notchangeddbname As String
    Notchangeddbname = Me.Coyear.Column(8)
    
    For Each Tbldef In CurrentDb.TableDefs
        If Tbldef.Connect Like "*=Acc*" Then
'            If Tbldef.Connect Like "*Dsn=Acc84*" Then
'                Tbldef.Connect = Tbldef.Connect
'            Else
                Tbldef.Connect = Strdb & Stracc & Strmaindsn
                Tbldef.RefreshLink
'            End If
        ElseIf Tbldef.Connect Like "*=Cash*" Then
            If Tbldef.Name = "Check" Or Tbldef.Name = "Povouch" Or Tbldef.Name = "Bundle" Then
                Tbldef.Connect = Strdb & Notchangeddbname & Strmaindsn
            Else
                Tbldef.Connect = Strdb & Strcash & Strmaindsn
            End If
            Tbldef.RefreshLink
        ElseIf Tbldef.Connect Like "*=Inv*" Then
            Tbldef.Connect = Strdb & Strinv & Strmaindsn
            Tbldef.RefreshLink
        ElseIf Tbldef.Connect Like "*=Sales*" Then
            Tbldef.Connect = Strdb & Strsales & Strmaindsn
            Tbldef.RefreshLink
        End If
    Next
    If Nz(DLookup("Vunique", "Globals"), 0) = 0 Then
        CurrentDb.Execute "Create unique index Acc_idxv on t_voucher (Voucher_No asc) "
    End If
    If Nz(DLookup("Vrunique", "Globals"), 0) = 0 Then
        CurrentDb.Execute "Create unique index Acc_idx on t_voucher_row (Voucher_No asc,Record_No asc) "
    End If
    
    Kasr
    Me.RecordSource = "Globals"
    Me.Command15.Enabled = True
    DoCmd.Close acForm, "Main"
End Sub

Private Sub Managedates()
    Nowdate = CurrentDateFarsi(Date)
    Startdate = Tsal & "0101"
    If Mid(Nowdate, 1, 4) = Me.Tsal Then
        Enddate = Nowdate
    Else
        Nowdate = Tsal & "1230"
        Enddate = Tsal & "1230"
    End If
    Me.Tsdate = Startdate
    Me.Tedate = Enddate
    Me.Tcurdate = Nowdate
End Sub
Private Sub Kasr()
    If Nz(DLookup("Kasrlongs", "Globals"), 0) = True Then
        S = "UPDATE [Check] SET [Check].Ctocash = True, [Check].Calcit = True, [Check].D5 = Currentdatefarsi(Date()), [Check].D3 = Currentdatefarsi(Date())WHERE (((Check.Ctocash)=False) AND ((Check.Maturity)=Currentdatefarsi(Date())) AND ((Check.Checktype)<>0 And (Check.Checktype)<>1) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND ((Check.EbtorKharj)=False));"
        docommand S
    End If
End Sub
