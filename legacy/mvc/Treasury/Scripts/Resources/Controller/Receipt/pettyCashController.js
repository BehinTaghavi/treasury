app.controller('pettyCashController', ["$scope", "$http", "$sce", "pettyCashSharedProperties", function ($scope, $http, $sce, pettyCashSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.dt;
    $scope.dt1;
    var receiptItem = {
        ReceiptId: null,
        Cheque_Number: "",
        openPersian: "",
        DueDate: "",
        UniqueAccount: { Name: "", Code: 0 },
        ChequePrice_Rial: 1000,
        ExchangeTypeID: null,
        ExchangeRate: 1,
        EquivalencePrice: null,
        BehalfID: null,
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
    $scope.show = function (id, typeId) {
        $http.post("/Payment/Receipt/Show?headerId=" + id + (typeId != 0 ? "&receiptPayTypeId=" + typeId : "&receiptPayTypeId=0")).then(function (response) {
            $scope.model = response.data;
            console.log(response.data)
            $scope.model.Header.ReceiptPayTypeDetailLs = response.data.Header.ReceiptPayTypeDetailLs.filter(function (el) {
                return el.Receipt_Pay_TypeDetailID == 18 ||
                       el.Receipt_Pay_TypeDetailID == 19 ||
                       el.Receipt_Pay_TypeDetailID == 21
            });
            $scope.model.Header.ReceiptPayTypeId = typeId;
            //$scope.model.Header.ReceiptPayTypeDetailLs[0].Receipt_Pay_TypeDetailID;
        });
    }
    $scope.addCheque = function () {
        $scope.model.detailsLs.push({
            Cheque_Number: "",
            DueDate: "",
            ChequePrice_Rial: null,
            Bank_AccountNo: "",
            BankID: null,
            Bank_BranchCode: "",
            CityID: null,
            ExchangeRate: null,
            ExchangeTypeID: null,
            BehalfID: null,

            BehalfGroupId: null,
            UniqueAccount: {
                Code: null,
                Name: ""
            }
        })
    }
    $scope.save = function (id) {

        var model = {
            HeaderId: 0,
            ReceiptDate: $scope.model.Header.ReceiptDate,
            ReceiptPayTypeId: 23,
            isRiali: $scope.model.Header.Arzi,
            DetailsLs: []
        }
        $scope.model.detailsLs.forEach(function (item) {
            var data = {
                BankID: item.BankID,
                Bank_AccountNo: item.Bank_AccountNo,
                Bank_BranchCode: item.Bank_BranchCode,
                Bank_BranchDesc: item.Bank_BranchDesc,
                BehalfGroupId: item.BehalfGroupId,
                BehalfID: item.BehalfID,
                ChequePrice_Rial: item.ChequePrice_Rial,
                Cheque_Number: item.Cheque_Number,
                Cheque_OwnerName: item.Cheque_OwnerName,
                CityID: item.CityID,
                DueDate: item.DueDate,
                ExchangeRate: item.ExchangeRate,
                ExchangeTypeID: item.ExchangeTypeID,
                Unique_Account_no: item.UniqueAccount.Code,
                provinceID: item.provinceID,
                References: [],
                OthersPropertyLs: item.OtherProperyLS
            };
            if (item.Refrences != undefined) {
                item.Refrences.forEach(function (element) {
                    data.References.push({
                        Id: element.referenceTypeID,
                        Value: element.value
                    })
                })
            }

            model.DetailsLs.push(data)
        })

        $http.post("/Payment/Receipt/Save", model).success(function (response) {
            alertify.success("ثبت دریافت با موفقیت انجام شد");
            location.replace("/Payment/Receipt/Index?RecieptTypeId=" + model.ReceiptPayTypeId);
        }).error(function (model, status, header, config) {
            alertify.error("ثب دریافت با خطا مواجه شد");
        });

    }
    $scope.loadCity = function (item) {
        $http.get("/Payment/Receipt/GetCityLs?provinceID=" + item.provinceID).then(function (response) {
            item.CityLs = response.data;
        });
    }
    $scope.loadBehalf = function (item) {
        $http.get("/Payment/Receipt/GetBehalfLs?BehalfGroupId=" + item.BehalfGroupId).then(function (response) {
            console.log(response.data);

            item.BehalfLs = response.data.BehalfList;
            item.Refrences = response.data.Refrences;
        });
    }
    $scope.show(window.location.href.split('=')[1] != undefined ? window.location.href.split('=')[1] : 0, (window.location.href.split('=')[2] != undefined ? window.location.href.split('=')[2] : 0));
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
            $(".modal.md.ang").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang").modal('show');
            $('.modal.md.ang').on('hidden.bs.modal', function () {
                uc = pettyCashSharedProperties.GetActiveUC();
            })
        });
    }
    $scope.setUC = function () {
        console.log($scope.uc);
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.Id = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    $scope.removeItem = function (item) {
        var index = $scope.model.detailsLs.indexOf(item);
        $scope.model.detailsLs.splice(index, 1);
    }
    $scope.copyItem = function (item) {
        console.log(item.Refrences);
        var newItem = {
            "Cheque_Number": item.Cheque_Number,
            "DueDate": item.DueDate,
            "ChequePrice_Rial": item.ChequePrice_Rial,
            "Bank_AccountNo": item.Bank_AccountNo,
            "BankID": item.BankID,
            "Bank_BranchCode": item.Bank_BranchCode,
            "CityID": item.CityID,
            "CityLs": [],
            "ExchangeRate": item.ExchangeRate,
            "ExchangeTypeID": item.ExchangeTypeID,
            "BehalfLs": [],
            "BehalfID": item.BehalfID,
            "BehalfGroupId": item.BehalfGroupId,
            "Bank_BranchDesc": item.Bank_BranchDesc,
            "Cheque_OwnerName": item.Cheque_OwnerName,
            "Refrences": [],
            "UniqueAccount": { Code: item.UniqueAccount.Code, Name: item.UniqueAccount.Name, },
            "OtherProperyLS": [],
            "provinceID": item.provinceID,
        }
        angular.forEach(item.Refrences, function (el) {
            newItem.Refrences.push({
                ExtensionData: el.ExtensionData,
                ReferenceTypes_By_BehalfGroup: el.ReferenceTypes_By_BehalfGroup,
                Request_Payment: el.Request_Payment,
                referenceTypeDescription: el.referenceTypeDescription,
                referenceTypeDescription2: el.referenceTypeDescription2,
                referenceTypeID: el.referenceTypeID,
                value: el.value
            });
        });
        angular.forEach(item.BehalfLs, function (el) {
            newItem.BehalfLs.push(el);
        });
        angular.forEach(item.OtherProperyLS, function (el) {
            newItem.OtherProperyLS.push(el);
        });
        angular.forEach(item.CityLs, function (el) {
            newItem.CityLs.push(el);
        });
        $scope.model.detailsLs.push(newItem);
    }
    $scope.setNowDate = function (state, item) {
        if (state) {
            var d = new Date();
            var today = d.getFullYear() + "-" + parseInt(d.getMonth() + 1) + "-" + d.getDate();
            item.DueDate = moment(today, 'YYYY-M-D').format('jYYYY/jM/jD');
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
})