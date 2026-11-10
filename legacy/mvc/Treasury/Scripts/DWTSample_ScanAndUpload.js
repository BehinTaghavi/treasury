Dynamsoft.WebTwainEnv.RegisterEvent('OnWebTwainReady', Dynamsoft_OnReady);

function Dynamsoft_OnReady() {
    DWObject = Dynamsoft.WebTwainEnv.GetWebTwain('dwtcontrolContainer');
    DWObject.RegisterEvent("OnPostAllTransfers", Dynamsoft_OnPostAllTransfers);
}

function AcquireImage() {
    if (DWObject) {
        DWObject.IfDisableSourceAfterAcquire = true;
        DWObject.IfShowUI = true;
        if (DWObject.SourceCount > 0) {
            DWObject.SelectSource();
            DWObject.AcquireImage();
        }
        else
            alert("No TWAIN compatible drivers detected.");
    }

}

function SaveImage() {
    var d = new Date();
    if (DWObject) {
        //DWObject.IfShowFileDialog = true;
        if (DWObject.HowManyImagesInBuffer > 0) {
            DWObject.SaveAsJPEG("D:\\1\\1.jpg", DWObject.CurrentImageIndexInBuffer);
        }

        //DWObject.SaveAllAsMultiPageTIFF("WebTWAINImage.tiff");
    }
}
function btn_SaveOnClick() {
    if (DWObject.HowManyImagesInBuffer == 0) {
        alert("No images in buffer.");
        return;
    }
    DWObject.SaveAsJPEG("D:\\test.jpg");
}
/// <summary>
/// This is an asynchronous upload. OnSuccess and OnFailure are callback functions.
/// OnSuccess is the callback function for successful loads while OnFailure is for failed ones.
/// </summary>

function OnSuccess() {
    console.log('successful');
}
function OnFailure(errorCode, errorString) {
    alert(errorString);
}

function LoadImage() {
    if (DWObject) {
        DWObject.IfShowFileDialog = true;
        DWObject.LoadImageEx("", EnumDWT_ImageType.IT_ALL, OnSuccess, OnFailure);
    }
}

/// <summary>
/// This is an asynchronous upload. OnHttpUploadSuccess and OnHttpUploadFailure are callback functions.
/// OnHttpUploadSuccess is the callback function for successful uploads while OnHttpUploadFailure is for failed ones.
/// function Dynamsoft_OnPostAllTransfers is registered to the event 'OnPostAllTransfers', which would fire after all transfers end.
/// Please check HomeController -> SaveToFile to learn how to process uploaded files on server side.
/// </summary>

function OnHttpUploadSuccess() {
    console.log('successful');
}

function OnHttpUploadFailure(errorCode, errorString, sHttpResponse) {
    alert(errorString + sHttpResponse);
}

function Dynamsoft_OnPostAllTransfers() {
    if (DWObject) {
        if (DWObject.HowManyImagesInBuffer > 0) {
            //var strHTTPServer = location.hostname;
            //DWObject.HTTPPort = location.port == "" ? 80 : location.port;
            //var CurrentPathName = unescape(location.pathname); // get current PathName in plain ASCII
            //var CurrentPath = CurrentPathName.substring(0, CurrentPathName.lastIndexOf("/") + 1);
            //var strActionPage = CurrentPath + "Scan/SaveToFile";//CurrentPath + "Home/SaveToFile";
            //var uploadfilename = "WebTWAINImage.jpg";

            //DWObject.HTTPUploadAllThroughPostAsMultiPageTIFF(strHTTPServer, strActionPage, uploadfilename);

        }
    }
}

function btnScan_onclick() {
    DWObject.SelectSource();
    DWObject.OpenSource();
    DWObject.AcquireImage();
}
function btnUpload_onclick() {
    var strActionPage;
    var strHostIP;
    var CurrentPathName = unescape(location.pathname); // get current PathName in 
    var CurrentPath = CurrentPathName.substring(0, CurrentPathName.lastIndexOf("/")
    + 1);
    strActionPage = CurrentPath + "SaveToFile.aspx"; //the ActionPage's file path
    strHostIP = "localhost"; //The host's IP or name 
    DWObject.HTTPPort = 80;
    DWObject.HTTPUploadThroughPost(strHostIP, 0, strActionPage, "imageData.tif");
    if (DWObject.ErrorCode != 0)
        alert(DWObject.ErrorString);
    else //succeed
        alert("Image Uploaded successfully");
}



//function btn_saveToDB(id) {
//    DWObject.HTTPPort = 4844;
//    var CurrentPathName = unescape(location.pathname); // get current PathName in plain ASCII
//    var CurrentPath = CurrentPathName.substring(0, CurrentPathName.lastIndexOf("/") + 1);
//    var strActionPage = CurrentPath + "Scan/SaveToFile?id=" + id;
//    var strHostIP = "localhost"; // modify the IP for cross-domain access
//    var sFun = function () {
//        updateTable(); closeModal('md');
//    }, fFun = function () {
//        alert('faild');
//       // updateTable(); closeModal('md');
//    };
//    DWObject.HTTPUploadThroughPostEx(
//    strHostIP,
//    DWObject.CurrentImageIndexInBuffer,
//    strActionPage,
//    "test.jpg",
//    1,// JPEG
//    sFun, fFun
//    );
//}
function btn_saveToDB(id, BankID, BasicBankID) {
    var strActionPage;
    var strHostIP;

    var CurrentPathName = unescape(location.pathname); // get current PathName in plain ASCII 
    var CurrentPath = CurrentPathName.substring(0, CurrentPathName.lastIndexOf("/") + 1);
    strActionPage = CurrentPath + "Scan/SaveToFile?id=" + id;

    strHostIP = "localhost"; //The host's IP or name 

    DWObject.HTTPPort = 4844;
    DWObject.HTTPUploadThroughPostEx(strHostIP, 0, strActionPage, "imageData.jpg", 1);

    if (DWObject.ErrorCode != 0)
    {
        updateTableChequeLs(BankID,BasicBankID); closeModal('md');
    }  
    else //succeded
    {
        updateTableChequeLs(BankID,BasicBankID); closeModal('md');
    }
   
}