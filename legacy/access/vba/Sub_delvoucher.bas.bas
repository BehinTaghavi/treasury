Attribute VB_Name = "Sub_delvoucher"
Option Compare Database

Public Sub DelVoucher(Myvno As Long, Mycheckid As Long)
    DoCmd.SetWarnings False
    Dim S As String
            
    S = "Delete * from Voucher_Row Where Vid= " & Myvno & " And Checkid=" & Mycheckid
    DoCmd.RunSQL S
        
    DoCmd.SetWarnings True
End Sub
