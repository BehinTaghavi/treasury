Attribute VB_Name = "Form_Sal1"
Attribute VB_Base = "0{0218E052-F620-4C93-93B2-CAF1153915CC}"
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
            Me.Tempid = !EMPNAME
            Me.Isadd = Nz(!isadmin, 0)
            Me.Canvp = Nz(!Canverifypay, 0)
            menucontrol
        Me.L0.Visible = True
        DoEvents
        Nochangedate = Nz(DLookup("Lastcreatev", "Globals"), Me.Combo4 & "0101")
        Me.L0.Visible = False
        DoEvents
            Me.Visible = False
            DoCmd.OpenForm "Main"
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
    'Me.Arzifarsi = Nz(DLookup("Arziinfarsi", "Globals"), False)
'    mystr = "select * from Ms where App=0 Order by Ord;"
'    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
'    With rst
'        If .RecordCount = 0 Then
'            MsgBox "سطوح دسترسي نامشخص است"
'            Exit Sub
'        Else
'            .MoveLast
'            .MoveFirst
'            Set myMenuBar = CommandBars.ActiveMenuBar
'        End If
'        Dim g As Integer
'            While Not .EOF
'                menuname = !mname
'                Set newmenu = myMenuBar.Controls(menuname)
'                tofcontrols = newmenu.commandbar.Controls.Count
'
'                For intI = 1 To tofcontrols
'                    newmenu.commandbar.Controls(intI).Enabled = False
'                Next
'            .MoveNext
'            Wend
'    End With
    
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
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            Set myMenuBar = CommandBars.ActiveMenuBar
        End If
        
        Dim g As Integer
            While Not .EOF
                menuname = !Mname
                Set newmenu = myMenuBar.Controls(menuname)
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
    CurrentDb.Execute "Create unique index Acc_idx on t_voucher_row (Voucher_No asc,Record_No asc) "
    Me.RecordSource = "Globals"
    Me.Command15.Enabled = True
    DoCmd.Close acForm, "Main"
End Sub

