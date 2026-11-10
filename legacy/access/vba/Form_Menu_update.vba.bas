Attribute VB_Name = "Form_Menu_update"
Attribute VB_Base = "0{1040E8A1-F05E-11D7-BC94-8154B80D7F75}"
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

Private Sub Command15_Click()
    
    Dim menuname As String, tofcontrols As Byte
    Dim intI As Integer
    

    mystr = "Select * from Ms where App=0 Order by Ord"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
            .MoveFirst
            While Not .EOF
            .Edit
                For intI = 1 To 12
                    .Fields(intI + 3) = Null
                Next
            .Update
            .MoveNext
            Wend
    .Close
    End With
    
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        .MoveFirst
        Set myMenuBar = CommandBars.ActiveMenuBar
        Dim g As Integer
            While Not .EOF
            
             menuname = !Mname
            .Edit
                Set newmenu = myMenuBar.Controls(menuname)
                tofcontrols = newmenu.commandbar.Controls.Count
                
                For intI = 1 To tofcontrols
                    rst.Fields(intI + 3) = newmenu.commandbar.Controls(intI).Caption
                Next
            .Update
            .MoveNext
            Wend
     MsgBox "finished"
    .Close
    End With
    
End Sub


