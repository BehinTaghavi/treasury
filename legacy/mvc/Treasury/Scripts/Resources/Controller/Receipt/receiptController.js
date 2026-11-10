app.controller('receiptController', ["$scope", "$http", "$sce", "pettyCashSharedProperties", function ($scope, $http, $sce, pettyCashSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.BehalfDescription = "";
    $scope.BehalfDescription2 = "";
    $scope.addedBehalf_BehalfGroupId = 0;
    $scope.selectedBehalfIndex = 1;
    $scope.selectedBehalfGRIndex = 1;
    $scope.DatePattern = /^[1-4]\d{3}\/((0[1-6]\/((3[0-1])|([1-2][0-9])|(0[1-9])))|((1[0-2]|(0[7-9]))\/(30|([1-2][0-9])|(0[1-9]))))$/;
    $scope.getQueryParams = function (qs) {
        qs = qs.split('+').join(' ');
        var params = {},
            tokens,
            re = /[?&]?([^=]+)=([^&]*)/g;
        while (tokens = re.exec(qs)) {
            params[decodeURIComponent(tokens[1])] = decodeURIComponent(tokens[2]);
        }
        return params;
    }
    $scope.hasDateError = false;
    $scope.headerId = $scope.getQueryParams(window.location.href.split("?")[1]).headerId;
    $scope.dt;
    $scope.dt1;
    var receiptItem = {
        ReceiptId: null,
        Cheque_Number: "",
        openPersian: "",
        DueDate: "",
        UniqueAccount: { Name: "", Code: 0 },
        UniqueAccount2: { Name: "", Code: 0 },
        ChequePrice_Rial: 1000,
        ExchangeTypeID: null,
        EquivalencePrice: null,
        BehalfID: null,
        BehalfGroupId: null,
        Description: "",
        BankID: null,
        isOpen: false,
        Bank_AccountNo: "",
        Bank_BranchDesc: "",
        Bank_BranchCode: "",
        CityID: null
    }

    $scope.wordifyfa = function (num, level) {
        'use strict';
        if (num === null) {
            return "";
        }
        if (num < 0) {
            num = num * -1;
            return "منفی " + $scope.wordifyfa(num, level);
        }
        if (num === 0) {
            if (level === 0) {
                return "صفر";
            } else {
                return "";
            }
        }
        var result = "",
            yekan = ["یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه"],
            dahgan = ["بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود"],
            sadgan = ["یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد"],
            dah = ["ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هیجده", "نوزده"];
        if (level > 0) {
            result += " و ";
            level -= 1;
        }

        if (num < 10) {
            result += yekan[num - 1];
        } else if (num < 20) {
            result += dah[num - 10];
        } else if (num < 100) {
            result += dahgan[parseInt(num / 10, 10) - 2] + $scope.wordifyfa(num % 10, level + 1);
        } else if (num < 1000) {
            result += sadgan[parseInt(num / 100, 10) - 1] + $scope.wordifyfa(num % 100, level + 1);
        } else if (num < 1000000) {
            result += $scope.wordifyfa(parseInt(num / 1000, 10), level) + " هزار " + $scope.wordifyfa(num % 1000, level + 1);
        } else if (num < 1000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000, 10), level) + " میلیون " + $scope.wordifyfa(num % 1000000, level + 1);
        } else if (num < 1000000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000000, 10), level) + " میلیارد " + $scope.wordifyfa(num % 1000000000, level + 1);
        } else if (num < 1000000000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000000000, 10), level) + " تریلیارد " + $scope.wordifyfa(num % 1000000000000, level + 1);
        }
        return result;

    };
    $scope.wordifyRialsInTomans = function (num) {
        'use strict';
        return $scope.wordifyfa(num, 0);
    };
    $scope.show = function (id, typeId, ReceiptDetailId) {
        $http.post("/Payment/Receipt/Show?headerId=" + id + (typeId != 0 ? "&receiptPayTypeId=" + typeId : "&receiptPayTypeId=0") + "&ReceiptDetailId=" + ReceiptDetailId).then(function (response) {
            $scope.model = response.data;
            //console.log(response.data);
            $scope.model.Header.ReceiptPayTypeDetailLs = response.data.Header.ReceiptPayTypeDetailLs.filter(function (el) {
                return el.Receipt_Pay_TypeDetailID == 18 ||
                    el.Receipt_Pay_TypeDetailID == 19 ||
                    el.Receipt_Pay_TypeDetailID == 21
            });
            //$scope.model.Header.basicBank = null;
            //$scope.model.Header.bank = null;

            $scope.model.Header.ReceiptPayTypeId = typeId;
            angular.forEach($scope.model.OtherProperty, function (item) {
                if ($scope.model.detailsLs) {

                }
            });
            angular.forEach($scope.model.detailsLs, function (item) {
                angular.forEach(item.OtherPropertyDetailsList, function (e) {
                    if (e.OtherPropertyDetails != undefined && e.OtherPropertyDetails.length == 0) {
                        e.OtherPropertyDetails.push({
                            Description1_Value: "",
                            Description2_Value: "",
                            Description3_Value: "",
                            Description4_Value: "",
                            Description5_Value: ""
                        })
                    }
                })
            })
            if (id > 0) {
                if (response.data.OtherProperty && response.data.OtherProperty.length > 0) {
                    $scope.model.detailsLs[0]["OtherProperyLS"] = response.data.OtherProperty.filter(function (el) {
                        return el.Selected == true
                    }).map(function (a) { return a.Reciept_OtherProperyID; });
                }

            }
            if ($scope.model.detailsLs.length == 0) {
                $scope.addCheque();
            }
        });
        console.log($scope.model)
    }
    $scope.addCheque = function () {
        var item = {
            Cheque_Number: "",
            DueDate: "",
            ChequePrice_Rial: null,
            Bank_AccountNo: "",
            BankID: null,
            Bank_BranchCode: "",
            CityID: null,
            ExchangeRate: null,
            ExchangeTypeID: null,
            BreakPriceLs: [],
            BehalfID: null,
            OtherPropertyDetailsList: angular.copy($scope.model.OtherPropertyDetailsList),
            BehalfGroupId: null,
            UniqueAccount: {
                Code: null,
                Name: ""
            },
            UniqueAccount2: {
                Code: null,
                Name: ""
            }
        };
        if ($scope.model.Header.ReceiptPayTypeId == 18 || $scope.model.Header.ReceiptPayTypeId == 23) {
            $scope.setNowDate(true, item);
        }
        $scope.model.detailsLs.push(item);
    }
    $scope.addPriceDetails = function (item) {
        item.BreakPriceLs.push({
            Exchange_Amount: null,
            ExchangeTypeID: null,
            Exchange_Rate: null
        });
    }
    //Rahimi for test
    $scope.cashTest = function (ex) {
        console.log(ex);
    }
    $scope.showBehalfDialog = function (item, index) {
        $scope.addedBehalf_BehalfGroupId = item.BehalfGroupId;
        $scope.selectedBehalfIndex = $scope.model.detailsLs.indexOf(item);
        $("#BehalfForm").find(".modal-title").html("اضافه کردن بابت");
        $("#BehalfForm").modal('show');
    }
    $scope.showBehalfGroupDialog = function (item, index) {

        $scope.selectedBehalfGRIndex = $scope.model.detailsLs.indexOf(item);
        $("#BehalfGroupForm").find(".modal-title").html("اضافه کردن گروه بابت");
        $("#BehalfGroupForm").modal('show');
    }
    $scope.addBehalfGroup = function () {
        var model = {
            BehalfGroupDescription: $scope.BehalfGroupDescription,
            account_code: $scope.account_code,
            referenceTypeID: $scope.referenceTypeID
        }

        $http.post("/BasicInfo/BehalfGroup/ReceiptBehalfGrCreate", model).success(function (response) {
            alertify.success("ثبت گروه بابت  با موفقیت انجام شد");



            //var obj = $scope.model.detailsLs;
            //console.log(obj);
            //for (var el in obj) {

            //    alert(obj[el].BehalfGroupID);
            //}
            //for (var i = 0; i < tList.length; i++) {
            //    alert(tList[i]);
            //}
            //$scope.model.detailsLs[$scope.selectedBehalfGRIndex].BehalfGroupLs.push();
            //console.log(response);
            $scope.BehalfGroupLs = response;
            //$scope.model.detailsLs[$scope.selectedBehalfIndex].BehalfGroupLs = response;           
            //$scope.model.detailsLs[$scope.selectedBehalfGRIndex].BehalfGroupID = response[0].BehalfGroupID;
            $("#BehalfGroupForm").modal('hide');
        }).error(function (model, status, header, config) {
            alertify.error("ثبت گروه بابت با خطا مواجه شد");
        });
    }
    $scope.addBehalf = function () {
        var model = {
            BehalfGroupID: $scope.addedBehalf_BehalfGroupId,
            BehalfDescription: $scope.BehalfDescription,
            BehalfDescription2: $scope.BehalfDescription2
        }

        $http.post("/BasicInfo/Behalf/ReceiptBehalfCreate", model).success(function (response) {
            alertify.success("ثبت بابت  با موفقیت انجام شد");
            //alert($scope.selectedBehalfIndex);
            //console.log($scope.selectedBehalfIndex);
            $scope.model.detailsLs[$scope.selectedBehalfIndex].BehalfLs = response;
            $scope.model.detailsLs[$scope.selectedBehalfIndex].BehalfID = response[0].BehalfID;
            $("#BehalfForm").modal('hide');
        }).error(function (model, status, header, config) {
            alertify.error("ثبت بابت با خطا مواجه شد");
        });
    }
    $scope.sumBreakPricels = function (list) {
        var sum = 0;
        angular.forEach(list, function (item) {
            //sum += parseInt((item.Exchange_Amount * item.Exchange_Rate));
            sum += parseInt(item.ChequePrice_Rial);
        });
        return sum;
    }



    $scope.save = function (id) {
        $scope.isDisable = true;
        $scope.isDisableBtn = true;
        var model = {
            HeaderId: $scope.getQueryParams(window.location.href.split("?")[1]).headerId,
            ReceiptDetailId: $scope.getQueryParams(window.location.href.split("?")[1]).ReceiptDetailId,
            ReceiptDate: $scope.model.Header.ReceiptDate,
            ReceiptPayTypeId: $scope.model.Header.ReceiptPayTypeId,
            isRiali: $scope.model.Header.Arzi,
            DetailsLs: [],
        }

        $scope.model.detailsLs.forEach(function (item) {
            var data = {
                ReceiptDetailsid: item.ReceiptDetailsid,
                BankID: item.BankID,
                Bank_AccountNo: item.Bank_AccountNo,
                Bank_BranchCode: item.Bank_BranchCode,
                Bank_BranchDesc: item.Bank_BranchDesc,
                BehalfGroupId: item.BehalfGroupId,
                BehalfID: item.BehalfID,
                ChequePrice_Rial: ($scope.model.Header.Arzi && $scope.model.Header.ReceiptPayTypeId == 22) ? $scope.sumBreakPricels(item.BreakPriceLs) : item.ChequePrice_Rial,
                ExchangeAmount: item.ExchangeAmount,
                Cheque_Number: item.Cheque_Number,
                Cheque_OwnerName: item.Cheque_OwnerName,
                CityID: item.CityID,
                CashDeskID: item.CashDeskID,
                ExchangeRate: item.ExchangeRate,
                DueDate: item.DueDate,
                ExchangeRate: ($scope.model.Header.Arzi ? (($scope.model.Header.ReceiptPayTypeId == 22) ? 1 : item.ExchangeRate) : null),
                ExchangeTypeID: ($scope.model.Header.Arzi ? (($scope.model.Header.ReceiptPayTypeId == 22) ? $scope.model.ExchangeTypeList[0].ExchangeTypeID : item.ExchangeTypeID) : null),
                Unique_Account_no: item.UniqueAccount.Code,
                Unique_Account_no_Two: item.UniqueAccount2.Code,
                provinceID: item.provinceID,
                Refrences: [],
                BreakPricels: angular.copy(item.BreakPriceLs),
                OthersPropertyLs: item.OtherProperyLS,
                OtherPropertyDetailsList: (item.OtherPropertyDetailsList != null ? ($.map(angular.copy(item.OtherPropertyDetailsList), function (value, index) {
                    return [value];
                })) : null),
            };
            angular.forEach(data.OtherPropertyDetailsList, function (e) {
                e.OtherPropertyDetails = (e.OtherPropertyDetails != null ? ($.map(angular.copy(e.OtherPropertyDetails), function (value, index) {
                    return [value];
                })) : null)
            })
            if (item.Refrences != undefined) {
                item.Refrences.forEach(function (element) {
                    data.Refrences.push({
                        Id: element.Id,
                        Value: element.Value
                    })
                })
            }
            if ($scope.model.Header.ReceiptPayTypeId == 23 || $scope.model.Header.ReceiptPayTypeId == 24) {
                //data.Bank_AccountNo = $scope.model.Header.bank.AccountNumber;
                data.BankID = $scope.model.Header.basicbank.BasicBankId;
                data.mainBankID = $scope.model.Header.bank.BankID;
                //data.Bank_BranchDesc = $scope.model.Header.bank.BranchName;
                //data.Bank_BranchCode = $scope.model.Header.bank.BranchCode;
                //data.CityID = $scope.model.Header.bank.CityID;
            }

            // Added BY Rahimi14020212
            console.log(data)
            angular.forEach(data.BreakPricels, function (e) {
                e.ExchangeTypeID = ($scope.model.Header.Arzi ? (($scope.model.Header.ReceiptPayTypeId == 22) ? $scope.model.ExchangeTypeList[0].ExchangeTypeID : e.ExchangeTypeID) : null);
            })
            //////////////////
            model.DetailsLs.push(data)
        })
        $http.post("/Payment/Receipt/Save", model).then(function (response) {

            if (response.data.result == true) {
                alertify.success("ثبت دریافت  با موفقیت انجام شد");
                setTimeout(function () {
                    location.replace("/Payment/Receipt/ShowAllReceiptList");
                }, 2000)
            }
            else {
                alertify.error(response.data.message);
                $scope.isDisableBtn = false;
            }


        });
        //$http.post("/Payment/Receipt/Save", model).success(function (response) {
        //    alertify.success("ثبت دریافت  با موفقیت انجام شد");
        //    location.replace("/Payment/Receipt/ShowAllReceiptList");
        //}).error(function (model, status, header, config) {
        //    alertify.error("ثبت دریافت با خطا مواجه شد");
        //});

    }
    $scope.CheckedArzi = function () {
        if (typeof $scope.model.detailsLs != "undefined") {
            angular.forEach($scope.model.detailsLs, function (item) {
                item.ChequePrice_Rial = "";
                $http.get("/Payment/Receipt/GetCashExchange?CashDeskID=" + item.CashDeskID + " &ReceiptDate=" + $scope.model.Header.ReceiptDate).then(function (response) {
                    $('#ShowExchangeOfCashDesk').html("<label class='control-label'>ارز : " + response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues + "</label>");

                    //item.ExchangeOfCashDesk = response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues;
                    item.ExchangeTypeValueId = response.data.ExchangeTypeValueId;
                    if (response.data.ExchangeTypeValueId != 4) {
                        item.ExchangeRate = response.data.nerkh;
                    }
                    else {
                        item.ExchangeRate = 0;
                    }

                    angular.forEach(item.BreakPriceLs, function (ex) {
                        $scope.AssignExchangeBreakList(ex);
                    });
                });
            });
        }

    }

    // Added By Rahimi 14020211

    $scope.AssignExchangeBreakList = function (ex) {
        $http.get("/Payment/Receipt/GetCashExchange?CashDeskID=" + ex.CashDeskID + " &ReceiptDate=" + $scope.model.Header.ReceiptDate).then(function (response) {
            $('#ShowExchangeOfCashDesk').html("<label class='control-label'>ارز : " + response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues + "</label>");

            //item.ExchangeOfCashDesk = response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues;
            ex.ExchangeTypeValueId = response.data.ExchangeTypeValueId;
            if (response.data.ExchangeTypeValueId != 4) {
                ex.Exchange_Rate = response.data.nerkh;
            }
            else {
                ex.Exchange_Rate = 0;
            }

        });
    }

    // Added By Rahimi 14020211

    $scope.loadCity = function (item) {
        $http.get("/Payment/Receipt/GetCityLs?provinceID=" + item.ProvinceID).then(function (response) {
            item.CityLs = response.data;
        });
    }
    $scope.loadCashInfo = function (item) {
        $http.get("/Payment/Receipt/GetCashExchange?CashDeskID=" + item.CashDeskID + " &ReceiptDate=" + $scope.model.Header.ReceiptDate).then(function (response) {

            $('#ShowExchangeOfCashDesk').html("<label class='control-label'>ارز : " + response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues + "</label>");
            item.ExchangeTypeValueId = response.data.ExchangeTypeValueId;
            //item.ExchangeOfCashDesk = response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues;

            if (response.data.ExchangeTypeValueId != 4) {
                item.ExchangeRate = response.data.nerkh;
                //item.Exchange_Rate.disabled = true;
            }
            else {
                item.ExchangeRate = 0;
            }

        });
    }

    $scope.loadCashInfo2 = function (item) {
        console.log(item)
        $http.get("/Payment/Receipt/GetCashExchange?CashDeskID=" + item.CashDeskId + " &ReceiptDate=" + $scope.model.Header.ReceiptDate).then(function (response) {

            $('#ShowExchangeOfCashDesk').html("<label class='control-label'>ارز : " + response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues + "</label>");
            item.ExchangeTypeValueId = response.data.ExchangeTypeValueId;
            //item.ExchangeOfCashDesk = response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues;
            
            if (response.data.ExchangeTypeValueId != 4) {
                item.Exchange_Rate = response.data.nerkh;
                //item.Exchange_Rate.disabled = true;
            }
            else {
                item.Exchange_Rate = 0;
            }

        });
    }

    $scope.changePrice = function () {
        if (typeof $scope.model.detailsLs != "undefined") {
            angular.forEach($scope.model.detailsLs, function (item) {
                $http.get("/Payment/Receipt/GetCashExchange?CashDeskID=" + item.CashDeskID + " &ReceiptDate=" + $scope.model.Header.ReceiptDate).then(function (response) {
                    $('#ShowExchangeOfCashDesk').html("<label class='control-label'>ارز : " + response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues + "</label>");
                    item.ExchangeTypeValueId = response.data.ExchangeTypeValueId;
                    //item.ExchangeOfCashDesk = response.data.ExchangeTypeDescription + "-" + response.data.ExchangeTypeValues;
                    if (response.data.ExchangeTypeValueId != 4) {
                        item.ExchangeRate = response.data.nerkh;
                    }
                    else {
                        item.ExchangeRate = 0;
                    }
                });
            });
        }
    }
    $scope.changeText = function (DateNow) {
        if ($scope.model.Header.ReceiptDate > DateNow) {
            $scope.hasDateError = true;
        }
        else {
            $scope.hasDateError = false;
        }
    }
    $http.get("/Payment/Receipt/GetBehlafGroupLs?IsReceipt=" + true).then(function (response) {
        $scope.BehalfGroupLs = response.data;
    });
    //$scope.BehalfGroupLs = function () {
    //    $http.get("/Payment/Receipt/GetBehlafGroupLs").then(function (response) {
    //        alert(response.data);
    //        console.log(response.data);
    //    });
    //}

    //$scope.loadBehalfGroup = function ($scope) {

    //} 
    $scope.loadBehalf = function (item) {
        $http.get("/Payment/Receipt/GetBehalfLs?BehalfGroupId=" + item.BehalfGroupId).then(function (response) {
            item.BehalfLs = response.data.BehalfList;
            item.Refrences = response.data.Refrences;
            item.BehalfGroup_Account = response.data.BehalfGroup_Account;
        });
    }
    $scope.show($scope.getQueryParams(window.location.href.split("?")[1]).headerId, $scope.getQueryParams(window.location.href.split("?")[1]).ReceiptPayTypeId, $scope.getQueryParams(window.location.href.split("?")[1]).ReceiptDetailId);
    $scope.today = function () {
        $scope.dt = new Date();
    };
    $scope.today();
    $scope.clear = function () {
        $scope.dt = null;
    };
    $scope.disabled = function (date, mode) {
        return (mode === 'day' && date.getDay() === 5);
    };
    $scope.toggleMin = function () {
        $scope.minDate = $scope.minDate ? null : new Date();
    };
    $scope.toggleMin();
    $scope.dateOptions = {
        formatYear: 'yyyy',
        startingDay: 8
    };
    $scope.initDate = new Date('2016-15-20');
    $scope.formats = ['dd-MMMM-yyyy', 'yyyy/MM/dd', 'yyyy/MMMM/dd', 'dd.MM.yyyy', 'shortDate'];
    $scope.format = $scope.formats[1];
    $scope.changeUniqueAccount = function (uc) {
        pettyCashSharedProperties.SetActiveUC(uc);
        $http.get("/Payment/PettyCash/getPettyCashUC").then(function (response) {
            $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
            $(".modal.md.ang.acc").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang.acc").modal('show');
            $("#UniqueAccountType, #Code, #Name").val('');
        });
    }

    $scope.setChequeOwnerName = function (item) {
        $scope.Cheque_OwnerName = item.UniqueAccount.Name
    }
    $scope.setUC = function () {
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        console.log('uc' + $scope.uc);
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    $scope.removeItem = function (item) {
        var index = $scope.model.detailsLs.indexOf(item);
        $scope.model.detailsLs.splice(index, 1);
    }
    $scope.removePriceDetails = function (list, item) {
        if (list.length > 1) {
            var index = list.indexOf(item);
            list.splice(index, 1);
        }
        else {
            alert("حداقل 1 نوع ارز انتخاب شود");
        }

    }
    $scope.removePriceDetailsItems = function (item) {
        if (confirm("آیا از حذف جزئیات مبالغ مطمئن هستید؟")) {
            item.BreakPriceLs = [];
            $scope.addPriceDetails(item);
        }
    }
    $scope.sumprice = function () {
        var sum = 0;
        if (typeof $scope.model.detailsLs != "undefined") {
            angular.forEach($scope.model.detailsLs, function (item) {
                sum += parseInt(item.ChequePrice_Rial);
            });
        }
        if ($scope.price != 0 && $scope.price != sum) {
            $scope.isDisable = true;
        }
        else {
            $scope.isDisable = false;
        }
        return sum;
    }
    $scope.copyItem = function (item) {
        console.log(item);
        var newItem = angular.copy(item);
        newItem.Cheque_Number = "";
        //var newItem = {
        //    "Cheque_Number": "",
        //    "DueDate": item.DueDate,
        //    "ChequePrice_Rial": item.ChequePrice_Rial,
        //    "Bank_AccountNo": item.Bank_AccountNo,
        //    "BankID": item.BankID,
        //    "Bank_BranchCode": item.Bank_BranchCode,
        //    "ProvinceLs": item.ProvinceLs,
        //    "ProvinceID": item.ProvinceID,
        //    "CityID": item.CityID,
        //    "CityLs": [],
        //    "ExchangeRate": item.ExchangeRate,
        //    "ExchangeTypeID": item.ExchangeTypeID,
        //    "BehalfLs": [],
        //    "BehalfID": item.BehalfID,
        //    "BehalfGroupId": item.BehalfGroupId,
        //    "Bank_BranchDesc": item.Bank_BranchDesc,
        //    "Cheque_OwnerName": item.Cheque_OwnerName,
        //    "Refrences": [],
        //    "UniqueAccount": { Code: item.UniqueAccount.Code, Name: item.UniqueAccount.Name, },
        //    "OtherProperyLS": [],
        //    "OtherPropertyDetails" : []

        //}
        //angular.forEach(item.Refrences, function (el) {
        //    newItem.Refrences.push({
        //        ExtensionData: el.ExtensionData,
        //        ReferenceTypes_By_BehalfGroup: el.ReferenceTypes_By_BehalfGroup,
        //        Request_Payment: el.Request_Payment,
        //        referenceTypeDescription: el.referenceTypeDescription,
        //        referenceTypeDescription2: el.referenceTypeDescription2,
        //        referenceTypeID: el.referenceTypeID,
        //        Value: el.Value,
        //        Title: el.Title
        //    });
        //});
        //angular.forEach(item.BehalfLs, function (el) {
        //    newItem.BehalfLs.push(el);
        //});
        //angular.forEach(item.OtherProperyLS, function (el) {
        //    newItem.OtherProperyLS.push(el);
        //});
        //angular.forEach(item.OtherPropertyDetails, function (el) {
        //    newItem.OtherPropertyDetails.push(el);
        //});
        //angular.forEach(item.CityLs, function (el) {
        //    newItem.CityLs.push(el);
        //});
        $scope.model.detailsLs.push(newItem);
    }
    $scope.copyOtherPropertyDetails = function (item, opdl) {
        var newItem = angular.copy(opdl);
        newItem.Description1_Value = newItem.Description2_Value = newItem.Description3_Value = newItem.Description4_Value = newItem.Description5_Value = "";
        item.OtherPropertyDetailsList.push(newItem);
    }
    $scope.removeOtherPropertyDetails = function (item, opdl) {
        if (confirm("آیا مطمئن هستید؟")) {
            var index = item.OtherPropertyDetailsList.indexOf(opdl);
            item.OtherPropertyDetailsList.splice(index, 1);
        }
    }
    $scope.setNowDate = function (state, item) {
        if (state) {
            var d = new Date();
            var today = d.getFullYear() + "-" + parseInt(d.getMonth() + 1) + "-" + d.getDate();
            item.DueDate = moment(today, 'YYYY-M-D').format('jYYYY/jMM/jDD');
        }
    }
    $scope.guid = function () {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }
    $scope.$watch('model.Header.Arzi', function () {

        if ($scope.model != undefined && $scope.model.Header.ReceiptPayTypeId == 22) {
            angular.forEach($scope.model.detailsLs, function (item) {
                if (item.BreakPriceLs.length == 0) {
                    $scope.addPriceDetails(item);
                }
            })
        }
    });

}])
    .service('pettyCashSharedProperties', function () {
        var activeUC = { Code: 0, Name: "" };
        return {
            GetActiveUC: function () {
                return activeUC;
            },
            SetActiveUC: function (uc) {
                activeUC = uc;
            }
        };
    }).filter('otherPropertyDetailsFilter', function () {
        return function (items, item, i) {
            var filtered = [];
            angular.forEach(items, function (e) {
                if (item.OtherProperyLS != null && item.OtherProperyLS.indexOf(e.Reciept_OtherProperyID) != -1 && i.Reciept_OtherProperyID == e.Reciept_OtherProperyID) {
                    filtered.push(e);
                }
            });
            return filtered;
        };
    });